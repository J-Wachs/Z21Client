using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Reflection;
using Z21Client;
using Z21Client.Models;

namespace Z21ClientTest;

public class Z21ClientTests
{
    private readonly Mock<ILogger<Z21Client.Z21Client>> _loggerMock;
    private readonly Mock<IZ21UdpClient> _udpClientMock;
    private readonly Z21Client.Z21Client _client;

    public Z21ClientTests()
    {
        _loggerMock = new Mock<ILogger<Z21Client.Z21Client>>();
        _udpClientMock = new Mock<IZ21UdpClient>();
        _client = new Z21Client.Z21Client(_loggerMock.Object, _udpClientMock.Object);
    }

    [Fact]
    public void Constructor_InitializesClient()
    {
        // Assert
        Assert.NotNull(_client);
        Assert.False(_client.IsConnected);
    }

    [Fact]
    public async Task GetBroadcastFlagsAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetBroadcastFlagsAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x04, 0x00, 0x51, 0x00 }, captured[0]);
    }

    [Fact]
    public async Task GetFirmwareVersionAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetFirmwareVersionAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x07, 0x00, 0x40, 0x00, 0xF1, 0x0A, 0xFB }, captured[0]);
    }

    [Fact]
    public async Task GetHardwareInfoAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetHardwareInfoAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x04, 0x00, 0x1A, 0x00 }, captured[0]);
    }

    [Fact]
    public async Task GetSerialNumberAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetSerialNumberAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x04, 0x00, 0x10, 0x00 }, captured[0]);
    }

    [Fact]
    public async Task GetSystemStateAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetSystemStateAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x04, 0x00, 0x85, 0x00 }, captured[0]);
    }

    [Fact]
    public async Task GetZ21CodeAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetZ21CodeAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x04, 0x00, 0x18, 0x00 }, captured[0]);
    }

    [Fact]
    public async Task SetEmergencyStopAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetEmergencyStopAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x06, 0x00, 0x40, 0x00, 0x80, 0x80 }, captured[0]);
    }

    [Fact]
    public async Task SetTrackPowerOnAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetTrackPowerOnAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x07, 0x00, 0x40, 0x00, 0x21, 0x81, 0xA0 }, captured[0]);
    }

    [Fact]
    public async Task SetTrackPowerOffAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetTrackPowerOffAsync();

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x07, 0x00, 0x40, 0x00, 0x21, 0x80, 0xA1 }, captured[0]);
    }

    [Fact]
    public async Task GetCVValueFromProgTrackAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetCVValueFromProgTrackAsync(5);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x09, 0x00, 0x40, 0x00, 0x23, 0x11, 0x00, 0x04, 0x36 }, captured[0]);
    }

    [Fact]
    public async Task GetCVValueFromPOMAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetCVValueFromPOMAsync(3, 5);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x0C, 0x00, 0x40, 0x00, 0xE6, 0x30, 0x00, 0x03, 0xE4, 0x04, 0x00, 0x35 }, captured[0]);
    }

    [Fact]
    public async Task GetLocoInfoAsync_SendsExpectedPackets()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetLocoInfoAsync(3);

        Assert.Equal(2, captured.Count);
        Assert.Equal(new byte[] { 0x09, 0x00, 0x40, 0x00, 0xE3, 0xF0, 0x00, 0x03, 0x10 }, captured[0]);
        Assert.Equal(new byte[] { 0x06, 0x00, 0x60, 0x00, 0x00, 0x03 }, captured[1]);
    }

    [Fact]
    public async Task GetLocoModeAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetLocoModeAsync(3);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x06, 0x00, 0x60, 0x00, 0x00, 0x03 }, captured[0]);
    }

    [Fact]
    public async Task GetLocoSlotInfoAsync_ValidSlot_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetLocoSlotInfoAsync(5);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x06, 0x00, 0xAF, 0x00, 0x00, 0x05 }, captured[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public async Task GetLocoSlotInfoAsync_InvalidSlot_DoesNotSend(byte slot)
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetLocoSlotInfoAsync(slot);

        Assert.Empty(captured);
    }

    [Fact]
    public async Task GetRailComDataAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetRailComDataAsync(0x0102);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x07, 0x00, 0x89, 0x00, 0x01, 0x02, 0x01 }, captured[0]);
    }

    [Fact]
    public async Task GetRBusDataAsync_ValidGroup_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetRBusDataAsync(0);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x05, 0x00, 0x81, 0x00, 0x00 }, captured[0]);
    }

    [Fact]
    public async Task GetRBusDataAsync_InvalidGroup_DoesNotSend()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetRBusDataAsync(2);

        Assert.Empty(captured);
    }

    [Fact]
    public async Task GetTurnoutInfoAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetTurnoutInfoAsync(5);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x08, 0x00, 0x40, 0x00, 0x43, 0x00, 0x05, 0x46 }, captured[0]);
    }

    [Fact]
    public async Task GetTurnoutModeAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.GetTurnoutModeAsync(5);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x06, 0x00, 0x70, 0x00, 0x00, 0x05 }, captured[0]);
    }

    [Fact]
    public async Task SetCVBitOnPOMAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetCVBitOnPOMAsync(3, 5, Bits.Two, true);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x0C, 0x00, 0x40, 0x00, 0xE6, 0x30, 0x00, 0x03, 0xE8, 0x04, 0x0A, 0x33 }, captured[0]);
    }

    [Fact]
    public async Task SetCVValueOnPOMAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetCVValueOnPOMAsync(3, 5, 7);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x0C, 0x00, 0x40, 0x00, 0xE6, 0x30, 0x00, 0x03, 0xEC, 0x04, 0x07, 0x3A }, captured[0]);
    }

    [Fact]
    public async Task SetCVValueOnProgTrackAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetCVValueOnProgTrackAsync(5, 7);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x0A, 0x00, 0x40, 0x00, 0x24, 0x12, 0x00, 0x04, 0x07, 0x35 }, captured[0]);
    }

    [Fact]
    public async Task SetLocoDriveAsync_Dcc128Forwards_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetLocoDriveAsync(3, 10, NativeSpeedSteps.Steps128, DrivingDirection.Forward, LocoMode.DCC);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x0A, 0x00, 0x40, 0x00, 0xE4, 0x13, 0x00, 0x03, 0x8B, 0x7F }, captured[0]);
    }

    [Fact]
    public async Task SetLocoFunctionAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetLocoFunctionAsync(3, 5);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x0A, 0x00, 0x40, 0x00, 0xE4, 0xF8, 0x00, 0x03, 0x85, 0x9A }, captured[0]);
    }

    [Fact]
    public async Task SetLocoModeAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetLocoModeAsync(3, LocoMode.DCC);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x07, 0x00, 0x61, 0x00, 0x00, 0x03, 0x00 }, captured[0]);
    }

    [Fact]
    public async Task SetTurnoutModeAsync_SendsExpectedPacket()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetTurnoutModeAsync(3, TurnoutMode.MM);

        Assert.Single(captured);
        Assert.Equal(new byte[] { 0x07, 0x00, 0x71, 0x00, 0x00, 0x03, 0x01 }, captured[0]);
    }

    [Fact]
    public async Task SetTurnoutPositionAsync_SendsExpectedPackets()
    {
        var (_, _, client, captured) = CreateConnectedClient();

        await client.SetTurnoutPositionAsync(3, TurnoutPosition.Position2);

        Assert.Equal(2, captured.Count);
        Assert.Equal(new byte[] { 0x09, 0x00, 0x40, 0x00, 0x53, 0x00, 0x03, 0x89, 0xD9 }, captured[0]);
        Assert.Equal(new byte[] { 0x09, 0x00, 0x40, 0x00, 0x53, 0x00, 0x03, 0x81, 0xD1 }, captured[1]);
    }

    [Fact]
    public async Task ConnectAsync_AlreadyConnected_ReturnsTrueWithoutPinging()
    {
        var udpMock = new Mock<IZ21UdpClient>();
        var loggerMock = new Mock<ILogger<Z21Client.Z21Client>>();
        var client = new Z21Client.Z21Client(loggerMock.Object, udpMock.Object);
        SetConnectedState(client);

        var result = await client.ConnectAsync("127.0.0.1");

        Assert.True(result);
        udpMock.Verify(u => u.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Never);
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        var result = await Record.ExceptionAsync(async () => await _client.DisconnectAsync());

        Assert.Null(result);
    }

    private static (Mock<ILogger<Z21Client.Z21Client>> Logger, Mock<IZ21UdpClient> Udp, Z21Client.Z21Client Client, List<byte[]> Captured) CreateConnectedClient()
    {
        var logger = new Mock<ILogger<Z21Client.Z21Client>>();
        var udp = new Mock<IZ21UdpClient>();
        var captured = new List<byte[]>();

        udp.Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()))
            .ReturnsAsync((byte[] datagram, int bytes, IPEndPoint? _) =>
            {
                captured.Add(datagram.Take(bytes).ToArray());
                return bytes;
            });

        var client = new Z21Client.Z21Client(logger.Object, udp.Object);
        SetConnectedState(client);

        return (logger, udp, client, captured);
    }

    private static void SetConnectedState(Z21Client.Z21Client client)
    {
        var isConnectedField = typeof(Z21Client.Z21Client).GetField("<IsConnected>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!;
        isConnectedField.SetValue(client, true);

        var remoteEndPointField = typeof(Z21Client.Z21Client).GetField("_remoteEndPoint", BindingFlags.NonPublic | BindingFlags.Instance)!;
        remoteEndPointField.SetValue(client, new IPEndPoint(IPAddress.Loopback, 21105));
    }
}
