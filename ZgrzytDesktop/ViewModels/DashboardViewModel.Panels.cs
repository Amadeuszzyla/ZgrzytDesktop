using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public ObservableCollection<string> UiCultures => SettingsPanel.UiCultures;

    public string LblSettingsTitle => SettingsPanel.LblSettingsTitle;

    public string LblSettingsSubtitle => SettingsPanel.LblSettingsSubtitle;

    public string LblSettingsLanguage => SettingsPanel.LblSettingsLanguage;

    public string LblSettingsSave => SettingsPanel.LblSettingsSave;

    public string LblSettingsRefreshSession => SettingsPanel.LblSettingsRefreshSession;

    public string LblStatsTitle => StatisticsPanel.LblStatsTitle;

    public string LblStatsLoadAll => StatisticsPanel.LblStatsLoadAll;

    public ObservableCollection<Models.AuditLogEntry> SettingsAuditLogEntries => AuditPanel.AuditLogEntries;

    public bool HasNoSettingsAuditLogEntries => AuditPanel.HasNoAuditLogEntries;

    public SettingsPanelViewModel SettingsPanel { get; private set; } = null!;

    public StatisticsPanelViewModel StatisticsPanel { get; private set; } = null!;

    public AuditPanelViewModel AuditPanel { get; private set; } = null!;

    public string SelectedThemeMode => SettingsPanel.SelectedThemeMode;

    public string SelectedUiCulture
    {
        get => SettingsPanel.SelectedUiCulture;
        set => SettingsPanel.SelectedUiCulture = value;
    }

    public string SettingsStatusMessage
    {
        get => SettingsPanel.SettingsStatusMessage;
        set => SettingsPanel.SettingsStatusMessage = value;
    }

    public IAsyncRelayCommand SaveSettingsCommand => SettingsPanel.SaveSettingsCommand;

    public IAsyncRelayCommand RefreshSessionCommand => SettingsPanel.RefreshSessionCommand;

    public IAsyncRelayCommand LoadAuditLogsCommand => AuditPanel.LoadAuditLogsCommand;

    public IAsyncRelayCommand LoadAllPagesStatisticsCommand => StatisticsPanel.LoadAllPagesStatisticsCommand;

    public int StatsTotalTickets => StatisticsPanel.StatsTotalTickets;

    public int StatsNewTickets => StatisticsPanel.StatsNewTickets;

    public int StatsInProgressTickets => StatisticsPanel.StatsInProgressTickets;

    public int StatsClosedTickets => StatisticsPanel.StatsClosedTickets;

    public int StatsLowPriorityTickets => StatisticsPanel.StatsLowPriorityTickets;

    public int StatsMediumPriorityTickets => StatisticsPanel.StatsMediumPriorityTickets;

    public int StatsHighPriorityTickets => StatisticsPanel.StatsHighPriorityTickets;

    public int StatsAssignedTickets => StatisticsPanel.StatsAssignedTickets;

    public int StatsUnassignedTickets => StatisticsPanel.StatsUnassignedTickets;

    public double StatsStatusChartMaximum => StatisticsPanel.StatsStatusChartMaximum;

    public double StatsPriorityChartMaximum => StatisticsPanel.StatsPriorityChartMaximum;

    public double StatsAssignmentChartMaximum => StatisticsPanel.StatsAssignmentChartMaximum;

    public string StatsScopeMessage => StatisticsPanel.StatsScopeMessage;

    public bool IsLoadingAllStatistics => StatisticsPanel.IsLoadingAllStatistics;

    private DashboardVmBridge CreateModuleBridge() =>
        new()
        {
            ExecuteApiAsyncCore = ExecuteApiAsync,
            ShowToastKey = ShowToastKey,
            ShowToastRaw = ShowToast,
            LogAuditAsync = LogAuditAsync,
            GetIsOffline = () => IsOffline,
            SetIsOffline = value => IsOffline = value,
            NotifyLocalization = NotifyLocalizationProperties,
            GetCurrentSection = () => CurrentSection
        };

    private void InitializeDashboardPanels()
    {
        var bridge = CreateModuleBridge();

        AuditPanel = new AuditPanelViewModel(_auditLogService);
        SettingsPanel = new SettingsPanelViewModel(
            _settingsService,
            _authService,
            bridge,
            () => AuditPanel.RefreshAsync(),
            _onAutoLogoutSettingsChanged);
        StatisticsPanel = new StatisticsPanelViewModel(
            _ticketService,
            bridge,
            () => TicketsPanel.IsNotLoading);
    }
}
