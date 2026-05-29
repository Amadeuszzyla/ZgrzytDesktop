using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Tests.Helpers;

public class SafeFireAndForgetTests
{
    [Fact]
    public void Run_NullTask_DoesNothing()
    {
        SafeFireAndForget.Run((Task?)null, _ => throw new InvalidOperationException("should not run"));
    }

    [Fact]
    public void Run_CompletedTask_DoesNotInvokeErrorHandler()
    {
        var invoked = false;

        SafeFireAndForget.Run(Task.CompletedTask, _ => invoked = true);

        Assert.False(invoked);
    }

    [Fact]
    public async Task Run_FaultedTask_InvokesErrorHandler()
    {
        Exception? captured = null;
        var faulted = Task.FromException(new InvalidOperationException("boom"));

        SafeFireAndForget.Run(faulted, ex => captured = ex);

        await Task.Delay(50);

        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal("boom", captured!.Message);
    }

    [Fact]
    public async Task Run_AsyncFault_InvokesErrorHandler()
    {
        Exception? captured = null;

        SafeFireAndForget.Run(ThrowAfterDelayAsync(), ex => captured = ex);

        await Task.Delay(100);

        Assert.IsType<InvalidOperationException>(captured);
        Assert.Equal("async boom", captured!.Message);
    }

    [Fact]
    public async Task Run_ErrorHandlerThrows_DoesNotPropagate()
    {
        SafeFireAndForget.Run(
            Task.FromException(new InvalidOperationException("boom")),
            _ => throw new InvalidOperationException("handler failed"));

        await Task.Delay(50);
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

    private static async Task ThrowAfterDelayAsync()
    {
        await Task.Delay(10);
        throw new InvalidOperationException("async boom");
    }
}
