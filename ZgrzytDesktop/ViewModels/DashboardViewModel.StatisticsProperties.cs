namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
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
        private set => SetProperty(ref _isLoadingAllStatistics, value);
    }

    private void InitializeStatisticsCollections()
    {
        // Statystyki nie wymagają kolekcji pomocniczych w UI.
    }
}
