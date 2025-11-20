namespace Z21Client.Infrastructure;

/// <summary>
/// Contains constants for the Z21 LAN protocol, such as headers and X-headers,
/// based on the official Z21 LAN Protocol Specification.
/// </summary>
internal static class Z21ProtocolConstants
{
    /// <summary>
    /// Standard header for general Z21 responses and requests.
    /// </summary>
    internal const ushort HeaderGeneral = 0x0000;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_CODE.
    /// </summary>
    internal const ushort HeaderGetCode = 0x0018;

    /// <summary>
    /// Header for requests related to LAN_GET_SERIAL_NUMBER.
    /// </summary>
    internal const ushort HeaderGetSerialNumber = 0x0010;

    /// <summary>
    /// Header for requests related to LAN_GET_HWINFO.
    /// </summary>
    internal const ushort HeaderGetHardwareInfo = 0x001A;

    /// <summary>
    /// Header for requests related to LAN_LOGOFF.
    /// </summary>
    internal const ushort HeaderLogoff = 0x0030;

    /// <summary>
    /// Standard header for X-Bus messages transmitted over LAN.
    /// </summary>
    internal const ushort HeaderXBus = 0x0040;

    /// <summary>
    /// Header for requests related to LAN_SET_BROADCASTFLAGS.
    /// </summary>
    internal const ushort HeaderSetBroadcastFlags = 0x0050;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_BROADCASTFLAGS.
    /// </summary>
    internal const ushort HeaderGetBroadcastFlags = 0x0051;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_LOCOMODE.
    /// </summary>
    internal const ushort HeaderGetLocoMode = 0x0060;

    /// <summary>
    /// Header for requests related to LAN_SET_LOCOMODE.
    /// </summary>
    internal const ushort HeaderSetLocoMode = 0x0061;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_TURNOUTMODE.
    /// </summary>
    internal const ushort HeaderGetTurnoutMode = 0x0070;

    /// <summary>
    /// Header for requests related to LAN_SET_TURNOUTMODE.
    /// </summary>
    internal const ushort HeaderSetTurnoutMode = 0x0071;

    /// <summary>
    /// Header for responses containing R-Bus data (LAN_RMBUS_DATACHANGED).
    /// </summary>
    internal const ushort HeaderRBusDataChanged = 0x0080;

    /// <summary>
    /// Header for requests related to LAN_RMBUS_GETDATA.
    /// </summary>
    internal const ushort HeaderRBusGetData = 0x0081;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_SYSTEM_STATE.
    /// </summary>
    internal const ushort HeaderGetSystemState = 0x0085;

    /// <summary>
    /// Header for *responses* containing the system state.
    /// </summary>
    internal const ushort HeaderSystemStateResponse = 0x0084;

    /// <summary>
    /// Header for responses containing RailCom data (LAN_RAILCOM_DATACHANGED).
    /// </summary>
    internal const ushort HeaderRailComDataChanged = 0x0088;

    /// <summary>
    /// Header for requests related to LAN_RAILCOM_GETDATA.
    /// </summary>
    internal const ushort HeaderGetRailComData = 0x0089;

    internal const ushort LengthGetLocoInfo = 0x0009;

    internal const ushort LengthGetLocoSlotInfo = 0x0006;
    /// <summary>
    /// Length of a LAN_GET_LOCOMODE request packet.
    /// </summary>
    internal const ushort LengthGetLocoMode = 0x0006;

    internal const ushort LengthSetLocoFunction = 0x000A;

    /// <summary>
    /// Length of a LAN_SET_LOCOMODE request packet.
    /// </summary>
    internal const ushort LengthSetLocoMode = 0x0007;

    /// <summary>
    /// Length of a LAN_GET_TURNOUTMODE request packet.
    /// </summary>
    internal const ushort LengthGetTurnoutMode = 0x0006;

    /// <summary>
    /// Length of a LAN_SET_TURNOUTMODE request packet.
    /// </summary>
    internal const ushort LengthSetTurnoutMode = 0x0007;

    internal const ushort LengthSetTurnoutPosition = 0x0009;

    /// <summary>
    /// Length of a LAN_SET_BROADCASTFLAGS request packet.
    /// </summary>
    internal const ushort LengthSetBroadcastFlags = 0x0008;

    internal const ushort XHeader = 0x0040;

    /// <summary>
    /// X-Header for a response that emergency stop was issued (LAN_BC_STOPPED).
    /// </summary>
    internal const byte XHeaderEmergencyStop = 0x81;

    internal const byte XHeaderGetLocoInfo = 0xE3;

    internal const ushort XHeaderSetLocoFunction = 0xF8E4;

    internal const byte XHeaderSetTurnoutPosition = 0x53;

    /// <summary>
    /// X-Header for a response containing turnout information (LAN_X_TURNOUT_INFO).
    /// </summary>
    internal const byte XHeaderTurnoutInfo = 0x43;

    /// <summary>
    /// X-Header for a response containing track power information (LAN_X_BC_TRACK_POWER_OFF, LAN_X_BC_TRACK_POWER_ON, LAN_X_BC_PROGRAMMING_MODE, LAN_X_BC_TRACK_SHORT_CIRCUIT).
    /// </summary>
    internal const byte XHeaderTrackPower = 0x61;

    /// <summary>
    /// X-Header for a response that a command was unknown (LAN_X_UNKNOWN_COMMAND).
    /// </summary>
    internal const ushort XHeaderUnknownCommand = 0x8261;

    /// <summary>
    /// X-Header for a response containing locomotive information (LAN_X_LOCO_INFO).
    /// </summary>
    internal const byte XHeaderLocoInfo = 0xEF;

    /// <summary>
    /// X-Header for a response containing the firmware version (LAN_X_GET_FIRMWARE_VERSION).
    /// </summary>
    internal const byte XHeaderFirmwareVersion = 0xF3;

    internal const ushort HeaderGetLocoSlotInfo = 0x00AF;
}
