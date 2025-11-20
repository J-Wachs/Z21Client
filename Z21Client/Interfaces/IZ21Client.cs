using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// Defines the public contract for a Z21 client.
/// </summary>
public interface IZ21Client : IAsyncDisposable
{
    /// <summary>
    /// Occurs when the currently set broadcast flags are received from the Z21.
    /// </summary>
    event EventHandler<BroadcastFlagsChangedEventArgs> BroadcastFlagsReceived;

    /// <summary>
    /// Occurs when the client's connection state to the Z21 changes (e.g., connection is lost).
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

    /// <summary>
    /// Occurs when an emergency stop command is received from the Z21.
    /// </summary>
    event EventHandler? EmergencyStopReceived;

    /// <summary>
    /// Occurs when the firmware version is received from the Z21.
    /// </summary>
    event EventHandler<FirmwareVersion> FirmwareVersionReceived;

    /// <summary>
    /// Occurs when hardware information is received from the Z21.
    /// </summary>
    event EventHandler<HardwareInfo> HardwareInfoReceived;

    /// <summary>
    /// Occurs when locomotive information is received as a broadcast from the Z21.
    /// </summary>
    event EventHandler<LocoInfo> LocoInfoReceived;

    /// <summary>
    /// Occurs when a locomotive's protocol mode is received from the Z21.
    /// </summary>
    event EventHandler<LocoModeChangedEventArgs> LocoModeReceived;

    /// <summary>
    /// Occurs when a locomotive slot information is received from the Z21.
    /// </summary>
    public event EventHandler<LocoSlotInfo>? LocoSlotInfoReceived;

    /// <summary>
    /// Occurs when RailCom data for a locomotive is received from the Z21.
    /// </summary>
    event EventHandler<RailComData> RailComDataReceived;

    /// <summary>
    /// Occurs when R-Bus feedback data is received from the Z21.
    /// </summary>
    event EventHandler<RBusData> RBusDataReceived;

    /// <summary>
    /// Occurs when the serial number is received from the Z21.
    /// </summary>
    event EventHandler<SerialNumber> SerialNumberReceived;

    /// <summary>
    /// Occurs when the system state is received from the Z21.
    /// </summary>
    event EventHandler<SystemStateChangedEventArgs> SystemStateChanged;

    /// <summary>
    /// Occurs when trackk power information is received from the Z21.
    /// </summary>
    event EventHandler<TrackPowerInfo>? TrackPowerInfoReceived;

    /// <summary>
    /// Occurs when turnout (switch) information is received as a broadcast from the Z21.
    /// </summary>
    event EventHandler<TurnoutInfo> TurnoutInfoReceived;

    /// <summary>
    /// Occurs when a turnout's protocol mode is received from the Z21.
    /// </summary>
    event EventHandler<TurnoutModeChangedEventArgs> TurnoutModeReceived;

    /// <summary>
    /// Occurs when the Z21's feature scope code is received.
    /// </summary>
    event EventHandler<Z21Code> Z21CodeReceived;

    /// <summary>
    /// Connects to the Z21 command station.
    /// </summary>
    /// <param name="host">The IP address or hostname of the Z21.</param>
    /// <param name="port">The UDP port, typically 21105.</param>
    /// <returns>A task that represents the asynchronous connect operation, returning true if successful.</returns>
    Task<bool> ConnectAsync(string host, int port = 21105);

    /// <summary>
    /// Disconnects from the Z21 command station, sending a logoff command.
    /// </summary>
    /// <returns>A task that represents the asynchronous disconnect operation.</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a request to get the current broadcast flags from the Z21.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetBroadcastFlagsAsync();

    /// <summary>
    /// Sends a request to get the firmware version from the Z21.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetFirmwareVersionAsync();

    /// <summary>
    /// Sends a request to get hardware information from the Z21.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetHardwareInfoAsync();

    /// <summary>
    /// Sends a request to get locomotive information for a specific address.
    /// </summary>
    /// <param name="address">The locomotive address to query.</param>
    /// <returns></returns>
    Task GetLocoInfoAsync(ushort address);

    /// <summary>
    /// Sends a request to get the protocol mode (DCC or MM) for a specific locomotive address.
    /// </summary>
    /// <param name="address">The locomotive address to query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetLocoModeAsync(ushort address);

    Task GetLocoSlotInfoAsync(byte slotNumber);

    /// <summary>
    /// Sends a request to get RailCom data for a specific locomotive address.
    /// </summary>
    /// <param name="locoAddress">The locomotive address to query. Use 0 to poll the next loco in the Z21's circular buffer.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetRailComDataAsync(ushort locoAddress);

    /// <summary>
    /// Sends a request to get the data for a specific group of R-Bus feedback modules.
    /// </summary>
    /// <param name="groupIndex">The group index to query (0 for modules 1-10, 1 for 11-20, etc.).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetRBusDataAsync(int groupIndex);

    /// <summary>
    /// Sends a request to get the serial number from the Z21.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetSerialNumberAsync();

    /// <summary>
    /// Sends a request to get the current system state from the Z21.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetSystemStateAsync();

    /// <summary>
    /// Sends a request to get the protocol mode (DCC or MM) for a specific turnout address.
    /// </summary>
    /// <param name="address">The turnout address to query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetTurnoutModeAsync(ushort address);

    /// <summary>
    /// Sends a request to get the feature scope code from the Z21.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GetZ21CodeAsync();

    /// <summary>
    /// Set a finction for a specific locomotive.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="functionIndex"></param>
    /// <returns></returns>
    Task SetLocoFunctionAsync(ushort address, byte functionIndex);

    /// <summary>
    /// Sends a command to set the drive parameters for a specific locomotive.
    /// </summary>
    /// <param name="address">The locomotive address to control.</param>
    /// <param name="speed">The desired speed in (0-126).</param>
    /// <param name="nativeSpeedSteps">The native speed steps used by z21/Z21.</param>
    /// <param name="direction">The driving direction.</param>
    /// <param name="locoMode">The protocol mode (DCC or MM).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetLocoDriveAdaptSpeedAsync(ushort address, byte speed, NativeSpeedSteps nativeSpeedStep, DrivingDirection direction, LocoMode locoMode);

    /// <summary>
    /// Sends a command to set the protocol mode (DCC or MM) for a specific locomotive address.
    /// </summary>
    /// <param name="address">The locomotive address to configure.</param>
    /// <param name="mode">The protocol mode to set.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetLocoModeAsync(ushort address, LocoMode mode);

    /// <summary>
    /// Sends an emergency stop command to the Z21.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetEmergencyStopAsync();

    /// <summary>
    /// Sends a command to set the protocol mode (DCC or MM) for a specific turnout address.
    /// </summary>
    /// <param name="address">The turnout address to configure.</param>
    /// <param name="mode">The protocol mode to set.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetTurnoutModeAsync(ushort address, TurnoutMode mode);

    /// <summary>
    /// Set the position for a specific turnout.
    /// </summary>
    /// <param name="address">The turnout address to set.</param>
    /// <param name="position">The position to set turnout to.</param>
    /// <returns></returns>
    Task SetTurnoutPositionAsync(ushort address, TurnoutPosition position);

    /// <summary>
    /// Sends a command to turn on track power.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetTrackPowerOnAsync();

    /// <summary>
    /// Sends a command to turn off track power.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetTrackPowerOffAsync();
}