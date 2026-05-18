using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly TicketService _ticketService;
    private readonly ApiService _apiService;
    private readonly SettingsService _settingsService;
    private readonly LocalTicketCacheService _ticketCacheService;
    private readonly SystemNotificationService _notificationService = new();
    private readonly Func<Task> _onLogoutRequested;
    private readonly List<Ticket> _allTickets = new();
    private readonly DispatcherTimer _ticketPollingTimer;

    private string _currentSection = "Tickets";

    private string _statusMessage = "Gotowy.";
    private string _detailsStatusMessage = "Wybierz zgłoszenie z listy.";
    private string _settingsStatusMessage = "Ustawienia gotowe.";
    private string _pollingStatusMessage = "Auto-sprawdzanie nowych zgłoszeń działa na ostatniej stronie.";
    private string _newMessageText = string.Empty;
    private string _searchText = string.Empty;
    private string _apiBaseUrl = "http://127.0.0.1:9000/api/";

    private string _newTicketTitle = string.Empty;
    private string _newTicketDescription = string.Empty;
    private string _newTicketPriority = "niski";
    private string _createTicketStatusMessage = string.Empty;

    private string? _selectedStatus;
    private string? _selectedPriority;

    private string _selectedFilterStatus = "Wszystkie";
    private string _selectedFilterPriority = "Wszystkie";

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
        "nowe",
        "w trakcie",
        "zamknięte"
    };

    public ObservableCollection<string> AvailablePriorities { get; } = new()
    {
        "niski",
        "średni",
        "wysoki"
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
                OnPropertyChanged(nameof(CurrentSectionTitle));
            }
        }
    }

    public bool IsTicketsPageVisible => CurrentSection == "Tickets";

    public bool IsDetailsPageVisible => CurrentSection == "Details";

    public bool IsSettingsPageVisible => CurrentSection == "Settings";

    public string CurrentSectionTitle => CurrentSection switch
    {
        "Tickets" => "Zgłoszenia",
        "Details" => "Szczegóły zgłoszenia",
        "Settings" => "Ustawienia",
        _ => "ZGRZYT Desktop"
    };

    public bool CanOpenDetailsPage => SelectedTicket is not null || TicketDetails is not null;

    public Ticket? SelectedTicket
    {
        get => _selectedTicket;
        set
        {
            if (SetProperty(ref _selectedTicket, value) && value is not null)
            {
                OnPropertyChanged(nameof(CanOpenDetailsPage));
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

    public string CreateTicketStatusMessage
    {
        get => _createTicketStatusMessage;
        set => SetProperty(ref _createTicketStatusMessage, value);
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
                {
                    CurrentPage = value.Value;
                }
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
                {
                    PageSize = value.Value;
                }
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
                {
                    _ = LoadTicketsAsync();
                }
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
                {
                    SetCurrentPageSilently(value);
                }

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
            }
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
            {
                OnPropertyChanged(nameof(CanTestApiConnection));
            }
        }
    }

    public bool CanTestApiConnection => !IsTestingApiConnection;

    public bool IsCheckingForNewTickets
    {
        get => _isCheckingForNewTickets;
        set
        {
            if (SetProperty(ref _isCheckingForNewTickets, value))
            {
                OnPropertyChanged(nameof(CanCheckForNewTickets));
            }
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
        TicketService ticketService,
        ApiService apiService,
        SettingsService settingsService,
        LocalTicketCacheService ticketCacheService,
        Func<Task> onLogoutRequested)
    {
        CurrentUser = currentUser;
        _ticketService = ticketService;
        _apiService = apiService;
        _settingsService = settingsService;
        _ticketCacheService = ticketCacheService;
        _onLogoutRequested = onLogoutRequested;

        ApiBaseUrl = _apiService.CurrentApiBaseUrl;

        ShowTicketsPageCommand = new RelayCommand(ShowTicketsPage);
        ShowDetailsPageCommand = new RelayCommand(ShowDetailsPage);
        ShowSettingsPageCommand = new RelayCommand(ShowSettingsPage);

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

        _notificationService.ShowInfo(
            "ZGRZYT Desktop",
            $"Zalogowano jako {CurrentUser.Name}."
        );

        _ = LoadTicketsAsync();
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
                ApiBaseUrl = ApiBaseUrl
            };

            await _settingsService.SaveAsync(settings);

            var normalizedUrl = _settingsService.NormalizeApiBaseUrl(settings.ApiBaseUrl);
            ApiBaseUrl = normalizedUrl;
            _apiService.SetBaseAddress(normalizedUrl);

            SettingsStatusMessage = "Ustawienia zostały zapisane.";

            _notificationService.ShowSuccess(
                "ZGRZYT Desktop",
                "Ustawienia aplikacji zostały zapisane."
            );
        }
        catch
        {
            SettingsStatusMessage = "Nie udało się zapisać ustawień.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Nie udało się zapisać ustawień aplikacji."
            );
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
                _notificationService.ShowSuccess(
                    "ZGRZYT Desktop",
                    "Połączenie z API działa poprawnie."
                );
            }
            else
            {
                _notificationService.ShowError(
                    "ZGRZYT Desktop",
                    "Nie udało się połączyć z API."
                );
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
                priority: GetSelectedFilterValue(SelectedFilterPriority)
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
                StatusMessage = "Brak zgłoszeń.";
            }
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Brak połączenia z API. Pokazuję dane offline."
            );

            await LoadTicketsFromCacheAsync();
        }
        catch (ApiException ex)
        {
            StatusMessage = GetApiErrorMessage(ex);

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                GetApiErrorMessage(ex)
            );
        }
        catch
        {
            StatusMessage = "Wystąpił nieoczekiwany błąd podczas pobierania zgłoszeń.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Wystąpił błąd podczas pobierania zgłoszeń."
            );
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
        {
            Tickets.Add(ticket);
        }

        UpdatePageNumbers();

        StatusMessage = cachedTickets.Count > 0
            ? $"Brak połączenia z API. Pokazuję dane offline: {Tickets.Count} zgłoszeń."
            : "Brak połączenia z API i brak zapisanych danych offline.";
    }

    private async Task CheckForNewTicketsAsync(bool manualCheck)
    {
        if (IsOffline || IsLoading || IsCheckingForNewTickets)
            return;

        if (!IsOnLastPage)
        {
            if (manualCheck)
            {
                PollingStatusMessage = "Nowe zgłoszenia są sprawdzane tylko na ostatniej stronie.";
            }

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
                priority: GetSelectedFilterValue(SelectedFilterPriority)
            );

            if (response is null)
                return;

            if (response.Total > TotalTickets)
            {
                var difference = response.Total - TotalTickets;

                _notificationService.ShowInfo(
                    "ZGRZYT Desktop",
                    $"Pojawiło się nowych zgłoszeń: {difference}."
                );

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

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Utracono połączenie z API."
            );
        }
        catch
        {
            if (manualCheck)
            {
                PollingStatusMessage = "Nie udało się sprawdzić nowych zgłoszeń.";
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
        {
            Tickets.Add(ticket);
        }

        UpdatePageNumbers();
        RefreshPaginationProperties();
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedFilterStatus = "Wszystkie";
        SelectedFilterPriority = "Wszystkie";
        SetCurrentPageSilently(1);

        _ = LoadTicketsAsync();
    }

    private async Task CreateTicketAsync()
    {
        if (IsOffline)
        {
            CreateTicketStatusMessage = "Nie można utworzyć zgłoszenia w trybie offline.";

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Nie można utworzyć zgłoszenia w trybie offline."
            );

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

        try
        {
            IsLoading = true;
            CreateTicketStatusMessage = "Tworzenie zgłoszenia...";

            var request = new CreateTicketRequest
            {
                Title = NewTicketTitle.Trim(),
                Description = NewTicketDescription.Trim(),
                Priority = NewTicketPriority
            };

            var createdTicket = await _ticketService.CreateTicketAsync(request);

            IsOffline = false;

            NewTicketTitle = string.Empty;
            NewTicketDescription = string.Empty;
            NewTicketPriority = "niski";

            SetCurrentPageSilently(1);
            await LoadTicketsAsync();

            if (createdTicket is not null)
            {
                SelectedTicket = Tickets.FirstOrDefault(ticket => ticket.Id == createdTicket.Id);
            }

            CreateTicketStatusMessage = "Zgłoszenie zostało utworzone.";

            _notificationService.ShowSuccess(
                "ZGRZYT Desktop",
                "Nowe zgłoszenie zostało utworzone."
            );
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            CreateTicketStatusMessage = "Brak połączenia z API. Nie można utworzyć zgłoszenia offline.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Brak połączenia z API. Nie można utworzyć zgłoszenia."
            );
        }
        catch (ApiException ex)
        {
            CreateTicketStatusMessage = GetApiErrorMessage(ex);

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                GetApiErrorMessage(ex)
            );
        }
        catch
        {
            CreateTicketStatusMessage = "Wystąpił nieoczekiwany błąd podczas tworzenia zgłoszenia.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Wystąpił błąd podczas tworzenia zgłoszenia."
            );
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
                    {
                        Messages.Add(message);
                    }
                }

                SelectedStatus = cachedTicket?.Status;
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

            if (ticket?.Messages is not null)
            {
                foreach (var message in ticket.Messages)
                {
                    Messages.Add(message);
                }
            }

            SelectedStatus = ticket?.Status;
            SelectedPriority = ticket?.Priority;

            DetailsStatusMessage = ticket is null
                ? "Nie udało się pobrać szczegółów zgłoszenia."
                : $"Wybrano zgłoszenie #{ticket.Id}.";
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Pokazuję dane dostępne offline.";

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Brak połączenia z API. Szczegóły zgłoszenia są dostępne offline."
            );
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                GetApiErrorMessage(ex)
            );
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas pobierania szczegółów zgłoszenia.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Wystąpił błąd podczas pobierania szczegółów zgłoszenia."
            );
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task SendMessageAsync()
    {
        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można wysłać wiadomości w trybie offline.";

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Nie można wysłać wiadomości w trybie offline."
            );

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

            _notificationService.ShowSuccess(
                "ZGRZYT Desktop",
                "Wiadomość została wysłana."
            );
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Nie można wysłać wiadomości offline.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Brak połączenia z API. Wiadomość nie została wysłana."
            );
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                GetApiErrorMessage(ex)
            );
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas wysyłania wiadomości.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Wystąpił błąd podczas wysyłania wiadomości."
            );
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

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Brak uprawnień do edycji zgłoszenia."
            );

            return;
        }

        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można edytować zgłoszenia w trybie offline.";

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Nie można edytować zgłoszenia w trybie offline."
            );

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
                Status = SelectedStatus,
                Priority = SelectedPriority
            };

            await _ticketService.UpdateTicketAsync(ticketId, request);

            IsOffline = false;

            await LoadTicketDetailsAsync(ticketId);
            await LoadTicketsAsync();

            DetailsStatusMessage = "Zmiany zostały zapisane.";

            _notificationService.ShowSuccess(
                "ZGRZYT Desktop",
                "Zmiany w zgłoszeniu zostały zapisane."
            );
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Nie można zapisać zmian offline.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Brak połączenia z API. Zmiany nie zostały zapisane."
            );
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                GetApiErrorMessage(ex)
            );
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas zapisywania zmian.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Wystąpił błąd podczas zapisywania zmian."
            );
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

            _notificationService.ShowSuccess(
                "ZGRZYT Desktop",
                "Zgłoszenie zostało zamknięte."
            );
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                GetApiErrorMessage(ex)
            );
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił błąd podczas zamykania zgłoszenia.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Nie udało się zamknąć zgłoszenia."
            );
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

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Brak uprawnień do przypisania zgłoszenia."
            );

            return;
        }

        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można przypisać zgłoszenia w trybie offline.";

            _notificationService.ShowWarning(
                "ZGRZYT Desktop",
                "Nie można przypisać zgłoszenia w trybie offline."
            );

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

            _notificationService.ShowSuccess(
                "ZGRZYT Desktop",
                "Zgłoszenie zostało przypisane do Ciebie."
            );
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Nie można przypisać zgłoszenia offline.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Brak połączenia z API. Zgłoszenie nie zostało przypisane."
            );
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                GetApiErrorMessage(ex)
            );
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas przypisywania zgłoszenia.";

            _notificationService.ShowError(
                "ZGRZYT Desktop",
                "Wystąpił błąd podczas przypisywania zgłoszenia."
            );
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task LogoutAsync()
    {
        _ticketPollingTimer.Stop();

        _notificationService.ShowInfo(
            "ZGRZYT Desktop",
            "Wylogowano z aplikacji."
        );

        await _onLogoutRequested();
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

    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();

        var pageCount = Math.Max(1, LastPage);

        for (var i = 1; i <= pageCount; i++)
        {
            PageNumbers.Add(i);
        }

        if (PageNumbers.Contains(CurrentPage))
        {
            SetSelectedPageNumberSilently(CurrentPage);
        }
        else
        {
            SetSelectedPageNumberSilently(null);
        }

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