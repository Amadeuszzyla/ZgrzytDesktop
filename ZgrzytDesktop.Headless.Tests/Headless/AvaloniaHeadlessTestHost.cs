using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using ZgrzytDesktop;

namespace ZgrzytDesktop.Headless.Tests.Headless;

internal static class AvaloniaHeadlessTestHost
{
    private static readonly object Sync = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (Sync)
        {
            if (_initialized)
                return;

            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();

            _initialized = true;
        }
    }

    public static void RunOnUiThread(Action action)
    {
        EnsureInitialized();

        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Invoke(action);
    }
}
