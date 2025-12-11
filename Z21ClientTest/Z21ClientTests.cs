using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Sockets;
using Z21Client;
using Z21Client.Infrastructure;

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
        var responsePacket = new byte[12];

        // Write Length (12)
        BitConverter.GetBytes((ushort)12).CopyTo(responsePacket, 0);

        // Write Header (HeaderGetHardwareInfo)
        // We assume Z21ProtocolConstants is available. If the namespace differs, 
        // you might need to add 'using Z21Client.Infrastructure;' or similar.
        // Assuming the value used in Z21Client is 0x1A00 (LAN_GET_HWINFO)
        // If Z21ProtocolConstants is internal, we can try to rely on the constant usage or hardcode for test:
        // Let's assume Z21ProtocolConstants is accessible.
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetHardwareInfo).CopyTo(responsePacket, 2);

        // Write HwType (e.g., 0x00000200 for z21Start)
        BitConverter.GetBytes((uint)0x00000200).CopyTo(responsePacket, 4);

        // Write FwVersion (e.g., 0x20010000 for 1.20 - format depends on logic, passing generic high value)
        // Logic: string fwString = (fwValue >> 8)...
        // Let's use 0x01230000 -> 1.23
        BitConverter.GetBytes((uint)0x00007801).CopyTo(responsePacket, 8); // 0x0178 = 1.20 decimal approx

        var fakeResult = new UdpReceiveResult(responsePacket, new IPEndPoint(IPAddress.Loopback, 21105));

        // 3. Setup ReceiveAsync to return the packet ONCE, then delay forever (to simulate idle connection)
        // This prevents the ReceiveLoop in Z21Client from spinning infinitely consuming CPU or reading nulls.
        _udpClientMock.SetupSequence(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResult) // First call: Return handshake response
            .Returns(async () => {      // Subsequent calls: Wait indefinitely
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
    public async Task ConnectAsync_ShouldLogWarning_WhenAlreadyConnected()
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
        await _z21Client.ConnectAsync(validHost, validPort);

        // Act
        // Attempt to connect a second time
        bool result = await _z21Client.ConnectAsync(validHost, validPort);

        // Assert
        Assert.True(result);

        // Verify warning log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    public void Dispose()
    {
        _z21Client.DisposeAsync().AsTask().Wait();
    }
}