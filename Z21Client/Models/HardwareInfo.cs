namespace Z21Client.Models;

/// <summary>
/// Represents the hardware type of a Z21 device.
/// </summary>
public enum HardwareType : uint
{
    /// <summary>
    /// Unknown hardware type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Black Z21 (hardware variant from 2012).
    /// </summary>
    Z21Old = 0x00000200,

    /// <summary>
    /// Black Z21 (hardware variant from 2013).
    /// </summary>
    Z21New = 0x00000201,

    /// <summary>
    /// SmartRail (from 2012).
    /// </summary>
    SmartRail = 0x00000202,

    /// <summary>
    /// White z21 starter set variant (from 2013).
    /// </summary>
    z21Small = 0x00000203,

    /// <summary>
    /// White z21Start starter set variant (from 2016).
    /// </summary>
    z21Start = 0x00000204,

    /// <summary>
    /// 10806 Z21 Single Booster (zLink).
    /// </summary>
    SingleBooster = 0x00000205,

    /// <summary>
    /// 10807 Z21 Dual Booster (zLink).
    /// </summary>
    DualBooster = 0x00000206,

    /// <summary>
    /// 10870 Z21 XL Series (from 2020).
    /// </summary>
    Z21Xl = 0x00000211,

    /// <summary>
    /// 10869 Z21 XL Booster (from 2021, zLink).
    /// </summary>
    XlBooster = 0x00000212,

    /// <summary>
    /// 10836 Z21 SwitchDecoder (zLink).
    /// </summary>
    Z21SwitchDecoder = 0x00000301,

    /// <summary>
    /// 10836 Z21 SignalDecoder (zLink).
    /// </summary>
    Z21SignalDecoder = 0x00000302
}

/// <summary>
/// Represents the hardware information and firmware version of a Z21 device.
/// </summary>
/// <param name="HwType">The type of the hardware.</param>
/// <param name="FwVersion">The firmware version.</param>
public sealed record HardwareInfo(HardwareType HwType, FirmwareVersion FwVersion);

/// <summary>
/// Represents the serial number of a Z21 device.
/// </summary>
/// <param name="Value">The 32-bit serial number.</param>
public sealed record SerialNumber(uint Value);
