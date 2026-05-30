using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public SettingsPanelViewModel SettingsPanel { get; private set; } = null!;

    public StatisticsPanelViewModel StatisticsPanel { get; private set; } = null!;

    public AuditPanelViewModel AuditPanel { get; private set; } = null!;

    private IDashboardContext CreateDashboardContext() =>
        new DashboardContext(
            executeApiAsync: ExecuteApiAsync,
            showToastKey: ShowToastKey,
            showToastRaw: ShowToast,
            logAuditAsync: LogAuditAsync,
            getIsOffline: () => IsOffline,
            setIsOffline: value => IsOffline = value,
            notifyLocalization: NotifyLocalizationProperties,
            getCurrentSection: () => CurrentSection);

    private void InitializeDashboardPanels()
    {
        _dashboardContext = CreateDashboardContext();

        AuditPanel = new AuditPanelViewModel(_auditLogService);
        SettingsPanel = new SettingsPanelViewModel(
            _settingsService,
            _authService,
            _dashboardContext,
            () => AuditPanel.RefreshAsync(),
            _onAutoLogoutSettingsChanged);
        StatisticsPanel = new StatisticsPanelViewModel(
            _ticketService,
            _dashboardContext,
            () => TicketsPanel.IsNotLoading);
    }
}
