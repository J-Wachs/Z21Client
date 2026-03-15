using Z21Client.Models;

namespace Z21Client.Helpers;

/// <summary>
/// Helper class to manage asynchronous operations that rely on events, such as waiting for a response after sending a command.
/// </summary>
public static class AsyncEventHelper
{
    /// <summary>
    /// METHOD 1: The simple one (Used for POM, where we only wait for a response or timeout) 
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="triggerAction"></param>
    /// <param name="subscribe"></param>
    /// <param name="unsubscribe"></param>
    /// <param name="timeoutMs"></param>
    /// <returns></returns>
    public static async Task<(bool Success, TResult? Result)> ExecuteAndWaitAsync<TResult>(
        Func<Task> triggerAction,
        Action<EventHandler<TResult>> subscribe,
        Action<EventHandler<TResult>> unsubscribe,
        int timeoutMs = 3000)
    {
        var tcs = new TaskCompletionSource<TResult>();
        void Handler(object? s, TResult e) => tcs.TrySetResult(e);

        try
        {
            subscribe(Handler);
            await triggerAction();

            var timeoutTask = Task.Delay(timeoutMs);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == tcs.Task)
                return (true, await tcs.Task);
            else
                return (false, default);
        }
        finally
        {
            unsubscribe(Handler);
        }
    }

    /// <summary>
    /// METHOD 2: The advanced one (Used for ProgTrack, where we also listen for NACK)
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TFailure"></typeparam>
    /// <param name="triggerAction"></param>
    /// <param name="subscribeSuccess"></param>
    /// <param name="unsubscribeSuccess"></param>
    /// <param name="subscribeFailure"></param>
    /// <param name="unsubscribeFailure"></param>
    /// <param name="timeoutMs"></param>
    /// <returns></returns>
    public static async Task<(AsyncStatus Status, TSuccess? Result, TFailure? Error)> ExecuteAndWaitWithFailureAsync<TSuccess, TFailure>(
        Func<Task> triggerAction,
        Action<EventHandler<TSuccess>> subscribeSuccess,
        Action<EventHandler<TSuccess>> unsubscribeSuccess,
        Action<EventHandler<TFailure>> subscribeFailure,
        Action<EventHandler<TFailure>> unsubscribeFailure,
        int timeoutMs = 3000)
    {
        var tcsSuccess = new TaskCompletionSource<TSuccess>();
        var tcsFailure = new TaskCompletionSource<TFailure>();

        void SuccessHandler(object? s, TSuccess e) => tcsSuccess.TrySetResult(e);
        void FailureHandler(object? s, TFailure e) => tcsFailure.TrySetResult(e);

        try
        {
            subscribeSuccess(SuccessHandler);
            subscribeFailure(FailureHandler);
            await triggerAction();

            var timeoutTask = Task.Delay(timeoutMs);

            // Race between 3 participants: Success, Failure, or the stopwatch
            var completedTask = await Task.WhenAny(tcsSuccess.Task, tcsFailure.Task, timeoutTask);

            if (completedTask == tcsSuccess.Task)
            {
                return (AsyncStatus.Success, await tcsSuccess.Task, default);
            }
            else if (completedTask == tcsFailure.Task)
            {
                return (AsyncStatus.FailureEvent, default, await tcsFailure.Task);
            }
            else
            {
                return (AsyncStatus.Timeout, default, default);
            }
        }
        finally
        {
            unsubscribeSuccess(SuccessHandler);
            unsubscribeFailure(FailureHandler);
        }
    }
}
