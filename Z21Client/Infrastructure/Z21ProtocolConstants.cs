namespace Z21Client.Infrastructure;

/// <summary>
/// Contains constants for the Z21 LAN protocol, such as headers and X-headers,
/// based on the official Z21 LAN Protocol Specification.
/// </summary>
public static class Z21ProtocolConstants
{
    /// <summary>
    /// Standard header for general Z21 responses and requests.
    /// </summary>
    public const ushort HeaderGeneral = 0x0000;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_CODE.
    /// </summary>
    public const ushort HeaderGetCode = 0x0018;

    /// <summary>
    /// Header for requests related to LAN_GET_SERIAL_NUMBER.
    /// </summary>
    public const ushort HeaderGetSerialNumber = 0x0010;

    /// <summary>
    /// Header for requests related to LAN_GET_HWINFO.
    /// </summary>
    public const ushort HeaderGetHardwareInfo = 0x001A;

    /// <summary>
    /// Header for requests related to LAN_LOGOFF.
    /// </summary>
    public const ushort HeaderLogoff = 0x0030;

    /// <summary>
    /// Standard header for X-Bus messages transmitted over LAN.
    /// </summary>
    public const ushort HeaderXBus = 0x0040;

    /// <summary>
    /// Header for requests related to LAN_SET_BROADCASTFLAGS.
    /// </summary>
    public const ushort HeaderSetBroadcastFlags = 0x0050;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_BROADCASTFLAGS.
    /// </summary>
    public const ushort HeaderGetBroadcastFlags = 0x0051;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_LOCOMODE.
    /// </summary>
    public const ushort HeaderGetLocoMode = 0x0060;

    /// <summary>
    /// Header for requests related to LAN_SET_LOCOMODE.
    /// </summary>
    public const ushort HeaderSetLocoMode = 0x0061;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_TURNOUTMODE.
    /// </summary>
    public const ushort HeaderGetTurnoutMode = 0x0070;

    /// <summary>
    /// Header for requests related to LAN_SET_TURNOUTMODE.
    /// </summary>
    public const ushort HeaderSetTurnoutMode = 0x0071;

    /// <summary>
    /// Header for responses containing R-Bus data (LAN_RMBUS_DATACHANGED).
    /// </summary>
    public const ushort HeaderRBusDataChanged = 0x0080;

    /// <summary>
    /// Header for requests related to LAN_RMBUS_GETDATA.
    /// </summary>
    public const ushort HeaderGetRBusData = 0x0081;

    /// <summary>
    /// Header for requests and responses related to LAN_GET_SYSTEM_STATE.
    /// </summary>
    public const ushort HeaderGetSystemState = 0x0085;

    /// <summary>
    /// Header for *responses* containing the system state.
    /// </summary>
    public const ushort HeaderSystemStateResponse = 0x0084;

    /// <summary>
    /// Header for responses containing RailCom data (LAN_RAILCOM_DATACHANGED).
    /// </summary>
    public const ushort HeaderRailComDataChanged = 0x0088;

    /// <summary>
    /// Header for requests related to LAN_RAILCOM_GETDATA.
    /// </summary>
    public const ushort HeaderGetRailComData = 0x0089;

    /// <summary>
    /// Represents the constant length value used for sending the Get RBus data command.
    /// </summary>
    public const ushort LengthGetRBusData = 0x0005;

    /// <summary>
    /// Represents the protocol message length for retrieving locomotive information.
    /// </summary>
    public const ushort LengthGetLocoInfo = 0x0009;

    /// <summary>
    /// Represents the fixed message length, in bytes, for the Get Loco Slot Info command.
    /// </summary>
    public const ushort LengthGetLocoSlotInfo = 0x0006;

    /// <summary>
    /// Length of a LAN_GET_LOCOMODE request packet.
    /// </summary>
    public const ushort LengthGetLocoMode = 0x0006;

    /// <summary>
    /// Represents the protocol message length for setting a locomotive function command.
    /// </summary>
    public const ushort LengthSetLocoFunction = 0x000A;

    /// <summary>
    /// Length of a LAN_SET_LOCO_DRIVE request packet.
    /// </summary>
    public const ushort LengthSetLocoDrive = 0x000A;

    /// <summary>
    /// Length of a LAN_SET_LOCOMODE request packet.
    /// </summary>
    public const ushort LengthSetLocoMode = 0x0007;

    /// <summary>
    /// Length of a LAN_RAILCOM_GETDATA request packet.
    /// </summary>
    public const ushort LengthGetRailComData = 0x0007;


    /// <summary>
    /// Length of a LAN_X_GET_TURNOUTINFO request packet.
    /// </summary>
    public const ushort LengthGetTurnoutInfo = 0x0008;

    /// <summary>
    /// Length of a LAN_GET_TURNOUTMODE request packet.
    /// </summary>
    public const ushort LengthGetTurnoutMode = 0x0006;

    /// <summary>
    /// Length of a LAN_SET_TURNOUTMODE request packet.
    /// </summary>
    public const ushort LengthSetTurnoutMode = 0x0007;

    /// <summary>
    /// Represents the fixed message length, in bytes, for a 'Set Turnout Position' command.
    /// </summary>
    public const ushort LengthSetTurnoutPosition = 0x0009;

    /// <summary>
    /// Length of a LAN_SET_BROADCASTFLAGS request packet.
    /// </summary>
    public const ushort LengthSetBroadcastFlags = 0x0008;

    /// <summary>
    /// Represents the value of the X header field as defined by the protocol specification.
    /// </summary>
    public const ushort XHeader = 0x0040;

    /// <summary>
    /// X-Header for a response that emergency stop was issued (LAN_BC_STOPPED).
    /// </summary>
    public const byte XHeaderEmergencyStop = 0x81;

    /// <summary>
    /// Represents the protocol header value used to set the locomotive drive state in XHeader messages.
    /// </summary>
    public const byte XHeaderSetLocoDrive = 0xE4;

    /// <summary>
    /// Represents the protocol header value used to request locomotive information in XHeader-based communications.
    /// </summary>
    public const byte XHeaderGetLocoInfo = 0xE3;

    /// <summary>
    /// X-Header for a request to get turnout information (LAN_X_GET_TURNOUTINFO).
    /// </summary>
    public const byte XHeaderGetTurnoutInfo = 0x43;

    /// <summary>
    /// Represents the protocol header value used to identify a locomotive function set command in XHeader
    /// communication.
    /// </summary>
    public const ushort XHeaderSetLocoFunction = 0xF8E4;

    /// <summary>
    /// Represents the protocol header value used to set the turnout position in a command message.
    /// </summary>
    public const byte XHeaderSetTurnoutPosition = 0x53;

    /// <summary>
    /// X-Header for a response containing turnout information (LAN_X_TURNOUT_INFO).
    /// </summary>
    public const byte XHeaderTurnoutInfo = 0x43;

    /// <summary>
    /// X-Header for a response containing track power information (LAN_X_BC_TRACK_POWER_OFF, LAN_X_BC_TRACK_POWER_ON, LAN_X_BC_PROGRAMMING_MODE, LAN_X_BC_TRACK_SHORT_CIRCUIT).
    /// </summary>
    public const byte XHeaderTrackPower = 0x61;

    /// <summary>
    /// X-Header for a response that a command was unknown (LAN_X_UNKNOWN_COMMAND).
    /// </summary>
    public const ushort XHeaderUnknownCommand = 0x8261;

    /// <summary>
    /// X-Header for a response containing locomotive information (LAN_X_LOCO_INFO).
    /// </summary>
    public const byte XHeaderLocoInfo = 0xEF;

    /// <summary>
    /// X-Header for a response containing the firmware version (LAN_X_GET_FIRMWARE_VERSION).
    /// </summary>
    public const byte XHeaderFirmwareVersion = 0xF3;

    /// <summary>
    /// Represents the protocol header value used to request locomotive slot information.
    /// </summary>
    public const ushort HeaderGetLocoSlotInfo = 0x00AF;
}
