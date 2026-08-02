using Z21Client.Helpers;
using Z21Client.Models;

namespace Z21ClientTest.Helpers;

public class AsyncEventHelperTests
{
    [Fact]
    public async Task ExecuteAndWaitAsync_EventRaisedBeforeTimeout_ReturnsSuccessAndResult()
    {
        EventHandler<int>? handler = null;
        Action<EventHandler<int>> subscribe = h => handler = h;
        Action<EventHandler<int>> unsubscribe = h => handler -= h;

        Task TriggerAction()
        {
            handler?.Invoke(this, 42);
            return Task.CompletedTask;
        }

        (bool success, int result) = await AsyncEventHelper.ExecuteAndWaitAsync<int>(TriggerAction, subscribe, unsubscribe, 5000);

        Assert.True(success);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAndWaitAsync_EventNotRaised_ReturnsFalseAndDefaultResult()
    {
        Action<EventHandler<int>> subscribe = h => { };
        Action<EventHandler<int>> unsubscribe = h => { };

        Task TriggerAction() => Task.Delay(10);

        (bool success, int result) = await AsyncEventHelper.ExecuteAndWaitAsync<int>(TriggerAction, subscribe, unsubscribe, 10);

        Assert.False(success);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAndWaitWithFailureAsync_SuccessEventRaised_ReturnsSuccessAndResult()
    {
        EventHandler<int>? successHandler = null;
        EventHandler<string>? failureHandler = null;

        Action<EventHandler<int>> subscribeSuccess = h => successHandler = h;
        Action<EventHandler<int>> unsubscribeSuccess = h => successHandler -= h;
        Action<EventHandler<string>> subscribeFailure = h => failureHandler = h;
        Action<EventHandler<string>> unsubscribeFailure = h => failureHandler -= h;

        Task TriggerAction()
        {
            successHandler?.Invoke(this, 42);
            return Task.CompletedTask;
        }

        (AsyncStatus status, int? result, string? error) = await AsyncEventHelper.ExecuteAndWaitWithFailureAsync<int, string>(
            TriggerAction, subscribeSuccess, unsubscribeSuccess, subscribeFailure, unsubscribeFailure, 5000);

        Assert.Equal(AsyncStatus.Success, status);
        Assert.Equal(42, result);
        Assert.Null(error);
    }

    [Fact]
    public async Task ExecuteAndWaitWithFailureAsync_FailureEventRaised_ReturnsFailureEventAndError()
    {
        EventHandler<int>? successHandler = null;
        EventHandler<string>? failureHandler = null;

        Action<EventHandler<int>> subscribeSuccess = h => successHandler = h;
        Action<EventHandler<int>> unsubscribeSuccess = h => successHandler -= h;
        Action<EventHandler<string>> subscribeFailure = h => failureHandler = h;
        Action<EventHandler<string>> unsubscribeFailure = h => failureHandler -= h;

        Task TriggerAction()
        {
            failureHandler?.Invoke(this, "NACK");
            return Task.CompletedTask;
        }

        (AsyncStatus status, int? result, string? error) = await AsyncEventHelper.ExecuteAndWaitWithFailureAsync<int, string>(
            TriggerAction, subscribeSuccess, unsubscribeSuccess, subscribeFailure, unsubscribeFailure, 5000);

        Assert.Equal(AsyncStatus.FailureEvent, status);
        Assert.Equal(0, result);
        Assert.Equal("NACK", error);
    }

    [Fact]
    public async Task ExecuteAndWaitWithFailureAsync_NoEventRaised_ReturnsTimeout()
    {
        Action<EventHandler<int>> subscribeSuccess = h => { };
        Action<EventHandler<int>> unsubscribeSuccess = h => { };
        Action<EventHandler<string>> subscribeFailure = h => { };
        Action<EventHandler<string>> unsubscribeFailure = h => { };

        Task TriggerAction() => Task.Delay(10);

        (AsyncStatus status, int? result, string? error) = await AsyncEventHelper.ExecuteAndWaitWithFailureAsync<int, string>(
            TriggerAction, subscribeSuccess, unsubscribeSuccess, subscribeFailure, unsubscribeFailure, 10);

        Assert.Equal(AsyncStatus.Timeout, status);
        Assert.Equal(0, result);
        Assert.Null(error);
    }
}
