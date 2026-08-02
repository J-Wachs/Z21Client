using Z21Client;

namespace Z21ClientTest;

public class Z21UdpClientTests
{
    [Fact]
    public void Bind_CreatesUdpClient()
    {
        // Arrange
        using var client = new Z21UdpClient();

        // Act
        client.Bind();

        // Assert
        Assert.NotNull(client.Client);
        client.Close();
    }
}
