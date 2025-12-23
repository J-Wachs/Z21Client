namespace Z21Client.Models;

/// <summary>
/// Provides data for the event that is raised when broadcast flags are read.
/// </summary>
/// <param name="flags">A bitwise combination of values from the BroadcastFlags enumeration representing the enabled broadcast features at
/// the time of the event.</param>
public sealed record BroadcastFlagsStatus(uint flags)
{
    public uint Flags { get; } = flags;

    public bool IsBasicInfoEnabled => (Flags & (uint)BroadcastFlags.Basic) != 0;

    public bool IsRBusEnabled => (Flags & (uint)BroadcastFlags.RBus) != 0;

    public bool IsRailComSubscribedEnabled => (Flags & (uint)BroadcastFlags.RailComSubscribed) != 0;

    public bool IsFastClockEnabled => (Flags & (uint)BroadcastFlags.FastClock) != 0;

    public bool IsSystemStateEnabled => (Flags & (uint)BroadcastFlags.SystemState) != 0;

    public bool IsAllLocoInfoEnabled => (Flags & (uint)BroadcastFlags.AllLocoInfo) != 0;

    public bool IsCanBoosterEnabled => (Flags & (uint)BroadcastFlags.CanBooster) != 0;

    public bool IsAllRailComEnabled => (Flags & (uint)BroadcastFlags.AllRailCom) != 0;

    public bool IsLocoNetGeneralEnabled => (Flags & (uint)BroadcastFlags.LocoNetGeneral) != 0;

    public bool IsLocoNetLocosEnabled => (Flags & (uint)BroadcastFlags.LocoNetLocos) != 0;

    public bool IsLocoNetSwitchesEnabled => (Flags & (uint)BroadcastFlags.LocoNetSwitches) != 0;

    public bool IsLocoNetDetectorEnabled => (Flags & (uint)BroadcastFlags.LocoNetDetector) != 0;
}
