using Z21Client.Models;

namespace Z21ClientTest.Models;

public class RBusDataTests
{
    [Fact]
    public void IsSensorActive_ReturnsTrueWhenBitIsSet()
    {
        // Arrange
        var feedbackData = new byte[10];
        feedbackData[0] = 0b00000001; // module 1, port 1 active
        var rBusData = new RBusData(0, feedbackData);

        // Act
        var result = rBusData.IsSensorActive(1, 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSensorActive_ReturnsFalseForInvalidPort()
    {
        // Arrange
        var rBusData = new RBusData(0, new byte[10]);

        // Act
        var result = rBusData.IsSensorActive(1, 0);

        // Assert
        Assert.False(result);
    }
}
