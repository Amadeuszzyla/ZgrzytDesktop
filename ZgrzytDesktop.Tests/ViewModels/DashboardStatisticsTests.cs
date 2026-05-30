using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.ViewModels;

public class DashboardStatisticsTests
{
    public DashboardStatisticsTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task LoadTickets_UpdatesStatusCounts()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = TicketTestDataBuilder.CreatePage(
                1,
                TicketTestDataBuilder.CreateMixedStatisticsSet(),
                total: 5,
                lastPage: 1)
        };

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it", tickets: tickets);

        try
        {
            await vm.TicketsPanel.RefreshTicketsNowCommand.ExecuteAsync(null);

            Assert.Equal(5, vm.StatisticsPanel.StatsTotalTickets);
            Assert.Equal(2, vm.StatisticsPanel.StatsNewTickets);
            Assert.Equal(1, vm.StatisticsPanel.StatsInProgressTickets);
            Assert.Equal(2, vm.StatisticsPanel.StatsClosedTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LoadTickets_UpdatesPriorityCounts()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = TicketTestDataBuilder.CreatePage(
                1,
                TicketTestDataBuilder.CreateMixedStatisticsSet(),
                total: 5,
                lastPage: 1)
        };

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it", tickets: tickets);

        try
        {
            await vm.TicketsPanel.RefreshTicketsNowCommand.ExecuteAsync(null);

            Assert.Equal(2, vm.StatisticsPanel.StatsLowPriorityTickets);
            Assert.Equal(2, vm.StatisticsPanel.StatsMediumPriorityTickets);
            Assert.Equal(1, vm.StatisticsPanel.StatsHighPriorityTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LoadTickets_UpdatesAssignmentCounts()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = TicketTestDataBuilder.CreatePage(
                1,
                TicketTestDataBuilder.CreateMixedStatisticsSet(),
                total: 5,
                lastPage: 1)
        };

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it", tickets: tickets);

        try
        {
            await vm.TicketsPanel.RefreshTicketsNowCommand.ExecuteAsync(null);

            Assert.Equal(3, vm.StatisticsPanel.StatsAssignedTickets);
            Assert.Equal(2, vm.StatisticsPanel.StatsUnassignedTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task EmptyTickets_ChartMaximums_AreAtLeastOne()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = TicketTestDataBuilder.CreatePage(1, [], total: 0, lastPage: 1)
        };

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard(tickets: tickets);

        try
        {
            await vm.TicketsPanel.RefreshTicketsNowCommand.ExecuteAsync(null);

            Assert.Equal(0, vm.StatisticsPanel.StatsTotalTickets);
            Assert.Equal(1, vm.StatisticsPanel.StatsStatusChartMaximum);
            Assert.Equal(1, vm.StatisticsPanel.StatsPriorityChartMaximum);
            Assert.Equal(1, vm.StatisticsPanel.StatsAssignmentChartMaximum);
            Assert.Contains("Brak zgłoszeń", vm.StatisticsPanel.StatsScopeMessage, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LoadAllPagesStatistics_AggregatesMultiplePages()
    {
        var pageOneTickets = TicketTestDataBuilder.CreateMixedStatisticsSet().Take(2).ToList();
        var pageTwoTickets = TicketTestDataBuilder.CreateMixedStatisticsSet().Skip(2).ToList();

        var tickets = new FakeTicketService();
        tickets.PagedResponses[1] = TicketTestDataBuilder.CreatePage(1, pageOneTickets, total: 5, lastPage: 2);
        tickets.PagedResponses[2] = TicketTestDataBuilder.CreatePage(2, pageTwoTickets, total: 5, lastPage: 2);

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it", tickets: tickets);

        try
        {
            await vm.StatisticsPanel.LoadAllPagesStatisticsCommand.ExecuteAsync(null);

            Assert.Equal(2, tickets.GetTicketsCallCount);
            Assert.Equal(5, vm.StatisticsPanel.StatsTotalTickets);
            Assert.Equal(2, vm.StatisticsPanel.StatsNewTickets);
            Assert.Equal(1, vm.StatisticsPanel.StatsInProgressTickets);
            Assert.Equal(2, vm.StatisticsPanel.StatsClosedTickets);
            Assert.Contains("łącznie w systemie: 5", vm.StatisticsPanel.StatsScopeMessage, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
