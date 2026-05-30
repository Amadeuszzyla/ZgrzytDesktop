using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Diagnostics;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ITicketService _ticketService;
    private readonly ISettingsService _settingsService;
    private readonly ILocalTicketCacheService _ticketCacheService;
    private readonly ILocalAuditLogService _auditLogService;
    private readonly ILocalDiagnosticLogService? _diagnosticLogService;
    private readonly IUserAdminService _userAdminService;
    private readonly Func<Task> _onLogoutRequested;
    private readonly Action<bool, int>? _onAutoLogoutSettingsChanged;
    private IDashboardContext _dashboardContext = null!;
    private DashboardTicketPollingCoordinator? _ticketPolling;

    public User CurrentUser { get; }

    public DashboardViewModel(
        User currentUser,
        IAuthService authService,
        ITicketService ticketService,
        ISettingsService settingsService,
        ILocalTicketCacheService ticketCacheService,
        ILocalAuditLogService auditLogService,
        IUserAdminService userAdminService,
        Func<Task> onLogoutRequested,
        Action<bool, int>? onAutoLogoutSettingsChanged = null,
        ILocalDiagnosticLogService? diagnosticLogService = null)
        : this(
            currentUser,
            authService,
            ticketService,
            settingsService,
            ticketCacheService,
            auditLogService,
            userAdminService,
            onLogoutRequested,
            BootstrapOptions.Production,
            onAutoLogoutSettingsChanged,
            diagnosticLogService)
    {
    }

    internal DashboardViewModel(
        User currentUser,
        IAuthService authService,
        ITicketService ticketService,
        ISettingsService settingsService,
        ILocalTicketCacheService ticketCacheService,
        ILocalAuditLogService auditLogService,
        IUserAdminService userAdminService,
        Func<Task> onLogoutRequested,
        BootstrapOptions bootstrap,
        Action<bool, int>? onAutoLogoutSettingsChanged = null,
        ILocalDiagnosticLogService? diagnosticLogService = null)
    {
        CurrentUser = currentUser;
        _authService = authService;
        _ticketService = ticketService;
        _settingsService = settingsService;
        _ticketCacheService = ticketCacheService;
        _auditLogService = auditLogService;
        _diagnosticLogService = diagnosticLogService;
        _userAdminService = userAdminService;
        _onLogoutRequested = onLogoutRequested;
        _onAutoLogoutSettingsChanged = onAutoLogoutSettingsChanged;

        using (StartupPerf.Measure("DashboardViewModel ctor"))
        {
            _toast.PropertyChanged += (_, e) =>
            {
                var forwarded = e.PropertyName switch
                {
                    nameof(DashboardToastViewModel.Message) => nameof(ToastMessage),
                    nameof(DashboardToastViewModel.IsVisible) => nameof(IsToastVisible),
                    nameof(DashboardToastViewModel.Background) => nameof(ToastBackground),
                    nameof(DashboardToastViewModel.Foreground) => nameof(ToastForeground),
                    _ => null
                };

                if (forwarded is not null)
                    OnPropertyChanged(forwarded);
            };

            using (StartupPerf.Measure("Initialize API coordinator"))
                InitializeApiCoordinator();

            using (StartupPerf.Measure("Initialize dashboard panels"))
                InitializeDashboardPanels();

            using (StartupPerf.Measure("Initialize request account panel"))
                InitializeRequestAccountPanel();

            using (StartupPerf.Measure("Initialize tickets panel"))
                InitializeTicketsPanel();

            using (StartupPerf.Measure("Initialize ticket details panel"))
                InitializeTicketDetailsPanel();

            using (StartupPerf.Measure("Initialize admin panel"))
                InitializeAdminPanel();

            using (StartupPerf.Measure("Initialize navigation"))
                InitializeNavigation();

            using (StartupPerf.Measure("Initialize commands"))
                InitializeCommands();

            using (StartupPerf.Measure("Initialize ticket collections"))
                InitializeTicketCollections();

            using (StartupPerf.Measure("Apply settings bootstrap"))
                SettingsPanel.ApplyBootstrapFromSettings();

            AppStrings.ApplyCulture(SettingsPanel.SelectedUiCulture);
            SettingsService.ApplyThemeMode(SettingsPanelViewModel.LightThemeMode);
            ApplyDefaultSortAndAdminFilter();
            TicketsPanel.ConfigureQueueViewsForRole(CanManageTickets);
            PollingStatusMessage = TicketsPanel.AutoRefreshStatusText;

            if (bootstrap.EnableTimers)
            {
                using (StartupPerf.Measure("Initialize timers"))
                    InitializeTimers();
            }

            TicketsPanel.BootstrapPaginationSelection();
        }

        if (bootstrap.ShowLoginToast)
            ShowToastKey("Toast_LoggedIn", ToastTypes.Info, CurrentUser.Name);

        if (bootstrap.RunInitialLoad)
            SafeFireAndForget.Run(RunInitialTicketsLoadAsync());
    }

    private async Task RunInitialTicketsLoadAsync()
    {
        using (StartupPerf.Measure("LoadTicketsAsync (initial)"))
            await LoadTicketsAsync();

        StartupPerf.NotifyInitialTicketsLoadFinished();
    }

    private void ApplyDefaultSortAndAdminFilter()
    {
        TicketsPanel.ApplyDefaultSort();
        AdminPanel.ApplyDefaultFilter();
    }

    internal bool IsTicketPollingActive => _ticketPolling?.IsActive ?? false;
    internal async Task RunAutoRefreshForTestsAsync() => await AutoRefreshTicketsAsync();

    private void InitializeTimers()
    {
        EnsureToastHideTimer();

        if (_ticketPolling is not null)
            return;

        _ticketPolling = new DashboardTicketPollingCoordinator(AutoRefreshTicketsAsync);
        _ticketPolling.Start(TicketsPanelViewModel.AutoRefreshIntervalSeconds);
    }

}
