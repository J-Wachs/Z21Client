using Z21Client.Models;

namespace Z21ClientTest.Models;

public class Z21InfoTests
{
    [Fact]
    public void ToString_IncludesIpAndHardwareInfo()
    {
        // Arrange
        var hardwareInfo = new HardwareInfo(HardwareType.Z21New, new FirmwareVersion(1, 43));
        var z21Info = new Z21Info("192.168.1.10", hardwareInfo);

        // Act
        var result = z21Info.ToString();

        // Assert
        Assert.Contains("192.168.1.10", result);
        Assert.Contains("Z21New", result);
    }
}
