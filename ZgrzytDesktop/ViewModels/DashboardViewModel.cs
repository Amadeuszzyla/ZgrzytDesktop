using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly TicketService _ticketService;
    private readonly ApiService _apiService;
    private readonly SettingsService _settingsService;
    private readonly LocalTicketCacheService _ticketCacheService;
    private readonly LocalAuditLogService _auditLogService;
    private readonly UserAdminService _userAdminService;
    private readonly Func<Task> _onLogoutRequested;
    private readonly List<Ticket> _allTickets = new();
    private readonly DispatcherTimer _ticketPollingTimer;
    private readonly DispatcherTimer _toastHideTimer;

    private string _toastMessage = string.Empty;
    private bool _isToastVisible;
    private string _toastBackground = "#2563EB";
    private string _toastForeground = "#FFFFFF";

    private string _currentSection = "Tickets";

    private string _statusMessage = "Gotowy.";
    private string _detailsStatusMessage = "Wybierz zgłoszenie z listy.";
    private string _settingsStatusMessage = "Ustawienia gotowe.";
    private string _pollingStatusMessage = string.Empty;
    private string _adminTab = "Users";
    private bool _autoRefreshErrorToastShown;
    private string _newMessageText = string.Empty;
    private string _searchText = string.Empty;
    private string _apiBaseUrl = "http://127.0.0.1:9000/api/";
    private string _selectedThemeMode = "System";

    private string _newTicketTitle = string.Empty;
    private string _newTicketDescription = string.Empty;
    private string _newTicketPriority = "niski";
    private string _selectedNewTicketCategory = "Hardware";
    private string _createTicketStatusMessage = string.Empty;

    private string _requestName = string.Empty;
    private string _requestLogin = string.Empty;
    private string _requestEmail = string.Empty;
    private string _requestPassword = string.Empty;
    private string _requestPasswordConfirmation = string.Empty;
    private string _requestAccountStatusMessage = string.Empty;
    private bool _isRequestingAccount;

    private int _statsTotalTickets;
    private int _statsNewTickets;
    private int _statsInProgressTickets;
    private int _statsClosedTickets;
    private int _statsLowPriorityTickets;
    private int _statsMediumPriorityTickets;
    private int _statsHighPriorityTickets;
    private int _statsAssignedTickets;
    private int _statsUnassignedTickets;
    private double _statsStatusChartMaximum = 1;
    private double _statsPriorityChartMaximum = 1;
    private double _statsAssignmentChartMaximum = 1;
    private string _statsScopeMessage = "Brak pobranych zgłoszeń.";
    private bool _isLoadingAllStatistics;
    private string _adminStatusMessage = string.Empty;
    private User? _selectedAdminUser;

    private string? _selectedStatus;
    private string? _selectedPriority;

    private string _selectedFilterStatus = "Wszystkie";
    private string _selectedFilterPriority = "Wszystkie";
    private string _selectedTicketQueueView = "Wszystkie";

    private bool _isLoading;
    private bool _isLoadingDetails;
    private bool _isTestingApiConnection;
    private bool _isCheckingForNewTickets;
    private bool _isOffline;
    private bool _isChangingPageInternally;

    private int _currentPage = 1;
    private int _lastPage = 1;
    private int _pageSize = 10;
    private int _totalTickets;

    private int? _selectedPageNumber = 1;
    private int? _selectedPageSize = 10;

    private Ticket? _selectedTicket;
    private Ticket? _ticketDetails;

    public User CurrentUser { get; }

    public ObservableCollection<Ticket> Tickets { get; } = new();

    public ObservableCollection<Message> Messages { get; } = new();

    public bool HasNoMessages => Messages.Count == 0;

    public ObservableCollection<AuditLogEntry> TicketAuditLogEntries { get; } = new();

    public bool HasNoTicketAuditLogEntries => TicketAuditLogEntries.Count == 0;

    public ObservableCollection<AuditLogEntry> SettingsAuditLogEntries { get; } = new();

    public bool HasNoSettingsAuditLogEntries => SettingsAuditLogEntries.Count == 0;

    public ObservableCollection<User> AdminUsers { get; } = new();

    public ObservableCollection<int> PageNumbers { get; } = new();

    public ObservableCollection<int> PageSizeOptions { get; } = new()
    {
        5,
        10,
        20,
        50
    };

    public ObservableCollection<string> AvailableStatuses { get; } = new()
    {
        "Nowe",
        "W toku",
        "Rozwiązane"
    };

    public ObservableCollection<string> AvailablePriorities { get; } = new()
    {
        "niski",
        "średni",
        "wysoki"
    };

    public ObservableCollection<string> NewTicketCategories { get; } = new(TicketCategoryHelper.Categories);

    public ObservableCollection<string> TicketQueueViews { get; } = new()
    {
        "Wszystkie",
        "Aktywne",
        "Nieprzypisane"
    };

    public ObservableCollection<string> FilterStatuses { get; } = new()
    {
        "Wszystkie",
        "nowe",
        "w trakcie",
        "zamknięte"
    };

    public ObservableCollection<string> FilterPriorities { get; } = new()
    {
        "Wszystkie",
        "niski",
        "średni",
        "wysoki"
    };

    public ObservableCollection<string> ThemeModes { get; } = new()
    {
        "System",
        "Light",
        "Dark"
    };

    public string CurrentSection
    {
        get => _currentSection;
        set
        {
            if (SetProperty(ref _currentSection, value))
            {
                OnPropertyChanged(nameof(IsTicketsPageVisible));
                OnPropertyChanged(nameof(IsDetailsPageVisible));
                OnPropertyChanged(nameof(IsSettingsPageVisible));
                OnPropertyChanged(nameof(IsRequestAccountPageVisible));
                OnPropertyChanged(nameof(IsStatisticsPageVisible));
                OnPropertyChanged(nameof(IsAdminPageVisible));
                OnPropertyChanged(nameof(CurrentSectionTitle));
                OnPropertyChanged(nameof(IsTicketsNavActive));
                OnPropertyChanged(nameof(IsRequestAccountNavActive));
                OnPropertyChanged(nameof(ShowRequestAccountNav));
                OnPropertyChanged(nameof(ShowAdministrationNav));
                OnPropertyChanged(nameof(IsStatisticsNavActive));
                OnPropertyChanged(nameof(IsSettingsNavActive));
                OnPropertyChanged(nameof(IsAdminNavActive));
                OnPropertyChanged(nameof(IsAdminUsersPanelVisible));
                OnPropertyChanged(nameof(IsAdminNewAccountPanelVisible));
            }
        }
    }

    public const int TicketAutoRefreshIntervalSeconds = 45;

    public bool IsTicketsNavActive => CurrentSection == "Tickets";

    public bool IsRequestAccountNavActive => CurrentSection == "RequestAccount";

    public bool IsStatisticsNavActive => CurrentSection == "Statistics";

    public bool IsSettingsNavActive => CurrentSection == "Settings";

    public bool IsAdminNavActive => CurrentSection == "Admin";

    public bool IsTicketsPageVisible => CurrentSection == "Tickets";

    public bool IsDetailsPageVisible => CurrentSection == "Details";

    public bool IsSettingsPageVisible => CurrentSection == "Settings";

    public bool IsRequestAccountPageVisible => CurrentSection == "RequestAccount";

    public bool IsStatisticsPageVisible => CurrentSection == "Statistics";

    public bool IsAdminPageVisible => CurrentSection == "Admin";

    public bool IsAdminRole =>
        string.Equals(CurrentUser.Role, "admin", StringComparison.OrdinalIgnoreCase);

    public bool IsStaffRole =>
        IsAdminRole ||
        string.Equals(CurrentUser.Role, "it", StringComparison.OrdinalIgnoreCase);

    public bool ShowAdministrationNav => IsStaffRole;

    public bool ShowRequestAccountNav => !IsStaffRole;

    public string AdminTab
    {
        get => _adminTab;
        set
        {
            if (SetProperty(ref _adminTab, value))
            {
                OnPropertyChanged(nameof(IsAdminUsersTabActive));
                OnPropertyChanged(nameof(IsAdminNewAccountTabActive));
                OnPropertyChanged(nameof(IsAdminUsersPanelVisible));
                OnPropertyChanged(nameof(IsAdminNewAccountPanelVisible));
            }
        }
    }

    public bool IsAdminUsersTabActive => AdminTab == "Users";

    public bool IsAdminNewAccountTabActive => AdminTab == "NewAccount";

    public bool IsAdminUsersPanelVisible => IsAdminUsersTabActive && IsAdminRole;

    public bool IsAdminNewAccountPanelVisible => IsAdminNewAccountTabActive && IsStaffRole;

    public string CurrentSectionTitle => CurrentSection switch
    {
        "Tickets" => "Zgłoszenia",
        "Details" => "Szczegóły zgłoszenia",
        "Settings" => "Ustawienia",
        "RequestAccount" => "Zgłoś nowe konto",
        "Statistics" => "Statystyki",
        "Admin" => "Administracja",
        _ => "ZGRZYT Desktop"
    };

    public bool CanOpenDetailsPage => SelectedTicket is not null || TicketDetails is not null;

    public string PagePositionText => $"Strona {CurrentPage} z {LastPage}";

    public string PageTotalText => $"Razem: {TotalTickets}";

    public string AutoRefreshStatusText =>
        $"Aplikacja automatycznie odświeża listę co {TicketAutoRefreshIntervalSeconds} sekund.";

    public Ticket? SelectedTicket
    {
        get => _selectedTicket;
        set
        {
            if (SetProperty(ref _selectedTicket, value))
            {
                OnPropertyChanged(nameof(CanOpenDetailsPage));

                if (value is not null)
                    _ = LoadTicketDetailsAndOpenAsync(value.Id);
            }
        }
    }

    public Ticket? TicketDetails
    {
        get => _ticketDetails;
        set
        {
            if (SetProperty(ref _ticketDetails, value))
            {
                OnPropertyChanged(nameof(CanCloseOwnTicket));
                OnPropertyChanged(nameof(CanCloseTicket));
                OnPropertyChanged(nameof(CanDeleteTicket));
                OnPropertyChanged(nameof(CanOpenDetailsPage));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string DetailsStatusMessage
    {
        get => _detailsStatusMessage;
        set => SetProperty(ref _detailsStatusMessage, value);
    }

    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => SetProperty(ref _settingsStatusMessage, value);
    }

    public string PollingStatusMessage
    {
        get => _pollingStatusMessage;
        set => SetProperty(ref _pollingStatusMessage, value);
    }

    public string NewMessageText
    {
        get => _newMessageText;
        set => SetProperty(ref _newMessageText, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    public string SelectedThemeMode
    {
        get => _selectedThemeMode;
        set => SetProperty(ref _selectedThemeMode, value);
    }

    public string NewTicketTitle
    {
        get => _newTicketTitle;
        set => SetProperty(ref _newTicketTitle, value);
    }

    public string NewTicketDescription
    {
        get => _newTicketDescription;
        set => SetProperty(ref _newTicketDescription, value);
    }

    public string NewTicketPriority
    {
        get => _newTicketPriority;
        set => SetProperty(ref _newTicketPriority, value);
    }

    public string SelectedNewTicketCategory
    {
        get => _selectedNewTicketCategory;
        set => SetProperty(ref _selectedNewTicketCategory, value);
    }

    public string CreateTicketStatusMessage
    {
        get => _createTicketStatusMessage;
        set => SetProperty(ref _createTicketStatusMessage, value);
    }

    public string RequestName
    {
        get => _requestName;
        set => SetProperty(ref _requestName, value);
    }

    public string RequestLogin
    {
        get => _requestLogin;
        set => SetProperty(ref _requestLogin, value);
    }

    public string RequestEmail
    {
        get => _requestEmail;
        set => SetProperty(ref _requestEmail, value);
    }

    public string RequestPassword
    {
        get => _requestPassword;
        set => SetProperty(ref _requestPassword, value);
    }

    public string RequestPasswordConfirmation
    {
        get => _requestPasswordConfirmation;
        set => SetProperty(ref _requestPasswordConfirmation, value);
    }

    public string RequestAccountStatusMessage
    {
        get => _requestAccountStatusMessage;
        set => SetProperty(ref _requestAccountStatusMessage, value);
    }

    public bool IsRequestingAccount
    {
        get => _isRequestingAccount;
        private set
        {
            if (SetProperty(ref _isRequestingAccount, value))
                OnPropertyChanged(nameof(CanRequestAccount));
        }
    }

    public bool CanRequestAccount => CanUseOnlineActions && !IsRequestingAccount;

    public string ToastMessage
    {
        get => _toastMessage;
        private set => SetProperty(ref _toastMessage, value);
    }

    public bool IsToastVisible
    {
        get => _isToastVisible;
        private set => SetProperty(ref _isToastVisible, value);
    }

    public string ToastBackground
    {
        get => _toastBackground;
        private set => SetProperty(ref _toastBackground, value);
    }

    public string ToastForeground
    {
        get => _toastForeground;
        private set => SetProperty(ref _toastForeground, value);
    }

    public int StatsTotalTickets
    {
        get => _statsTotalTickets;
        private set => SetProperty(ref _statsTotalTickets, value);
    }

    public int StatsNewTickets
    {
        get => _statsNewTickets;
        private set => SetProperty(ref _statsNewTickets, value);
    }

    public int StatsInProgressTickets
    {
        get => _statsInProgressTickets;
        private set => SetProperty(ref _statsInProgressTickets, value);
    }

    public int StatsClosedTickets
    {
        get => _statsClosedTickets;
        private set => SetProperty(ref _statsClosedTickets, value);
    }

    public int StatsLowPriorityTickets
    {
        get => _statsLowPriorityTickets;
        private set => SetProperty(ref _statsLowPriorityTickets, value);
    }

    public int StatsMediumPriorityTickets
    {
        get => _statsMediumPriorityTickets;
        private set => SetProperty(ref _statsMediumPriorityTickets, value);
    }

    public int StatsHighPriorityTickets
    {
        get => _statsHighPriorityTickets;
        private set => SetProperty(ref _statsHighPriorityTickets, value);
    }

    public int StatsAssignedTickets
    {
        get => _statsAssignedTickets;
        private set => SetProperty(ref _statsAssignedTickets, value);
    }

    public int StatsUnassignedTickets
    {
        get => _statsUnassignedTickets;
        private set => SetProperty(ref _statsUnassignedTickets, value);
    }

    public double StatsStatusChartMaximum
    {
        get => _statsStatusChartMaximum;
        private set => SetProperty(ref _statsStatusChartMaximum, value);
    }

    public double StatsPriorityChartMaximum
    {
        get => _statsPriorityChartMaximum;
        private set => SetProperty(ref _statsPriorityChartMaximum, value);
    }

    public double StatsAssignmentChartMaximum
    {
        get => _statsAssignmentChartMaximum;
        private set => SetProperty(ref _statsAssignmentChartMaximum, value);
    }

    public string StatsScopeMessage
    {
        get => _statsScopeMessage;
        private set => SetProperty(ref _statsScopeMessage, value);
    }

    public string? SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public string? SelectedPriority
    {
        get => _selectedPriority;
        set => SetProperty(ref _selectedPriority, value);
    }

    public string SelectedFilterStatus
    {
        get => _selectedFilterStatus;
        set => SetProperty(ref _selectedFilterStatus, value);
    }

    public string SelectedFilterPriority
    {
        get => _selectedFilterPriority;
        set => SetProperty(ref _selectedFilterPriority, value);
    }

    public string SelectedTicketQueueView
    {
        get => _selectedTicketQueueView;
        set
        {
            if (SetProperty(ref _selectedTicketQueueView, value))
            {
                SetCurrentPageSilently(1);
                _ = LoadTicketsAsync();
            }
        }
    }

    public int? SelectedPageNumber
    {
        get => _selectedPageNumber;
        set
        {
            if (SetProperty(ref _selectedPageNumber, value))
            {
                if (value is null)
                    return;

                if (value.Value < 1)
                    return;

                if (value.Value != CurrentPage)
                    CurrentPage = value.Value;
            }
        }
    }

    public int? SelectedPageSize
    {
        get => _selectedPageSize;
        set
        {
            if (SetProperty(ref _selectedPageSize, value))
            {
                if (value is null || value.Value <= 0)
                    return;

                if (value.Value != PageSize)
                    PageSize = value.Value;
            }
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (value < 1)
                value = 1;

            var maxPage = Math.Max(1, LastPage);

            if (value > maxPage)
                value = maxPage;

            if (SetProperty(ref _currentPage, value))
            {
                SetSelectedPageNumberSilently(value);
                RefreshPaginationProperties();

                if (!_isChangingPageInternally)
                    _ = LoadTicketsAsync();
            }
        }
    }

    public int LastPage
    {
        get => _lastPage;
        set
        {
            value = Math.Max(1, value);

            if (SetProperty(ref _lastPage, value))
            {
                if (CurrentPage > value)
                    SetCurrentPageSilently(value);

                UpdatePageNumbers();
                RefreshPaginationProperties();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value <= 0)
                value = 10;

            if (SetProperty(ref _pageSize, value))
            {
                SetSelectedPageSizeSilently(value);
                SetCurrentPageSilently(1);
                _ = LoadTicketsAsync();
            }
        }
    }

    public int TotalTickets
    {
        get => _totalTickets;
        set
        {
            if (SetProperty(ref _totalTickets, value))
            {
                OnPropertyChanged(nameof(PageInfoText));
                OnPropertyChanged(nameof(PagePositionText));
                OnPropertyChanged(nameof(PageTotalText));
            }
        }
    }

    public string PageInfoText => $"{PagePositionText} | {PageTotalText}";

    public bool IsOnLastPage => CurrentPage >= LastPage;

    public bool CanGoPreviousPage => CurrentPage > 1 && !IsLoading;

    public bool CanGoNextPage => CurrentPage < LastPage && !IsLoading;

    public bool CanRefreshTicketsNow => !IsLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsNotLoading));
                OnPropertyChanged(nameof(CanUseOnlineActions));
                OnPropertyChanged(nameof(CanRequestAccount));
                RefreshPaginationProperties();
            }
        }
    }

    public bool IsNotLoading => !IsLoading;

    public bool IsLoadingDetails
    {
        get => _isLoadingDetails;
        set
        {
            if (SetProperty(ref _isLoadingDetails, value))
            {
                OnPropertyChanged(nameof(IsNotLoadingDetails));
                OnPropertyChanged(nameof(CanUseOnlineDetailsActions));
                OnPropertyChanged(nameof(CanSendMessage));
                OnPropertyChanged(nameof(CanEditTicket));
                OnPropertyChanged(nameof(CanAssignTicket));
                OnPropertyChanged(nameof(CanCloseOwnTicket));
                OnPropertyChanged(nameof(CanCloseTicket));
                OnPropertyChanged(nameof(CanDeleteTicket));
            }
        }
    }

    public bool IsNotLoadingDetails => !IsLoadingDetails;

    public bool IsTestingApiConnection
    {
        get => _isTestingApiConnection;
        set
        {
            if (SetProperty(ref _isTestingApiConnection, value))
                OnPropertyChanged(nameof(CanTestApiConnection));
        }
    }

    public bool CanTestApiConnection => !IsTestingApiConnection;

    public bool IsCheckingForNewTickets
    {
        get => _isCheckingForNewTickets;
        set
        {
            if (SetProperty(ref _isCheckingForNewTickets, value))
                OnPropertyChanged(nameof(CanRefreshTicketsNow));
        }
    }

    public bool IsOffline
    {
        get => _isOffline;
        set
        {
            if (SetProperty(ref _isOffline, value))
            {
                OnPropertyChanged(nameof(IsOnline));
                OnPropertyChanged(nameof(ConnectionStatusText));
                OnPropertyChanged(nameof(CanUseOnlineActions));
                OnPropertyChanged(nameof(CanRequestAccount));
                OnPropertyChanged(nameof(CanUseOnlineDetailsActions));
                OnPropertyChanged(nameof(CanSendMessage));
                OnPropertyChanged(nameof(CanEditTicket));
                OnPropertyChanged(nameof(CanAssignTicket));
                OnPropertyChanged(nameof(CanCloseOwnTicket));
                OnPropertyChanged(nameof(CanCloseTicket));
                OnPropertyChanged(nameof(CanDeleteTicket));
            }
        }
    }

    public bool IsOnline => !IsOffline;

    public string ConnectionStatusText => IsOffline ? "Tryb offline" : "Online";

    public bool CanManageTickets =>
        string.Equals(CurrentUser.Role, "admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(CurrentUser.Role, "it", StringComparison.OrdinalIgnoreCase);

    public bool IsRegularUser =>
        string.Equals(CurrentUser.Role, "user", StringComparison.OrdinalIgnoreCase);

    public bool CanUseOnlineActions => !IsOffline && !IsLoading;

    public bool CanUseOnlineDetailsActions => !IsOffline && !IsLoadingDetails;

    public bool CanSendMessage => !IsOffline && !IsLoadingDetails;

    public bool CanEditTicket => CanManageTickets && !IsOffline && !IsLoadingDetails;

    public bool CanAssignTicket => CanManageTickets && !IsOffline && !IsLoadingDetails;

    public bool CanCloseOwnTicket =>
        IsRegularUser &&
        TicketDetails is not null &&
        TicketDetails.UserId == CurrentUser.Id &&
        !string.Equals(TicketDetails.Status, "zamknięte", StringComparison.OrdinalIgnoreCase);

    public bool CanCloseTicket =>
        !IsOffline &&
        !IsLoadingDetails &&
        TicketDetails is not null &&
        !string.Equals(TicketDetails.Status, "zamknięte", StringComparison.OrdinalIgnoreCase) &&
        (CanManageTickets || CanCloseOwnTicket);

    public bool CanDeleteTicket =>
        CanManageTickets &&
        !IsOffline &&
        !IsLoadingDetails &&
        TicketDetails is not null;

    public bool IsLoadingAllStatistics
    {
        get => _isLoadingAllStatistics;
        private set => SetProperty(ref _isLoadingAllStatistics, value);
    }

    public string AdminStatusMessage
    {
        get => _adminStatusMessage;
        private set => SetProperty(ref _adminStatusMessage, value);
    }

    public User? SelectedAdminUser
    {
        get => _selectedAdminUser;
        set
        {
            if (SetProperty(ref _selectedAdminUser, value))
            {
                OnPropertyChanged(nameof(CanBanAdminUser));
                OnPropertyChanged(nameof(CanActivateAdminUser));
                OnPropertyChanged(nameof(CanUnbanAdminUser));
            }
        }
    }

    public bool CanBanAdminUser =>
        IsAdminRole && SelectedAdminUser is not null && !SelectedAdminUser.Ban;

    public bool CanActivateAdminUser =>
        IsAdminRole && SelectedAdminUser is not null && !SelectedAdminUser.Active && !SelectedAdminUser.Ban;

    public bool CanUnbanAdminUser =>
        IsAdminRole && SelectedAdminUser is not null && SelectedAdminUser.Ban;

    public IRelayCommand ShowTicketsPageCommand { get; }

    public IRelayCommand ShowSettingsPageCommand { get; }

    public IRelayCommand ShowAdminUsersTabCommand { get; }

    public IRelayCommand ShowAdminNewAccountTabCommand { get; }

    public IRelayCommand ShowRequestAccountPageCommand { get; }

    public IRelayCommand ShowStatisticsPageCommand { get; }

    public IAsyncRelayCommand RequestAccountCommand { get; }

    public IAsyncRelayCommand LoadTicketsCommand { get; }

    public IAsyncRelayCommand SearchTicketsCommand { get; }

    public IRelayCommand ClearFiltersCommand { get; }

    public IAsyncRelayCommand CreateTicketCommand { get; }

    public IAsyncRelayCommand SendMessageCommand { get; }

    public IAsyncRelayCommand UpdateTicketCommand { get; }

    public IAsyncRelayCommand AssignToMeCommand { get; }

    public IAsyncRelayCommand CloseTicketCommand { get; }

    public IAsyncRelayCommand DeleteTicketCommand { get; }

    public IAsyncRelayCommand LoadAllPagesStatisticsCommand { get; }

    public IAsyncRelayCommand RefreshSessionCommand { get; }

    public IAsyncRelayCommand LoadAdminUsersCommand { get; }

    public IAsyncRelayCommand BanAdminUserCommand { get; }

    public IAsyncRelayCommand ActivateAdminUserCommand { get; }

    public IRelayCommand ShowAdminPageCommand { get; }

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public IAsyncRelayCommand LoadAuditLogsCommand { get; }

    public IAsyncRelayCommand ClearAuditLogsCommand { get; }

    public IAsyncRelayCommand TestApiConnectionCommand { get; }

    public IAsyncRelayCommand FirstPageCommand { get; }

    public IAsyncRelayCommand PreviousPageCommand { get; }

    public IAsyncRelayCommand NextPageCommand { get; }

    public IAsyncRelayCommand LastPageCommand { get; }

    public IAsyncRelayCommand RefreshTicketsNowCommand { get; }

    public IAsyncRelayCommand LogoutCommand { get; }

    public DashboardViewModel(
        User currentUser,
        AuthService authService,
        TicketService ticketService,
        ApiService apiService,
        SettingsService settingsService,
        LocalTicketCacheService ticketCacheService,
        LocalAuditLogService auditLogService,
        Func<Task> onLogoutRequested)
    {
        CurrentUser = currentUser;
        _authService = authService;
        _ticketService = ticketService;
        _apiService = apiService;
        _settingsService = settingsService;
        _ticketCacheService = ticketCacheService;
        _auditLogService = auditLogService;
        _userAdminService = new UserAdminService(apiService);
        _onLogoutRequested = onLogoutRequested;

        ApiBaseUrl = _apiService.CurrentApiBaseUrl;

        var appSettings = _settingsService.LoadSync();
        SelectedThemeMode = appSettings.ThemeMode;
        SettingsService.ApplyThemeMode(appSettings.ThemeMode);

        ConfigureTicketQueueViewsForRole();

        PollingStatusMessage = AutoRefreshStatusText;

        ShowTicketsPageCommand = new RelayCommand(ShowTicketsPage);
        ShowSettingsPageCommand = new RelayCommand(ShowSettingsPage);
        ShowRequestAccountPageCommand = new RelayCommand(ShowRequestAccountPage);
        ShowStatisticsPageCommand = new RelayCommand(ShowStatisticsPage);
        ShowAdminPageCommand = new RelayCommand(ShowAdminPage);
        ShowAdminUsersTabCommand = new RelayCommand(() => AdminTab = "Users");
        ShowAdminNewAccountTabCommand = new RelayCommand(() => AdminTab = "NewAccount");
        RequestAccountCommand = new AsyncRelayCommand(RequestAccountAsync);

        LoadTicketsCommand = new AsyncRelayCommand(() => LoadTicketsAsync());
        SearchTicketsCommand = new AsyncRelayCommand(SearchTicketsAsync);
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        CreateTicketCommand = new AsyncRelayCommand(CreateTicketAsync);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
        UpdateTicketCommand = new AsyncRelayCommand(UpdateTicketAsync);
        AssignToMeCommand = new AsyncRelayCommand(AssignToMeAsync);
        CloseTicketCommand = new AsyncRelayCommand(CloseTicketAsync);
        DeleteTicketCommand = new AsyncRelayCommand(DeleteTicketAsync);
        LoadAllPagesStatisticsCommand = new AsyncRelayCommand(LoadAllPagesStatisticsAsync);
        RefreshSessionCommand = new AsyncRelayCommand(RefreshSessionAsync);
        LoadAdminUsersCommand = new AsyncRelayCommand(LoadAdminUsersAsync);
        BanAdminUserCommand = new AsyncRelayCommand(BanAdminUserAsync, () => CanBanAdminUser);
        ActivateAdminUserCommand = new AsyncRelayCommand(ActivateAdminUserAsync, () => CanActivateAdminUser);

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        LoadAuditLogsCommand = new AsyncRelayCommand(RefreshSettingsAuditLogAsync);
        ClearAuditLogsCommand = new AsyncRelayCommand(ClearSettingsAuditLogAsync);
        TestApiConnectionCommand = new AsyncRelayCommand(TestApiConnectionAsync);

        FirstPageCommand = new AsyncRelayCommand(GoToFirstPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(GoToPreviousPageAsync);
        NextPageCommand = new AsyncRelayCommand(GoToNextPageAsync);
        LastPageCommand = new AsyncRelayCommand(GoToLastPageAsync);
        RefreshTicketsNowCommand = new AsyncRelayCommand(() => RefreshTicketsNowAsync());

        LogoutCommand = new AsyncRelayCommand(LogoutAsync);

        _toastHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        _toastHideTimer.Tick += (_, _) =>
        {
            _toastHideTimer.Stop();
            IsToastVisible = false;
        };

        _ticketPollingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(TicketAutoRefreshIntervalSeconds)
        };

        _ticketPollingTimer.Tick += async (_, _) =>
        {
            await AutoRefreshTicketsAsync();
        };

        _ticketPollingTimer.Start();

        SetSelectedPageNumberSilently(CurrentPage);
        SetSelectedPageSizeSilently(PageSize);
        UpdatePageNumbers();

        ShowToast($"Zalogowano jako {CurrentUser.Name}.", "info");

        _ = LoadTicketsAsync();
    }

    public void ShowToast(string message, string type = "info")
    {
        if (ApiErrorSanitizer.IsHtmlResponse(message))
        {
            message = ApiErrorSanitizer.SanitizeForDisplay(
                message,
                System.Net.HttpStatusCode.InternalServerError);
        }

        void DisplayToast()
        {
            _toastHideTimer.Stop();

            ToastMessage = message;
            ApplyToastStyle(type);
            IsToastVisible = true;
            _toastHideTimer.Start();
        }

        if (Dispatcher.UIThread.CheckAccess())
            DisplayToast();
        else
            Dispatcher.UIThread.Post(DisplayToast);
    }

    private void ApplyToastStyle(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case "success":
                ToastBackground = "#059669";
                ToastForeground = "#FFFFFF";
                break;
            case "warning":
                ToastBackground = "#D97706";
                ToastForeground = "#FFFFFF";
                break;
            case "error":
                ToastBackground = "#DC2626";
                ToastForeground = "#FFFFFF";
                break;
            default:
                ToastBackground = "#2563EB";
                ToastForeground = "#FFFFFF";
                break;
        }
    }

    private void ShowTicketsPage()
    {
        CurrentSection = "Tickets";
    }

    private void ShowSettingsPage()
    {
        CurrentSection = "Settings";
        _ = RefreshSettingsAuditLogAsync();
    }

    private void ConfigureTicketQueueViewsForRole()
    {
        TicketQueueViews.Clear();
        TicketQueueViews.Add("Wszystkie");

        if (CanManageTickets)
        {
            TicketQueueViews.Add("Aktywne");
            TicketQueueViews.Add("Nieprzypisane");
        }

        if (!TicketQueueViews.Contains(SelectedTicketQueueView))
            SelectedTicketQueueView = "Wszystkie";
    }

    private void ShowRequestAccountPage()
    {
        CurrentSection = "RequestAccount";
    }

    private void ShowStatisticsPage()
    {
        CurrentSection = "Statistics";
    }

    private void ShowAdminPage()
    {
        CurrentSection = "Admin";
        AdminTab = IsAdminRole ? "Users" : "NewAccount";

        if (IsAdminRole)
            _ = LoadAdminUsersAsync();
    }

    private async Task RequestAccountAsync()
    {
        if (IsOffline)
        {
            RequestAccountStatusMessage = "Nie można wysłać prośby w trybie offline.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestName))
        {
            RequestAccountStatusMessage = "Podaj imię i nazwisko.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestLogin))
        {
            RequestAccountStatusMessage = "Podaj login.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestEmail))
        {
            RequestAccountStatusMessage = "Podaj adres e-mail.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestPassword))
        {
            RequestAccountStatusMessage = "Podaj hasło.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestPasswordConfirmation))
        {
            RequestAccountStatusMessage = "Potwierdź hasło.";
            return;
        }

        if (!string.Equals(RequestPassword, RequestPasswordConfirmation, StringComparison.Ordinal))
        {
            RequestAccountStatusMessage = "Hasła nie są identyczne.";
            return;
        }

        try
        {
            IsRequestingAccount = true;
            RequestAccountStatusMessage = "Wysyłanie prośby...";

            var request = new RequestAccountRequest
            {
                Name = RequestName.Trim(),
                Login = RequestLogin.Trim(),
                Email = RequestEmail.Trim(),
                Password = RequestPassword,
                PasswordConfirmation = RequestPasswordConfirmation
            };

            var success = await _authService.RequestAccountAsync(request);

            if (!success)
            {
                RequestAccountStatusMessage = "Nie udało się wysłać prośby o utworzenie konta.";
                return;
            }

            IsOffline = false;

            RequestName = string.Empty;
            RequestLogin = string.Empty;
            RequestEmail = string.Empty;
            RequestPassword = string.Empty;
            RequestPasswordConfirmation = string.Empty;

            RequestAccountStatusMessage = "Prośba o utworzenie konta została wysłana.";
            ShowToast("Prośba o utworzenie konta została wysłana.", "success");

            await LogAuditAsync(
                "RequestAccount",
                null,
                $"Wysłano prośbę o utworzenie konta: {request.Login}.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            RequestAccountStatusMessage = "Brak połączenia z API. Nie można wysłać prośby offline.";
            ShowToast("Brak połączenia z API. Nie można wysłać prośby offline.", "warning");
        }
        catch (ApiException ex)
        {
            RequestAccountStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            RequestAccountStatusMessage = "Wystąpił nieoczekiwany błąd podczas wysyłania prośby.";
            ShowToast("Wystąpił nieoczekiwany błąd podczas wysyłania prośby.", "error");
        }
        finally
        {
            IsRequestingAccount = false;
        }
    }

    private async Task SearchTicketsAsync()
    {
        SetCurrentPageSilently(1);
        await LoadTicketsAsync();
    }

    private async Task GoToFirstPageAsync()
    {
        if (CurrentPage == 1)
            return;

        CurrentPage = 1;
        await Task.CompletedTask;
    }

    private async Task GoToPreviousPageAsync()
    {
        if (CurrentPage <= 1)
            return;

        CurrentPage--;
        await Task.CompletedTask;
    }

    private async Task GoToNextPageAsync()
    {
        if (CurrentPage >= LastPage)
            return;

        CurrentPage++;
        await Task.CompletedTask;
    }

    private async Task GoToLastPageAsync()
    {
        if (CurrentPage == LastPage)
            return;

        CurrentPage = LastPage;
        await Task.CompletedTask;
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            var existing = await _settingsService.LoadAsync();

            var settings = new AppSettings
            {
                ApiBaseUrl = existing.ApiBaseUrl,
                ThemeMode = SelectedThemeMode
            };

            await _settingsService.SaveAsync(settings);

            SelectedThemeMode = settings.ThemeMode;
            SettingsService.ApplyThemeMode(settings.ThemeMode);

            SettingsStatusMessage = string.Empty;

            await LogAuditAsync("Zapis ustawień", null, "Zmieniono ustawienia aplikacji.");

            ShowToast("Ustawienia zapisane", "success");

            if (CurrentSection == "Settings")
                await RefreshSettingsAuditLogAsync();
        }
        catch
        {
            ShowToast("Nie udało się zapisać ustawień.", "error");
        }
    }

    private async Task TestApiConnectionAsync()
    {
        try
        {
            IsTestingApiConnection = true;
            SettingsStatusMessage = "Testowanie połączenia z API...";

            var normalizedUrl = _settingsService.NormalizeApiBaseUrl(ApiBaseUrl);
            _apiService.SetBaseAddress(normalizedUrl);

            var result = await _apiService.TestConnectionAsync();

            IsOffline = !result.Success;
            SettingsStatusMessage = result.Message;

            if (result.Success)
            {
                ShowToast("Połączenie z API działa poprawnie.", "success");
            }
            else
            {
                ShowToast("Nie udało się połączyć z API.", "error");
            }
        }
        finally
        {
            IsTestingApiConnection = false;
        }
    }

    private async Task LoadTicketsAsync(bool silentRefresh = false)
    {
        try
        {
            if (!silentRefresh)
            {
                IsLoading = true;
                StatusMessage = "Pobieranie zgłoszeń...";
            }

            var response = await _ticketService.GetTicketsAsync(
                page: CurrentPage,
                perPage: PageSize,
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                status: GetSelectedFilterValue(SelectedFilterStatus),
                priority: GetSelectedFilterValue(SelectedFilterPriority),
                queueView: GetSelectedTicketQueueView()
            );

            IsOffline = false;

            _allTickets.Clear();

            if (response?.Data is not null)
            {
                _allTickets.AddRange(response.Data);

                TotalTickets = response.Total;
                LastPage = Math.Max(1, (int)Math.Ceiling((double)TotalTickets / PageSize));

                if (CurrentPage > LastPage)
                {
                    SetCurrentPageSilently(LastPage);
                    await LoadTicketsAsync(silentRefresh);
                    return;
                }

                await _ticketCacheService.SaveTicketsAsync(_allTickets);

                ApplyVisibleTickets();
                UpdateTicketStatistics();

                StatusMessage = $"Pobrano zgłoszeń: {Tickets.Count} z {TotalTickets}";

                if (!silentRefresh)
                    PollingStatusMessage = AutoRefreshStatusText;
            }
            else
            {
                Tickets.Clear();
                TotalTickets = 0;
                LastPage = 1;
                UpdatePageNumbers();
                UpdateTicketStatistics();
                StatusMessage = "Brak zgłoszeń.";
            }
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;

            if (!silentRefresh)
                ShowToast("Brak połączenia z API. Pokazuję dane offline.", "warning");

            await LoadTicketsFromCacheAsync();
        }
        catch (ApiException ex)
        {
            StatusMessage = GetApiErrorMessage(ex);

            if (!silentRefresh)
                ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            StatusMessage = "Wystąpił nieoczekiwany błąd podczas pobierania zgłoszeń.";

            if (!silentRefresh)
                ShowToast("Wystąpił błąd podczas pobierania zgłoszeń.", "error");
        }
        finally
        {
            if (!silentRefresh)
                IsLoading = false;

            RefreshPaginationProperties();
        }
    }

    private async Task LoadTicketsFromCacheAsync()
    {
        var cachedTickets = await _ticketCacheService.LoadTicketsAsync();

        _allTickets.Clear();
        _allTickets.AddRange(cachedTickets);

        TotalTickets = cachedTickets.Count;
        LastPage = Math.Max(1, (int)Math.Ceiling((double)TotalTickets / PageSize));

        if (CurrentPage > LastPage)
            SetCurrentPageSilently(LastPage);

        var pageItems = _allTickets
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Tickets.Clear();

        foreach (var ticket in pageItems)
            Tickets.Add(ticket);

        UpdatePageNumbers();

        StatusMessage = cachedTickets.Count > 0
            ? $"Brak połączenia z API. Pokazuję dane offline: {Tickets.Count} zgłoszeń."
            : "Brak połączenia z API i brak zapisanych danych offline.";

        UpdateTicketStatistics();
    }

    private void UpdateTicketStatistics()
    {
        ApplyTicketStatistics(_allTickets, TotalTickets, fromCurrentPageOnly: true);
    }

    private void ApplyTicketStatistics(IReadOnlyList<Ticket> tickets, int totalInSystem, bool fromCurrentPageOnly)
    {
        StatsTotalTickets = tickets.Count;
        StatsNewTickets = tickets.Count(ticket =>
            string.Equals(ticket.Status, "nowe", StringComparison.OrdinalIgnoreCase));
        StatsInProgressTickets = tickets.Count(ticket =>
            string.Equals(ticket.Status, "w trakcie", StringComparison.OrdinalIgnoreCase));
        StatsClosedTickets = tickets.Count(ticket =>
            string.Equals(ticket.Status, "zamknięte", StringComparison.OrdinalIgnoreCase));

        StatsLowPriorityTickets = tickets.Count(ticket =>
            string.Equals(ticket.Priority, "niski", StringComparison.OrdinalIgnoreCase));
        StatsMediumPriorityTickets = tickets.Count(ticket =>
            string.Equals(ticket.Priority, "średni", StringComparison.OrdinalIgnoreCase));
        StatsHighPriorityTickets = tickets.Count(ticket =>
            string.Equals(ticket.Priority, "wysoki", StringComparison.OrdinalIgnoreCase));

        StatsAssignedTickets = tickets.Count(ticket => ticket.AssignedItId.HasValue);
        StatsUnassignedTickets = Math.Max(0, tickets.Count - StatsAssignedTickets);

        StatsStatusChartMaximum = Math.Max(
            1,
            Math.Max(StatsNewTickets, Math.Max(StatsInProgressTickets, StatsClosedTickets)));
        StatsPriorityChartMaximum = Math.Max(
            1,
            Math.Max(StatsLowPriorityTickets, Math.Max(StatsMediumPriorityTickets, StatsHighPriorityTickets)));
        StatsAssignmentChartMaximum = Math.Max(
            1,
            Math.Max(StatsAssignedTickets, StatsUnassignedTickets));

        StatsScopeMessage = StatsTotalTickets == 0
            ? "Brak zgłoszeń do analizy."
            : fromCurrentPageOnly && totalInSystem > StatsTotalTickets
                ? $"Statystyki dla {StatsTotalTickets} zgłoszeń na bieżącej stronie listy (łącznie w systemie: {totalInSystem})."
                : $"Statystyki dla {StatsTotalTickets} zgłoszeń (łącznie w systemie: {totalInSystem}).";
    }

    private async Task RefreshTicketsNowAsync()
    {
        await LoadTicketsAsync();
        PollingStatusMessage = $"{AutoRefreshStatusText} Ostatnie odświeżenie: {DateTime.Now:HH:mm:ss}.";
    }

    private async Task AutoRefreshTicketsAsync()
    {
        if (CurrentSection != "Tickets" || IsOffline || IsLoading)
            return;

        try
        {
            await LoadTicketsAsync(silentRefresh: true);
            _autoRefreshErrorToastShown = false;
            PollingStatusMessage = AutoRefreshStatusText;
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            PollingStatusMessage = "Brak połączenia z API. Automatyczne odświeżanie wstrzymane.";

            if (!_autoRefreshErrorToastShown)
            {
                _autoRefreshErrorToastShown = true;
                ShowToast("Utracono połączenie z API.", "warning");
            }
        }
        catch
        {
            PollingStatusMessage = "Nie udało się automatycznie odświeżyć listy.";

            if (!_autoRefreshErrorToastShown)
            {
                _autoRefreshErrorToastShown = true;
                ShowToast("Nie udało się odświeżyć listy zgłoszeń.", "error");
            }
        }
    }

    private void ApplyVisibleTickets()
    {
        Tickets.Clear();

        foreach (var ticket in _allTickets)
            Tickets.Add(ticket);

        UpdatePageNumbers();
        RefreshPaginationProperties();
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedFilterStatus = "Wszystkie";
        SelectedFilterPriority = "Wszystkie";
        SetSelectedTicketQueueViewSilently("Wszystkie");
        SetCurrentPageSilently(1);

        _ = LoadTicketsAsync();
    }

    private async Task CreateTicketAsync()
    {
        if (IsOffline)
        {
            CreateTicketStatusMessage = "Nie można utworzyć zgłoszenia w trybie offline.";

            ShowToast("Nie można utworzyć zgłoszenia w trybie offline.", "warning");

            return;
        }

        if (string.IsNullOrWhiteSpace(NewTicketTitle))
        {
            CreateTicketStatusMessage = "Podaj tytuł zgłoszenia.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTicketDescription))
        {
            CreateTicketStatusMessage = "Podaj opis zgłoszenia.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedNewTicketCategory))
        {
            CreateTicketStatusMessage = "Wybierz kategorię zgłoszenia.";
            return;
        }

        try
        {
            IsLoading = true;
            CreateTicketStatusMessage = "Tworzenie zgłoszenia...";

            var request = new CreateTicketRequest
            {
                Title = TicketCategoryHelper.FormatTitle(SelectedNewTicketCategory, NewTicketTitle),
                Description = TicketCategoryHelper.FormatDescription(
                    SelectedNewTicketCategory,
                    NewTicketDescription),
                Priority = NewTicketPriority
            };

            var createdTicket = await _ticketService.CreateTicketAsync(request);

            IsOffline = false;

            NewTicketTitle = string.Empty;
            NewTicketDescription = string.Empty;
            NewTicketPriority = "niski";
            SelectedNewTicketCategory = "Hardware";

            SetCurrentPageSilently(1);
            await LoadTicketsAsync();

            if (createdTicket is not null)
                SelectedTicket = Tickets.FirstOrDefault(ticket => ticket.Id == createdTicket.Id);

            CreateTicketStatusMessage = "Zgłoszenie zostało utworzone.";

            ShowToast("Nowe zgłoszenie zostało utworzone.", "success");

            if (createdTicket is not null)
            {
                await LogAuditAsync(
                    "CreateTicket",
                    createdTicket.Id,
                    $"Utworzono zgłoszenie: {createdTicket.Title}");
            }
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            CreateTicketStatusMessage = "Brak połączenia z API. Nie można utworzyć zgłoszenia offline.";

            ShowToast("Brak połączenia z API. Nie można utworzyć zgłoszenia.", "error");
        }
        catch (ApiException ex)
        {
            CreateTicketStatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            CreateTicketStatusMessage = "Wystąpił nieoczekiwany błąd podczas tworzenia zgłoszenia.";

            ShowToast("Wystąpił błąd podczas tworzenia zgłoszenia.", "error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTicketDetailsAndOpenAsync(int ticketId)
    {
        await LoadTicketDetailsAsync(ticketId);
        CurrentSection = "Details";
    }

    private async Task LoadTicketDetailsAsync(int ticketId)
    {
        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Pobieranie szczegółów zgłoszenia...";

            if (IsOffline)
            {
                var cachedTicket = _allTickets.FirstOrDefault(ticket => ticket.Id == ticketId);

                TicketDetails = cachedTicket;

                Messages.Clear();

                if (cachedTicket?.Messages is not null)
                {
                    foreach (var message in cachedTicket.Messages)
                        Messages.Add(message);
                }

                NotifyMessagesUiState();

                SelectedStatus = StatusDisplayHelper.ToDisplayStatus(cachedTicket?.Status);
                SelectedPriority = cachedTicket?.Priority;

                DetailsStatusMessage = cachedTicket is null
                    ? "Nie znaleziono zgłoszenia w danych offline."
                    : $"Wybrano zgłoszenie #{cachedTicket.Id} z cache offline.";

                return;
            }

            var ticket = await _ticketService.GetTicketAsync(ticketId);

            IsOffline = false;

            if (!IsValidTicketForDisplay(ticket))
            {
                DetailsStatusMessage =
                    "Serwer zwrócił nieprawidłowe dane zgłoszenia. Poprzednie dane pozostają na ekranie.";

                ShowToast(
                    "Serwer zwrócił stronę błędu zamiast danych API. Sprawdź endpoint lub uprawnienia.",
                    "error");

                return;
            }

            TicketDetails = ticket;

            Messages.Clear();

            var messages = await _ticketService.GetTicketMessagesAsync(ticketId);

            foreach (var message in messages)
                Messages.Add(message);

            NotifyMessagesUiState();

            SelectedStatus = StatusDisplayHelper.ToDisplayStatus(ticket?.Status);
            SelectedPriority = ticket?.Priority;

            DetailsStatusMessage = ticket is null
                ? "Nie udało się pobrać szczegółów zgłoszenia."
                : $"Wybrano zgłoszenie #{ticket.Id}.";
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Pokazuję dane dostępne offline.";

            ShowToast("Brak połączenia z API. Szczegóły zgłoszenia są dostępne offline.", "warning");
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas pobierania szczegółów zgłoszenia.";

            ShowToast("Wystąpił błąd podczas pobierania szczegółów zgłoszenia.", "error");
        }
        finally
        {
            IsLoadingDetails = false;

            if (TicketDetails?.Id is int detailsTicketId)
                await RefreshTicketAuditLogAsync(detailsTicketId);
        }
    }

    private async Task SendMessageAsync()
    {
        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można wysłać wiadomości w trybie offline.";

            ShowToast("Nie można wysłać wiadomości w trybie offline.", "warning");

            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewMessageText))
        {
            DetailsStatusMessage = "Treść wiadomości nie może być pusta.";
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Wysyłanie wiadomości...";

            var ticketId = TicketDetails.Id;
            var messageBody = NewMessageText.Trim();

            await _ticketService.SendMessageAsync(ticketId, messageBody);

            IsOffline = false;

            NewMessageText = string.Empty;

            await LoadTicketDetailsAsync(ticketId);

            DetailsStatusMessage = "Wiadomość została wysłana.";

            ShowToast("Wiadomość została wysłana.", "success");

            await LogAuditAsync(
                "SendMessage",
                ticketId,
                "Wysłano wiadomość w zgłoszeniu.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Nie można wysłać wiadomości offline.";

            ShowToast("Brak połączenia z API. Wiadomość nie została wysłana.", "error");
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas wysyłania wiadomości.";

            ShowToast("Wystąpił błąd podczas wysyłania wiadomości.", "error");
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task UpdateTicketAsync()
    {
        if (!CanManageTickets)
        {
            DetailsStatusMessage = "Brak uprawnień do edycji zgłoszenia.";

            ShowToast("Brak uprawnień do edycji zgłoszenia.", "warning");

            return;
        }

        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można edytować zgłoszenia w trybie offline.";

            ShowToast("Nie można edytować zgłoszenia w trybie offline.", "warning");

            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedStatus))
        {
            DetailsStatusMessage = "Wybierz status zgłoszenia.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPriority))
        {
            DetailsStatusMessage = "Wybierz priorytet zgłoszenia.";
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Zapisywanie zmian...";

            var ticketId = TicketDetails.Id;

            var request = new UpdateTicketRequest
            {
                Status = StatusDisplayHelper.ToApiStatus(SelectedStatus),
                Priority = SelectedPriority
            };

            await _ticketService.UpdateTicketAsync(ticketId, request);

            IsOffline = false;

            await LoadTicketDetailsAsync(ticketId);
            await LoadTicketsAsync();

            DetailsStatusMessage = "Zmiany zostały zapisane.";

            ShowToast("Zmiany w zgłoszeniu zostały zapisane.", "success");

            await LogAuditAsync(
                "UpdateTicket",
                ticketId,
                $"Zmieniono status na „{SelectedStatus}”, priorytet na „{SelectedPriority}”.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Nie można zapisać zmian offline.";

            ShowToast("Brak połączenia z API. Zmiany nie zostały zapisane.", "error");
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas zapisywania zmian.";

            ShowToast("Wystąpił błąd podczas zapisywania zmian.", "error");
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task CloseTicketAsync()
    {
        if (!CanCloseTicket)
        {
            DetailsStatusMessage = "Brak uprawnień do zamknięcia zgłoszenia.";
            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie.";
            return;
        }

        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można zamknąć zgłoszenia w trybie offline.";
            return;
        }

        var ticketId = TicketDetails.Id;
        var previousDetails = TicketDetails;
        var previousStatus = SelectedStatus;
        var previousPriority = SelectedPriority;

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Zamykanie zgłoszenia...";

            var request = new UpdateTicketRequest
            {
                Status = "zamknięte"
            };

            var updatedTicket = await _ticketService.UpdateTicketAsync(ticketId, request);

            if (updatedTicket is not null && !IsValidTicketForDisplay(updatedTicket))
            {
                throw new ApiException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Serwer zwrócił stronę błędu zamiast danych API. Sprawdź endpoint lub uprawnienia.");
            }

            IsOffline = false;

            await LoadTicketDetailsAsync(ticketId);
            await LoadTicketsAsync();

            DetailsStatusMessage = "Zgłoszenie zostało zamknięte.";

            ShowToast("Zgłoszenie zostało zamknięte.", "success");

            await LogAuditAsync(
                "CloseTicket",
                ticketId,
                "Zamknięto zgłoszenie.");
        }
        catch (ApiException ex)
        {
            TicketDetails = previousDetails;
            SelectedStatus = previousStatus;
            SelectedPriority = previousPriority;

            var errorMessage = GetApiErrorMessage(ex);

            DetailsStatusMessage = errorMessage;

            ShowToast(errorMessage, "error");

            await LogAuditAsync(
                "CloseTicket",
                ticketId,
                "Nie udało się zamknąć zgłoszenia: brak uprawnień lub błąd serwera.");
        }
        catch
        {
            TicketDetails = previousDetails;
            SelectedStatus = previousStatus;
            SelectedPriority = previousPriority;

            DetailsStatusMessage = "Wystąpił błąd podczas zamykania zgłoszenia.";

            ShowToast("Nie udało się zamknąć zgłoszenia.", "error");

            await LogAuditAsync(
                "CloseTicket",
                ticketId,
                "Nie udało się zamknąć zgłoszenia: brak uprawnień lub błąd serwera.");
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task DeleteTicketAsync()
    {
        if (!CanDeleteTicket || TicketDetails is null)
        {
            DetailsStatusMessage = "Brak uprawnień do usunięcia zgłoszenia.";
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Usuwanie zgłoszenia...";

            var ticketId = TicketDetails.Id;
            await _ticketService.DeleteTicketAsync(ticketId);

            TicketDetails = null;
            SelectedTicket = null;
            CurrentSection = "Tickets";

            await LoadTicketsAsync();

            DetailsStatusMessage = "Zgłoszenie zostało usunięte.";
            ShowToast("Zgłoszenie zostało usunięte.", "success");

            await LogAuditAsync("DeleteTicket", ticketId, "Usunięto zgłoszenie.");
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            DetailsStatusMessage = "Nie udało się usunąć zgłoszenia.";
            ShowToast("Nie udało się usunąć zgłoszenia.", "error");
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task LoadAllPagesStatisticsAsync()
    {
        if (IsOffline)
        {
            StatsScopeMessage = "Statystyki wielostronicowe wymagają połączenia z API.";
            return;
        }

        try
        {
            IsLoadingAllStatistics = true;

            var aggregated = new List<Ticket>();
            var page = 1;
            var lastPage = 1;
            var totalInSystem = 0;

            do
            {
                var response = await _ticketService.GetTicketsAsync(
                    page: page,
                    perPage: 50,
                    queueView: TicketQueueView.All);

                if (response?.Data is null || response.Data.Count == 0)
                    break;

                aggregated.AddRange(response.Data);
                lastPage = Math.Max(1, response.LastPage);
                totalInSystem = response.Total;
                page++;
            } while (page <= lastPage);

            ApplyTicketStatistics(aggregated, totalInSystem, fromCurrentPageOnly: false);
            ShowToast($"Zaktualizowano statystyki ({aggregated.Count} zgłoszeń).", "success");
        }
        catch (ApiException ex)
        {
            StatsScopeMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            StatsScopeMessage = "Nie udało się pobrać statystyk ze wszystkich stron.";
            ShowToast("Nie udało się pobrać statystyk.", "error");
        }
        finally
        {
            IsLoadingAllStatistics = false;
        }
    }

    private async Task RefreshSessionAsync()
    {
        try
        {
            SettingsStatusMessage = "Odświeżanie sesji...";

            var refreshed = await _authService.RefreshTokenAsync();

            if (refreshed)
            {
                SettingsStatusMessage = "Sesja została odświeżona.";
                ShowToast("Token sesji został odświeżony.", "success");
                return;
            }

            SettingsStatusMessage = "Serwer nie zwrócił nowego tokenu (sprawdź POST /api/refresh).";
            ShowToast("Nie udało się odświeżyć sesji.", "warning");
        }
        catch (ApiException ex)
        {
            SettingsStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            SettingsStatusMessage = "Nie udało się odświeżyć sesji.";
            ShowToast("Nie udało się odświeżyć sesji.", "error");
        }
    }

    private async Task LoadAdminUsersAsync()
    {
        if (!IsAdminRole)
        {
            AdminStatusMessage = "Panel administracji jest dostępny tylko dla roli admin.";
            return;
        }

        try
        {
            AdminStatusMessage = "Pobieranie użytkowników...";

            var users = await _userAdminService.GetUsersAsync();

            AdminUsers.Clear();

            if (users is null || users.Count == 0)
            {
                AdminStatusMessage = "Brak użytkowników do wyświetlenia.";
                return;
            }

            foreach (var user in users.OrderBy(user => user.Login))
                AdminUsers.Add(user);

            AdminStatusMessage = $"Załadowano {AdminUsers.Count} użytkowników.";
        }
        catch (ApiException ex)
        {
            AdminStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            AdminStatusMessage = "Nie udało się pobrać listy użytkowników.";
            ShowToast("Nie udało się pobrać użytkowników.", "error");
        }
    }

    private async Task BanAdminUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        try
        {
            AdminStatusMessage = $"Banowanie użytkownika {SelectedAdminUser.Login}...";
            var login = SelectedAdminUser.Login;

            await _userAdminService.BanUserAsync(SelectedAdminUser.Id);
            await LoadAdminUsersAsync();
            ShowToast("Użytkownik został zbanowany.", "success");

            await LogAuditAsync("BanUser", null, $"Zbanowano użytkownika: {login}.");
        }
        catch (ApiException ex)
        {
            AdminStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            AdminStatusMessage = "Nie udało się zbanować użytkownika.";
            ShowToast("Nie udało się zbanować użytkownika.", "error");
        }
    }

    private async Task ActivateAdminUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        try
        {
            var login = SelectedAdminUser.Login;
            var wasBanned = SelectedAdminUser.Ban;

            AdminStatusMessage = $"Aktywacja użytkownika {login}...";
            await _userAdminService.ActivateUserAsync(SelectedAdminUser.Id);
            await LoadAdminUsersAsync();
            ShowToast("Użytkownik został aktywowany.", "success");

            await LogAuditAsync(
                wasBanned ? "UnbanUser" : "ActivateUser",
                null,
                wasBanned
                    ? $"Odbanowano użytkownika: {login}."
                    : $"Aktywowano użytkownika: {login}.");
        }
        catch (ApiException ex)
        {
            AdminStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            AdminStatusMessage = "Nie udało się aktywować użytkownika.";
            ShowToast("Nie udało się aktywować użytkownika.", "error");
        }
    }

    private async Task AssignToMeAsync()
    {
        if (!CanManageTickets)
        {
            DetailsStatusMessage = "Brak uprawnień do przypisania zgłoszenia.";

            ShowToast("Brak uprawnień do przypisania zgłoszenia.", "warning");

            return;
        }

        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można przypisać zgłoszenia w trybie offline.";

            ShowToast("Nie można przypisać zgłoszenia w trybie offline.", "warning");

            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie.";
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Przypisywanie zgłoszenia...";

            var ticketId = TicketDetails.Id;

            var request = new UpdateTicketRequest
            {
                AssignedItId = CurrentUser.Id
            };

            await _ticketService.UpdateTicketAsync(ticketId, request);

            IsOffline = false;

            await LoadTicketDetailsAsync(ticketId);
            await LoadTicketsAsync();

            DetailsStatusMessage = "Zgłoszenie zostało przypisane do Ciebie.";

            ShowToast("Zgłoszenie zostało przypisane do Ciebie.", "success");

            await LogAuditAsync(
                "AssignToMe",
                ticketId,
                $"Przypisano zgłoszenie do użytkownika {CurrentUser.Login}.");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Nie można przypisać zgłoszenia offline.";

            ShowToast("Brak połączenia z API. Zgłoszenie nie zostało przypisane.", "error");
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas przypisywania zgłoszenia.";

            ShowToast("Wystąpił błąd podczas przypisywania zgłoszenia.", "error");
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task LogoutAsync()
    {
        _ticketPollingTimer.Stop();

        await LogAuditAsync("Logout", null, "Wylogowano użytkownika z aplikacji desktopowej.");

        ShowToast("Wylogowano z aplikacji.", "info");

        await _onLogoutRequested();
    }

    private async Task LogAuditAsync(string action, int? ticketId, string description)
    {
        if (ApiErrorSanitizer.IsHtmlResponse(description))
        {
            description = ApiErrorSanitizer.SanitizeForDisplay(
                description,
                System.Net.HttpStatusCode.InternalServerError);
        }

        await _auditLogService.AddAsync(new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            UserLogin = CurrentUser.Login,
            Action = action,
            TicketId = ticketId,
            Description = description
        });
    }

    private async Task RefreshTicketAuditLogAsync(int ticketId)
    {
        var entries = await _auditLogService.LoadForTicketAsync(ticketId);

        TicketAuditLogEntries.Clear();

        foreach (var entry in entries)
            TicketAuditLogEntries.Add(entry);

        OnPropertyChanged(nameof(HasNoTicketAuditLogEntries));
    }

    private async Task RefreshSettingsAuditLogAsync()
    {
        var entries = await _auditLogService.LoadAsync();

        SettingsAuditLogEntries.Clear();

        foreach (var entry in entries.OrderByDescending(e => e.Timestamp))
            SettingsAuditLogEntries.Add(entry);

        OnPropertyChanged(nameof(HasNoSettingsAuditLogEntries));
    }

    private async Task ClearSettingsAuditLogAsync()
    {
        await _auditLogService.ClearAsync();
        await RefreshSettingsAuditLogAsync();
        ShowToast("Lokalny audyt został wyczyszczony.", "info");
    }

    private void SetCurrentPageSilently(int page)
    {
        _isChangingPageInternally = true;
        CurrentPage = page;
        _isChangingPageInternally = false;

        SetSelectedPageNumberSilently(CurrentPage);
    }

    private void SetSelectedPageNumberSilently(int? page)
    {
        if (_selectedPageNumber == page)
            return;

        _selectedPageNumber = page;
        OnPropertyChanged(nameof(SelectedPageNumber));
    }

    private void SetSelectedPageSizeSilently(int? pageSize)
    {
        if (_selectedPageSize == pageSize)
            return;

        _selectedPageSize = pageSize;
        OnPropertyChanged(nameof(SelectedPageSize));
    }

    private void SetSelectedTicketQueueViewSilently(string value)
    {
        if (_selectedTicketQueueView == value)
            return;

        _selectedTicketQueueView = value;
        OnPropertyChanged(nameof(SelectedTicketQueueView));
    }

    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();

        var pageCount = Math.Max(1, LastPage);

        for (var i = 1; i <= pageCount; i++)
            PageNumbers.Add(i);

        if (PageNumbers.Contains(CurrentPage))
            SetSelectedPageNumberSilently(CurrentPage);
        else
            SetSelectedPageNumberSilently(null);

        RefreshPaginationProperties();
    }

    private void RefreshPaginationProperties()
    {
        OnPropertyChanged(nameof(PageInfoText));
        OnPropertyChanged(nameof(IsOnLastPage));
        OnPropertyChanged(nameof(CanGoPreviousPage));
        OnPropertyChanged(nameof(CanGoNextPage));
        OnPropertyChanged(nameof(CanRefreshTicketsNow));
        OnPropertyChanged(nameof(PagePositionText));
        OnPropertyChanged(nameof(CanCloseTicket));
    }

    private TicketQueueView GetSelectedTicketQueueView()
    {
        return SelectedTicketQueueView switch
        {
            "Aktywne" => TicketQueueView.Active,
            "Nieprzypisane" => TicketQueueView.Unassigned,
            _ => TicketQueueView.All
        };
    }

    private static string? GetSelectedFilterValue(string value)
    {
        return string.Equals(value, "Wszystkie", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;
    }

    private void NotifyMessagesUiState()
    {
        OnPropertyChanged(nameof(HasNoMessages));
    }

    private static string GetApiErrorMessage(ApiException ex)
    {
        return ApiErrorSanitizer.SanitizeApiErrorMessage(
            ex.ResponseContent ?? ex.Message,
            ex.StatusCode);
    }

    private static bool IsValidTicketForDisplay(Ticket? ticket)
    {
        if (ticket is null)
            return false;

        return !ApiErrorSanitizer.IsHtmlResponse(ticket.Title) &&
               !ApiErrorSanitizer.IsHtmlResponse(ticket.Description);
    }
}