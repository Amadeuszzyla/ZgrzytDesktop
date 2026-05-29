using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class StatisticsPanelViewModelTests
{
    public StatisticsPanelViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void ApplyFromTickets_UpdatesStatusAndPriorityCounts()
    {
        var panel = CreatePanel();
        var tickets = TicketTestDataBuilder.CreateMixedStatisticsSet();

        panel.ApplyFromTickets(tickets, totalInSystem: 5, fromCurrentPageOnly: true);

        Assert.Equal(5, panel.StatsTotalTickets);
        Assert.Equal(2, panel.StatsNewTickets);
        Assert.Equal(1, panel.StatsInProgressTickets);
        Assert.Equal(2, panel.StatsClosedTickets);
        Assert.Equal(2, panel.StatsLowPriorityTickets);
        Assert.Equal(1, panel.StatsHighPriorityTickets);
        Assert.Equal(3, panel.StatsAssignedTickets);
        Assert.Equal(2, panel.StatsUnassignedTickets);
    }

    [Fact]
    public void ApplyFromTickets_WhenEmpty_SetsChartMaximumsToOne()
    {
        var panel = CreatePanel();

        panel.ApplyFromTickets([], totalInSystem: 0, fromCurrentPageOnly: true);

        Assert.Equal(0, panel.StatsTotalTickets);
        Assert.Equal(1, panel.StatsStatusChartMaximum);
        Assert.Equal(1, panel.StatsPriorityChartMaximum);
        Assert.Equal(1, panel.StatsAssignmentChartMaximum);
        Assert.Contains("Brak zgłoszeń", panel.StatsScopeMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void NotifyLocalization_RefreshesScopeMessage_PlAndEn()
    {
        var panel = CreatePanel();
        panel.ApplyFromTickets(TicketTestDataBuilder.CreateMixedStatisticsSet(), totalInSystem: 12, fromCurrentPageOnly: true);

        AppStrings.ApplyCulture("pl");
        panel.NotifyLocalization();
        Assert.Contains("bieżącej stronie", panel.StatsScopeMessage, StringComparison.Ordinal);

        AppStrings.ApplyCulture("en");
        panel.NotifyLocalization();
        Assert.Contains("current list", panel.StatsScopeMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAllPagesStatistics_AggregatesMultiplePages()
    {
        var pageOneTickets = TicketTestDataBuilder.CreateMixedStatisticsSet().Take(2).ToList();
        var pageTwoTickets = TicketTestDataBuilder.CreateMixedStatisticsSet().Skip(2).ToList();

        var tickets = new FakeTicketService();
        tickets.PagedResponses[1] = TicketTestDataBuilder.CreatePage(1, pageOneTickets, total: 5, lastPage: 2);
        tickets.PagedResponses[2] = TicketTestDataBuilder.CreatePage(2, pageTwoTickets, total: 5, lastPage: 2);

        var panel = CreatePanel(tickets);

        await panel.LoadAllPagesStatisticsCommand.ExecuteAsync(null);

        Assert.Equal(2, tickets.GetTicketsCallCount);
        Assert.Equal(5, panel.StatsTotalTickets);
        Assert.Contains("łącznie w systemie: 5", panel.StatsScopeMessage, StringComparison.Ordinal);
    }

    private static StatisticsPanelViewModel CreatePanel(FakeTicketService? tickets = null)
    {
        tickets ??= new FakeTicketService();

        return new StatisticsPanelViewModel(
            tickets,
            TestDashboardContext.CreateDefault(AppSections.Statistics),
            () => true);
    }
}
