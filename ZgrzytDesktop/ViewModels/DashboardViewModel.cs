using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
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

    private string _currentSection = AppSections.Tickets;

    private string _statusMessage = "Gotowy.";
    private string _detailsStatusMessage = "Wybierz zgłoszenie z listy.";
    private string _settingsStatusMessage = "Ustawienia gotowe.";
    private string _pollingStatusMessage = string.Empty;
    private string _adminTab = "Users";
    private bool _autoRefreshErrorToastShown;
    private string _newMessageText = string.Empty;
    private string _searchText = string.Empty;
    private string _selectedThemeMode = "System";
    private string _selectedUiCulture = "pl";
    private string _adminUnbanPassword = string.Empty;

    private TicketSortFieldOption? _selectedTicketSortField;
    private TicketSortDirectionOption? _selectedTicketSortDirection;
    private AdminListFilterOption? _selectedAdminUserListFilterOption;

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

    public ObservableCollection<TicketSortFieldOption> TicketSortFields { get; } =
        new(TicketSortHelper.Fields);

    public ObservableCollection<TicketSortDirectionOption> TicketSortDirections { get; } =
        new(TicketSortHelper.Directions);

    public ObservableCollection<AdminListFilterOption> AdminUserListFilterOptions { get; } =
        new(AdminListFilterOption.All);

    public ObservableCollection<string> UiCultures { get; } = new()
    {
        "pl",
        "en"
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

    public bool IsTicketsNavActive => CurrentSection == AppSections.Tickets;

    public bool IsRequestAccountNavActive => CurrentSection == AppSections.RequestAccount;

    public bool IsStatisticsNavActive => CurrentSection == AppSections.Statistics;

    public bool IsSettingsNavActive => CurrentSection == AppSections.Settings;

    public bool IsAdminNavActive => CurrentSection == AppSections.Admin;

    public bool IsTicketsPageVisible => CurrentSection == AppSections.Tickets;

    public bool IsDetailsPageVisible => CurrentSection == AppSections.Details;

    public bool IsSettingsPageVisible => CurrentSection == AppSections.Settings;

    public bool IsRequestAccountPageVisible => CurrentSection == AppSections.RequestAccount;

    public bool IsStatisticsPageVisible => CurrentSection == AppSections.Statistics;

    public bool IsAdminPageVisible => CurrentSection == AppSections.Admin;

    public bool IsAdminRole =>
        string.Equals(CurrentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

    public bool IsStaffRole =>
        IsAdminRole ||
        string.Equals(CurrentUser.Role, AppRoles.It, StringComparison.OrdinalIgnoreCase);

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
        AppSections.Tickets => AppStrings.Get("Section_Tickets"),
        AppSections.Details => AppStrings.Get("Section_Details"),
        AppSections.Settings => AppStrings.Get("Section_Settings"),
        AppSections.RequestAccount => AppStrings.Get("Section_RequestAccount"),
        AppSections.Statistics => AppStrings.Get("Section_Statistics"),
        AppSections.Admin => AppStrings.Get("Section_Admin"),
        _ => AppStrings.Get("App_Title")
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

    public string SelectedThemeMode
    {
        get => _selectedThemeMode;
        set => SetProperty(ref _selectedThemeMode, value);
    }

    public string SelectedUiCulture
    {
        get => _selectedUiCulture;
        set => SetProperty(ref _selectedUiCulture, value);
    }

    public TicketSortFieldOption? SelectedTicketSortField
    {
        get => _selectedTicketSortField;
        set
        {
            if (SetProperty(ref _selectedTicketSortField, value))
            {
                SetCurrentPageSilently(1);
                _ = LoadTicketsAsync();
            }
        }
    }

    public TicketSortDirectionOption? SelectedTicketSortDirection
    {
        get => _selectedTicketSortDirection;
        set
        {
            if (SetProperty(ref _selectedTicketSortDirection, value))
            {
                SetCurrentPageSilently(1);
                _ = LoadTicketsAsync();
            }
        }
    }

    public AdminListFilterOption? SelectedAdminUserListFilterOption
    {
        get => _selectedAdminUserListFilterOption;
        set
        {
            if (SetProperty(ref _selectedAdminUserListFilterOption, value))
                _ = LoadAdminUsersAsync();
        }
    }

    public string AdminUnbanPassword
    {
        get => _adminUnbanPassword;
        set => SetProperty(ref _adminUnbanPassword, value);
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
        string.Equals(CurrentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(CurrentUser.Role, AppRoles.It, StringComparison.OrdinalIgnoreCase);

    public bool IsRegularUser =>
        string.Equals(CurrentUser.Role, AppRoles.User, StringComparison.OrdinalIgnoreCase);

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

    public IAsyncRelayCommand UnbanAdminUserCommand { get; }

    public IRelayCommand ShowAdminPageCommand { get; }

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public IAsyncRelayCommand LoadAuditLogsCommand { get; }

    public IAsyncRelayCommand ClearAuditLogsCommand { get; }

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
        UserAdminService userAdminService,
        Func<Task> onLogoutRequested)
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

        var appSettings = _settingsService.LoadSync();
        SelectedThemeMode = appSettings.ThemeMode;
        SelectedUiCulture = SettingsService.NormalizeUiCulture(appSettings.UiCulture);
        AppStrings.ApplyCulture(SelectedUiCulture);
        SettingsService.ApplyThemeMode(appSettings.ThemeMode);

        _selectedTicketSortField = TicketSortHelper.DefaultField;
        _selectedTicketSortDirection = TicketSortHelper.DefaultDirection;
        _selectedAdminUserListFilterOption = AdminListFilterOption.All[0];
        OnPropertyChanged(nameof(SelectedTicketSortField));
        OnPropertyChanged(nameof(SelectedTicketSortDirection));
        OnPropertyChanged(nameof(SelectedAdminUserListFilterOption));

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
        UnbanAdminUserCommand = new AsyncRelayCommand(UnbanAdminUserAsync, () => CanUnbanAdminUser);

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        LoadAuditLogsCommand = new AsyncRelayCommand(RefreshSettingsAuditLogAsync);
        ClearAuditLogsCommand = new AsyncRelayCommand(ClearSettingsAuditLogAsync);

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
}
