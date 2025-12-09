namespace Z21Client.Models;

/// <summary>
/// Represents the serial number of a Z21 device.
/// </summary>
/// <param name="Value">The 32-bit serial number.</param>
public sealed record SerialNumber(uint Value);
