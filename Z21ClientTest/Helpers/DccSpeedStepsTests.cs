using Z21Client.Helpers;
using Z21Client.Models;

namespace Z21ClientTest.Helpers;

public class DccSpeedStepsTests
{
    [Fact]
    public void GetSpeedStep_Dcc14_Stop_ReturnsZero()
    {
        byte result = InvokeGetSpeedStep(0, SpeedSteps.Steps14);

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetSpeedStep_Dcc28_EStop_ReturnsZero()
    {
        byte result = InvokeGetSpeedStep(0b10001, SpeedSteps.Steps28);

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetSpeedStep_Dcc128_MaxValue_ReturnsOneHundredTwentySix()
    {
        byte result = InvokeGetSpeedStep(127, SpeedSteps.Steps128);

        Assert.Equal(126, result);
    }

    [Fact]
    public void GetSpeedStepReverse_Dcc14_StepOne_ReturnsTwo()
    {
        byte result = InvokeGetSpeedStepReverse(1, SpeedSteps.Steps14);

        Assert.Equal(2, result);
    }

    private static byte InvokeGetSpeedStep(byte rocoValue, SpeedSteps speedSteps)
    {
        var method = typeof(DccSpeedSteps).GetMethod("GetSpeedStep", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (byte)method.Invoke(null, [rocoValue, speedSteps])!;
    }

    private static byte InvokeGetSpeedStepReverse(byte value, SpeedSteps speedSteps)
    {
        var method = typeof(DccSpeedSteps).GetMethod("GetSpeedStepReverse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (byte)method.Invoke(null, [value, speedSteps])!;
    }
}
