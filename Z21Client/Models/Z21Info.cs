namespace Z21Client.Models;

/// <summary>
/// Response from Z21 GetInfo command.
/// </summary>
/// <param name="IpAddress"></param>
/// <param name="HardwareInfo"></param>
public record Z21Info(string IpAddress, HardwareInfo HardwareInfo)
{
    // Text to display e.g. in a dropdown
    public override string ToString()
    {
        return $"{IpAddress} ({HardwareInfo.HwType}, FW: {HardwareInfo.FwVersion})";
    }
}
