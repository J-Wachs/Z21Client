using Z21Client.Helpers;
using Z21Client.Models;

namespace Z21ClientTest.Helpers;

public class Z21ProtocolNameTests
{
    [Fact]
    public void GetName_ReturnsExpectedProtocolName()
    {
        // Arrange
        var locoMode = LocoMode.DCC;
        var speedSteps = NativeSpeedSteps.Steps28;

        // Act
        var result = Z21ProtocolName.GetName(locoMode, speedSteps);

        // Assert
        Assert.Equal("DCC28", result);
    }

    [Fact]
    public void GetProtocol_ReturnsExpectedLocomotiveProtocol()
    {
        // Arrange
        var locoMode = LocoMode.MM;
        var speedSteps = NativeSpeedSteps.Steps14;

        // Act
        var result = Z21ProtocolName.GetProtocol(locoMode, speedSteps);

        // Assert
        Assert.Equal(LocomotiveProtocol.MM1_14, result);
    }
}
