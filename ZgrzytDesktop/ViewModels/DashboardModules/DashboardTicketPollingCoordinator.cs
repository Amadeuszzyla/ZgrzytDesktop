using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Background ticket list auto-refresh timer for the dashboard shell.
/// </summary>
public sealed class DashboardTicketPollingCoordinator : IDisposable
{
    private readonly Func<Task> _onTick;
    private DispatcherTimer? _timer;

    public DashboardTicketPollingCoordinator(Func<Task> onTick)
    {
        _onTick = onTick;
    }

    public bool IsActive => _timer?.IsEnabled ?? false;

    public void Start(int intervalSeconds)
    {
        if (_timer is not null)
            return;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(intervalSeconds)
        };

        _timer.Tick += (_, _) => SafeFireAndForget.Run(_onTick());
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer is null)
            return;

        _timer.IsEnabled = false;
        _timer.Stop();
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer = null;
    }
}
