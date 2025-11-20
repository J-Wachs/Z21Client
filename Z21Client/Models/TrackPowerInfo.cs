namespace Z21Client.Models;

/// <summary>
/// Represents the state of a turnout (switch).
/// </summary>
public enum TrackPowerState
{
    /// <summary>
    /// The track power is off.
    /// </summary>
    Off = 0,

    /// <summary>
    /// The track power is on.
    /// </summary>
    On = 1,

    /// <summary>
    /// The track power is in programming mode.
    /// </summary>
    ProgrammingMode = 2,

    /// <summary>
    /// There is a short circuit on the track.
    /// </summary>
    ShortCircuit = 8
}

/// <summary>
/// Represents information about the track power on the Z21.
/// </summary>
/// <param name="State">The current state of the power.</param>
public sealed record TrackPowerInfo(TrackPowerState State);
