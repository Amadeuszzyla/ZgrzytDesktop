using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
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
        Action<bool, int>? onAutoLogoutSettingsChanged = null)
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
            onAutoLogoutSettingsChanged)
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
        Action<bool, int>? onAutoLogoutSettingsChanged = null)
    {
        CurrentUser = currentUser;
        _authService = authService;
        _ticketService = ticketService;
        _settingsService = settingsService;
        _ticketCacheService = ticketCacheService;
        _auditLogService = auditLogService;
        _userAdminService = userAdminService;
        _onLogoutRequested = onLogoutRequested;
        _onAutoLogoutSettingsChanged = onAutoLogoutSettingsChanged;

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

        InitializeDashboardPanels();
        InitializeRequestAccountPanel();
        InitializeTicketsPanel();
        InitializeTicketDetailsPanel();
        InitializeAdminPanel();
        InitializeCommands();
        InitializeTicketCollections();
        SettingsPanel.ApplyBootstrapFromSettings();
        AppStrings.ApplyCulture(SettingsPanel.SelectedUiCulture);
        SettingsService.ApplyThemeMode(SettingsPanelViewModel.LightThemeMode);
        ApplyDefaultSortAndAdminFilter();
        TicketsPanel.ConfigureQueueViewsForRole(CanManageTickets);
        PollingStatusMessage = TicketsPanel.AutoRefreshStatusText;
        if (bootstrap.EnableTimers)
            InitializeTimers();
        TicketsPanel.BootstrapPaginationSelection();

        if (bootstrap.ShowLoginToast)
            ShowToastKey("Toast_LoggedIn", ToastTypes.Info, CurrentUser.Name);

        if (bootstrap.RunInitialLoad)
            SafeFireAndForget.Run(LoadTicketsAsync());
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
