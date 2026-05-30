using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed class StatisticsPanelViewModel : ViewModelBase
{
    private readonly ITicketService _ticketService;
    private readonly IDashboardContext _context;
    private readonly Func<bool> _getTicketsNotLoading;

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
    private string _statsScopeMessage = AppStrings.Get("Stats_Scope_NoData");
    private bool _isLoadingAllStatistics;
    private bool _fromCurrentPageOnly;
    private int _totalInSystem;

    public StatisticsPanelViewModel(
        ITicketService ticketService,
        IDashboardContext context,
        Func<bool> getTicketsNotLoading)
    {
        _ticketService = ticketService;
        _context = context;
        _getTicketsNotLoading = getTicketsNotLoading;

        LoadAllPagesStatisticsCommand = new AsyncRelayCommand(LoadAllPagesStatisticsAsync);
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

    public bool IsLoadingAllStatistics
    {
        get => _isLoadingAllStatistics;
        private set
        {
            if (SetProperty(ref _isLoadingAllStatistics, value))
                OnPropertyChanged(nameof(IsNotLoading));
        }
    }

    public bool IsNotLoading => _getTicketsNotLoading() && !IsLoadingAllStatistics;

    public string LblStatsTitle => AppStrings.Get("Stats_Title");

    public string LblStatsLoadAll => AppStrings.Get("Stats_LoadAll");

    public string LblStatsKpiAll => AppStrings.Get("Stats_KpiAll");

    public string LblStatsKpiNew => AppStrings.Get("Stats_KpiNew");

    public string LblStatsKpiInProgress => AppStrings.Get("Stats_KpiInProgress");

    public string LblStatsKpiClosed => AppStrings.Get("Stats_KpiClosed");

    public string LblStatsKpiHighPriority => AppStrings.Get("Stats_KpiHighPriority");

    public string LblStatsLoadingAllPages => AppStrings.Get("Stats_LoadingAllPages");

    public string LblStatsChartByStatus => AppStrings.Get("Stats_ChartByStatus");

    public string LblStatsChartByPriority => AppStrings.Get("Stats_ChartByPriority");

    public string LblStatsChartAssignment => AppStrings.Get("Stats_ChartAssignment");

    public string LblStatsAssigned => AppStrings.Get("Stats_Assigned");

    public string LblStatsUnassigned => AppStrings.Get("Stats_Unassigned");

    public string LblStatsPriorityLow => AppStrings.Get("Stats_PriorityLow");

    public string LblStatsPriorityMedium => AppStrings.Get("Stats_PriorityMedium");

    public string LblStatsPriorityHigh => AppStrings.Get("Stats_PriorityHigh");

    public IAsyncRelayCommand LoadAllPagesStatisticsCommand { get; }

    public void NotifyLocalization()
    {
        OnPropertyChanged(nameof(LblStatsTitle));
        OnPropertyChanged(nameof(LblStatsLoadAll));
        OnPropertyChanged(nameof(LblStatsKpiAll));
        OnPropertyChanged(nameof(LblStatsKpiNew));
        OnPropertyChanged(nameof(LblStatsKpiInProgress));
        OnPropertyChanged(nameof(LblStatsKpiClosed));
        OnPropertyChanged(nameof(LblStatsKpiHighPriority));
        OnPropertyChanged(nameof(LblStatsLoadingAllPages));
        OnPropertyChanged(nameof(LblStatsChartByStatus));
        OnPropertyChanged(nameof(LblStatsChartByPriority));
        OnPropertyChanged(nameof(LblStatsChartAssignment));
        OnPropertyChanged(nameof(LblStatsAssigned));
        OnPropertyChanged(nameof(LblStatsUnassigned));
        OnPropertyChanged(nameof(LblStatsPriorityLow));
        OnPropertyChanged(nameof(LblStatsPriorityMedium));
        OnPropertyChanged(nameof(LblStatsPriorityHigh));
        RefreshStatsScopeMessage();
    }

    public void NotifyTicketsLoadingChanged() => OnPropertyChanged(nameof(IsNotLoading));

    public void ApplyFromTickets(IReadOnlyList<Ticket> tickets, int totalInSystem, bool fromCurrentPageOnly) =>
        ApplyTicketStatistics(tickets, totalInSystem, fromCurrentPageOnly);

    private void ApplyTicketStatistics(IReadOnlyList<Ticket> tickets, int totalInSystem, bool fromCurrentPageOnly)
    {
        var snapshot = TicketStatisticsCalculator.Compute(tickets);

        StatsTotalTickets = snapshot.Total;
        StatsNewTickets = snapshot.New;
        StatsInProgressTickets = snapshot.InProgress;
        StatsClosedTickets = snapshot.Closed;
        StatsLowPriorityTickets = snapshot.LowPriority;
        StatsMediumPriorityTickets = snapshot.MediumPriority;
        StatsHighPriorityTickets = snapshot.HighPriority;
        StatsAssignedTickets = snapshot.Assigned;
        StatsUnassignedTickets = snapshot.Unassigned;

        StatsStatusChartMaximum = Math.Max(
            1,
            Math.Max(StatsNewTickets, Math.Max(StatsInProgressTickets, StatsClosedTickets)));
        StatsPriorityChartMaximum = Math.Max(
            1,
            Math.Max(StatsLowPriorityTickets, Math.Max(StatsMediumPriorityTickets, StatsHighPriorityTickets)));
        StatsAssignmentChartMaximum = Math.Max(
            1,
            Math.Max(StatsAssignedTickets, StatsUnassignedTickets));

        _fromCurrentPageOnly = fromCurrentPageOnly;
        _totalInSystem = totalInSystem;
        RefreshStatsScopeMessage();
    }

    private void RefreshStatsScopeMessage()
    {
        StatsScopeMessage = StatsTotalTickets == 0
            ? AppStrings.Get("Stats_Scope_NoTickets")
            : _fromCurrentPageOnly && _totalInSystem > StatsTotalTickets
                ? AppStrings.GetFormat("Stats_Scope_CurrentPage", StatsTotalTickets, _totalInSystem)
                : AppStrings.GetFormat("Stats_Scope_AllPages", StatsTotalTickets, _totalInSystem);
    }

    private async Task LoadAllPagesStatisticsAsync()
    {
        if (_context.IsOffline)
        {
            StatsScopeMessage = AppStrings.Get("Stats_AllPagesRequiresOnline");
            return;
        }

        try
        {
            IsLoadingAllStatistics = true;

            await _context.ExecuteApiAsync(
                async () =>
                {
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
                    _context.ShowToastKey("Stats_Loaded", ToastTypes.Success, aggregated.Count);
                },
                new DashboardApiExecutionOptions
                {
                    SetStatusMessage = message => StatsScopeMessage = message,
                    UnexpectedStatusMessageKey = "Stats_LoadAllPagesFailed",
                    UnexpectedToastMessageKey = "Stats_LoadAllFailed",
                    SetOfflineOnServiceUnavailable = false
                });
        }
        finally
        {
            IsLoadingAllStatistics = false;
        }
    }
}
