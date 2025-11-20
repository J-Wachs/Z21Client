using System.Text;

namespace Z21Dashboard.Application.Models;

public sealed class BroadcastFlagsChangedEventArgs : EventArgs
{
    public uint Flags { get; }

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


    public BroadcastFlagsChangedEventArgs(uint flags)
    {
        Flags = flags;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Basic info:          {(IsBasicInfoEnabled ? "On" : "Off")}");
        sb.AppendLine($"R-Bus:               {(IsRBusEnabled ? "On" : "Off")}");
        sb.AppendLine($"RailCom subscribed:  {(IsRailComSubscribedEnabled ? "On" : "Off")}");
        sb.AppendLine($"System State:        {(IsSystemStateEnabled ? "On" : "Off")}");
        sb.AppendLine($"All loco info:       {(IsAllLocoInfoEnabled ? "On" : "Off")}");
        sb.AppendLine($"RailCom all:         {(IsAllRailComEnabled ? "On" : "Off")}");
        return sb.ToString();
    }
}
