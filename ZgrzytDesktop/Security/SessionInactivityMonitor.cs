using System;
using System.Threading;
using System.Threading.Tasks;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Security;

public sealed class SessionInactivityMonitor : IDisposable
{
    private readonly Func<DateTime> _utcNow;
    private readonly object _sync = new();
    private DateTime _lastActivityUtc;
    private bool _enabled = true;
    private int _timeoutMinutes = 30;
    private Timer? _timer;
    private Func<Task>? _onExpired;
    private bool _running;

    public SessionInactivityMonitor(Func<DateTime>? utcNow = null)
    {
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public void Configure(bool enabled, int timeoutMinutes)
    {
        lock (_sync)
        {
            _enabled = enabled;
            _timeoutMinutes = NormalizeTimeout(timeoutMinutes);
        }
    }

    public static int NormalizeTimeout(int timeoutMinutes) =>
        timeoutMinutes switch
        {
            15 => 15,
            60 => 60,
            _ => 30
        };

    public void RecordActivity()
    {
        lock (_sync)
            _lastActivityUtc = _utcNow();
    }

    public void Start(Func<Task> onExpired)
    {
        Stop();

        lock (_sync)
        {
            _onExpired = onExpired;
            _lastActivityUtc = _utcNow();
            _running = true;
            _timer = new Timer(_ => CheckExpiration(), null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _timer?.Dispose();
            _timer = null;
            _running = false;
            _onExpired = null;
        }
    }

    public bool IsExpiredForTests()
    {
        lock (_sync)
        {
            if (!_running || !_enabled)
                return false;

            return (_utcNow() - _lastActivityUtc).TotalMinutes >= _timeoutMinutes;
        }
    }

    internal bool CheckExpirationForTests()
    {
        CheckExpiration();
        return !IsRunningForTests();
    }

    internal bool IsRunningForTests()
    {
        lock (_sync)
            return _running;
    }

    private void CheckExpiration()
    {
        Func<Task>? callback = null;

        lock (_sync)
        {
            if (!_running || !_enabled || _onExpired is null)
                return;

            if ((_utcNow() - _lastActivityUtc).TotalMinutes < _timeoutMinutes)
                return;

            callback = _onExpired;
            _running = false;
            _timer?.Dispose();
            _timer = null;
        }

        SafeFireAndForget.Run(callback);
    }

    public void Dispose() => Stop();
}
