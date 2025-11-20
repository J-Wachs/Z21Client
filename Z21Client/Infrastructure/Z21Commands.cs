namespace Z21Client.Infrastructure;

/// <summary>
/// Contains predefined byte arrays for static Z21 LAN protocol commands.
/// This class uses constants from <see cref="Z21ProtocolConstants"/> to construct the commands.
/// </summary>
internal static class Z21Commands
{
    /// <summary>
    /// Command to request the currently active broadcast flags. (LAN_GET_BROADCASTFLAGS)
    /// </summary>
    internal static readonly byte[] GetBroadcastFlags = { 0x04, 0x00, (byte)(Z21ProtocolConstants.HeaderGetBroadcastFlags & 0xFF), (byte)(Z21ProtocolConstants.HeaderGetBroadcastFlags >> 8) };

    internal static readonly byte[] GetCode = { 0x04, 0x00, (byte)(Z21ProtocolConstants.HeaderGetCode & 0xFF), (byte)(Z21ProtocolConstants.HeaderGetCode >> 8) };

    /// <summary>
    /// Command to request the firmware version of the Z21. (LAN_X_GET_FIRMWARE_VERSION)
    /// </summary>
    internal static readonly byte[] GetFirmwareVersion = { 0x07, 0x00, (byte)(Z21ProtocolConstants.HeaderXBus & 0xFF), (byte)(Z21ProtocolConstants.HeaderXBus >> 8), 0xF1, 0x0A, 0xFB };

    /// <summary>
    /// Command to request hardware information and firmware version. (LAN_GET_HWINFO)
    /// </summary>
    internal static readonly byte[] GetHardwareInfo = { 0x04, 0x00, (byte)(Z21ProtocolConstants.HeaderGetHardwareInfo & 0xFF), (byte)(Z21ProtocolConstants.HeaderGetHardwareInfo >> 8) };

    /// <summary>
    /// Command to request the next locomotive in the RailCom list. (LAN_RAILCOM_GETDATA)
    /// </summary>
    //internal static readonly byte[] GetRailComDataNext = { 0x07, 0x00, (byte)(Z21ProtocolConstants.HeaderRailComGetData & 0xFF), (byte)(Z21ProtocolConstants.HeaderRailComGetData >> 8), 0x01, 0x00, 0x00 };
    internal static readonly byte[] GetRailComDataNext = { 0x04, 0x00, (byte)(Z21ProtocolConstants.HeaderGetRailComData & 0xFF), (byte)(Z21ProtocolConstants.HeaderGetRailComData >> 8) };

    /// <summary>
    /// Command to request the serial number of the Z21. (LAN_GET_SERIAL_NUMBER)
    /// </summary>
    internal static readonly byte[] GetSerialNumber = { 0x04, 0x00, (byte)(Z21ProtocolConstants.HeaderGetSerialNumber & 0xFF), (byte)(Z21ProtocolConstants.HeaderGetSerialNumber >> 8) };

    /// <summary>
    /// Command to request the current system state. (LAN_SYSTEMSTATE_GETDATA)
    /// </summary>
    internal static readonly byte[] GetSystemState = { 0x04, 0x00, (byte)(Z21ProtocolConstants.HeaderGetSystemState & 0xFF), (byte)(Z21ProtocolConstants.HeaderGetSystemState >> 8) };

    /// <summary>
    /// Command to log off the client from the Z21. (LAN_LOGOFF)
    /// </summary>
    internal static readonly byte[] Logoff = { 0x04, 0x00, (byte)(Z21ProtocolConstants.HeaderLogoff & 0xFF), (byte)(Z21ProtocolConstants.HeaderLogoff >> 8) };

    /// <summary>
    /// Command to set an emergency stop in the Z21. (LAN_X_SET_STOP)
    /// </summary>
    internal static readonly byte[] SetEmergencyStop = { 0x06, 0x00, (byte)(Z21ProtocolConstants.HeaderXBus & 0xFF), (byte)(Z21ProtocolConstants.HeaderXBus >> 8), 0x80, 0x80 };

    /// <summary>
    /// Command to turn on track power. (LAN_X_SET_TRACK_POWER_ON)
    /// </summary>
    internal static readonly byte[] SetTrackPowerOn = { 0x07, 0x00, (byte)(Z21ProtocolConstants.HeaderXBus & 0xFF), (byte)(Z21ProtocolConstants.HeaderXBus >> 8), 0x21, 0x81, 0xA0 };

    /// <summary>
    /// Command to turn off track power. (LAN_X_SET_TRACK_POWER_OFF)
    /// </summary>
    internal static readonly byte[] SetTrackPowerOff = { 0x07, 0x00, (byte)(Z21ProtocolConstants.HeaderXBus & 0xFF), (byte)(Z21ProtocolConstants.HeaderXBus >> 8), 0x21, 0x80, 0xA1 };
}
