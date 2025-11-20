namespace Z21Client.Models;

/// <summary>
/// Defines the speed step modes for controlling a locomotive. This represents the corrected speed
/// steps, eg MM2/14 is represented as 148 speed steps, and not 28 as z21/Z21 does.
/// </summary>
public enum SpeedSteps
{
    /// <summary>
    /// Speed steps are not known.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// 14 speed steps or 14 speed steps for MM1 and MM2.
    /// </summary>
    Steps14 = 0,

    /// <summary>
    /// 28 speed steps or 28 speed steps for MM2.
    /// </summary>
    Steps28 = 2,

    /// <summary>
    /// 128 speed steps for DCC.
    /// </summary>
    Steps128 = 3
}
