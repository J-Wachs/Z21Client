namespace Z21Client.Models;

/// <summary>
/// Defines bit flags for Z21 broadcasts that a client can subscribe to.
/// </summary>
[Flags]
public enum BroadcastFlags : uint
{
    /// <summary>
    /// No broadcasts are subscribed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Broadcasts for driving, switching, and basic status messages.
    /// </summary>
    Basic = 0x00000001,

    /// <summary>
    /// Broadcasts for R-Bus feedback devices.
    /// </summary>
    RBus = 0x00000002,

    /// <summary>
    /// Broadcasts for RailCom data of subscribed locomotives.
    /// </summary>
    RailComSubscribed = 0x00000004,

    /// <summary>
    /// Broadcasts for fast clock time messages. (FW >= 1.43)
    /// </summary>
    FastClock = 0x00000010,

    /// <summary>
    /// Broadcasts for detailed Z21 system status changes.
    /// </summary>
    SystemState = 0x00000100,

    /// <summary>
    /// Extends basic flag to receive LAN_X_LOCO_INFO for all controlled locomotives without subscription. (FW >= 1.20)
    /// </summary>
    AllLocoInfo = 0x00010000,

    /// <summary>
    /// Forwards CAN-Bus booster status messages. (FW >= 1.41)
    /// </summary>
    CanBooster = 0x00020000,

    /// <summary>
    /// Broadcasts for RailCom data of all controlled locomotives without subscription. (FW >= 1.29)
    /// </summary>
    AllRailCom = 0x00040000,

    /// <summary>
    /// Forwards general LocoNet messages (without locos and switches).
    /// </summary>
    LocoNetGeneral = 0x01000000,

    /// <summary>
    /// Forwards locomotive-specific LocoNet messages.
    /// </summary>
    LocoNetLocos = 0x02000000,

    /// <summary>
    /// Forwards switch-specific LocoNet messages.
    /// </summary>
    LocoNetSwitches = 0x04000000,

    /// <summary>
    /// Forwards LocoNet track occupancy detector status changes. (FW >= 1.22)
    /// </summary>
    LocoNetDetector = 0x08000000
}