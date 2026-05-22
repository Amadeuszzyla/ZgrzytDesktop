using System;
using System.Collections.ObjectModel;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _statusMessage = "Gotowy.";
    private string _pollingStatusMessage = string.Empty;
    private string _searchText = string.Empty;
    private bool _autoRefreshErrorToastShown;

    private string _newTicketTitle = string.Empty;
    private string _newTicketDescription = string.Empty;
    private string _newTicketPriority = TicketPriorities.Low;
    private string _selectedNewTicketCategory = "Hardware";
    private string _createTicketStatusMessage = string.Empty;

    private TicketSortFieldOption? _selectedTicketSortField;
    private TicketSortDirectionOption? _selectedTicketSortDirection;

    private string _selectedFilterStatus = FilterLabels.All;
    private string _selectedFilterPriority = FilterLabels.All;
    private string _selectedTicketQueueView = FilterLabels.All;

    private bool _isLoading;
    private bool _isCheckingForNewTickets;
    private bool _isChangingPageInternally;

    private int _currentPage = 1;
    private int _lastPage = 1;
    private int _pageSize = 10;
    private int _totalTickets;

    private int? _selectedPageNumber = 1;
    private int? _selectedPageSize = 10;

    private Ticket? _selectedTicket;

    public ObservableCollection<Ticket> Tickets { get; } = new();

    public ObservableCollection<int> PageNumbers { get; } = new();

    public ObservableCollection<int> PageSizeOptions { get; } = new();

    public ObservableCollection<string> AvailableStatuses { get; } = new();

    public ObservableCollection<string> AvailablePriorities { get; } = new();

    public ObservableCollection<string> NewTicketCategories { get; } = new();

    public ObservableCollection<string> TicketQueueViews { get; } = new();

    public ObservableCollection<string> FilterStatuses { get; } = new();

    public ObservableCollection<string> FilterPriorities { get; } = new();

    public ObservableCollection<TicketSortFieldOption> TicketSortFields { get; } = new();

    public ObservableCollection<TicketSortDirectionOption> TicketSortDirections { get; } = new();

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

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string PollingStatusMessage
    {
        get => _pollingStatusMessage;
        set => SetProperty(ref _pollingStatusMessage, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
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
                StatisticsPanel.NotifyTicketsLoadingChanged();
                OnPropertyChanged(nameof(CanUseOnlineActions));
                OnPropertyChanged(nameof(CanRequestAccount));
                RefreshPaginationProperties();
            }
        }
    }

    public bool IsNotLoading => !IsLoading;

    public bool IsCheckingForNewTickets
    {
        get => _isCheckingForNewTickets;
        set
        {
            if (SetProperty(ref _isCheckingForNewTickets, value))
                OnPropertyChanged(nameof(CanRefreshTicketsNow));
        }
    }

    public bool CanManageTickets =>
        string.Equals(CurrentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(CurrentUser.Role, AppRoles.It, StringComparison.OrdinalIgnoreCase);

    public bool IsRegularUser =>
        string.Equals(CurrentUser.Role, AppRoles.User, StringComparison.OrdinalIgnoreCase);

    public bool CanUseOnlineActions => !IsOffline && !IsLoading;

    private void InitializeTicketCollections()
    {
        foreach (var size in new[] { 5, 10, 20, 50 })
            PageSizeOptions.Add(size);

        foreach (var status in new[]
                 {
                     TicketStatuses.DisplayNowe,
                     TicketStatuses.DisplayWToku,
                     TicketStatuses.DisplayRozwiazane
                 })
            AvailableStatuses.Add(status);

        foreach (var priority in new[]
                 {
                     TicketPriorities.Low,
                     TicketPriorities.Medium,
                     TicketPriorities.High
                 })
            AvailablePriorities.Add(priority);

        foreach (var category in TicketCategoryHelper.Categories)
            NewTicketCategories.Add(category);

        foreach (var view in new[]
                 {
                     FilterLabels.All,
                     FilterLabels.Active,
                     FilterLabels.Unassigned
                 })
            TicketQueueViews.Add(view);

        foreach (var status in new[]
                 {
                     FilterLabels.All,
                     TicketStatuses.Nowe,
                     TicketStatuses.WTrakcie,
                     TicketStatuses.Zamkniete
                 })
            FilterStatuses.Add(status);

        foreach (var priority in new[]
                 {
                     FilterLabels.All,
                     TicketPriorities.Low,
                     TicketPriorities.Medium,
                     TicketPriorities.High
                 })
            FilterPriorities.Add(priority);

        foreach (var field in TicketSortHelper.Fields)
            TicketSortFields.Add(field);

        foreach (var direction in TicketSortHelper.Directions)
            TicketSortDirections.Add(direction);
    }
}
