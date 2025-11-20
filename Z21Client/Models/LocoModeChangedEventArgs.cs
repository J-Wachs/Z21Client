namespace Z21Client.Models;

public enum LocoMode
{
    DCC = 0,
    MM = 1
}

public sealed class LocoModeChangedEventArgs(ushort address, LocoMode mode) : EventArgs
{
    public ushort Address { get; } = address;
    public LocoMode Mode { get; } = mode;
}
