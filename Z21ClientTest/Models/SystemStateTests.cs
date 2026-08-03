using Z21Client.Models;

namespace Z21ClientTest.Models;

public class SystemStateTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange & Act
        var state = new SystemState(
            mainCurrentmA: 100,
            progCurrentmA: 50,
            mainCurrentFilteredmA: 90,
            temperatureC: 25,
            supplyVoltagemV: 12000,
            vccVoltagemV: 5000,
            centralState: 0,
            centralStateEx: 0);

        // Assert
        Assert.Equal(100, state.MainCurrentmA);
        Assert.Equal(50, state.ProgCurrentmA);
        Assert.Equal(90, state.MainCurrentFilteredmA);
    }

    [Fact]
    public void Caps_ParsesDccCapability()
    {
        // Arrange & Act
        var caps = new SystemState.Caps(SystemState.CapabilitiesDcc);

        // Assert
        Assert.Equal(Protocols.DCC, caps.Protocols);
    }
}
