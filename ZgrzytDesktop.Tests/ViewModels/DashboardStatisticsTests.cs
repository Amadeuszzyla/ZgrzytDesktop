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

            Assert.Equal(5, vm.StatsTotalTickets);
            Assert.Equal(2, vm.StatsNewTickets);
            Assert.Equal(1, vm.StatsInProgressTickets);
            Assert.Equal(2, vm.StatsClosedTickets);
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

            Assert.Equal(2, vm.StatsLowPriorityTickets);
            Assert.Equal(2, vm.StatsMediumPriorityTickets);
            Assert.Equal(1, vm.StatsHighPriorityTickets);
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

            Assert.Equal(3, vm.StatsAssignedTickets);
            Assert.Equal(2, vm.StatsUnassignedTickets);
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

            Assert.Equal(0, vm.StatsTotalTickets);
            Assert.Equal(1, vm.StatsStatusChartMaximum);
            Assert.Equal(1, vm.StatsPriorityChartMaximum);
            Assert.Equal(1, vm.StatsAssignmentChartMaximum);
            Assert.Contains("Brak zgłoszeń", vm.StatsScopeMessage, StringComparison.Ordinal);
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
            await vm.LoadAllPagesStatisticsCommand.ExecuteAsync(null);

            Assert.Equal(2, tickets.GetTicketsCallCount);
            Assert.Equal(5, vm.StatsTotalTickets);
            Assert.Equal(2, vm.StatsNewTickets);
            Assert.Equal(1, vm.StatsInProgressTickets);
            Assert.Equal(2, vm.StatsClosedTickets);
            Assert.Contains("łącznie w systemie: 5", vm.StatsScopeMessage, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
