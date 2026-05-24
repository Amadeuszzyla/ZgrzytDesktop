using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketsPanelViewModel : ViewModelBase
{
    public const int AutoRefreshIntervalSeconds = 45;

    private readonly ITicketService _ticketService;
    private readonly ILocalTicketCacheService _ticketCacheService;
    private readonly TicketsPanelCallbacks _callbacks;
    private readonly List<Ticket> _allTickets = new();

    private string _statusMessage = AppStrings.Get("Tickets_StatusReady");
    private bool _autoRefreshErrorToastShown;

    private bool _isLoading;
    private bool _isCheckingForNewTickets;

    private Ticket? _selectedTicket;

    public TicketsPanelViewModel(
        ITicketService ticketService,
        ILocalTicketCacheService ticketCacheService,
        TicketsPanelCallbacks callbacks)
    {
        _ticketService = ticketService;
        _ticketCacheService = ticketCacheService;
        _callbacks = callbacks;

        InitializeCollections();
        Tickets.CollectionChanged += OnTicketsCollectionChanged;

        LoadTicketsCommand = new AsyncRelayCommand(() => LoadTicketsAsync());
        SearchTicketsCommand = new AsyncRelayCommand(SearchTicketsAsync);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        RefreshTicketsNowCommand = new AsyncRelayCommand(RefreshTicketsNowAsync);
        FirstPageCommand = new AsyncRelayCommand(GoToFirstPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(GoToPreviousPageAsync);
        NextPageCommand = new AsyncRelayCommand(GoToNextPageAsync);
        LastPageCommand = new AsyncRelayCommand(GoToLastPageAsync);
        CreateTicketCommand = new AsyncRelayCommand(CreateTicketAsync);
    }

    public ObservableCollection<Ticket> Tickets { get; } = new();

    public bool HasNoTickets => !IsLoading && Tickets.Count == 0;

    public ObservableCollection<string> NewTicketCategories { get; } = new();

    public ObservableCollection<int> PageNumbers { get; } = new();

    public ObservableCollection<int> PageSizeOptions { get; } = new();

    public ObservableCollection<TicketFilterOption> TicketQueueViewOptions { get; } = new();

    public ObservableCollection<TicketFilterOption> FilterStatusOptions { get; } = new();

    public ObservableCollection<TicketFilterOption> FilterPriorityOptions { get; } = new();

    public ObservableCollection<TicketFilterOption> FilterAssignmentOptions { get; } = new();

    public ObservableCollection<TicketCategoryFilterOption> FilterCategoryOptions { get; } = new();

    public ObservableCollection<TicketSortFieldOption> TicketSortFields { get; } = new();

    public ObservableCollection<TicketSortDirectionOption> TicketSortDirections { get; } = new();

    public IReadOnlyList<Ticket> AllTickets => _allTickets;

    public string AutoRefreshStatusText =>
        AppStrings.GetFormat("Tickets_AutoRefreshStatus", AutoRefreshIntervalSeconds);

    public Ticket? SelectedTicket
    {
        get => _selectedTicket;
        set
        {
            if (!SetProperty(ref _selectedTicket, value))
                return;

            if (value is not null)
                _callbacks.TicketSelected(value.Id);
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsNotLoading));
                NotifyTicketsUiState();
                _callbacks.NotifyTicketsLoadingChanged();
                _callbacks.NotifyOnlineActionsChanged();
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

    public IAsyncRelayCommand LoadTicketsCommand { get; }

    public IAsyncRelayCommand SearchTicketsCommand { get; }

    public IRelayCommand ClearFiltersCommand { get; }

    public IAsyncRelayCommand RefreshTicketsNowCommand { get; }

    public IAsyncRelayCommand FirstPageCommand { get; }

    public IAsyncRelayCommand PreviousPageCommand { get; }

    public IAsyncRelayCommand NextPageCommand { get; }

    public IAsyncRelayCommand LastPageCommand { get; }

    public IAsyncRelayCommand CreateTicketCommand { get; }

    public void NotifyLocalization()
    {
        OnPropertyChanged(nameof(AutoRefreshStatusText));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(LblFilterCategory));
        RefreshFilterCollections();
        RefreshFilterCategoryOptions();
        RefreshCategoryOptions();
        RefreshTicketDisplayLabels();
        OnPropertyChanged(nameof(TicketSortFields));
        OnPropertyChanged(nameof(TicketSortDirections));
        OnPropertyChanged(nameof(SelectedTicketSortField));
        OnPropertyChanged(nameof(SelectedTicketSortDirection));
    }

    public void RefreshTicketDisplayLabels()
    {
        var tickets = Tickets.ToList();
        Tickets.Clear();
        foreach (var ticket in tickets)
            Tickets.Add(ticket);
    }

    public void ApplyDefaultSort()
    {
        _selectedTicketSortField = TicketSortHelper.DefaultField;
        _selectedTicketSortDirection = TicketSortHelper.DefaultDirection;
        OnPropertyChanged(nameof(SelectedTicketSortField));
        OnPropertyChanged(nameof(SelectedTicketSortDirection));
    }

    public void ConfigureQueueViewsForRole(bool canManageTickets)
    {
        var queueView = _selectedTicketQueueView;

        TicketQueueViewOptions.Clear();
        TicketQueueViewOptions.Add(new TicketFilterOption(FilterLabels.All, TicketFilterOptionKind.Queue));

        if (canManageTickets)
        {
            TicketQueueViewOptions.Add(new TicketFilterOption(FilterLabels.Active, TicketFilterOptionKind.Queue));
            TicketQueueViewOptions.Add(new TicketFilterOption(FilterLabels.Unassigned, TicketFilterOptionKind.Queue));
        }

        if (TicketQueueViewOptions.All(option => option.Value != queueView))
            SetSelectedTicketQueueViewSilently(FilterLabels.All);
        else
            SetSelectedTicketQueueViewSilently(queueView);

        OnPropertyChanged(nameof(SelectedTicketQueueViewOption));
    }

    public void BootstrapPaginationSelection()
    {
        SetSelectedPageNumberSilently(CurrentPage);
        SetSelectedPageSizeSilently(PageSize);
        UpdatePageNumbers();
    }

    public Ticket? FindCachedTicket(int ticketId) =>
        _allTickets.FirstOrDefault(ticket => ticket.Id == ticketId);

    private void OnTicketsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        NotifyTicketsUiState();

    private void NotifyTicketsUiState() => OnPropertyChanged(nameof(HasNoTickets));
}
