using Z21Client.Models;

namespace Z21ClientTest.Models;

public class RailComDataTests
{
    [Fact]
    public void Constructor_ParsesLocoAddressFromSpan()
    {
        // Arrange
        var data = new byte[13];
        data[0] = 0x05;
        data[1] = 0x00;
        data[9] = 0x00;
        data[10] = 0x20;
        data[11] = 0x10;

        // Act
        var railComData = new RailComData(data);

        // Assert
        Assert.Equal((ushort)5, railComData.LocoAddress);
        Assert.Equal((byte)0x20, railComData.Speed);
    }
}
