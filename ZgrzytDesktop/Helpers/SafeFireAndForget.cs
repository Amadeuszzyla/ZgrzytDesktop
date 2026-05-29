using System;
using System.Threading.Tasks;

namespace ZgrzytDesktop.Helpers;

/// <summary>
/// Observes fire-and-forget <see cref="Task"/> instances so unhandled exceptions
/// do not tear down the UI process.
/// </summary>
public static class SafeFireAndForget
{
    public static void Run(Task? task, Action<Exception>? onError = null)
    {
        if (task is null)
            return;

        if (task.IsCompleted)
        {
            ObserveCompleted(task, onError);
            return;
        }

        _ = ObserveAsync(task, onError);
    }

    public static void Run(Func<Task>? taskFactory, Action<Exception>? onError = null)
    {
        if (taskFactory is null)
            return;

        try
        {
            Run(taskFactory(), onError);
        }
        catch (Exception ex)
        {
            InvokeErrorHandler(ex, onError);
        }
    }

    private static async Task ObserveAsync(Task task, Action<Exception>? onError)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            InvokeErrorHandler(ex, onError);
        }
    }

    private static void ObserveCompleted(Task task, Action<Exception>? onError)
    {
        if (!task.IsFaulted)
            return;

        var exception = task.Exception?.GetBaseException() ?? task.Exception;

        if (exception is not null)
            InvokeErrorHandler(exception, onError);
    }

    private static void InvokeErrorHandler(Exception exception, Action<Exception>? onError)
    {
        if (onError is null)
            return;

        try
        {
            onError(exception);
        }
        catch
        {
            // Error handler must not crash the application.
        }
    }
}
