namespace Z21Client.Models;

/// <summary>
/// Represents the state of a turnout (switch).
/// </summary>
public enum TurnoutState
{
    /// <summary>
    /// The state is not yet known or reported.
    /// </summary>
    NotSwitched = 0,

    /// <summary>
    /// The turnout is in the primary position (P=0).
    /// </summary>
    Position1 = 1,

    /// <summary>
    /// The turnout is in the secondary position (P=1).
    /// </summary>
    Position2 = 2,

    /// <summary>
    /// The reported state is invalid.
    /// </summary>
    Invalid = 3
}

/// <summary>
/// Represents information about a specific turnout received from the Z21.
/// </summary>
/// <param name="Address">The address of the turnout.</param>
/// <param name="State">The current state of the turnout.</param>
public sealed record TurnoutInfo(ushort Address, TurnoutState State);
