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
    private string _pollingStatusMessage = "Auto-sprawdzanie nowych zgłoszeń działa na ostatniej stronie.";
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
    private double _statsStatusChartMaximum = 1;
    private double _statsPriorityChartMaximum = 1;
    private string _statsScopeMessage = "Brak pobranych zgłoszeń.";

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

    public ObservableCollection<AuditLogEntry> TicketAuditLogEntries { get; } = new();

    public bool HasNoTicketAuditLogEntries => TicketAuditLogEntries.Count == 0;

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
                OnPropertyChanged(nameof(CurrentSectionTitle));
            }
        }
    }

    public bool IsTicketsPageVisible => CurrentSection == "Tickets";

    public bool IsDetailsPageVisible => CurrentSection == "Details";

    public bool IsSettingsPageVisible => CurrentSection == "Settings";

    public bool IsRequestAccountPageVisible => CurrentSection == "RequestAccount";

    public bool IsStatisticsPageVisible => CurrentSection == "Statistics";

    public string CurrentSectionTitle => CurrentSection switch
    {
        "Tickets" => "Zgłoszenia",
        "Details" => "Szczegóły zgłoszenia",
        "Settings" => "Ustawienia",
        "RequestAccount" => "Zgłoszenie utworzenia konta",
        "Statistics" => "Statystyki",
        _ => "ZGRZYT Desktop"
    };

    public bool CanOpenDetailsPage => SelectedTicket is not null || TicketDetails is not null;

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
                OnPropertyChanged(nameof(CanCloseTicket));
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
                OnPropertyChanged(nameof(PageInfoText));
        }
    }

    public string PageInfoText => $"Strona {CurrentPage} z {LastPage} | Razem: {TotalTickets}";

    public bool IsOnLastPage => CurrentPage >= LastPage;

    public bool CanGoPreviousPage => CurrentPage > 1 && !IsLoading;

    public bool CanGoNextPage => CurrentPage < LastPage && !IsLoading;

    public bool CanCheckForNewTickets => IsOnLastPage && !IsLoading && !IsCheckingForNewTickets;

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
                OnPropertyChanged(nameof(CanCloseTicket));
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
                OnPropertyChanged(nameof(CanCheckForNewTickets));
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
                OnPropertyChanged(nameof(CanCloseTicket));
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

    public bool CanCloseTicket =>
        CanManageTickets &&
        !IsOffline &&
        !IsLoadingDetails &&
        TicketDetails is not null &&
        !string.Equals(TicketDetails.Status, "zamknięte", StringComparison.OrdinalIgnoreCase);

    public IRelayCommand ShowTicketsPageCommand { get; }

    public IRelayCommand ShowDetailsPageCommand { get; }

    public IRelayCommand ShowSettingsPageCommand { get; }

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

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public IAsyncRelayCommand TestApiConnectionCommand { get; }

    public IAsyncRelayCommand FirstPageCommand { get; }

    public IAsyncRelayCommand PreviousPageCommand { get; }

    public IAsyncRelayCommand NextPageCommand { get; }

    public IAsyncRelayCommand LastPageCommand { get; }

    public IAsyncRelayCommand CheckForNewTicketsCommand { get; }

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
        _onLogoutRequested = onLogoutRequested;

        ApiBaseUrl = _apiService.CurrentApiBaseUrl;

        var appSettings = _settingsService.LoadSync();
        SelectedThemeMode = appSettings.ThemeMode;

        ShowTicketsPageCommand = new RelayCommand(ShowTicketsPage);
        ShowDetailsPageCommand = new RelayCommand(ShowDetailsPage);
        ShowSettingsPageCommand = new RelayCommand(ShowSettingsPage);
        ShowRequestAccountPageCommand = new RelayCommand(ShowRequestAccountPage);
        ShowStatisticsPageCommand = new RelayCommand(ShowStatisticsPage);
        RequestAccountCommand = new AsyncRelayCommand(RequestAccountAsync);

        LoadTicketsCommand = new AsyncRelayCommand(LoadTicketsAsync);
        SearchTicketsCommand = new AsyncRelayCommand(SearchTicketsAsync);
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        CreateTicketCommand = new AsyncRelayCommand(CreateTicketAsync);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
        UpdateTicketCommand = new AsyncRelayCommand(UpdateTicketAsync);
        AssignToMeCommand = new AsyncRelayCommand(AssignToMeAsync);
        CloseTicketCommand = new AsyncRelayCommand(CloseTicketAsync);

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        TestApiConnectionCommand = new AsyncRelayCommand(TestApiConnectionAsync);

        FirstPageCommand = new AsyncRelayCommand(GoToFirstPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(GoToPreviousPageAsync);
        NextPageCommand = new AsyncRelayCommand(GoToNextPageAsync);
        LastPageCommand = new AsyncRelayCommand(GoToLastPageAsync);
        CheckForNewTicketsCommand = new AsyncRelayCommand(() => CheckForNewTicketsAsync(manualCheck: true));

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
            Interval = TimeSpan.FromSeconds(8)
        };

        _ticketPollingTimer.Tick += async (_, _) =>
        {
            await CheckForNewTicketsAsync(manualCheck: false);
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

    private void ShowDetailsPage()
    {
        if (!CanOpenDetailsPage)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie z listy.";
            return;
        }

        CurrentSection = "Details";
    }

    private void ShowSettingsPage()
    {
        CurrentSection = "Settings";
    }

    private void ShowRequestAccountPage()
    {
        CurrentSection = "RequestAccount";
    }

    private void ShowStatisticsPage()
    {
        CurrentSection = "Statistics";
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
            SettingsStatusMessage = "Zapisywanie ustawień...";

            var settings = new AppSettings
            {
                ApiBaseUrl = ApiBaseUrl,
                ThemeMode = SelectedThemeMode
            };

            await _settingsService.SaveAsync(settings);

            var normalizedUrl = _settingsService.NormalizeApiBaseUrl(settings.ApiBaseUrl);
            ApiBaseUrl = normalizedUrl;
            _apiService.SetBaseAddress(normalizedUrl);

            SelectedThemeMode = settings.ThemeMode;
            SettingsService.ApplyThemeMode(settings.ThemeMode);

            SettingsStatusMessage = "Ustawienia zostały zapisane.";

            ShowToast("Ustawienia aplikacji zostały zapisane.", "success");
        }
        catch
        {
            SettingsStatusMessage = "Nie udało się zapisać ustawień.";

            ShowToast("Nie udało się zapisać ustawień aplikacji.", "error");
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

    private async Task LoadTicketsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Pobieranie zgłoszeń...";

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
                    await LoadTicketsAsync();
                    return;
                }

                await _ticketCacheService.SaveTicketsAsync(_allTickets);

                ApplyVisibleTickets();
                UpdateTicketStatistics();

                StatusMessage = $"Pobrano zgłoszeń: {Tickets.Count} z {TotalTickets}";
                PollingStatusMessage = IsOnLastPage
                    ? "Jesteś na ostatniej stronie — aplikacja automatycznie sprawdza nowe zgłoszenia."
                    : "Auto-sprawdzanie nowych zgłoszeń działa tylko na ostatniej stronie.";
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

            ShowToast("Brak połączenia z API. Pokazuję dane offline.", "warning");

            await LoadTicketsFromCacheAsync();
        }
        catch (ApiException ex)
        {
            StatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            StatusMessage = "Wystąpił nieoczekiwany błąd podczas pobierania zgłoszeń.";

            ShowToast("Wystąpił błąd podczas pobierania zgłoszeń.", "error");
        }
        finally
        {
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
        var tickets = _allTickets;

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

        StatsStatusChartMaximum = Math.Max(
            1,
            Math.Max(StatsNewTickets, Math.Max(StatsInProgressTickets, StatsClosedTickets)));
        StatsPriorityChartMaximum = Math.Max(
            1,
            Math.Max(StatsLowPriorityTickets, Math.Max(StatsMediumPriorityTickets, StatsHighPriorityTickets)));

        StatsScopeMessage = StatsTotalTickets == 0
            ? "Brak zgłoszeń do analizy na bieżącej stronie."
            : TotalTickets > StatsTotalTickets
                ? $"Statystyki dla {StatsTotalTickets} zgłoszeń pobranych na bieżącej stronie (łącznie w systemie: {TotalTickets})."
                : $"Statystyki dla {StatsTotalTickets} pobranych zgłoszeń.";
    }

    private async Task CheckForNewTicketsAsync(bool manualCheck)
    {
        if (IsOffline || IsLoading || IsCheckingForNewTickets)
            return;

        if (!IsOnLastPage)
        {
            if (manualCheck)
                PollingStatusMessage = "Nowe zgłoszenia są sprawdzane tylko na ostatniej stronie.";

            return;
        }

        try
        {
            IsCheckingForNewTickets = true;

            var response = await _ticketService.GetTicketsAsync(
                page: CurrentPage,
                perPage: PageSize,
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                status: GetSelectedFilterValue(SelectedFilterStatus),
                priority: GetSelectedFilterValue(SelectedFilterPriority),
                queueView: GetSelectedTicketQueueView()
            );

            if (response is null)
                return;

            if (response.Total > TotalTickets)
            {
                var difference = response.Total - TotalTickets;

                ShowToast($"Pojawiło się nowych zgłoszeń: {difference}.", "info");

                PollingStatusMessage = $"Wykryto nowe zgłoszenia: {difference}. Lista została odświeżona.";

                await LoadTicketsAsync();
            }
            else if (manualCheck)
            {
                PollingStatusMessage = "Brak nowych zgłoszeń.";
            }
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            PollingStatusMessage = "Brak połączenia z API. Auto-sprawdzanie zatrzymane do czasu powrotu połączenia.";

            ShowToast("Utracono połączenie z API.", "warning");
        }
        catch
        {
            if (manualCheck)
            {
                PollingStatusMessage = "Nie udało się sprawdzić nowych zgłoszeń.";
                ShowToast("Nie udało się sprawdzić nowych zgłoszeń.", "error");
            }
        }
        finally
        {
            IsCheckingForNewTickets = false;
            RefreshPaginationProperties();
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

                SelectedStatus = StatusDisplayHelper.ToDisplayStatus(cachedTicket?.Status);
                SelectedPriority = cachedTicket?.Priority;

                DetailsStatusMessage = cachedTicket is null
                    ? "Nie znaleziono zgłoszenia w danych offline."
                    : $"Wybrano zgłoszenie #{cachedTicket.Id} z cache offline.";

                return;
            }

            var ticket = await _ticketService.GetTicketAsync(ticketId);

            IsOffline = false;

            TicketDetails = ticket;

            Messages.Clear();

            var messages = await _ticketService.GetTicketMessagesAsync(ticketId);

            foreach (var message in messages)
                Messages.Add(message);

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
        if (!CanManageTickets)
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

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Zamykanie zgłoszenia...";

            var ticketId = TicketDetails.Id;

            var request = new UpdateTicketRequest
            {
                Status = "zamknięte"
            };

            await _ticketService.UpdateTicketAsync(ticketId, request);

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
            DetailsStatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił błąd podczas zamykania zgłoszenia.";

            ShowToast("Nie udało się zamknąć zgłoszenia.", "error");
        }
        finally
        {
            IsLoadingDetails = false;
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
        OnPropertyChanged(nameof(CanCheckForNewTickets));
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

    private static string GetApiErrorMessage(ApiException ex)
    {
        return ex.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized =>
                "Sesja wygasła albo użytkownik nie jest zalogowany.",

            System.Net.HttpStatusCode.Forbidden =>
                "Brak uprawnień do wykonania tej operacji.",

            System.Net.HttpStatusCode.NotFound =>
                "Nie znaleziono wybranego zasobu.",

            System.Net.HttpStatusCode.Conflict =>
                "Operacja jest sprzeczna z aktualnym stanem danych.",

            System.Net.HttpStatusCode.UnprocessableEntity =>
                "Dane formularza są niepoprawne. Sprawdź wymagane pola.",

            System.Net.HttpStatusCode.ServiceUnavailable =>
                "Brak połączenia z API. Sprawdź, czy backend Laravel działa.",

            System.Net.HttpStatusCode.InternalServerError =>
                "Wystąpił błąd po stronie serwera.",

            _ => ex.Message
        };
    }
}