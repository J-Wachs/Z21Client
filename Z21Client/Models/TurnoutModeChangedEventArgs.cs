namespace Z21Client.Models;

public enum TurnoutMode
{
    DCC = 0,
    MM = 1
}

/// <summary>
/// Represents event data for a received turnout mode.
/// </summary>
public sealed class TurnoutModeChangedEventArgs : EventArgs
{
    /// <summary>
    /// The address of the turnout.
    /// </summary>
    public ushort Address { get; }

    /// <summary>
    /// The protocol mode of the turnout (DCC or MM).
    /// </summary>
    public TurnoutMode Mode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TurnoutModeChangedEventArgs"/> class.
    /// </summary>
    /// <param name="address">The address of the turnout.</param>
    /// <param name="mode">The protocol mode of the turnout.</param>
    public TurnoutModeChangedEventArgs(ushort address, TurnoutMode mode)
    {
        Address = address;
        Mode = mode;
    }
}
