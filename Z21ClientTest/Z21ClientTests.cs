using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Z21Client;
using Z21Client.Infrastructure;
using Z21Client.Models;

namespace Z21Dashboard.UnitTests;

public class Z21ClientTests : IDisposable
{
    private readonly Mock<IZ21UdpClient> _udpClientMock;
    private readonly Mock<ILogger<Z21Client.Z21Client>> _loggerMock;
    private readonly Z21Client.Z21Client _z21Client;

    public Z21ClientTests()
    {
        _udpClientMock = new Mock<IZ21UdpClient>();
        _loggerMock = new Mock<ILogger<Z21Client.Z21Client>>();
        _z21Client = new Z21Client.Z21Client(_loggerMock.Object, _udpClientMock.Object);
    }

    /*
    [Fact]
    public async Task ConnectAsync_ShouldReturnTrue_WhenHandshakeSucceeds()
    {
        // Arrange
        string validHost = "127.0.0.1";
        int validPort = 21105;

        // 1. Setup Bind to succeed
        _udpClientMock.Setup(x => x.Bind(It.IsAny<int>()));

        // 2. Prepare a fake "Hardware Info" response packet
        // Structure: Length(2) + Header(2) + HwType(4) + FwVersion(4) = 12 bytes
        var responsePacket1 = new byte[12];

        // Write Length (12)
        BitConverter.GetBytes((ushort)12).CopyTo(responsePacket1, 0);

        // Write Header (HeaderGetHardwareInfo)
        // We assume Z21ProtocolConstants is available. If the namespace differs, 
        // you might need to add 'using Z21Client.Infrastructure;' or similar.
        // Assuming the value used in Z21Client is 0x1A00 (LAN_GET_HWINFO)
        // If Z21ProtocolConstants is internal, we can try to rely on the constant usage or hardcode for test:
        // Let's assume Z21ProtocolConstants is accessible.
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetHardwareInfo).CopyTo(responsePacket1, 2);

        // Write HwType (e.g., 0x00000200 for z21Start)
        BitConverter.GetBytes((uint)0x00000200).CopyTo(responsePacket1, 4);

        // Write FwVersion (e.g., 0x20010000 for 1.20 - format depends on logic, passing generic high value)
        // Logic: string fwString = (fwValue >> 8)...
        // Let's use 0x01230000 -> 1.23
        BitConverter.GetBytes((uint)0x00000122).CopyTo(responsePacket1, 8); // 0x0178 = 1.20 decimal approx
        var fakeResult1 = new UdpReceiveResult(responsePacket1, new IPEndPoint(IPAddress.Loopback, 21105));


        var responsePacket2 = new byte[5];
        BitConverter.GetBytes((ushort)5).CopyTo(responsePacket2, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetCode).CopyTo(responsePacket2, 2);
        responsePacket2[4] = (byte)Z21LockState.Locked;
        var fakeResult2 = new UdpReceiveResult(responsePacket2, new IPEndPoint(IPAddress.Loopback, 21105));


        var responsePacket3 = new byte[6];
        BitConverter.GetBytes((ushort)6).CopyTo(responsePacket3, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetCode).CopyTo(responsePacket3, 2);
        responsePacket3[4] = 123;
        responsePacket3[5] = 123;
        var fakeResult3 = new UdpReceiveResult(responsePacket3, new IPEndPoint(IPAddress.Loopback, 21105));


        // 3. Setup ReceiveAsync to return the packet ONCE, then delay forever (to simulate idle connection)
        // This prevents the ReceiveLoop in Z21Client from spinning infinitely consuming CPU or reading nulls.
        _udpClientMock.SetupSequence(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResult1) // First call: Return handshake response
            .ReturnsAsync(fakeResult2) // Second call: Return Z21 Code
            .ReturnsAsync(fakeResult3) // Third call: Return SerialNumber
            .Returns(async () => {     // Subsequent calls: Wait indefinitely
                await Task.Delay(Timeout.Infinite);
                return new UdpReceiveResult();
            });

        // Act
        bool result = await _z21Client.ConnectAsync(validHost, validPort);

        // Assert
        Assert.True(result, "ConnectAsync should return true when handshake succeeds");

        // Verify Bind was called
        _udpClientMock.Verify(x => x.Bind(validPort), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_ShouldLogWarning_WhenAlreadyConnected2()
    {
        // Arrange
        string validHost = "127.0.0.1";
        int validPort = 21105;

        // Setup successful handshake for the FIRST connection
        var responsePacket = new byte[12];
        BitConverter.GetBytes((ushort)12).CopyTo(responsePacket, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetHardwareInfo).CopyTo(responsePacket, 2);
        BitConverter.GetBytes((uint)0x00000200).CopyTo(responsePacket, 4);
        BitConverter.GetBytes((uint)0x00007801).CopyTo(responsePacket, 8);

        var fakeResult = new UdpReceiveResult(responsePacket, new IPEndPoint(IPAddress.Loopback, 21105));

        _udpClientMock.SetupSequence(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResult)
            .Returns(async () => {
                await Task.Delay(Timeout.Infinite);
                return new UdpReceiveResult();
            });

        _udpClientMock.Setup(x => x.Bind(It.IsAny<int>()));

        // Connect once successfully
        _ = await _z21Client.ConnectAsync(validHost, validPort);

        // Act
        // Attempt to connect a second time
        bool result = await _z21Client.ConnectAsync(validHost, validPort);

        // Assert
        Assert.False(result);

        // Verify warning log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)));
    }
    
    [Fact]
    public async Task ConnectAsync_ShouldLogWarning_WhenAlreadyConnected()
    {
        // --- ARRANGE ---
        var loggerMock = new Mock<ILogger<Z21Client.Z21Client>>();
        var udpClientMock = new Mock<IZ21UdpClient>();

        udpClientMock.Setup(x => x.Client).Returns(new UdpClient().Client);

        var networkChannel = Channel.CreateUnbounded<UdpReceiveResult>();

        udpClientMock.Setup(x => x.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()))
            .Callback<byte[], int, IPEndPoint>((data, len, ep) =>
            {
                ushort header = BitConverter.ToUInt16(data, 2);
                byte[] responsePacket = null;

                if (header == Z21ProtocolConstants.HeaderGetHardwareInfo)
                {
                    responsePacket = CreateHardwareInfoPacket();
                }
                else if (header == Z21ProtocolConstants.HeaderGetSystemState)
                {
                    responsePacket = CreateSystemStatePacket();
                }
                else if (header == Z21ProtocolConstants.HeaderGetCode)
                {
                    responsePacket = CreateZ21CodePacket();
                }
                else if (header == Z21ProtocolConstants.HeaderGetSerialNumber)
                {
                    responsePacket = CreateSerialNumberPacket();
                }

                if (responsePacket != null)
                {
                    networkChannel.Writer.TryWrite(new UdpReceiveResult(responsePacket, ep));
                }
            })
            .ReturnsAsync(0);

        udpClientMock.Setup(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                return await networkChannel.Reader.ReadAsync(token);
            });

        var z21Client = new Z21Client.Z21Client(loggerMock.Object, udpClientMock.Object);

        // --- ACT ---
        bool firstConnect = await z21Client.ConnectAsync("127.0.0.1", 21105);

        bool secondConnect = await z21Client.ConnectAsync("127.0.0.1", 21105);

        // --- ASSERT ---
        Assert.True(firstConnect);
        Assert.True(secondConnect);

        loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Already connected")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        await z21Client.DisconnectAsync();
    }

    private static byte[] CreateHardwareInfoPacket()
    {
        var data = new byte[12];
        BitConverter.GetBytes((ushort)12).CopyTo(data, 0);
        BitConverter.GetBytes((ushort)0x1A).CopyTo(data, 2);
        BitConverter.GetBytes((uint)0x00000200).CopyTo(data, 4);
        BitConverter.GetBytes((uint)0x00004201).CopyTo(data, 8); // FW 1.42
        return data;
    }

    private static byte[] CreateSystemStatePacket()
    {
        // "20, 0, 132, 0, 0, 0, 2, 0, 0, 0, 29, 0, 38, 70, 38, 70, 0, 32, 0, 123"

        var data = new byte[20];
        BitConverter.GetBytes((ushort)20).CopyTo(data, 0);
        BitConverter.GetBytes((ushort)0x84).CopyTo(data, 2);
        data[6] = 2;
        data[10] = 29;
        data[12] = 38;
        data[13] = 70;
        data[14] = 38;
        data[15] = 70;
        data[17] = 32;
        data[19] = 123;

        return data;
    }

    private static byte[] CreateZ21CodePacket()
    {
        var data = new byte[5];
        BitConverter.GetBytes((ushort)5).CopyTo(data, 0);
        BitConverter.GetBytes((ushort)0x18).CopyTo(data, 2);
        return data;
    }

    private static byte[] CreateSerialNumberPacket()
    {
        var data = new byte[8];
        BitConverter.GetBytes((ushort)8).CopyTo(data, 0);
        BitConverter.GetBytes((ushort)0x12).CopyTo(data, 2);
        return data;
    }

    */

    public void Dispose()
    {
        _z21Client.DisposeAsync().AsTask().Wait();
    }
}