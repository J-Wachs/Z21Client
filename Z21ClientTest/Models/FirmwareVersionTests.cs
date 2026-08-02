using Z21Client.Models;

namespace Z21ClientTest.Models;

public class FirmwareVersionTests
{
    [Fact]
    public void Version_ReturnsExpectedVersion()
    {
        // Arrange & Act
        var firmwareVersion = new FirmwareVersion(1, 42);

        // Assert
        Assert.Equal(new Version(1, 42), firmwareVersion.Version);
    }

    [Fact]
    public void ToString_ReturnsFormattedVersion()
    {
        // Arrange & Act
        var firmwareVersion = new FirmwareVersion(1, 5);

        // Assert
        Assert.Equal("V1.05", firmwareVersion.ToString());
    }
}
