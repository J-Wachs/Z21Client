namespace Z21Client.Models;

/// <summary>
/// Represents the feature scope (lock status) of a Z21 command station, particularly a "z21 start".
/// </summary>
public enum Z21LockState
{
    /// <summary>
    /// All features are permitted (e.g., a full Z21).
    /// </summary>
    NoLock = 0x00,

    /// <summary>
    /// "z21 start": Driving and switching via LAN/WLAN is blocked.
    /// </summary>
    Locked = 0x01,

    /// <summary>
    /// "z21 start": Driving and switching via LAN/WLAN is permitted (unlocked).
    /// </summary>
    Unlocked = 0x02
}


/// <summary>
/// Represents the Z21's feature code response.
/// </summary>
/// <param name="LockState">The parsed lock state of the Z21.</param>
public record Z21Code(Z21LockState LockState);
