using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Z21Client.Helpers;
using Z21Client.Infrastructure;
using Z21Client.Interfaces;
using Z21Client.Models;
using Z21Client.Resources.Localization;

namespace Z21Client;

/// <summary>
/// Implements the <see cref="IZ21Client"/> interface to provide a client for communicating
/// with a Roco Z21/z21 model railway command station over a local network.
/// </summary>
/// <remarks>
/// This class handles the low-level UDP communication, message parsing, and event invocation
/// based on the official Z21 LAN Protocol Specification.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="Z21Client"/> class.
/// </remarks>
/// <param name="logger">An ILogger instance for logging.</param>
public sealed class Z21Client(ILogger<Z21Client> logger) : IZ21Client
{
    // --- START: Added for Firmware Bug Workaround ---
    // This dictionary acts as a temporary holding area for loco info requests.
    // It's used to solve a race condition caused by a firmware bug where a LAN_X_GET_LOCO_INFO 
    // response doesn't correctly set protocol flags, requiring an immediate LAN_GET_LOCOMODE call.
    // We store the partial LocoInfo here and wait for the LocoMode response to complete it
    // before raising the final, correct LocoInfoReceived event.
    private readonly Dictionary<ushort, LocoInfo?> _pendingLocoInfoRequests = [];
    // --- END: Added for Firmware Bug Workaround ---

    private UdpClient? _udpClient;
    private IPEndPoint? _remoteEndPoint;
    private Task? _receiveTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private Timer? _keepAliveTimer;
    private Timer? _watchdogTimer;
    private DateTime _lastCommandSentTimestamp;
    private DateTime _lastMessageReceivedTimestamp;
    private int _failedPingCount;
    private HardwareInfo? _hardwareInfo;
    private BroadcastFlags _subscripedBroadcastFlags = BroadcastFlags.None;
    private EventHandler<LocoInfo>? _locoInfoReceived;
    private EventHandler<RBusData>? _rBusDataReceived;
    private EventHandler<RailComData>? _railComDataReceived;
    private EventHandler<SystemStateChangedEventArgs>? _systemStateChanged;
    private readonly SemaphoreSlim _sendToZ21Lock = new(1, 1);
    private bool _isz21 = false;

    private Timer? _railComPollingTimer;
    private readonly HashSet<ushort> _receivedRailComAddresses = [];

    #region Exposed events

    /// <inheritdoc/>
    public event EventHandler<BroadcastFlagsChangedEventArgs>? BroadcastFlagsReceived;

    /// <inheritdoc/>
    public event EventHandler? EmergencyStopReceived;

    /// <inheritdoc/>
    public event EventHandler<FirmwareVersion>? FirmwareVersionReceived;

    /// <inheritdoc/>
    public event EventHandler<HardwareInfo>? HardwareInfoReceived;

    /// <inheritdoc/>
    public event EventHandler<LocoInfo>? LocoInfoReceived
    {
        add
        {
            if (_locoInfoReceived is null && _hardwareInfo?.FwVersion.Version >= Z21FirmwareVersions.V1_20)
            {
                // "Subscribing to AllLocoInfoReceived event. Adding AllLocoInfo broadcast flag."
                logger.LogInformation(Messages.Text0001);
                _subscripedBroadcastFlags |= BroadcastFlags.AllLocoInfo;
                _ = SetBroadcastFlags(_subscripedBroadcastFlags);
            }
            _locoInfoReceived += value;
        }
        remove
        {
            _locoInfoReceived -= value;
            if (_locoInfoReceived is null && _hardwareInfo?.FwVersion.Version >= Z21FirmwareVersions.V1_20)
            {
                // "Unsubscribing from AllLocoInfoReceived event. Removing AllLocoInfo broadcast flag."
                logger.LogInformation(Messages.Text0002);
                _subscripedBroadcastFlags &= ~BroadcastFlags.AllLocoInfo;
                _ = SetBroadcastFlags(_subscripedBroadcastFlags);
            }
        }
    }

    public event EventHandler<LocoSlotInfo>? LocoSlotInfoReceived;

    /// <inheritdoc/>
    public event EventHandler<LocoModeChangedEventArgs>? LocoModeReceived;

    /// <inheritdoc/>
    public event EventHandler<RailComData>? RailComDataReceived
    {
        add
        {
            if (_railComDataReceived is null)
            {
                // "Subscribing to RailComDataReceived event. Adding AllRailCom broadcast flag and starting polling."
                logger.LogInformation(Messages.Text0003);
                _subscripedBroadcastFlags |= BroadcastFlags.AllRailCom;
                _ = SetBroadcastFlags(_subscripedBroadcastFlags);

                // Start the timer to poll for RailCom data every second
                _railComPollingTimer = new Timer(RailComPollingCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            }
            _railComDataReceived += value;
        }
        remove
        {
            _railComDataReceived -= value;
            if (_railComDataReceived is null)
            {
                // "Unsubscribing from RailComDataReceived event. Removing AllRailCom broadcast flag and stopping polling."
                logger.LogInformation(Messages.Text0004);

                // Stop and dispose the timer
                _railComPollingTimer?.Dispose();
                _railComPollingTimer = null;

                _subscripedBroadcastFlags &= ~BroadcastFlags.AllRailCom;
                _ = SetBroadcastFlags(_subscripedBroadcastFlags);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<RBusData>? RBusDataReceived
    {
        add
        {
            if (_rBusDataReceived is null)
            {
                // "Subscribing to RBusDataReceived event. Adding RBus broadcast flag."
                logger.LogInformation(Messages.Text0005);
                _subscripedBroadcastFlags |= BroadcastFlags.RBus;
                _ = SetBroadcastFlags(_subscripedBroadcastFlags);
            }
            _rBusDataReceived += value;
        }
        remove
        {
            _rBusDataReceived -= value;
            if (_rBusDataReceived is null)
            {
                // "Unsubscribing from RBusDataReceived event. Removing RBus broadcast flag."
                logger.LogInformation(Messages.Text0006);
                _subscripedBroadcastFlags &= ~BroadcastFlags.RBus;
                _ = SetBroadcastFlags(_subscripedBroadcastFlags);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<SerialNumber>? SerialNumberReceived;

    /// <inheritdoc/>
    public event EventHandler<SystemStateChangedEventArgs>? SystemStateChanged
    {
        add
        {
            if (_systemStateChanged is null)
            {
                // "Subscribing to SystemStateChanged event. Adding SystemState broadcast flag."
                logger.LogInformation(Messages.Text0007);
                _subscripedBroadcastFlags |= BroadcastFlags.SystemState;
                _ = SetBroadcastFlags(_subscripedBroadcastFlags);
            }
            _systemStateChanged += value;
        }
        remove
        {
            _systemStateChanged -= value;
            if (_systemStateChanged is null)
            {
                // "Unsubscribing from SystemStateChanged event. Removing SystemState broadcast flag."
                logger.LogInformation(Messages.Text0008);
                _subscripedBroadcastFlags &= ~BroadcastFlags.SystemState;
                _ = SetBroadcastFlags(_subscripedBroadcastFlags);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<TurnoutInfo>? TurnoutInfoReceived;

    /// <inheritdoc/>
    public event EventHandler<TrackPowerInfo>? TrackPowerInfoReceived;

    /// <inheritdoc/>
    public event EventHandler<TurnoutModeChangedEventArgs>? TurnoutModeReceived;

    /// <inheritdoc/>
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <inheritdoc/>
    public event EventHandler<Z21Code>? Z21CodeReceived;

    #endregion Exposed events

    #region Exposed methods

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(string host, int port = 21105)
    {
        // "Connecting to Z21 at {Host}:{Port}..."
        logger.LogInformation(Messages.Text0009, host, port);
        if (_udpClient is not null)
        {
            // "Already connected. Please disconnect first."
            logger.LogWarning(Messages.Text0010);
            return true;
        }

        if (!await PingHostAsync(host))
        {
            // "Connection failed: Host {Host} is not reachable (ping failed)."
            logger.LogError(Messages.Text0011, host);
            return false;
        }

        try
        {
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
        }
        catch (Exception ex)
        {
            // "Invalid IP address provided: {Host}"
            logger.LogError(ex, Messages.Text0012, host);
            return false;
        }

        try
        {
            _udpClient = new UdpClient(port);
            if (OperatingSystem.IsWindows())
            {
                _udpClient.AllowNatTraversal(true);
            }
            // "UdpClient created and bound to listen on local port {Port}"
            logger.LogInformation(Messages.Text0013, port);
        }
        catch (Exception ex)
        {
            // "Failed to create and bind UdpClient on port {Port}. The port may already be in use by another application."
            logger.LogError(ex, Messages.Text0014, port);
            return false;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        var handshakeComplete = new TaskCompletionSource<bool>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        void handshakeHandler(object? s, HardwareInfo e)
        {
            handshakeComplete.TrySetResult(true);
            _hardwareInfo = e;
            _isz21 = _hardwareInfo?.HwType is HardwareType.z21Small or HardwareType.z21Start;
        }
        HardwareInfoReceived += handshakeHandler;

        await GetHardwareInfoAsync();

        using (timeoutCts.Token.Register(() => handshakeComplete.TrySetResult(false)))
        {
            var result = await handshakeComplete.Task;
            HardwareInfoReceived -= handshakeHandler;

            if (!result)
            {
                // "Connection failed: Host responded to ping, but did not respond to Z21 command (handshake failed)."
                logger.LogError(Messages.Text0015);
                await DisconnectAsync();
                return false;
            }
        }

        _lastCommandSentTimestamp = DateTime.UtcNow;
        _lastMessageReceivedTimestamp = DateTime.UtcNow;
        _failedPingCount = 0;

        _subscripedBroadcastFlags = BroadcastFlags.Basic | BroadcastFlags.SystemState;
        await SetBroadcastFlags(_subscripedBroadcastFlags);

        _keepAliveTimer = new Timer(KeepAliveCallback, null, TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(45));
        _watchdogTimer = new Timer(WatchdogCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        // "Z21 client connection verified and fully started."
        logger.LogInformation(Messages.Text0016);
        return true;
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        // "Disconnecting from Z21..."
        logger.LogInformation(Messages.Text0017);

        if (_keepAliveTimer is not null)
        {
            await _keepAliveTimer.DisposeAsync();
            _keepAliveTimer = null;
        }

        if (_watchdogTimer is not null)
        {
            await _watchdogTimer.DisposeAsync();
            _watchdogTimer = null;
        }

        if (_railComPollingTimer is not null)
        {
            await _railComPollingTimer.DisposeAsync();
            _railComPollingTimer = null;
        }

        if (_udpClient is not null)
        {
            await SendCommandAsync(Z21Commands.Logoff);
        }

        if (_cancellationTokenSource is not null)
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            if (_receiveTask is not null)
            {
                try
                {
                    await _receiveTask.WaitAsync(TimeSpan.FromSeconds(1));
                }
                catch (OperationCanceledException)
                {
                    // "Receive task was successfully cancelled as expected during disconnect."
                    logger.LogDebug(Messages.Text0018);
                }
                catch (TimeoutException)
                {
                    // "Receive task did not cancel within the expected time during disconnect."
                    logger.LogWarning(Messages.Text0019);
                }
            }
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        if (_udpClient is not null)
        {
            _udpClient.Close();
            _udpClient.Dispose();
            _udpClient = null;
        }
        _hardwareInfo = null;
        // "Disconnected."
        logger.LogInformation(Messages.Text0020);
    }

    /// <inheritdoc/>
    public async Task GetBroadcastFlagsAsync()
    {
        await SendCommandAsync(Z21Commands.GetBroadcastFlags);
        // "GetBroadcastFlagsAsync: Requested the broadcast flags"
        logger.LogInformation(Messages.Text0085);
    }

    /// <inheritdoc/>
    public async Task GetFirmwareVersionAsync()
    {
        await SendCommandAsync(Z21Commands.GetFirmwareVersion);
        // "GetFirmwareVersionAsync: Requested the firmware version"
        logger.LogInformation(Messages.Text0086);
    }

    /// <inheritdoc/>
    public async Task GetHardwareInfoAsync()
    {
        await SendCommandAsync(Z21Commands.GetHardwareInfo);
        // "GetHardwareInfoAsync: Requested the hardware information"
        logger.LogInformation(Messages.Text0087);
    }

    /// <inheritdoc/>
    public async Task GetLocoInfoAsync(ushort address)
    {
        // --- START: Firmware Bug Workaround ---
        // Register this address as a pending request. This signals to the parsing methods
        // that we are actively waiting for a combined LocoInfo and LocoMode response
        // due to a firmware bug where LAN_X_GET_LOCO_INFO doesn't provide complete protocol data.
        _pendingLocoInfoRequests[address] = null;
        // --- END: Firmware Bug Workaround ---

        var command = new byte[Z21ProtocolConstants.LengthGetLocoInfo];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthGetLocoInfo).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.XHeader).CopyTo(command, 2);
        command[4] = Z21ProtocolConstants.XHeaderGetLocoInfo;
        command[5] = 0xF0;
        command[6] = (byte)(address >> 8);
        command[7] = (byte)(address & 0xFF);
        command[8] = CalculateChecksum(command);
        await SendCommandAsync(command);
        // "GetLocoInfoAsync: Requested loco info for address {Address}"
        logger.LogInformation(Messages.Text0021, address);

        // --- START: Firmware Bug Workaround ---
        // Immediately request the loco mode as well to get the correct protocol information.
        await GetLocoModeAsync(address);
        // --- END: Firmware Bug Workaround ---
    }

    /// <inheritdoc/>
    public async Task GetLocoModeAsync(ushort address)
    {
        var command = new byte[Z21ProtocolConstants.LengthGetLocoMode];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthGetLocoMode).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetLocoMode).CopyTo(command, 2);
        command[4] = (byte)(address >> 8);
        command[5] = (byte)(address & 0xFF);
        await SendCommandAsync(command);
        // "GetLocoModeAsync: Requested loco mode for address {Address}"
        logger.LogInformation(Messages.Text0022, address);
    }

    /// <inheritdoc/>
    public async Task GetLocoSlotInfoAsync(byte slotNumber)
    {
        var command = new byte[Z21ProtocolConstants.LengthGetLocoSlotInfo];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthGetLocoSlotInfo).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetLocoSlotInfo).CopyTo(command, 2);
        command[5] = slotNumber;
        await SendCommandAsync(command);
        // "GetLocoSlotInfoAsync: Requested loco info for slot {slotNumber}"
        logger.LogInformation(Messages.Text0084, slotNumber);
    }

    /// <inheritdoc/>
    public async Task GetRailComDataAsync(ushort locoAddress)
    {
        var command = new byte[Z21ProtocolConstants.LengthGetRailComData];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthGetRailComData).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetRailComData).CopyTo(command, 2);
        command[4] = 0x01;
        command[5] = (byte)(locoAddress & 0xFF);
        command[6] = (byte)(locoAddress >> 8);
        await SendCommandAsync(command);
        // "GetRailComDataAsync: Requested RailCom info for loco {locoAddress}"
        logger.LogInformation(Messages.Text0023, locoAddress);
    }


    /// <inheritdoc/>
    public async Task GetRBusDataAsync(int groupIndex)
    {
        var command = new byte[Z21ProtocolConstants.LengthGetRBusData];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthGetRBusData).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetRBusData).CopyTo(command, 2);
        command[4] = (byte)groupIndex;
        await SendCommandAsync(command);
        // "GetRBusDataAsync: Requested R-Bus data for group index {groupIndex}"
        logger.LogInformation(Messages.Text0024, groupIndex);
    }

    /// <inheritdoc/>
    public async Task GetSerialNumberAsync()
    {
        await SendCommandAsync(Z21Commands.GetSerialNumber);
        // "GetSerialtNumberAsync: Requested z21/Z21 serial number"
        logger.LogInformation(Messages.Text0025);
    }

    /// <inheritdoc/>
    public async Task GetSystemStateAsync()
    {
        await SendCommandAsync(Z21Commands.GetSystemState);
        // "GetSystemStateAsync: Requested system state"
        logger.LogInformation(Messages.Text0026);
    }

    /// <inheritdoc/>
    public async Task GetTurnoutModeAsync(ushort address)
    {
        var command = new byte[Z21ProtocolConstants.LengthGetTurnoutMode];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthGetTurnoutMode).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetTurnoutMode).CopyTo(command, 2);
        command[4] = (byte)(address >> 8);
        command[5] = (byte)(address & 0xFF);
        await SendCommandAsync(command);
        // "GetTurnoutModeAsync: Requested turnout mode for address {address}"
        logger.LogInformation(Messages.Text0027, address);
    }

    /// <inheritdoc/>
    public async Task GetZ21CodeAsync()
    {
        await SendCommandAsync(Z21Commands.GetCode);
        // "GetZ21CodeAsync: Requested z21/Z21 (feature) code"
        logger.LogInformation(Messages.Text0028);
    }

    /// <inheritdoc/>
    public async Task SetLocoDriveAsync(ushort address, byte speed, NativeSpeedSteps nativeSpeedStep, DrivingDirection direction, LocoMode locoMode)
    {
        const byte FixedValue = 0x10;
        byte rocoSpeedStep = GetRocoSpeedStep(speed, locoMode, nativeSpeedStep);

        // Create the 10-byte command array
        var command = new byte[Z21ProtocolConstants.LengthSetLocoDrive];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthSetLocoDrive).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.XHeader).CopyTo(command, 2);
        command[4] = Z21ProtocolConstants.XHeaderSetLocoDrive;
        // DB0: 0x1S (Speed steps)
        command[5] = (byte)(FixedValue | (byte)nativeSpeedStep);
        // DB1, DB2: Address
        byte adrMsb = (byte)(address >> 8);
        byte adrLsb = (byte)(address & 0xFF);
        // Set the high bits for X-Bus addressing
        command[6] = adrMsb;
        if (address >= 128)
        {
            command[6] |= 0xC0;
        }
        command[7] = adrLsb;

        // DB3: RVVVVVVV (Direction and Speed)
        // Ensure speed is within 7 bits (0-127)

        byte speedValue = (byte)(rocoSpeedStep & 0x7F);
        // Set the direction bit (bit 7)
        byte directionBit = (byte)((int)direction << 7);
        command[8] = (byte)(directionBit | speedValue);

        // DB4: Checksum
        command[9] = CalculateChecksum(command);

        // Send the command
        await SendCommandAsync(command);
        // "Set loco drive for address {Address}: Speed={Speed}, rocoValue={rocoValue} NativeSteps={NativeSpeedSteps}, Direction={Direction}"
        logger.LogInformation(Messages.Text0029, address, speed, rocoSpeedStep, nativeSpeedStep, direction);
    }

    /// <inheritdoc/>
    public async Task SetLocoFunctionAsync(ushort address, byte functionIndex)
    {
        var command = new byte[Z21ProtocolConstants.LengthSetLocoFunction];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthSetLocoFunction).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderXBus).CopyTo(command, 2);
        BitConverter.GetBytes(Z21ProtocolConstants.XHeaderSetLocoFunction).CopyTo(command, 4);
        byte adrMsb = (byte)(address >> 8);
        byte adrLsb = (byte)(address & 0xFF);
        // Set the high bits for X-Bus addressing
        command[6] = adrMsb;
        if (address >= 128)
        {
            command[6] |= 0xC0;
        }
        command[7] = adrLsb;

        command[8] = (byte)(0x80 | (functionIndex & 0b00111111)); // 0x80= Toggle function
        command[9] = CalculateChecksum(command);
        await SendCommandAsync(command);
        // "Toggle function {functionIndex} for loco address {address}"
        logger.LogInformation(Messages.Text0030, functionIndex, address);
    }

    /// <inheritdoc/>
    public async Task SetLocoModeAsync(ushort address, LocoMode mode)
    {
        var command = new byte[Z21ProtocolConstants.LengthSetLocoMode];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthSetLocoMode).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderSetLocoMode).CopyTo(command, 2);
        command[4] = (byte)(address >> 8);
        command[5] = (byte)(address & 0xFF);
        command[6] = (byte)mode;
        await SendCommandAsync(command);
        // "Setting loco mode for address {Address} to {Mode}"
        logger.LogInformation(Messages.Text0031, address, mode);
    }

    /// <inheritdoc/>
    public async Task SetEmergencyStopAsync()
    {
        await SendCommandAsync(Z21Commands.SetEmergencyStop);
        // "Sending Emergency Stop command."
        logger.LogWarning(Messages.Text0032);
    }

    /// <inheritdoc/>
    public async Task SetTurnoutModeAsync(ushort address, TurnoutMode mode)
    {
        var command = new byte[Z21ProtocolConstants.LengthSetTurnoutMode];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthSetTurnoutMode).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderSetTurnoutMode).CopyTo(command, 2);
        command[4] = (byte)(address >> 8);
        command[5] = (byte)(address & 0xFF);
        command[6] = (byte)mode;
        await SendCommandAsync(command);
        // "Setting turnout mode for address {Address} to {Mode}"
        logger.LogInformation(Messages.Text0033, address, mode);
    }

    /// <inheritdoc/>
    public async Task SetTurnoutPositionAsync(ushort address, TurnoutPosition position)
    {
        const byte FixedValue = 0x80;
        const byte TurnOnBit = 0x08;
        var turnoutPosition = (byte)FixedValue | ((byte)position & 1);

        // Turn on:
        turnoutPosition |= TurnOnBit;

        var command = new byte[Z21ProtocolConstants.LengthSetTurnoutPosition];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthSetTurnoutPosition).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.XHeader).CopyTo(command, 2);
        command[4] = Z21ProtocolConstants.XHeaderSetTurnoutPosition;
        command[5] = (byte)(address >> 8);
        command[6] = (byte)(address & 0xFF);
        command[7] = (byte)turnoutPosition;
        command[8] = CalculateChecksum(command);
        await SendCommandAsync(command);

        await Task.Delay(100);
        // Turn off:
        turnoutPosition &= ~TurnOnBit;
        command[7] = (byte)turnoutPosition;
        command[8] = CalculateChecksum(command);
        await SendCommandAsync(command);

        await Task.Delay(50);

        // "Setting turnout position address {address}, position {position}"
        logger.LogInformation(Messages.Text0034, address, position);
    }

    /// <inheritdoc/>
    public async Task SetTrackPowerOffAsync()
    {
        await SendCommandAsync(Z21Commands.SetTrackPowerOff);
        // "Setting track power off"
        logger.LogInformation(Messages.Text0035);
    }

    /// <inheritdoc/>
    public async Task SetTrackPowerOnAsync()
    {
        await SendCommandAsync(Z21Commands.SetTrackPowerOn);
        // "Setting track power on"
        logger.LogInformation(Messages.Text0036);
    }

    #endregion Exposed methods

    /// <summary>
    /// Converts the given speed step to the Roco-specific speed step value based on the loco mode and native speed steps.
    /// 
    /// The speed must be in internal interval based on the protocol speedstep.
    /// MM1 14 steps: 0-14
    /// MM2 14 steps: 0-14
    /// MM2 28 steps: 0-28
    /// DCC 14 steps : 0-14
    /// DCC 28 steps: 0-28
    /// DCC 128 steps: 0-126
    /// </summary>
    /// <param name="speed">The speed in internal interval based on the nativeSpeedStep.</param>
    /// <param name="locoMode"></param>
    /// <param name="nativeSpeedStep"></param>
    /// <returns></returns>
    private static byte GetRocoSpeedStep(byte speed, LocoMode locoMode, NativeSpeedSteps nativeSpeedStep)
    {
        byte convertedSpeedStep;

        if (locoMode is LocoMode.MM)
        {
            switch (nativeSpeedStep)
            {
                // The speed entered is in the MM steps, and must be converted to DCC steps for the command
                case NativeSpeedSteps.Steps14:
                    convertedSpeedStep = speed;
                    if (convertedSpeedStep > 14)
                    {
                        convertedSpeedStep = 14;
                    }
                    break;

                case NativeSpeedSteps.Steps28:
                    convertedSpeedStep = (byte)(speed * 2);
                    if (convertedSpeedStep > 28)
                    {
                        convertedSpeedStep = 28;
                    }
                    break;
                default:
                    convertedSpeedStep = (byte)(Math.Ceiling(speed * (decimal)4.6));
                    if (convertedSpeedStep > 126)
                    {
                        convertedSpeedStep = 126;
                    }
                    break;
            }
        }
        else // DCC
        {
            convertedSpeedStep = speed;
        }
        // Fetch the Roco value for the speed step
        var rocoSpeedStep = DccSpeedSteps.GetSpeedStepReverse(convertedSpeedStep, (SpeedSteps)nativeSpeedStep);

        // Message for deug at development time
        // logger.LogError("GetRocoSteedStep: Address {Address}: Speed={Speed}, rocoSpeedStep={rocoSpeedStep}, NativeSteps={NativeSpeedSteps}, Direction={Direction}, Mode={LocoMode}", address, speed, rocoSpeedStep, nativeSpeedStep, direction, locoMode);

        return rocoSpeedStep;
    }

    /// <summary>
    /// Sends an ICMP echo message to the Z21 and determines if it is reachable.
    /// </summary>
    /// <param name="host">The DNS name or IP address of the host to ping. Cannot be null or empty.</param>
    private async Task<bool> PingHostAsync(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 2000);
            return reply.Status == IPStatus.Success;
        }
        catch (Exception ex)
        {
            // "An exception occurred during ping to {Host}."
            logger.LogError(ex, Messages.Text0037, host);
            return false;
        }
    }

    private void KeepAliveCallback(object? state)
    {
        if (DateTime.UtcNow - _lastCommandSentTimestamp > TimeSpan.FromSeconds(40))
        {
            // "Sending keep-alive message to Z21."
            logger.LogInformation(Messages.Text0038);
            _ = GetSystemStateAsync();
        }
    }

    private void RailComPollingCallback(object? state)
    {
        _receivedRailComAddresses.Clear();
        _ = GetNextRailComDataAsync();
    }

    private async Task GetNextRailComDataAsync()
    {
        await SendCommandAsync(Z21Commands.GetRailComDataNext);
        // "Requesting RailCom data from next locomotive in ring buffer"
        logger.LogInformation(Messages.Text0039);
    }

    private async void WatchdogCallback(object? state)
    {
        if (DateTime.UtcNow - _lastMessageReceivedTimestamp < TimeSpan.FromSeconds(15))
        {
            return;
        }

        if (_failedPingCount >= 3)
        {
            // "Connection to Z21 lost. No response to multiple pings."
            logger.LogError(Messages.Text0040);
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Lost));
            await DisconnectAsync();
            return;
        }

        try
        {
            // "No message received from Z21 for a while. Pinging to check connection..."
            logger.LogWarning(Messages.Text0041);
            if (await PingHostAsync(_remoteEndPoint!.Address.ToString()))
            {
                // "Ping successful, Z21 is still on the network. Waiting for data."
                logger.LogInformation(Messages.Text0042);
            }
            else
            {
                _failedPingCount++;
                // "Ping failed. Failure count: {Count}"
                logger.LogWarning(Messages.Text0043, _failedPingCount);
            }
        }
        catch (Exception ex)
        {
            _failedPingCount++;
            // "An exception occurred during watchdog ping. Failure count: {Count}"
            logger.LogError(ex, Messages.Text0044, _failedPingCount);
        }
    }

    private async Task SendCommandAsync(byte[] command)
    {

        if (_udpClient is null || _remoteEndPoint is null)
        {
            // "Cannot send command. Client is not connected."
            logger.LogWarning(Messages.Text0045);
            return;
        }

        // As multiple threads may try to send commands simultaneously, we use a semaphore to ensure that we
        // only send one command at the time.
        await _sendToZ21Lock.WaitAsync();
        try
        {
            _ = await _udpClient.SendAsync(command, command.Length, _remoteEndPoint);
            _lastCommandSentTimestamp = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            // "Failed to send command to central station. Exception: {message}"
            logger.LogError(ex, Messages.Text0046, ex.Message);
        }
        finally
        {
            _sendToZ21Lock.Release();
        }
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_udpClient is null) break;

                UdpReceiveResult result = await _udpClient.ReceiveAsync(cancellationToken);

                if (_remoteEndPoint is not null && result.RemoteEndPoint.Address.Equals(_remoteEndPoint.Address))
                {
                    ProcessReceivedData(result.Buffer);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // "An error occurred in the receive loop. Exception: {message}"
                logger.LogError(ex, Messages.Text0047, ex.Message);
            }
        }
    }

    /// <summary>
    /// Processes the data received from the Z21, handling multiple concatenated messages.
    /// </summary>
    /// <param name="buffer">The recived data.</param>
    private void ProcessReceivedData(byte[] buffer)
    {
        _lastMessageReceivedTimestamp = DateTime.UtcNow;
        _failedPingCount = 0;

        var span = buffer.AsSpan();
        int offset = 0;

        while (offset < span.Length)
        {
            if (offset + 2 > span.Length)
            {
                // "Received malformed data: not enough bytes to read message length. Remaining bytes: {Length}"
                logger.LogWarning(Messages.Text0048, span.Length - offset);
                break;
            }

            ushort messageLength = BitConverter.ToUInt16(span[offset..]);

            if (messageLength == 0)
            {
                // "Encountered zero-length message in data stream. Stopping parse of this packet."
                logger.LogWarning(Messages.Text0049);
                break;
            }

            if (offset + messageLength > span.Length)
            {
                // "Received malformed data: buffer is smaller than the indicated message length. Expected: {Expected}, Actual remaining: {Actual}"
                logger.LogWarning(Messages.Text0050, messageLength, span.Length - offset);
                break;
            }

            var messageSpan = span.Slice(offset, messageLength);
            ProcessSingleMessage(messageSpan);

            offset += messageLength;
        }
    }

    /// <summary>
    /// Processes a single Z21 message based on its header.
    /// </summary>
    /// <param name="data"></param>
    private void ProcessSingleMessage(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4) return;

        ushort header = BitConverter.ToUInt16(data[2..]);

        switch (header)
        {
            case Z21ProtocolConstants.HeaderGetLocoSlotInfo:
                ParseLocoSlotInfo(data);
                break;
            case Z21ProtocolConstants.HeaderGetSerialNumber when data.Length >= 8:
                ParseSerialNumber(data);
                break;
            case Z21ProtocolConstants.HeaderGetCode when data.Length >= 5:
                ParseZ21Code(data);
                break;
            case Z21ProtocolConstants.HeaderGetHardwareInfo when data.Length >= 12:
                ParseHardwareInfo(data);
                break;
            case Z21ProtocolConstants.HeaderSystemStateResponse:
                ParseSystemState(data);
                break;
            case Z21ProtocolConstants.HeaderGetBroadcastFlags when data.Length >= 8:
                ParseBroadcastFlags(data);
                break;
            case Z21ProtocolConstants.HeaderGetLocoMode when data.Length >= 7:
                ParseLocoMode(data);
                break;
            case Z21ProtocolConstants.HeaderGetTurnoutMode when data.Length >= 7:
                ParseTurnoutMode(data);
                break;
            case Z21ProtocolConstants.HeaderRBusDataChanged when data.Length >= 15:
                ParseRBusData(data);
                break;
            case Z21ProtocolConstants.HeaderRailComDataChanged:
                ParseRailComData(data);
                break;
            case Z21ProtocolConstants.HeaderXBus when data.Length >= 7:
                ParseXBus(data);
                break;
            default:
                // "Received an unhandled or malformed packet. Header: 0x{Header:X4}, Length: {Length}"
                logger.LogWarning(Messages.Text0051, header, data.Length);
                break;
        }
    }

    /// <summary>
    /// Receives information about a locomotive slot. This event, and the command GetLocoSlotInfo, is not documented in the official documentation.
    /// </summary>
    /// <param name="data"></param>
    private void ParseLocoSlotInfo(ReadOnlySpan<byte> data)
    {
        if (LocoSlotInfoReceived is null) return;
        if (data.Length < 24)
        {
            // "Received Loco Slot Info packet is too short. Expected at least 24 bytes, got {Length}"
            logger.LogWarning(Messages.Text0052, data.Length);
            return;
        }

        ushort address = BitConverter.ToUInt16(data[9..]);
        if (address is 0)
        {
            // This indicates an empty slot, no need to process further.
            // Please note, that the data bytes *can* be filled with old data.
            return;
        }

        // Data received about the locomotive slot, is in another format/structure than from the documented events.
        // Because of this, the data must be converted.

        // No matter the speed steps reported the speed is a value between 0 and 127. In the documented LAN_X_LOCO_INFO
        // the speed steps returned, are between 0 and 14, 28 and 128 depending on the speed steps used.
        byte slotNumber = data[7];
        var rawSpeed = (byte)(data[12] & 0b01111111);
        byte speedSteps = 0;
        byte speed = 0;
        switch (data[18])
        {
            case 3:
                speedSteps = 0;
                speed = (byte)Math.Ceiling(rawSpeed / (double)8.2);
                speed = DccSpeedSteps.GetSpeedStepReverse(speed, SpeedSteps.Steps14);
                break;
            case 6:
                speedSteps = 2;
                speed = (byte)Math.Ceiling(rawSpeed / (double)4.6);
                speed = DccSpeedSteps.GetSpeedStepReverse(speed, SpeedSteps.Steps28);
                break;
            case 9:
                speedSteps = 4;
                speed = rawSpeed;
                break;
            case 67:
                speedSteps = 0;
                speedSteps |= 0x10;
                speed = (byte)(rawSpeed / (double)8.2);
                break;
            case 83:
                speedSteps = 2;
                speedSteps |= 0x10;
                speed = (byte)Math.Ceiling(rawSpeed / (double)4.1);
                break;
            case 117:
                speedSteps = 4;
                speedSteps |= 0x10;
                speed = rawSpeed;
                break;
        }

        // Set the direction bit 
        if ((data[14] & 0x20) == 0)
        {
            speed |= 0b10000000;
        }

        var locoSlotInfo = new LocoSlotInfo(slotNumber, new LocoInfo(
            address, speedSteps, speed, data[14], data[15], data[16], data[17], null, _hardwareInfo?.FwVersion
            ));

        LocoSlotInfoReceived.Invoke(this, locoSlotInfo);
        // "Loco Slot Info for slot {SlotNumber} received: Address={Address}"
        logger.LogInformation(Messages.Text0053, slotNumber, address);
    }

    /// <summary>
    /// Handles parsing of X-Bus messages. These messages contain various types of information identified by the X-Header.
    /// </summary>
    /// <param name="data">Data recived.</param>
    private void ParseXBus(ReadOnlySpan<byte> data)
    {
        byte xHeader = data[4];
        ushort xHeaderDb0 = BitConverter.ToUInt16(data[5..]);
        if (xHeaderDb0 is Z21ProtocolConstants.XHeaderUnknownCommand)
        {
            // "Send an unknown X-Bus command to Z21"
            logger.LogError(Messages.Text0054);
            return;
        }

        switch (xHeader)
        {
            case Z21ProtocolConstants.XHeaderEmergencyStop:
                ParseEmergencyStop(data);
                break;
            case Z21ProtocolConstants.XHeaderFirmwareVersion when data.Length >= 9:
                ParseFirmwareVersion(data);
                break;
            case Z21ProtocolConstants.XHeaderLocoInfo:
                ParseLocoInfo(data);
                break;
            case Z21ProtocolConstants.XHeaderTurnoutInfo:
                ParseTurnoutInfo(data);
                break;
            case Z21ProtocolConstants.XHeaderTrackPower:
                ParseTrackPowerState(data);
                break;
            default:
                // "Received an unhandled X-Bus command with X-Header: 0x{XHeader:X2}"
                logger.LogError(Messages.Text0055, xHeader);
                break;
        }
    }

    private void ParseRailComData(ReadOnlySpan<byte> data)
    {
        if (_railComDataReceived is null || data.Length < 17) return;
        var railComData = new RailComData(data[4..]);
        if (_railComPollingTimer is not null)
        {
            bool isNewAddressInCycle = _receivedRailComAddresses.Add(railComData.LocoAddress);
            if (isNewAddressInCycle)
            {
                _ = GetNextRailComDataAsync();
            }
        }
        _railComDataReceived.Invoke(this, railComData);
        // "RailCom data for loco {Address} received."
        logger.LogInformation(Messages.Text0056, railComData.LocoAddress);
    }

    private void ParseZ21Code(ReadOnlySpan<byte> data)
    {
        if (Z21CodeReceived is null) return;
        var lockState = (Z21LockState)data[4];
        var z21Code = new Z21Code(lockState);
        Z21CodeReceived.Invoke(this, z21Code);
        // "Z21 Code received: {LockState}"
        logger.LogInformation(Messages.Text0057, lockState);
    }

    private void ParseRBusData(ReadOnlySpan<byte> data)
    {
        if (_rBusDataReceived is null) return;
        int groupIndex = data[4];
        var feedbackData = data.Slice(5, 10);
        var rbusData = new RBusData(groupIndex, feedbackData);
        _rBusDataReceived.Invoke(this, rbusData);
        // "R-Bus data for group {GroupIndex} received."
        logger.LogInformation(Messages.Text0058, groupIndex);
    }

    private void ParseTurnoutMode(ReadOnlySpan<byte> data)
    {
        if (TurnoutModeReceived is null)
            return;

        ushort address = (ushort)((data[4] << 8) | data[5]);
        var mode = (TurnoutMode)data[6];
        var args = new TurnoutModeChangedEventArgs(address, mode);
        TurnoutModeReceived.Invoke(this, args);
        // "Turnout Mode for address {Address} received: {Mode}"
        logger.LogInformation(Messages.Text0059, address, mode);
    }

    private void ParseEmergencyStop(ReadOnlySpan<byte> data)
    {
        if (EmergencyStopReceived is null)
            return;

        if (data.Length < 7)
        {
            // "Received Emergency stop packet is too short. Expected 7 bytes, got {Length}"
            logger.LogWarning(Messages.Text0060, data.Length);
            return;
        }

        byte receivedChecksum = data[6];
        byte calculatedChecksum = CalculateChecksum(data);
        if (receivedChecksum != calculatedChecksum)
        {
            // "Received Emergency stop packet with invalid checksum. Received: 0x{Received:X2}, Calculated: 0x{Calculated:X2}. Packet discarded."
            logger.LogWarning(Messages.Text0061, receivedChecksum, calculatedChecksum);
            return;
        }
        EmergencyStopReceived.Invoke(this, EventArgs.Empty);

        // Have found that SystemStateChanged is not triggered by emergency stop, so manually request an update
        if (_systemStateChanged is not null)
        {
            //_ = GetSystemStateAsync();
        }
        // "Emergency stop received"
        logger.LogInformation(Messages.Text0062);
    }

    private void ParseTurnoutInfo(ReadOnlySpan<byte> data)
    {
        if (TurnoutInfoReceived is null) return;
        if (data.Length < 9)
        {
            // "Received Turnout Info packet is too short. Expected 9 bytes, got {Length}"
            logger.LogWarning(Messages.Text0063, data.Length);
            return;
        }

        byte receivedChecksum = data[8];
        byte calculatedChecksum = CalculateChecksum(data);
        if (receivedChecksum != calculatedChecksum)
        {
            // "Received Turnout Info packet with invalid checksum. Received: 0x{Received:X2}, Calculated: 0x{Calculated:X2}. Packet discarded."
            logger.LogWarning(Messages.Text0064, receivedChecksum, calculatedChecksum);
            return;
        }

        ushort address = (ushort)((data[5] << 8) | data[6]);
        var state = (TurnoutState)(data[7] & 0b00000011);
        var turnoutInfo = new TurnoutInfo(address, state);
        TurnoutInfoReceived.Invoke(this, turnoutInfo);
        // "Turnout Info for address {Address} received: {State}"
        logger.LogInformation(Messages.Text0065, address, state);
    }

    private void ParseTrackPowerState(ReadOnlySpan<byte> data)
    {
        if (TrackPowerInfoReceived is null)
            return;

        if (data.Length < 7)
        {
            // "Received Track Power Info packet is too short. Expected 7 bytes, got {Length}"
            logger.LogWarning(Messages.Text0066, data.Length);
            return;
        }

        byte receivedChecksum = data[6];
        byte calculatedChecksum = CalculateChecksum(data);
        if (receivedChecksum != calculatedChecksum)
        {
            // "Received Track Power Info packet with invalid checksum. Received: 0x{Received:X2}, Calculated: 0x{Calculated:X2}. Packet discarded."
            logger.LogWarning(Messages.Text0067, receivedChecksum, calculatedChecksum);
            return;
        }

        var state = (TrackPowerState)data[5];
        var trackPowerInfo = new TrackPowerInfo(state);
        TrackPowerInfoReceived.Invoke(this, trackPowerInfo);
        // "Track Power State received: {trackPowerInfo}"
        logger.LogInformation(Messages.Text0068, trackPowerInfo);

        // A change in track power does not generate a SystemStateChanged, so manually request an update
        if (_systemStateChanged is not null)
        {
            //_ = GetSystemStateAsync();
        }
    }

    private void ParseSerialNumber(ReadOnlySpan<byte> data)
    {
        if (SerialNumberReceived is null)
            return;

        uint serial = BitConverter.ToUInt32(data[4..]);
        var serialNumber = new SerialNumber(serial);
        SerialNumberReceived.Invoke(this, serialNumber);
        // "Serial Number received: {SerialNumber}"
        logger.LogInformation(Messages.Text0069, serial);
    }

    private void ParseHardwareInfo(ReadOnlySpan<byte> data)
    {
        if (HardwareInfoReceived is null)
            return;

        var hwType = (HardwareType)BitConverter.ToUInt32(data[4..]);
        uint fwValue = BitConverter.ToUInt32(data[8..]);
        string fwString = (fwValue >> 8).ToString("X") + "." + (fwValue & 0xFF).ToString("X2");
        if (Version.TryParse(fwString, out var parsedVersion))
        {
            _hardwareInfo = new HardwareInfo(hwType, new FirmwareVersion((byte)parsedVersion.Major, (byte)parsedVersion.Minor));
            HardwareInfoReceived.Invoke(this, _hardwareInfo);
            // "Hardware Info received: {HWType}, Firmware: {FWVersion}"
            logger.LogInformation(Messages.Text0070, hwType, fwString);
        }
        else
        {
            // "Failed to parse firmware version from LAN_GET_HWINFO response."
            logger.LogError(Messages.Text0071);
        }
    }

    private void ParseLocoMode(ReadOnlySpan<byte> data)
    {
        ushort address = (ushort)((data[4] << 8) | data[5]);
        LocoMode mode = (LocoMode)data[6];

        // --- START: Firmware Bug Workaround ---
        // Check if we are waiting for this LocoMode response to complete a pending LocoInfo request.
        if (_pendingLocoInfoRequests.TryGetValue(address, out var pendingLocoInfo))
        {
            // If pendingLocoInfo is not null, it means we have already received the partial LocoInfo.
            // We can now complete it and raise the event.
            if (pendingLocoInfo is not null)
            {
                // This is the second and final part of our requested data.
                // Update the loco info with the correct mode.
                var completedLocoInfo = new LocoInfo(pendingLocoInfo, mode);

                // Raise the final, correct event.
                _locoInfoReceived?.Invoke(this, completedLocoInfo);
                // "Firmware bug workaround: Combined LocoInfo and LocoMode for address {Address} and raised event."
                logger.LogInformation(Messages.Text0072, address);

                // Clean up the pending request.
                _pendingLocoInfoRequests.Remove(address);
            }
            // If pendingLocoInfo is null, it means this LocoMode arrived before the LocoInfo.
            // We just let ParseLocoInfo handle the completion when it arrives.
            return; // Stop processing to avoid raising a standalone LocoModeReceived event.
        }
        // --- END: Firmware Bug Workaround ---

        // If not part of a pending request, raise the event as usual.
        if (LocoModeReceived is not null)
        {
            var args = new LocoModeChangedEventArgs(address, mode);
            LocoModeReceived.Invoke(this, args);
            // "Loco Mode for address {Address} received: {Mode}"
            logger.LogInformation(Messages.Text0073, address, mode);
        }
    }

    private void ParseBroadcastFlags(ReadOnlySpan<byte> data)
    {
        if (BroadcastFlagsReceived is null)
            return;

        uint flags = BitConverter.ToUInt32(data[4..]);
        var args = new BroadcastFlagsChangedEventArgs(flags);
        BroadcastFlagsReceived.Invoke(this, args);
        // "Broadcast flags received and processed. Flags: 0x{Flags:X8}"
        logger.LogInformation(Messages.Text0074, flags);
    }

    private void ParseLocoInfo(ReadOnlySpan<byte> data)
    {
        if (_locoInfoReceived is null)
            return;

        int expectedMinLength = 14;
        if (data.Length < expectedMinLength)
        {
            // "Received Loco Info packet is too short. Expected at least {Length} bytes, got {ActualLength}"
            logger.LogWarning(Messages.Text0075, expectedMinLength, data.Length);
            return;
        }
        byte receivedChecksum = data[^1];
        byte calculatedChecksum = CalculateChecksum(data);
        if (receivedChecksum != calculatedChecksum)
        {
            // "Received Loco Info packet with invalid checksum. Received: 0x{Received:X2}, Calculated: 0x{Calculated:X2}. Packet discarded."
            logger.LogWarning(Messages.Text0076, receivedChecksum, calculatedChecksum);
            return;
        }
        ushort address = (ushort)(((data[5] & 0x3F) << 8) | data[6]);
        byte? db8 = null;
        if (_hardwareInfo?.FwVersion.Version >= Z21FirmwareVersions.V1_42 && data.Length >= 15)
        {
            db8 = data[13];
        }
        var locoInfo = new LocoInfo(address, data[7], data[8], data[9], data[10], data[11], data[12], db8, _hardwareInfo?.FwVersion);

        var speedSteps = (data[7] & 0b00000111) switch
        {
            0 => NativeSpeedSteps.Steps14,
            2 => NativeSpeedSteps.Steps28,
            4 => NativeSpeedSteps.Steps128,
            _ => NativeSpeedSteps.Unknown
        };

        // --- START: Firmware Bug Workaround ---
        // Check if this LocoInfo is part of a pending request we initiated.
        if (_pendingLocoInfoRequests.ContainsKey(address))
        {
            // This is a direct response to our GetLocoInfoAsync call.
            // Instead of raising the event immediately (as the protocol info might be wrong),
            // we store this partial info and wait for the corresponding LocoMode response.
            _pendingLocoInfoRequests[address] = locoInfo;
            // "Firmware bug workaround: Stored partial LocoInfo for address {Address}, awaiting LocoMode."
            logger.LogDebug(Messages.Text0077, address);
            return; // Stop processing here and wait for ParseLocoMode to complete the data.
        }
        // --- END: Firmware Bug Workaround ---

        _locoInfoReceived.Invoke(this, locoInfo);
        // "Loco Info for address {Address} received and processed."
        logger.LogInformation(Messages.Text0078, address);
    }

    private void ParseFirmwareVersion(ReadOnlySpan<byte> data)
    {
        if (FirmwareVersionReceived is null)
            return;

        string versionString = $"{data[6]:X}.{data[7]:X2}";
        if (Version.TryParse(versionString, out var parsedVersion))
        {
            var firmware = new FirmwareVersion((byte)parsedVersion.Major, (byte)parsedVersion.Minor);
            FirmwareVersionReceived.Invoke(this, firmware);
            // "Firmware version received and processed: {Version}"
            logger.LogInformation(Messages.Text0079, firmware);
        }
        else
        {
            // "Failed to parse firmware version from received data."
            logger.LogError(Messages.Text0080);
        }
    }

    private void ParseSystemState(ReadOnlySpan<byte> data)
    {
        if (_systemStateChanged is null)
        {
            return;
        }

        if (data.Length < 18)
        {
            // "System state packet is too short. Expected at least 18 bytes, got {Length}"
            logger.LogWarning(Messages.Text0081, data.Length);
            return;
        }

        byte? capabilities = null;
        if (_hardwareInfo?.FwVersion.Version >= Z21FirmwareVersions.V1_42 && data.Length >= 19)
        {
            capabilities = data[19];
        }
        // The z21s does not have a programming track. The value is forced to 0.
        var args = new SystemStateChangedEventArgs(
            mainCurrentmA: BitConverter.ToInt16(data[4..]),
            progCurrentmA: _isz21 ? 0 : BitConverter.ToInt16(data[6..]),
            mainCurrentFilteredmA: BitConverter.ToInt16(data[8..]),
            temperatureC: BitConverter.ToInt16(data[10..]),
            supplyVoltagemV: BitConverter.ToInt16(data[12..]),
            vccVoltagemV: BitConverter.ToInt16(data[14..]),
            centralState: data[16],
            centralStateEx: data[17],
            capabilities: capabilities
        );

        _systemStateChanged.Invoke(this, args);
        // "System state received and successfully processed."
        logger.LogInformation(Messages.Text0082, args.VccVoltagemV);
    }

    private async Task SetBroadcastFlags(BroadcastFlags subscripedBroadcastFlags)
    {
        var command = new byte[Z21ProtocolConstants.LengthSetBroadcastFlags];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthSetBroadcastFlags).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderSetBroadcastFlags).CopyTo(command, 2);
        BitConverter.GetBytes((uint)subscripedBroadcastFlags).CopyTo(command, 4);
        await SendCommandAsync(command);
        // "Setting broadcast flags to {subscripedBroadcastFlags}"
        logger.LogInformation(Messages.Text0083, subscripedBroadcastFlags);
    }

    private static byte CalculateChecksum(ReadOnlySpan<byte> data)
    {
        byte calculatedChecksum = 0;
        for (int i = 4; i < (data.Length - 1); i++)
        {
            calculatedChecksum ^= data[i];
        }
        return calculatedChecksum;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
