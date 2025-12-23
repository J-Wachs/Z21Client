namespace Z21Client.Models;

public enum LocoMode
{
    DCC = 0,
    MM = 1
}

public sealed record LocoModeStatus(ushort address, LocoMode mode)
{
    public ushort Address { get; } = address;
    public LocoMode Mode { get; } = mode;
}
