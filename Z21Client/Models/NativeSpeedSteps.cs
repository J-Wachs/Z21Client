namespace Z21Client.Models;

/// <summary>
/// Defines the speed step reported by z21/Z21.
/// </summary>
public enum NativeSpeedSteps
{
    /// <summary>
    /// Speed steps are not known.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// 14 speed steps or 14 speed steps for MM1.
    /// </summary>
    Steps14 = 0,

    /// <summary>
    /// 28 speed steps or 14 speed steps for MM2.
    /// </summary>
    Steps28 = 2,

    /// <summary>
    /// 128 speed steps for DCC or 28 speed steps for MM2.
    /// </summary>
    Steps128 = 3
}
