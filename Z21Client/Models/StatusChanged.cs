namespace Z21Client.Models;

/// <summary>
/// Status Changed structure for the Z21 Client.
/// </summary>
/// <param name="Status">The raw status byte received from the Z21.</param>
public sealed record StatusChanged(byte Status)
{
    private const byte EmergencyStopMask = 0x01;
    private const byte TrackVoltageOffMask = 0x02;
    private const byte ShortCircuitMask = 0x04;
    private const byte ProgrammingModeActiveMask = 0x20;

    /// <summary>
    /// Gets a value indicating whether the emergency stop is active.
    /// </summary>
    public bool EmergencyStop => (Status & EmergencyStopMask) != 0;

    /// <summary>
    /// Gets a value indicating whether the track voltage is off.
    /// </summary>
    public bool TrackVoltageOff => (Status & TrackVoltageOffMask) != 0;

    /// <summary>
    /// Gets a value indicating whether a short circuit is detected.
    /// </summary>
    public bool ShortCircuit => (Status & ShortCircuitMask) != 0;

    /// <summary>
    /// Gets a value indicating whether programming mode is active.
    /// </summary>
    public bool ProgrammingModeActive => (Status & ProgrammingModeActiveMask) != 0;
}
