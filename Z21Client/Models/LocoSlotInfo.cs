namespace Z21Client.Models;

/// <summary>
/// Locomotive information per slot in Z21
/// </summary>
/// <param name="SlotNumber"></param>
/// <param name="LocoInfo"></param>
public sealed record LocoSlotInfo(ushort SlotNumber, LocoInfo LocoInfo);
