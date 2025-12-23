namespace Z21Client.Models;

/// <summary>
/// Provides data for the <see cref="Application.Interfaces.IZ21Client.SystemStateChanged"/> event.
/// </summary>
public sealed record SystemState
{
    // CentralState Flags
    public const byte CentralStateEmergencyStop = 0b00000001;
    public const byte CentralStateTrackVoltageOff = 0b00000010;
    public const byte CentralStateShortCircuit = 0b00000100;
    public const byte CentralStateProgrammingMode = 0b00100000;

    // CentralStateEx Flags
    public const byte CentralStateExHighTemperature = 0b00000001;
    public const byte CentralStateExPowerLost = 0b00000010;
    public const byte CentralStateExShortCircuitExternal = 0b00000100;
    public const byte CentralStateExShortCircuitInternal = 0x08;
    public const byte CentralStateExRcn213Mode = 0b00100000;

    // Capabilities Flags
    public const byte CapabilitiesDcc = 0x01;
    public const byte CapabilitiesMM = 0x02;
    public const byte CapabilitiesRailCom = 0x08;
    public const byte CapabilitiesLocoCmds = 0x10;
    public const byte CapabilitiesAccessoryCmds = 0x20;
    public const byte CapabilitiesDetectorCmds = 0x40;
    public const byte CapabilitiesNeedsUnlockCode = 0x80;

    public int MainCurrentmA { get; }
    public int ProgCurrentmA { get; }
    public int MainCurrentFilteredmA { get; }
    public int TemperatureC { get; }
    public int SupplyVoltagemV { get; }
    public int VccVoltagemV { get; }
    public byte CentralState { get; }
    public byte CentralStateEx { get; }
    public Caps? Capabilities { get; }

    public SystemState(
        int mainCurrentmA,
        int progCurrentmA,
        int mainCurrentFilteredmA,
        int temperatureC,
        int supplyVoltagemV,
        int vccVoltagemV,
        byte centralState,
        byte centralStateEx,
        Caps? capabilities = null)
    {
        MainCurrentmA = mainCurrentmA;
        ProgCurrentmA = progCurrentmA;
        MainCurrentFilteredmA = mainCurrentFilteredmA;
        TemperatureC = temperatureC;
        SupplyVoltagemV = supplyVoltagemV;
        VccVoltagemV = vccVoltagemV;
        CentralState = centralState;
        CentralStateEx = centralStateEx;
        Capabilities = capabilities;
    }

    /// <summary>
    /// The capabilities of the Z21 system, parsed from the capabilities byte.
    /// </summary>
    public sealed record Caps
    {
        public Protocols Protocols { get; }
        public bool RailComEnabled { get; }
        public bool AcceptLANLocoCmds { get; }
        public bool AcceptLANAccCmds { get; }
        public bool AcceptLANDetCmds { get; }
        public bool NeedsUnlockCode { get; }

        public Caps(byte capabilities)
        {
            Protocols = (capabilities & (CapabilitiesDcc + CapabilitiesMM)) switch
            {
                CapabilitiesDcc + CapabilitiesMM => Protocols.DCC_MM,
                CapabilitiesDcc => Protocols.DCC,
                CapabilitiesMM => Protocols.MM,
                _ => Protocols.DCC
            };

            RailComEnabled = (capabilities & CapabilitiesRailCom) > 0;
            AcceptLANLocoCmds = (capabilities & CapabilitiesLocoCmds) > 0;
            AcceptLANAccCmds = (capabilities & CapabilitiesAccessoryCmds) > 0;
            AcceptLANDetCmds = (capabilities & CapabilitiesDetectorCmds) > 0;
            NeedsUnlockCode = (capabilities & CapabilitiesNeedsUnlockCode) > 0;
        }
    }
}
