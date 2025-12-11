using Z21Client.Models;

public record Z21Info(string IpAddress, HardwareInfo HardwareInfo)
{
    // Text to display e.g. in a dropdown
    public override string ToString()
    {
        return $"{IpAddress} ({HardwareInfo.HwType}, FW: {HardwareInfo.FwVersion})";
    }
}
