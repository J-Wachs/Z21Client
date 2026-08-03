using Z21Client.Models;

namespace Z21ClientTest.Models;

public class BroadcastFlagsStatusTests
{
    [Fact]
    public void IsBasicInfoEnabled_ReturnsTrueWhenBasicFlagSet()
    {
        // Arrange
        var status = new BroadcastFlagsStatus((uint)BroadcastFlags.Basic);

        // Act & Assert
        Assert.True(status.IsBasicInfoEnabled);
    }

    [Fact]
    public void IsSystemStateEnabled_ReturnsTrueWhenSystemStateFlagSet()
    {
        // Arrange
        var status = new BroadcastFlagsStatus((uint)BroadcastFlags.SystemState);

        // Act & Assert
        Assert.True(status.IsSystemStateEnabled);
    }
}
