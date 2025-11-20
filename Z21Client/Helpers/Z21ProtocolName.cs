using Z21Client.Models;

namespace Z21Client.Helpers;

/// <summary>
/// Convert Z21 protocol information to a human-readable name, taking
/// into account whether the protocol is Märklin Motorola and the number of speed steps.
/// </summary>
public static class Z21ProtocolName
{
    public static string GetName(LocoMode locoMode, NativeSpeedSteps speedSteps)
    {
        string protocolName;
        if (locoMode is LocoMode.MM)
        {
            protocolName = speedSteps switch
            {
                NativeSpeedSteps.Steps14 => "MM1/14",
                NativeSpeedSteps.Steps28 => "MM2/14",
                NativeSpeedSteps.Steps128 => "MM2/28",
                _ => "MM"
            };
        }
        else
        {
            protocolName = speedSteps switch
            {
                NativeSpeedSteps.Steps14 => "DCC14",
                NativeSpeedSteps.Steps28 => "DCC28",
                NativeSpeedSteps.Steps128 => "DCC128",
                _ => "DCC"
            };
        }

        return protocolName;
    }

    public static LocomotiveProtocol GetProtocol(LocoMode locoMode, NativeSpeedSteps speedSteps)
    {
        LocomotiveProtocol protocol;
        if (locoMode is LocoMode.MM)
        {
            protocol = speedSteps switch
            {
                NativeSpeedSteps.Steps14 => LocomotiveProtocol.MM1_14,
                NativeSpeedSteps.Steps28 => LocomotiveProtocol.MM2_14,
                NativeSpeedSteps.Steps128 => LocomotiveProtocol.MM2_28,
                _ => LocomotiveProtocol.MM1_14
            };
        }
        else
        {
            protocol = speedSteps switch
            {
                NativeSpeedSteps.Steps14 => LocomotiveProtocol.DCC14,
                NativeSpeedSteps.Steps28 => LocomotiveProtocol.DCC28,
                NativeSpeedSteps.Steps128 => LocomotiveProtocol.DCC128,
                _ => LocomotiveProtocol.DCC14
            };
        }

        return protocol;

    }
}
