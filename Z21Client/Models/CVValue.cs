namespace Z21Client.Models;

/// <summary>
/// Hold the CV and value.
/// </summary>
/// <param name="Cv"></param>
/// <param name="Value"></param>
public record CVValue(ushort Cv, byte Value);
