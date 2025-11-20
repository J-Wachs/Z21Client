namespace Z21Client.Models;

/// <summary>
/// Defines the possible connection states of the Z21 client.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// The client is disconnected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The client is actively trying to connect.
    /// </summary>
    Connecting,

    /// <summary>
    /// The client is successfully connected and communicating.
    /// </summary>
    Connected,

    /// <summary>
    /// The connection to the Z21 has been lost unexpectedly.
    /// </summary>
    Lost
}

/// <summary>
/// Provides data for the <see cref="Application.Interfaces.IZ21Client.ConnectionStateChanged"/> event.
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new connection state.
    /// </summary>
    public ConnectionState State { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="state">The new connection state.</param>
    public ConnectionStateChangedEventArgs(ConnectionState state)
    {
        State = state;
    }
}
