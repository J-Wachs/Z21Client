using Z21Client.Models;

namespace Z21Client.Helpers;

/// <summary>
/// Provides static lookup methods to convert Z21 speed values for DCC 14, 28, and 128-step modes
/// into an integer representing the speed step.
/// 
/// The values for the sppeed steps are based on the Roco/Z21 protocol documentation.
/// </summary>
internal static class DccSpeedSteps
{
    private static readonly Dictionary<int, int> Dcc14SpeedMap;
    private static readonly Dictionary<int, int> Dcc28SpeedMap;

    /// <summary>
    /// Static constructor to build the lookup tables upon first use.
    /// </summary>
    static DccSpeedSteps()
    {
        // --- Build the DCC 14-step map ---
        Dcc14SpeedMap = new Dictionary<int, int>
        {
            { 0, 0 }, // Stop
            { 1, 0 }, // E-Stop
            { 2, 1 }, { 3, 2 }, { 4, 3 }, { 5, 4 }, { 6, 5 }, { 7, 6 }, { 8, 7 },
            { 9, 8 }, { 10, 9 }, { 11, 10 }, { 12, 11 }, { 13, 12 }, { 14, 13 }, { 15, 14 }
        };

        // --- Programmatically build the DCC 28-step map ---
        Dcc28SpeedMap = new Dictionary<int, int>
        {
            { 0b00000, 0 }, // Stop
            { 0b10000, 0 }, // Stop (intermediate)
            { 0b00001, 0 }, // E-Stop
            { 0b10001, 0 }  // E-Stop (intermediate)
        };

        for (int i = 2; i <= 15; i++)
        {
            // Lower speed step (V5 bit is 0)
            int lowerKey = i;
            int lowerValue = (i - 2) * 2 + 1;
            Dcc28SpeedMap[lowerKey] = lowerValue;

            // Higher/intermediate speed step (V5 bit is 1)
            int upperKey = i | 0b10000; // Set the V5 bit
            int upperValue = (i - 2) * 2 + 2;
            Dcc28SpeedMap[upperKey] = upperValue;
        }
    }

    internal static byte GetSpeedStep(byte rocoValue, SpeedSteps speedSteps)
    {
        return speedSteps switch
        {
            SpeedSteps.Steps14 => GetSpeedStep14(rocoValue),
            SpeedSteps.Steps28 => GetSpeedStep28(rocoValue),
            SpeedSteps.Steps128 => GetSpeedStep128(rocoValue),
            _ => 0,
        };
    }

    internal static byte GetSpeedStepReverse(byte value, SpeedSteps speedSteps)
    {
        return speedSteps switch
        {
            SpeedSteps.Steps14 => GetSpeedStep14Reverse(value),
            SpeedSteps.Steps28 => GetSpeedStep28Reverse(value),
            SpeedSteps.Steps128 => GetSpeedStep128Reverse(value),
            _ => 0,
        };
    }

    /// <summary>
    /// Looks up the speed step for a given Roco/Z21 value in DCC 14-step mode.
    /// </summary>
    /// <param name="rocoValue">The byte value received from the Z21.</param>
    /// <returns>The matching speed step (0-14).</returns>
    private static byte GetSpeedStep14(byte rocoValue)
    {
        // Use the lower 4 bits (VVVV) for the key.
        int key = rocoValue & 0x0F;
        if (Dcc14SpeedMap.TryGetValue(key, out var speedStep))
        {
            return (byte)speedStep;
        }
        return 0;
    }

    /// <summary>
    /// Looks up the Roco/Z21 value for a given speed step in DCC 14-step mode.
    /// </summary>
    /// <param name="value">The value of the speed step</param>
    /// <returns>The matching Roco/Z21 value.</returns>
    private static byte GetSpeedStep14Reverse(byte value)
    {
        if (Dcc14SpeedMap.ContainsValue(value))
        {
            var res = Dcc14SpeedMap.FirstOrDefault(res => res.Value == value);
            return (byte)res.Key;
        }
        return 0;
    }

    /// <summary>
    /// Looks up the speed step for a given Roco/Z21 value in DCC 28-step mode.
    /// </summary>
    /// <param name="rocoValue">The byte value received from the Z21.</param>
    /// <returns>The matching speed step (0-28).</returns>
    private static byte GetSpeedStep28(byte rocoValue)
    {
        // Use the lower 5 bits (V5 VVVV) for the key.
        int key = rocoValue & 0x1F;
        if (Dcc28SpeedMap.TryGetValue(key, out var speedStep))
        {
            return (byte)speedStep;
        }
        return 0;
    }

    /// <summary>
    /// Looks up the Roco/Z21 value for a given speed step in DCC 28-step mode.
    /// </summary>
    /// <param name="value">The value of the speed step</param>
    /// <returns>The matching Roco/Z21 value.</returns>
    private static byte GetSpeedStep28Reverse(byte value)
    {
        if (Dcc28SpeedMap.ContainsValue(value))
        {
            var res = Dcc28SpeedMap.FirstOrDefault(res => res.Value == value);
            return (byte)res.Key;
        }
        return 0;
    }

    /// <summary>
    /// Calculates the speed step for a given Roco/Z21 value in DCC 128-step mode.
    /// </summary>
    /// <param name="rocoValue">The byte value received from the Z21 (0-127).</param>
    /// <returns>The matching speed step (0-126).</returns>
    private static byte GetSpeedStep128(byte rocoValue)
    {
        // For 128 steps, the protocol is much simpler.
        // The value is sent as a 7-bit number (0-127).
        int key = rocoValue & 0x7F; // Ensure we only read 7 bits.

        if (key == 0)
        {
            return 0; // Stop
        }
        if (key == 1)
        {
            return 0; // E-Stop
        }

        // For all other values, the speed step is the value minus 1.
        // Step 1 corresponds to value 2.
        // Step 126 corresponds to value 127.
        return (byte)(key - 1);
    }

    /// <summary>
    /// Looks up the Roco/Z21 value for a given speed step in DCC 128-step mode.
    /// </summary>
    /// <param name="value">The value of the speed step</param>
    /// <returns>The matching Roco/Z21 value.</returns>
    private static byte GetSpeedStep128Reverse(byte value)
    {
        if (value == 0)
        {
            return 0; // Stop
        }
        if (value == 1)
        {
            return 1; // E-Stop
        }
        if (value >= 2)
        {
            if (value > 126)
            {
                value = 126;
            }
            return (byte)(value + 1);
        }
        return 0;
    }
}
