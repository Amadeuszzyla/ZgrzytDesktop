using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class SafeFireAndForgetTests
{
    private static readonly TimeSpan HandlerWaitTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void Run_NullTask_DoesNothing()
    {
        SafeFireAndForget.Run((Task?)null, _ => throw new InvalidOperationException("should not run"));
    }

    [Fact]
    public async Task Run_CompletedTask_DoesNotInvokeErrorHandler()
    {
        var handlerInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        SafeFireAndForget.Run(Task.CompletedTask, _ => handlerInvoked.TrySetResult());

        var completedWithinTimeout = await Task.WhenAny(
            handlerInvoked.Task,
            Task.Delay(HandlerWaitTimeout)) == handlerInvoked.Task;

        Assert.False(completedWithinTimeout);
    }

    [Fact]
    public async Task Run_FaultedTask_InvokesErrorHandler()
    {
        var faulted = Task.FromException(new InvalidOperationException("boom"));

        var captured = await WaitForErrorHandlerAsync(onError =>
            SafeFireAndForget.Run(faulted, onError));

        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal("boom", captured.Message);
    }

    [Fact]
    public async Task Run_AsyncFault_InvokesErrorHandler()
    {
        var captured = await WaitForErrorHandlerAsync(onError =>
            SafeFireAndForget.Run(ThrowAfterDelayAsync(), onError));

        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal("async boom", captured.Message);
    }

    [Fact]
    public async Task Run_ErrorHandlerThrows_DoesNotPropagate()
    {
        var handlerInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        SafeFireAndForget.Run(
            Task.FromException(new InvalidOperationException("boom")),
            _ =>
            {
                handlerInvoked.TrySetResult();
                throw new InvalidOperationException("handler failed");
            });

        await handlerInvoked.Task.WaitAsync(HandlerWaitTimeout);
    }

    [Fact]
    public void Run_FactoryReturningFaultedTask_InvokesErrorHandler()
    {
        Exception? captured = null;

        SafeFireAndForget.Run(
            () => Task.FromException(new InvalidOperationException("factory boom")),
            ex => captured = ex);

        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal("factory boom", captured!.Message);
    }

    [Fact]
    public void Run_FactoryThrowingSynchronously_InvokesErrorHandler()
    {
        Exception? captured = null;

        SafeFireAndForget.Run(
            () => throw new InvalidOperationException("sync boom"),
            ex => captured = ex);

        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal("sync boom", captured!.Message);
    }

    [Fact]
    public void Run_NullFactory_DoesNothing()
    {
        SafeFireAndForget.Run((Func<Task>?)null, _ => throw new InvalidOperationException("should not run"));
    }

    private static async Task<Exception> WaitForErrorHandlerAsync(Action<Action<Exception>> runWithHandler)
    {
        var tcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        runWithHandler(ex => tcs.TrySetResult(ex));
        return await tcs.Task.WaitAsync(HandlerWaitTimeout);
    }

    private static async Task ThrowAfterDelayAsync()
    {
        await Task.Delay(10);
        throw new InvalidOperationException("async boom");
    }
}
