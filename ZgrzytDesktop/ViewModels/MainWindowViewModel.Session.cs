using System.Threading.Tasks;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Security;

namespace ZgrzytDesktop.ViewModels;

public partial class MainWindowViewModel
{
    private readonly SessionInactivityMonitor _sessionInactivityMonitor = new();

    internal SessionInactivityMonitor SessionInactivityMonitorForTests => _sessionInactivityMonitor;

    public void RecordUserActivity() => _sessionInactivityMonitor.RecordActivity();

    private void ApplyAutoLogoutSettings(bool enabled, int timeoutMinutes)
    {
        _sessionInactivityMonitor.Configure(enabled, timeoutMinutes);

        if (CurrentViewModel is DashboardViewModel)
            RestartInactivityMonitor();
    }

    private void RestartInactivityMonitor()
    {
        _sessionInactivityMonitor.Stop();

        var settings = _settingsService.LoadSync();
        _sessionInactivityMonitor.Configure(settings.AutoLogoutEnabled, settings.AutoLogoutTimeoutMinutes);

        if (!settings.AutoLogoutEnabled)
            return;

        _sessionInactivityMonitor.RecordActivity();
        _sessionInactivityMonitor.Start(LogoutDueToInactivityAsync);
    }

    private Task LogoutDueToInactivityAsync() =>
        LogoutAsync(AppStrings.Get("Security_SessionExpiredInactivity"));
}
