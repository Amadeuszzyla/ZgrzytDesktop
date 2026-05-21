using System;

using System.Collections.Generic;

using System.Threading.Tasks;

using Avalonia.Threading;

using ZgrzytDesktop.Cache;

using ZgrzytDesktop.Constants;

using ZgrzytDesktop.Helpers;

using ZgrzytDesktop.Models;

using ZgrzytDesktop.Resources;

using ZgrzytDesktop.Services;

using ZgrzytDesktop.Services.Interfaces;



namespace ZgrzytDesktop.ViewModels;



public partial class DashboardViewModel : ViewModelBase

{

    private readonly IAuthService _authService;

    private readonly ITicketService _ticketService;

    private readonly ApiService _apiService;

    private readonly ISettingsService _settingsService;

    private readonly LocalTicketCacheService _ticketCacheService;

    private readonly ILocalAuditLogService _auditLogService;

    private readonly IUserAdminService _userAdminService;

    private readonly Func<Task> _onLogoutRequested;

    private readonly List<Ticket> _allTickets = new();



    private DispatcherTimer? _ticketPollingTimer;

    private bool _pollingTimersInitialized;

    private DispatcherTimer _toastHideTimer = null!;



    public User CurrentUser { get; }



    public DashboardViewModel(

        User currentUser,

        IAuthService authService,

        ITicketService ticketService,

        ApiService apiService,

        ISettingsService settingsService,

        LocalTicketCacheService ticketCacheService,

        ILocalAuditLogService auditLogService,

        IUserAdminService userAdminService,

        Func<Task> onLogoutRequested)

        : this(

            currentUser,

            authService,

            ticketService,

            apiService,

            settingsService,

            ticketCacheService,

            auditLogService,

            userAdminService,

            onLogoutRequested,

            BootstrapOptions.Production)

    {

    }



    internal DashboardViewModel(

        User currentUser,

        IAuthService authService,

        ITicketService ticketService,

        ApiService apiService,

        ISettingsService settingsService,

        LocalTicketCacheService ticketCacheService,

        ILocalAuditLogService auditLogService,

        IUserAdminService userAdminService,

        Func<Task> onLogoutRequested,

        BootstrapOptions bootstrap)

    {

        CurrentUser = currentUser;

        _authService = authService;

        _ticketService = ticketService;

        _apiService = apiService;

        _settingsService = settingsService;

        _ticketCacheService = ticketCacheService;

        _auditLogService = auditLogService;

        _userAdminService = userAdminService;

        _onLogoutRequested = onLogoutRequested;



        InitializeCommands();

        InitializeTicketCollections();

        InitializeAdminCollections();

        InitializeSettingsCollections();

        InitializeStatisticsCollections();



        ApplyBootstrapFromSettings();

        ApplyDefaultSortAndAdminFilter();

        ConfigureTicketQueueViewsForRole();



        PollingStatusMessage = AutoRefreshStatusText;



        if (bootstrap.EnableTimers)

            InitializeTimers();



        SetSelectedPageNumberSilently(CurrentPage);

        SetSelectedPageSizeSilently(PageSize);

        UpdatePageNumbers();



        if (bootstrap.ShowLoginToast)

            ShowToast($"Zalogowano jako {CurrentUser.Name}.", ToastTypes.Info);



        if (bootstrap.RunInitialLoad)

            _ = LoadTicketsAsync();

    }



    private void ApplyBootstrapFromSettings()

    {

        var appSettings = _settingsService.LoadSync();

        SelectedThemeMode = appSettings.ThemeMode;

        SelectedUiCulture = SettingsService.NormalizeUiCulture(appSettings.UiCulture);

        AppStrings.ApplyCulture(SelectedUiCulture);

        SettingsService.ApplyThemeMode(appSettings.ThemeMode);

    }



    private void ApplyDefaultSortAndAdminFilter()

    {

        _selectedTicketSortField = TicketSortHelper.DefaultField;

        _selectedTicketSortDirection = TicketSortHelper.DefaultDirection;

        _selectedAdminUserListFilterOption = AdminListFilterOption.All[0];

        OnPropertyChanged(nameof(SelectedTicketSortField));

        OnPropertyChanged(nameof(SelectedTicketSortDirection));

        OnPropertyChanged(nameof(SelectedAdminUserListFilterOption));

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

            Interval = TimeSpan.FromSeconds(TicketAutoRefreshIntervalSeconds)

        };



        _ticketPollingTimer.Tick += async (_, _) =>

        {

            await AutoRefreshTicketsAsync();

        };



        _ticketPollingTimer.Start();

    }

}

