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
    public void ApplyFromTickets_WithoutResponseData_ShowsUnavailableMessage_Pl()
    {
        AppStrings.ApplyCulture("pl");

        var panel = CreatePanel();
        panel.ApplyFromTickets(TicketTestDataBuilder.CreateMixedStatisticsSet(), 5, fromCurrentPageOnly: true);

        Assert.False(panel.IsResponseTimeAvailable);
        Assert.Equal(
            "API nie dostarcza danych czasu pierwszej odpowiedzi.",
            panel.StatsResponseTimeMessage);
    }

    [Fact]
    public void ApplyFromTickets_WithoutResponseData_ShowsUnavailableMessage_En()
    {
        AppStrings.ApplyCulture("en");

        var panel = CreatePanel();
        panel.ApplyFromTickets(TicketTestDataBuilder.CreateMixedStatisticsSet(), 5, fromCurrentPageOnly: true);

        Assert.False(panel.IsResponseTimeAvailable);
        Assert.Equal(
            "The API does not provide first response time data.",
            panel.StatsResponseTimeMessage);
    }

    [Fact]
    public void NotifyLocalization_RefreshesResponseTimeMessage_WhenCultureChanges()
    {
        var panel = CreatePanel();
        var created = new DateTime(2026, 4, 1, 12, 0, 0);

        panel.ApplyFromTickets(
        [
            new Ticket
            {
                Id = 1,
                CreatedAt = created,
                FirstResponseAt = created.AddHours(2)
            }
        ],
            totalInSystem: 1,
            fromCurrentPageOnly: true);

        AppStrings.ApplyCulture("pl");
        panel.NotifyLocalization();
        Assert.Contains("first_response_at", panel.StatsResponseTimeMessage, StringComparison.Ordinal);

        AppStrings.ApplyCulture("en");
        panel.NotifyLocalization();
        Assert.Contains("first_response_at", panel.StatsResponseTimeMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nie dostarcza", panel.StatsResponseTimeMessage, StringComparison.OrdinalIgnoreCase);
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

        var bridge = new DashboardVmBridge
        {
            ExecuteApiAsyncCore = async (action, _, _, _, _, _, _, _) =>
            {
                await action();
                return true;
            },
            ShowToast = (_, _) => { },
            LogAuditAsync = (_, _, _, _) => Task.CompletedTask,
            GetIsOffline = () => false,
            SetIsOffline = _ => { },
            NotifyLocalization = () => { },
            GetCurrentSection = () => AppSections.Statistics
        };

        return new StatisticsPanelViewModel(tickets, bridge, () => true);
    }
}
