using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Z21Client.Helpers;
using Z21Client.Infrastructure;
using Z21Client.Interfaces;
using Z21Client.Models;

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

    private Timer? _railComPollingTimer;
    private readonly HashSet<ushort> _receivedRailComAddresses = [];

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
                logger.LogInformation("Subscribing to AllLocoInfoReceived event. Adding AllLocoInfo broadcast flag.");
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
                logger.LogInformation("Unsubscribing from AllLocoInfoReceived event. Removing AllLocoInfo broadcast flag.");
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
                logger.LogInformation("Subscribing to RailComDataReceived event. Adding AllRailCom broadcast flag and starting polling.");
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
                logger.LogInformation("Unsubscribing from RailComDataReceived event. Removing AllRailCom broadcast flag and stopping polling.");

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
                logger.LogInformation("Subscribing to RBusDataReceived event. Adding RBus broadcast flag.");
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
                logger.LogInformation("Unsubscribing from RBusDataReceived event. Removing RBus broadcast flag.");
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
                logger.LogInformation("Subscribing to SystemStateChanged event. Adding SystemState broadcast flag.");
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
                logger.LogInformation("Unsubscribing from SystemStateChanged event. Removing SystemState broadcast flag.");
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

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(string host, int port = 21105)
    {
        logger.LogInformation("Connecting to Z21 at {Host}:{Port}...", host, port);
        if (_udpClient is not null)
        {
            logger.LogWarning("Already connected. Please disconnect first.");
            return true;
        }

        if (!await PingHostAsync(host))
        {
            logger.LogError("Connection failed: Host {Host} is not reachable (ping failed).", host);
            return false;
        }

        try
        {
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid IP address provided: {Host}", host);
            return false;
        }

        try
        {
            _udpClient = new UdpClient(port);
            if (OperatingSystem.IsWindows())
            {
                _udpClient.AllowNatTraversal(true);
            }
            logger.LogInformation("UdpClient created and bound to listen on local port {Port}", port);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create and bind UdpClient on port {Port}. The port may already be in use by another application.", port);
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
        }
        HardwareInfoReceived += handshakeHandler;

        await GetHardwareInfoAsync();

        using (timeoutCts.Token.Register(() => handshakeComplete.TrySetResult(false)))
        {
            var result = await handshakeComplete.Task;
            HardwareInfoReceived -= handshakeHandler;

            if (!result)
            {
                logger.LogError("Connection failed: Host responded to ping, but did not respond to Z21 command (handshake failed).");
                await DisconnectAsync();
                return false;
            }
        }

        _lastCommandSentTimestamp = DateTime.UtcNow;
        _lastMessageReceivedTimestamp = DateTime.UtcNow;
        _failedPingCount = 0;

        _subscripedBroadcastFlags = BroadcastFlags.Basic;
        await SetBroadcastFlags(_subscripedBroadcastFlags);

        _keepAliveTimer = new Timer(KeepAliveCallback, null, TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(45));
        _watchdogTimer = new Timer(WatchdogCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        logger.LogInformation("Z21 client connection verified and fully started.");
        return true;
    }


    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        logger.LogInformation("Disconnecting from Z21...");

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
                    logger.LogDebug("Receive task was successfully cancelled as expected during disconnect.");
                }
                catch (TimeoutException)
                {
                    logger.LogWarning("Receive task did not cancel within the expected time during disconnect.");
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
        logger.LogInformation("Disconnected.");
    }

    /// <inheritdoc/>
    public async Task GetBroadcastFlagsAsync()
    {
        await SendCommandAsync(Z21Commands.GetBroadcastFlags);
    }

    /// <inheritdoc/>
    public async Task GetFirmwareVersionAsync()
    {
        await SendCommandAsync(Z21Commands.GetFirmwareVersion);
    }

    /// <inheritdoc/>
    public async Task GetHardwareInfoAsync()
    {
        await SendCommandAsync(Z21Commands.GetHardwareInfo);
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
        logger.LogInformation("GetLocoInfoAsync: Requested loco info for address {Address}", address);

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
        logger.LogInformation("GetLocoModeAsync: Requested loco mode for address {Address}", address);
    }

    /// <inheritdoc/>
    public async Task GetLocoSlotInfoAsync(byte slotNumber)
    {
        var command = new byte[Z21ProtocolConstants.LengthGetLocoSlotInfo];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthGetLocoSlotInfo).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderGetLocoSlotInfo).CopyTo(command, 2);
        command[5] = slotNumber;
        await SendCommandAsync(command);
        logger.LogInformation("GetLocoSlotInfoAsync: Requested loco info for slot {slotNumber}", slotNumber);
    }

    /// <inheritdoc/>
    public async Task GetRailComDataAsync(ushort locoAddress)
    {
        byte[] command = [0x07, 0x00, (byte)(Z21ProtocolConstants.HeaderGetRailComData & 0xFF), (byte)(Z21ProtocolConstants.HeaderGetRailComData >> 8), 0x01, (byte)(locoAddress & 0xFF), (byte)(locoAddress >> 8)];
        await SendCommandAsync(command);
        logger.LogInformation("GetRailComDataAsync: Requested RailCom info for loco {locoAddress}", locoAddress);
    }


    /// <inheritdoc/>
    public async Task GetRBusDataAsync(int groupIndex)
    {
        byte[] command = [0x05, 0x00, (byte)(Z21ProtocolConstants.HeaderRBusGetData & 0xFF), (byte)(Z21ProtocolConstants.HeaderRBusGetData >> 8), (byte)groupIndex];
        await SendCommandAsync(command);
        logger.LogInformation("GetRBusDataAsync: Requested R-Bus data for group index {groupIndex}", groupIndex);
    }

    /// <inheritdoc/>
    public async Task GetSerialNumberAsync()
    {
        await SendCommandAsync(Z21Commands.GetSerialNumber);
        logger.LogInformation("GetSerialtNumberAsync: Requested z21/Z21 serial number");
    }

    /// <inheritdoc/>
    public async Task GetSystemStateAsync()
    {
        await SendCommandAsync(Z21Commands.GetSystemState);
        logger.LogInformation("GetSystemStateAsync: Requested system state");
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
        logger.LogInformation("GetTurnoutModeAsync: Requested turnout mode for address {address}", address);
    }

    /// <inheritdoc/>
    public async Task GetZ21CodeAsync()
    {
        await SendCommandAsync(Z21Commands.GetCode);
        logger.LogInformation("GetZ21CodeAsync: Requested z21/Z21 (feature) code");
    }

    /// <inheritdoc/>
    public async Task SetLocoDriveAdaptSpeedAsync(ushort address, byte speed, NativeSpeedSteps nativeSpeedStep, DrivingDirection direction, LocoMode locoMode)
    {
        byte rocoSpeedStep;
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
        rocoSpeedStep = DccSpeedSteps.GetSpeedStepReverse(convertedSpeedStep, (SpeedSteps)nativeSpeedStep);

        //logger.LogError("SetLocoDriveAdaptAsync: Address {Address}: Speed={Speed}, rocoSpeedStep={rocoSpeedStep}, NativeSteps={NativeSpeedSteps}, Direction={Direction}, Mode={LocoMode}", address, speed, rocoSpeedStep, nativeSpeedStep, direction, locoMode);


        // Create the 10-byte command array
        var command = new byte[10];

        // Length: 0x0A
        BitConverter.GetBytes((ushort)0x0A).CopyTo(command, 0);

        // Header: 0x40 0x00
        BitConverter.GetBytes((ushort)0x0040).CopyTo(command, 2);

        // X-Header: 0xE4
        command[4] = 0xE4;

        // DB0: 0x1S (Speed steps)
        command[5] = (byte)(0x10 | (byte)nativeSpeedStep);

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

        // Adjust speed steps:
        //speed = DccSpeedSteps.GetSpeedStepReverse(speed, speedSteps);

        byte speedValue = (byte)(rocoSpeedStep & 0x7F);
        // Set the direction bit (bit 7)
        byte directionBit = (byte)((int)direction << 7);
        command[8] = (byte)(directionBit | speedValue);

        // DB4: Checksum
        command[9] = CalculateChecksum(command);

        // Send the command
        await SendCommandAsync(command);
        logger.LogInformation("Set loco drive for address {Address}: Speed={Speed}, rocoValue={rocoValue} NativeSteps={NativeSpeedSteps}, Direction={Direction}", address, speed, rocoSpeedStep, nativeSpeedStep, direction);
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
        logger.LogInformation("Toggle function {functionIndex} for loco address {address}", functionIndex, address);
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
        logger.LogInformation("Setting loco mode for address {Address} to {Mode}", address, mode);
    }

    /// <inheritdoc/>
    public async Task SetEmergencyStopAsync()
    {
        await SendCommandAsync(Z21Commands.SetEmergencyStop);
        logger.LogWarning("Sending Emergency Stop command.");
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
        logger.LogInformation("Setting turnout mode for address {Address} to {Mode}", address, mode);
    }

    /// <inheritdoc/>
    public async Task SetTurnoutPositionAsync(ushort address, TurnoutPosition position)
    {
        var turnoutPosition = (byte)0x80 | ((byte)position & 1);

        // Turn on:
        turnoutPosition |= 0x08;

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
        turnoutPosition = (byte)0x80 | ((byte)position & 1);
        command[7] = (byte)turnoutPosition;
        command[8] = CalculateChecksum(command);
        await SendCommandAsync(command);

        await Task.Delay(50);

        logger.LogInformation("Setting turnout position address {address}, position {position}", address, position);
    }

    /// <inheritdoc/>
    public async Task SetTrackPowerOffAsync()
    {
        await SendCommandAsync(Z21Commands.SetTrackPowerOff);
        logger.LogInformation("Setting track power off");
    }

    /// <inheritdoc/>
    public async Task SetTrackPowerOnAsync()
    {
        await SendCommandAsync(Z21Commands.SetTrackPowerOn);
        logger.LogInformation("Setting track power on");
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
            logger.LogError(ex, "An exception occurred during ping to {Host}.", host);
            return false;
        }
    }

    private void KeepAliveCallback(object? state)
    {
        if (DateTime.UtcNow - _lastCommandSentTimestamp > TimeSpan.FromSeconds(40))
        {
            logger.LogInformation("Sending keep-alive message to Z21.");
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
    }

    private async void WatchdogCallback(object? state)
    {
        if (DateTime.UtcNow - _lastMessageReceivedTimestamp < TimeSpan.FromSeconds(15))
        {
            return;
        }

        if (_failedPingCount >= 3)
        {
            logger.LogError("Connection to Z21 lost. No response to multiple pings.");
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Lost));
            await DisconnectAsync();
            return;
        }

        try
        {
            logger.LogWarning("No message received from Z21 for a while. Pinging to check connection...");
            if (await PingHostAsync(_remoteEndPoint!.Address.ToString()))
            {
                logger.LogInformation("Ping successful, Z21 is still on the network. Waiting for data.");
            }
            else
            {
                _failedPingCount++;
                logger.LogWarning("Ping failed. Failure count: {Count}", _failedPingCount);
            }
        }
        catch (Exception ex)
        {
            _failedPingCount++;
            logger.LogError(ex, "An exception occurred during watchdog ping. Failure count: {Count}", _failedPingCount);
        }
    }

    private async Task SendCommandAsync(byte[] command)
    {
        if (_udpClient is null || _remoteEndPoint is null)
        {
            logger.LogWarning("Cannot send command. Client is not connected.");
            return;
        }

        try
        {
            _ = await _udpClient.SendAsync(command, command.Length, _remoteEndPoint);
            _lastCommandSentTimestamp = DateTime.UtcNow;
            logger.LogDebug("Command sent: {Command}", BitConverter.ToString(command));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command to central station.");
        }
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        logger.LogDebug("Receive loop started. Waiting for data...");
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
                logger.LogError(ex, "An error occurred in the receive loop.");
            }
        }
        logger.LogDebug("Receive loop stopped.");
    }

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
                logger.LogWarning("Received malformed data: not enough bytes to read message length. Remaining bytes: {Length}", span.Length - offset);
                break;
            }

            ushort messageLength = BitConverter.ToUInt16(span[offset..]);

            if (messageLength == 0)
            {
                logger.LogWarning("Encountered zero-length message in data stream. Stopping parse of this packet.");
                break;
            }

            if (offset + messageLength > span.Length)
            {
                logger.LogWarning("Received malformed data: buffer is smaller than the indicated message length. Expected: {Expected}, Actual remaining: {Actual}", messageLength, span.Length - offset);
                break;
            }

            var messageSpan = span.Slice(offset, messageLength);
            ProcessSingleMessage(messageSpan);

            offset += messageLength;
        }
    }

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
                logger.LogWarning("Received an unhandled or malformed packet. Header: 0x{Header:X4}, Length: {Length}", header, data.Length);
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
            logger.LogWarning("Received Loco Slot Info packet is too short. Expected at least 24 bytes, got {Length}", data.Length);
            return;
        }

        ushort address = BitConverter.ToUInt16(data[9..]);
        if (address is 0)
        {
            return;
        }

        byte slotNumber = data[7];
        var rawSpeed = (byte)(data[12] & 0b01111111);
        byte speedSteps = 0;
        byte speed = 0;
        switch (data[18])
        {
            case 3: speedSteps = 0; speed = (byte)Math.Ceiling(rawSpeed / (double)8.2); speed = DccSpeedSteps.GetSpeedStep14Reverse(speed); break;
            case 6: speedSteps = 2; speed = (byte)Math.Ceiling(rawSpeed / (double)4.6); speed = DccSpeedSteps.GetSpeedStep28Reverse(speed); break;
            case 9: speedSteps = 4; speed = rawSpeed; break;
            case 67: speedSteps = 0; speedSteps |= 0x10; speed = (byte)(rawSpeed / (double)8.2); break;
            case 83: speedSteps = 2; speedSteps |= 0x10; speed = (byte)Math.Ceiling(rawSpeed / (double)4.1); break;
            case 117: speedSteps = 4; speedSteps |= 0x10; speed = rawSpeed; break;
        }

        if ((data[14] & 0x20) == 0)
        {
            speed |= 0b10000000;
        }

        var locoSlotInfo = new LocoSlotInfo(slotNumber, new LocoInfo(
            address, speedSteps, speed, data[14], data[15], data[16], data[17], null, _hardwareInfo?.FwVersion
            ));

        LocoSlotInfoReceived.Invoke(this, locoSlotInfo);
        logger.LogInformation("Loco Slot Info for slot {SlotNumber} received: Address={Address}, SpeedStep={SpeedStep}", slotNumber, address, speedSteps);
    }

    private void ParseXBus(ReadOnlySpan<byte> data)
    {
        byte xHeader = data[4];
        ushort xHeaderDb0 = BitConverter.ToUInt16(data[5..]);
        if (xHeaderDb0 is Z21ProtocolConstants.XHeaderUnknownCommand)
        {
            logger.LogInformation("Send an unknown X-Bus command to Z21");
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
            default: logger.LogInformation("Received an unhandled X-Bus command with X-Header: 0x{XHeader:X2}", xHeader); break;
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
        logger.LogInformation("RailCom data for loco {Address} received.", railComData.LocoAddress);
    }

    private void ParseZ21Code(ReadOnlySpan<byte> data)
    {
        if (Z21CodeReceived is null) return;
        var lockState = (Z21LockState)data[4];
        var z21Code = new Z21Code(lockState);
        Z21CodeReceived.Invoke(this, z21Code);
        logger.LogInformation("Z21 Code received: {LockState}", lockState);
    }

    private void ParseRBusData(ReadOnlySpan<byte> data)
    {
        if (_rBusDataReceived is null) return;
        int groupIndex = data[4];
        var feedbackData = data.Slice(5, 10);
        var rbusData = new RBusData(groupIndex, feedbackData);
        _rBusDataReceived.Invoke(this, rbusData);
        logger.LogInformation("R-Bus data for group {GroupIndex} received.", groupIndex);
    }

    private void ParseTurnoutMode(ReadOnlySpan<byte> data)
    {
        if (TurnoutModeReceived is null) return;
        ushort address = (ushort)((data[4] << 8) | data[5]);
        var mode = (TurnoutMode)data[6];
        var args = new TurnoutModeChangedEventArgs(address, mode);
        TurnoutModeReceived.Invoke(this, args);
        logger.LogInformation("Turnout Mode for address {Address} received: {Mode}", address, mode);
    }

    private void ParseEmergencyStop(ReadOnlySpan<byte> data)
    {
        if (EmergencyStopReceived is null) return;
        if (data.Length < 7) { logger.LogWarning("Received Emergency stop packet is too short. Expected 7 bytes, got {Length}", data.Length); return; }
        byte receivedChecksum = data[6];
        byte calculatedChecksum = CalculateChecksum(data);
        if (receivedChecksum != calculatedChecksum) { logger.LogWarning("Received Emergency stop packet with invalid checksum. Received: 0x{Received:X2}, Calculated: 0x{Calculated:X2}. Packet discarded.", receivedChecksum, calculatedChecksum); return; }
        EmergencyStopReceived.Invoke(this, EventArgs.Empty);
        if (_systemStateChanged is not null)
        {
            _ = GetSystemStateAsync();
        }
        logger.LogInformation("Emergency stop received");
    }

    private void ParseTurnoutInfo(ReadOnlySpan<byte> data)
    {
        if (TurnoutInfoReceived is null) return;
        if (data.Length < 9) { logger.LogWarning("Received Turnout Info packet is too short. Expected 9 bytes, got {Length}", data.Length); return; }
        byte receivedChecksum = data[8];
        byte calculatedChecksum = CalculateChecksum(data);
        if (receivedChecksum != calculatedChecksum) { logger.LogWarning("Received Turnout Info packet with invalid checksum. Received: 0x{Received:X2}, Calculated: 0x{Calculated:X2}. Packet discarded.", receivedChecksum, calculatedChecksum); return; }
        ushort address = (ushort)(((data[5] << 8) | data[6]));
        var state = (TurnoutState)(data[7] & 0b00000011);
        var turnoutInfo = new TurnoutInfo(address, state);
        TurnoutInfoReceived.Invoke(this, turnoutInfo);
        logger.LogInformation("Turnout Info for address {Address} received: {State}", address, state);
    }

    private void ParseTrackPowerState(ReadOnlySpan<byte> data)
    {
        if (TrackPowerInfoReceived is null) return;
        if (data.Length < 7)
        {
            logger.LogWarning("Received Track Power Info packet is too short. Expected 7 bytes, got {Length}", data.Length);
            return;
        }
        byte receivedChecksum = data[6];
        byte calculatedChecksum = CalculateChecksum(data);
        if (receivedChecksum != calculatedChecksum)
        {
            logger.LogWarning("Received Track Power Info packet with invalid checksum. Received: 0x{Received:X2}, Calculated: 0x{Calculated:X2}. Packet discarded.", receivedChecksum, calculatedChecksum);
            return;
        }
        var state = (TrackPowerState)(data[5]);
        var trackPowerInfo = new TrackPowerInfo(state);
        TrackPowerInfoReceived.Invoke(this, trackPowerInfo);
        logger.LogInformation("Track Power State received: {trackPowerInfo}", trackPowerInfo);
        if (_systemStateChanged is not null)
        {
            _ = GetSystemStateAsync();
        }
    }

    private void ParseSerialNumber(ReadOnlySpan<byte> data)
    {
        if (SerialNumberReceived is null) return;
        uint serial = BitConverter.ToUInt32(data[4..]);
        var serialNumber = new SerialNumber(serial);
        SerialNumberReceived.Invoke(this, serialNumber);
        logger.LogInformation("Serial Number received: {SerialNumber}", serial);
    }

    private void ParseHardwareInfo(ReadOnlySpan<byte> data)
    {
        if (HardwareInfoReceived is null) return;
        var hwType = (HardwareType)BitConverter.ToUInt32(data[4..]);
        uint fwValue = BitConverter.ToUInt32(data[8..]);
        string fwString = (fwValue >> 8).ToString("X") + "." + (fwValue & 0xFF).ToString("X2");
        if (Version.TryParse(fwString, out var parsedVersion))
        {
            _hardwareInfo = new HardwareInfo(hwType, new FirmwareVersion((byte)parsedVersion.Major, (byte)parsedVersion.Minor));
            HardwareInfoReceived.Invoke(this, _hardwareInfo);
            logger.LogInformation("Hardware Info received: {HWType}, Firmware: {FWVersion}", hwType, fwString);
        }
        else
        {
            logger.LogError("Failed to parse firmware version from LAN_GET_HWINFO response.");
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
                logger.LogInformation("Firmware bug workaround: Combined LocoInfo and LocoMode for address {Address} and raised event.", address);

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
            logger.LogInformation("Loco Mode for address {Address} received: {Mode}", address, mode);
        }
    }

    private void ParseBroadcastFlags(ReadOnlySpan<byte> data)
    {
        if (BroadcastFlagsReceived is null) return;
        uint flags = BitConverter.ToUInt32(data[4..]);
        var args = new BroadcastFlagsChangedEventArgs(flags);
        BroadcastFlagsReceived.Invoke(this, args);
        logger.LogInformation("Broadcast flags received and processed. Flags: 0x{Flags:X8}", flags);
    }

    private void ParseLocoInfo(ReadOnlySpan<byte> data)
    {
        logger.LogInformation("Loco Info start.");
        if (_locoInfoReceived is null) return;
        int expectedMinLength = 14;
        if (data.Length < expectedMinLength)
        {
            logger.LogWarning("Received Loco Info packet is too short. Expected at least {Length} bytes, got {ActualLength}", expectedMinLength, data.Length);
            return;
        }
        byte receivedChecksum = data[^1];
        byte calculatedChecksum = CalculateChecksum(data);
        if (receivedChecksum != calculatedChecksum)
        {
            logger.LogWarning("Received Loco Info packet with invalid checksum. Received: 0x{Received:X2}, Calculated: 0x{Calculated:X2}. Packet discarded.", receivedChecksum, calculatedChecksum);
            return;
        }
        ushort address = (ushort)(((data[5] & 0x3F) << 8) | data[6]);
        byte? db8 = null;
        if (_hardwareInfo?.FwVersion.Version >= Z21FirmwareVersions.V1_42 && data.Length >= 15)
        {
            db8 = data[13];
        }
        var locoInfo = new LocoInfo(address, data[7], data[8], data[9], data[10], data[11], data[12], db8, _hardwareInfo?.FwVersion);


        /*
        var rawSpeed = (byte)(data[8] & 0b01111111);
        var nativeSpeedSteps = (data[7] & 0b00000111) switch
        {
            0 => NativeSpeedSteps.Steps14,
            2 => NativeSpeedSteps.Steps28,
            4 => NativeSpeedSteps.Steps128,
            _ => NativeSpeedSteps.Unknown
        };
        var speed = DccSpeedSteps.GetSpeedStep(rawSpeed, (SpeedSteps)nativeSpeedSteps);
        logger.LogError("Parsed LocoInfo: Address={Address}, rawSpeed={rawSpeed}, convSpeed={speed}, speedStep={speed}, ", address, rawSpeed, locoInfo.CurrentSpeed, speed);
        */


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
            logger.LogDebug("Firmware bug workaround: Stored partial LocoInfo for address {Address}, awaiting LocoMode.", address);
            return; // Stop processing here and wait for ParseLocoMode to complete the data.
        }
        // --- END: Firmware Bug Workaround ---

        _locoInfoReceived.Invoke(this, locoInfo);
        logger.LogInformation("Loco Info for address {Address} received and processed.", address);
    }

    private void ParseFirmwareVersion(ReadOnlySpan<byte> data)
    {
        if (FirmwareVersionReceived is null) return;
        string versionString = $"{data[6]:X}.{data[7]:X2}";
        if (Version.TryParse(versionString, out var parsedVersion))
        {
            var firmware = new FirmwareVersion((byte)parsedVersion.Major, (byte)parsedVersion.Minor);
            FirmwareVersionReceived.Invoke(this, firmware);
            logger.LogInformation("Firmware version received and processed: {Version}", firmware);
        }
        else
        {
            logger.LogError("Failed to parse firmware version from received data.");
        }
    }

    private void ParseSystemState(ReadOnlySpan<byte> data)
    {
        if (_systemStateChanged is null) return;
        if (data.Length < 18)
        {
            logger.LogWarning("System state packet is too short. Expected at least 18 bytes, got {Length}", data.Length);
            return;
        }
        byte? capabilities = null;
        if (_hardwareInfo?.FwVersion.Version >= Z21FirmwareVersions.V1_42 && data.Length >= 19) 
        {
            capabilities = data[19];
        }
        var args = new SystemStateChangedEventArgs(
            mainCurrentmA: BitConverter.ToInt16(data[4..]),
            progCurrentmA: BitConverter.ToInt16(data[6..]),
            mainCurrentFilteredmA: BitConverter.ToInt16(data[8..]),
            temperatureC: BitConverter.ToInt16(data[10..]),
            supplyVoltagemV: BitConverter.ToInt16(data[12..]),
            vccVoltagemV: BitConverter.ToInt16(data[14..]),
            centralState: data[16],
            centralStateEx: data[17],
            capabilities: capabilities
        );
        _systemStateChanged.Invoke(this, args);
        logger.LogInformation("System state received and successfully processed.");
    }

    private async Task SetBroadcastFlags(BroadcastFlags subscripedBroadcastFlags)
    {
        var command = new byte[Z21ProtocolConstants.LengthSetBroadcastFlags];
        BitConverter.GetBytes(Z21ProtocolConstants.LengthSetBroadcastFlags).CopyTo(command, 0);
        BitConverter.GetBytes(Z21ProtocolConstants.HeaderSetBroadcastFlags).CopyTo(command, 2);
        BitConverter.GetBytes((uint)subscripedBroadcastFlags).CopyTo(command, 4);
        await SendCommandAsync(command);
        logger.LogInformation("Setting broadcast flags to {subscripedBroadcastFlags}", subscripedBroadcastFlags);
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
