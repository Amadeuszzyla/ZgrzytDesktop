using System;

using System.Collections.Generic;

using System.Threading.Tasks;

using Avalonia.Threading;

using ZgrzytDesktop.Cache;

using ZgrzytDesktop.Constants;

using ZgrzytDesktop.Helpers;

using ZgrzytDesktop.Models;

using ZgrzytDesktop.ViewModels.DashboardModules;

using ZgrzytDesktop.Resources;

using ZgrzytDesktop.Services;

using ZgrzytDesktop.Services.Interfaces;



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

    private DispatcherTimer? _ticketPollingTimer;

    private bool _pollingTimersInitialized;

    private DispatcherTimer _toastHideTimer = null!;



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



        InitializeDashboardPanels();

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

            _ = LoadTicketsAsync();

    }



    private void ApplyDefaultSortAndAdminFilter()
    {
        TicketsPanel.ApplyDefaultSort();
        AdminPanel.ApplyDefaultFilter();
    }



    internal bool IsTicketPollingActive => _ticketPollingTimer?.IsEnabled ?? false;

    internal async Task RunAutoRefreshForTestsAsync() => await AutoRefreshTicketsAsync();

    private void InitializeTimers()

    {

        EnsureToastHideTimer();

        if (_pollingTimersInitialized)
            return;

        _pollingTimersInitialized = true;



        _ticketPollingTimer = new DispatcherTimer

        {

            Interval = TimeSpan.FromSeconds(TicketsPanelViewModel.AutoRefreshIntervalSeconds)

        };



        _ticketPollingTimer.Tick += async (_, _) =>

        {

            await AutoRefreshTicketsAsync();

        };



        _ticketPollingTimer.Start();

    }

}