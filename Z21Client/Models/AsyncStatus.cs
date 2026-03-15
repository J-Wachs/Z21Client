namespace Z21Client.Models;

/// <summary>
/// Status of an asynchronous operation that waits for events, such as waiting for a response after sending a command. This is
/// used in the advanced method of AsyncEventHelper, where we listen for both success and failure events.
/// </summary>
public enum AsyncStatus
{
    Success,      // We received the response we were waiting for
    FailureEvent, // We received an error event (e.g. NACK)
    Timeout       // Time ran out
}
