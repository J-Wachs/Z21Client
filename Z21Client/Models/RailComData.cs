namespace Z21Client.Models;

/// <summary>
/// Represents RailCom data for a specific locomotive, received from the Z21.
/// </summary>
public sealed record RailComData
{
    /// <summary>
    /// Address of the detected decoder.
    /// </summary>
    public ushort LocoAddress { get; }

    /// <summary>
    /// Receive counter in the Z21.
    /// </summary>
    public uint ReceiveCounter { get; }

    /// <summary>
    /// Receive error counter in the Z21.
    /// </summary>
    public ushort ErrorCounter { get; }

    /// <summary>
    /// Raw byte containing the option flags.
    /// </summary>
    public byte Options { get; }

    /// <summary>
    /// Gets a value indicating whether Speed 1 data is available.
    /// </summary>
    public bool HasSpeed1 => (Options & 0x01) != 0;

    /// <summary>
    /// Gets a value indicating whether Speed 2 data is available.
    /// </summary>
    public bool HasSpeed2 => (Options & 0x02) != 0;

    /// <summary>
    /// Gets a value indicating whether Quality of Service (QoS) data is available.
    /// </summary>
    public bool HasQoS => (Options & 0x04) != 0;

    /// <summary>
    /// The reported speed (Speed 1 or 2).
    /// </summary>
    public byte Speed { get; }

    /// <summary>
    /// The reported Quality of Service (QoS).
    /// </summary>
    public byte QoS { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RailComData"/> class from a raw data span.
    /// </summary>
    /// <param name="data">A read-only span of bytes containing the RailComData structure.</param>
    public RailComData(ReadOnlySpan<byte> data)
    {
        LocoAddress = BitConverter.ToUInt16(data[..2]);
        ReceiveCounter = BitConverter.ToUInt32(data.Slice(2, 4));
        ErrorCounter = BitConverter.ToUInt16(data.Slice(6, 2));
        // Byte 8 is reserved
        Options = data[9];
        Speed = data[10];
        QoS = data[11];
        // Byte 12 is reserved
    }
}
