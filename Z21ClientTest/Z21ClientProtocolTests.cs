using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Sockets;
using Z21Client;
using Z21Client.Infrastructure;
using Z21Client.Models;

namespace Z21ClientTest;

public class Z21ClientProtocolTests : IDisposable
{
    private readonly Mock<IZ21UdpClient> _udpClientMock;
    private readonly Mock<ILogger<Z21Client.Z21Client>> _loggerMock;
    private readonly Z21Client.Z21Client _z21Client;

    public Z21ClientProtocolTests()
    {
        _udpClientMock = new Mock<IZ21UdpClient>();
        _loggerMock = new Mock<ILogger<Z21Client.Z21Client>>();
        _z21Client = new Z21Client.Z21Client(_loggerMock.Object, _udpClientMock.Object);
    }

    [Fact]
    public async Task Receive_SerialNumber_ShouldRaiseEvent_WithCorrectValue()
    {
        // Arrange
        uint expectedSerial = 123456;
        var tcs = new TaskCompletionSource<SerialNumber>();

        _z21Client.SerialNumberReceived += (sender, args) => tcs.SetResult(args);

        // LAN_GET_SERIAL_NUMBER_RESPONSE: Length(4) + Header(2) + Serial(4) = 8 bytes total ??
        // Actually Protocol usually sends Length (4 bytes LE) then Header then Data.
        // Assuming standard len(2)+header(2) for Z21 protocol wrapper inside UDP.
        var packet = new byte[8];
        BitConverter.GetBytes((ushort)8).CopyTo(packet, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetSerialNumber).CopyTo(packet, 2);
        BitConverter.GetBytes(expectedSerial).CopyTo(packet, 4);

        SetupMockSequence(packet);

        // Act
        await _z21Client.ConnectAsync("127.0.0.1");

        var result = await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(tcs.Task, result);
        var serialNumber = await tcs.Task;
        Assert.Equal(expectedSerial, serialNumber.Value);
    }

    [Fact]
    public async Task Receive_SystemState_ShouldRaiseEvent_WithCorrectVoltages()
    {
        // Arrange
        var tcs = new TaskCompletionSource<SystemStateChangedEventArgs>();
        _z21Client.SystemStateChanged += (sender, args) => tcs.SetResult(args);

        var packet = new byte[20];
        BitConverter.GetBytes((ushort)20).CopyTo(packet, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderSystemStateResponse).CopyTo(packet, 2);

        BitConverter.GetBytes((short)1500).CopyTo(packet, 4);  // MainCurrent
        BitConverter.GetBytes((short)500).CopyTo(packet, 6);   // ProgCurrent
        BitConverter.GetBytes((short)1400).CopyTo(packet, 8);  // FilteredMain
        BitConverter.GetBytes((short)35).CopyTo(packet, 10);   // Temp
        BitConverter.GetBytes((short)18000).CopyTo(packet, 12); // SupplyVoltage
        BitConverter.GetBytes((short)16500).CopyTo(packet, 14); // VccVoltage (Target)
        packet[16] = 0x00; // CentralState
        packet[17] = 0x00; // CentralStateEx

        SetupMockSequence(packet);

        // Act
        await _z21Client.ConnectAsync("127.0.0.1");

        var result = await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(tcs.Task, result);
        var systemState = await tcs.Task;
        Assert.Equal(16500, systemState.VccVoltagemV);
        Assert.Equal(35, systemState.TemperatureC);
    }

    [Fact]
    public async Task Receive_XBus_TrackPowerOff_ShouldRaiseEvent()
    {
        // Arrange
        var tcs = new TaskCompletionSource<TrackPowerInfo>();
        _z21Client.TrackPowerInfoReceived += (sender, args) => tcs.SetResult(args);

        var packet = new byte[7];
        BitConverter.GetBytes((ushort)7).CopyTo(packet, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderXBus).CopyTo(packet, 2);

        packet[4] = Z21ProtocolConstants.XHeaderTrackPower;
        packet[5] = (byte)TrackPowerState.Off;
        packet[6] = CalculateChecksum(packet);

        SetupMockSequence(packet);

        // Act
        await _z21Client.ConnectAsync("127.0.0.1");

        var result = await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(tcs.Task, result);
        var trackPower = await tcs.Task;
        Assert.Equal(TrackPowerState.Off, trackPower.State);
    }

    [Fact]
    public async Task Receive_LocoInfo_ShouldRaiseEvent_WithCorrectAddress()
    {
        // Arrange
        ushort expectedAddress = 3;
        var tcs = new TaskCompletionSource<LocoInfo>();
        _z21Client.LocoInfoReceived += (sender, args) => tcs.SetResult(args);

        var packet = new byte[14];
        BitConverter.GetBytes((ushort)14).CopyTo(packet, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderXBus).CopyTo(packet, 2);
        packet[4] = Z21ProtocolConstants.XHeaderLocoInfo; // 0xEF

        // Corrected Address Placement:
        // ParseLocoInfo uses: data[5] & 0x3F for MSB, data[6] for LSB
        packet[5] = (byte)((expectedAddress >> 8) & 0x3F); // MSB
        packet[6] = (byte)(expectedAddress & 0xFF);        // LSB

        // Data bytes follow from index 7
        packet[7] = 0; // Steps/Speed
        packet[8] = 0; // F0-F4
        packet[9] = 0; // F5-F12
        packet[10] = 0; // F13-F20
        packet[11] = 0; // F21-F28
        packet[12] = 0; // Reserved

        packet[13] = CalculateChecksum(packet);

        SetupMockSequence(packet);

        // Act
        await _z21Client.ConnectAsync("127.0.0.1");

        var result = await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(tcs.Task, result);
        var locoInfo = await tcs.Task;
        Assert.Equal(expectedAddress, locoInfo.Address);
    }

    private void SetupMockSequence(byte[] testPacket)
    {
        _udpClientMock.Setup(x => x.Bind(It.IsAny<int>()));

        var handshakePacket = new byte[12];
        BitConverter.GetBytes((ushort)12).CopyTo(handshakePacket, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetHardwareInfo).CopyTo(handshakePacket, 2);
        BitConverter.GetBytes((uint)0x00000200).CopyTo(handshakePacket, 4);
        BitConverter.GetBytes((uint)0x00007801).CopyTo(handshakePacket, 8);

        var handshakeResult = new UdpReceiveResult(handshakePacket, new IPEndPoint(IPAddress.Loopback, 21105));
        var testResult = new UdpReceiveResult(testPacket, new IPEndPoint(IPAddress.Loopback, 21105));

        _udpClientMock.SetupSequence(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(handshakeResult)
            .ReturnsAsync(testResult)
            .Returns(async () => {
                await Task.Delay(Timeout.Infinite);
                return new UdpReceiveResult();
            });
    }

    /// <summary>
    /// Calculates a checksum value by performing a bitwise XOR over a subset of the provided byte data.
    /// </summary>
    /// <remarks>This method excludes the first four bytes and the last byte of the input span from the
    /// checksum calculation. Ensure that the input span is sufficiently sized to avoid out-of-range access.</remarks>
    /// <param name="data">A span of bytes containing the data to process. The checksum is computed from bytes starting at index 4 up to
    /// the second-to-last byte. The span must have a length of at least 5.</param>
    /// <returns>A byte representing the calculated checksum for the specified range of the input data.</returns>
    private static byte CalculateChecksum(Span<byte> data)
    {
        byte calculatedChecksum = 0;
        for (int i = 4; i < (data.Length - 1); i++)
        {
            calculatedChecksum ^= data[i];
        }
        return calculatedChecksum;
    }

    public void Dispose()
    {
        _z21Client.DisposeAsync().AsTask().Wait();
        GC.SuppressFinalize(this);
    }
}
