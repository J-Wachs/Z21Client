using Z21Client.Helpers;
using Z21Client.Infrastructure;

namespace Z21Client.Models;

public sealed record LocoInfo
{
    /// <summary>
    /// The addresse of the locomotive-decoder.
    /// </summary>
    public ushort Address { get; init; }
    /// <summary>
    /// Flag for whether the locomotive is currently controlled by a hand-controller (multiMaus).
    /// </summary>
    public bool IsBusy { get; init; }
    /// <summary>
    /// The locomotive mode (DCC/MM) if known.
    /// </summary>
    public LocoMode? LocomotiveMode { get; init; }
    /// <summary>
    /// Gets the protocol used by the locomotive for communication, e.g. DCC28.
    /// </summary>
    public LocomotiveProtocol Protocol { get; init; }
    /// <summary>
    /// The protocol and speed steps as display string, e.g. "DCC28".
    /// </summary>
    public string? DisplayProtocol { get; init; }
    /// <summary>
    /// The speed steps corrected for MM/DCC as used in the application.
    /// </summary>
    public SpeedSteps SpeedSteps { get; init; }
    /// <summary>
    /// The speed steps as numeric value corrected for MM/DCC as used in the application.
    /// </summary>
    public byte SpeedStepsNumeric => SpeedSteps switch
    {
        SpeedSteps.Steps14 => 14,
        SpeedSteps.Steps28 => 28,
        SpeedSteps.Steps128 => 126, // Eventhough the protocol is 128, only 126 steps are used for speed control
        _ => 0
    };
    /// <summary>
    /// The speed steps as reported and used natively by the z21/Z21 central station.
    /// </summary>
    public NativeSpeedSteps NativeSpeedSteps { get; init; }
    /// <summary>
    /// The currect speed step corrected for MM/DCC as used in the application. The is
    /// MM2 with 14 speep steps, the speed value will be between 0 and 14, eventhough the z21/Z12
    /// uses 28 speed steps setting to indicate MM2 with 14 speed steps. The value will in this
    /// case be between 0 and 14.
    /// </summary>
    public byte CurrentSpeed { get; init; }
    /// <summary>
    /// Direction of the locmotive.
    /// </summary>
    public DrivingDirection Direction { get; init; }
    /// <summary>
    /// The function states F0 to F31 of the locomotive.
    /// </summary>
    public bool[] Functions { get; init; }

    // Primary constructor
    public LocoInfo(ushort address, byte db2, byte db3, byte db4, byte db5, byte db6, byte db7, byte? db8, FirmwareVersion? fwVersion)
    {
        Address = address;
        IsBusy = (db2 & 0b00001000) > 0;

        NativeSpeedSteps = (db2 & 0b00000111) switch
        {
            0 => NativeSpeedSteps.Steps14,
            2 => NativeSpeedSteps.Steps28,
            4 => NativeSpeedSteps.Steps128,
            _ => NativeSpeedSteps.Unknown
        };
        if (fwVersion is not null && fwVersion.Version >= Z21FirmwareVersions.V1_43)
        {
            LocomotiveMode = (db2 & 0b00010000) > 0 ? LocoMode.MM : LocoMode.DCC;
            DisplayProtocol = Z21ProtocolName.GetName((LocoMode)LocomotiveMode, NativeSpeedSteps);
            Protocol = Z21ProtocolName.GetProtocol((LocoMode)LocomotiveMode, NativeSpeedSteps);
        }

        Direction = (db3 & 0b10000000) > 0 ? DrivingDirection.Forward : DrivingDirection.Reverse;
        var rawSpeed = (byte)(db3 & 0b01111111);
        var speed = DccSpeedSteps.GetSpeedStep(rawSpeed, (SpeedSteps)NativeSpeedSteps);

        if (LocomotiveMode is LocoMode.MM)
        {
            // Please note!
            // In the MM protocol the speed is still reported in DCC speed steps. Meaning
            // even if the protocol is MM2_14, the 'rawspeed' will have a roco value (0 - 28) and we
            // will have to devide the value by a factor.
            CurrentSpeed = NativeSpeedSteps switch
            {
                NativeSpeedSteps.Steps14 => speed,
                NativeSpeedSteps.Steps28 => (byte)(Math.Floor(speed / (decimal)2)),
                NativeSpeedSteps.Steps128 => (byte)(Math.Floor(speed / (decimal)4.5)),
                _ => 0
            };
            SpeedSteps = NativeSpeedSteps switch
            {
                NativeSpeedSteps.Steps14 => SpeedSteps.Steps14,
                NativeSpeedSteps.Steps28 => SpeedSteps.Steps14,
                NativeSpeedSteps.Steps128 => SpeedSteps.Steps28,
                _ => SpeedSteps.Unknown
            };
        }
        else
        {
            CurrentSpeed = speed;
            SpeedSteps = (SpeedSteps)NativeSpeedSteps;
        }

        Functions = new bool[32];
        Functions[0] = (db4 & 0b00010000) > 0;
        Functions[1] = (db4 & 0b00000001) > 0;
        Functions[2] = (db4 & 0b00000010) > 0;
        Functions[3] = (db4 & 0b00000100) > 0;
        Functions[4] = (db4 & 0b00001000) > 0;

        for (int i = 0; i < 8; i++)
        {
            Functions[5 + i] = (db5 & (1 << i)) > 0;
            Functions[13 + i] = (db6 & (1 << i)) > 0;
            Functions[21 + i] = (db7 & (1 << i)) > 0;
        }

        if (db8.HasValue)
        {
            for (int i = 0; i < 3; i++)
            {
                Functions[29 + i] = (db8.Value & (1 << i)) > 0;
            }
        }
    }

    // --- Copy constructor for updating properties ---
    public LocoInfo(LocoInfo original, LocoMode newLocoMode)
    {
        Address = original.Address;
        IsBusy = original.IsBusy;
        NativeSpeedSteps = original.NativeSpeedSteps;
        CurrentSpeed = original.CurrentSpeed;
        Direction = original.Direction;
        Functions = original.Functions;

        DisplayProtocol = Z21ProtocolName.GetName(newLocoMode, NativeSpeedSteps);
        Protocol = Z21ProtocolName.GetProtocol(newLocoMode, NativeSpeedSteps);

        LocomotiveMode = newLocoMode;

        var speed = DccSpeedSteps.GetSpeedStepReverse(original.CurrentSpeed, original.SpeedSteps);

        if (newLocoMode is LocoMode.MM)
        {
            CurrentSpeed = SpeedSteps switch
            {
                SpeedSteps.Steps14 => DccSpeedSteps.GetSpeedStep(speed, SpeedSteps.Steps14),
                SpeedSteps.Steps28 => DccSpeedSteps.GetSpeedStep(speed, SpeedSteps.Steps14),
                SpeedSteps.Steps128 => (byte)(Math.Floor(speed / (decimal)4.6)),
                _ => 0
            };
            SpeedSteps = NativeSpeedSteps switch
            {
                NativeSpeedSteps.Steps14 => SpeedSteps.Steps14,
                NativeSpeedSteps.Steps28 => SpeedSteps.Steps14,
                NativeSpeedSteps.Steps128 => SpeedSteps.Steps28,
                _ => SpeedSteps.Unknown
            };
        }
        else
        {
            DisplayProtocol = original.DisplayProtocol;
            Protocol = original.Protocol;
            LocomotiveMode = newLocoMode;
            Protocol = original.Protocol;
            DisplayProtocol = original.DisplayProtocol;
            SpeedSteps = (SpeedSteps)original.NativeSpeedSteps;
        }
    }
}
