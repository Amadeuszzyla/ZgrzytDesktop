using System.Net;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Regression;

public class TicketQueueFilteringTests
{
    public TicketQueueFilteringTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task GetTicketsAsync_All_StillSendsStatusPriorityAndSortQueryParams()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, EmptyPageJson());
            var service = TestApiFactory.CreateTickets(api);

            await service.GetTicketsAsync(
                status: TicketStatuses.Nowe,
                priority: TicketPriorities.High,
                sortBy: "title",
                sortDirection: "asc",
                queueView: TicketQueueView.All);

            var uri = handler.Requests[0].Uri!.ToString();
            Assert.Contains("/api/tickets?", uri, StringComparison.Ordinal);
            Assert.Contains("status=nowe", uri, StringComparison.Ordinal);
            Assert.Contains("priority=wysoki", uri, StringComparison.Ordinal);
            Assert.Contains("sort_by=title", uri, StringComparison.Ordinal);
            Assert.Contains("sort_direction=asc", uri, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketsAsync_ActiveWithStatusFilter_AppliesLocalFiltering()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, ActiveQueueJson());
            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                status: TicketStatuses.Nowe,
                queueView: TicketQueueView.Active);

            Assert.NotNull(response);
            Assert.Single(response!.Data);
            Assert.Equal(TicketStatuses.Nowe, response.Data[0].Status);

            var uri = handler.Requests[0].Uri!.ToString();
            Assert.Contains("/api/active-tickets", uri, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("status=", uri, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketsAsync_ActiveWithPriorityFilter_AppliesLocalFiltering()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, ActiveQueueJson());
            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                priority: TicketPriorities.High,
                queueView: TicketQueueView.Active);

            Assert.NotNull(response);
            Assert.Single(response!.Data);
            Assert.Equal(TicketPriorities.High, response.Data[0].Priority);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketsAsync_ActiveWithSort_AppliesLocalSorting()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, ActiveQueueJson());
            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                sortBy: "title",
                sortDirection: "asc",
                queueView: TicketQueueView.Active);

            Assert.NotNull(response);
            Assert.Equal(["Alpha", "Bravo"], response!.Data.Select(ticket => ticket.Title));
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketsAsync_UnassignedWithStatusFilter_AppliesLocalFiltering()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, UnassignedQueueJson());
            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                status: TicketStatuses.WTrakcie,
                queueView: TicketQueueView.Unassigned);

            Assert.NotNull(response);
            Assert.Single(response!.Data);
            Assert.Equal(20, response.Data[0].Id);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketsAsync_UnassignedWithPriorityFilter_AppliesLocalFiltering()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, UnassignedQueueJson());
            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                priority: TicketPriorities.Medium,
                queueView: TicketQueueView.Unassigned);

            Assert.NotNull(response);
            Assert.Single(response!.Data);
            Assert.Equal(TicketPriorities.Medium, response.Data[0].Priority);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketsAsync_UnassignedWithSort_AppliesLocalSorting()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, UnassignedQueueJson());
            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                sortBy: "title",
                sortDirection: "desc",
                queueView: TicketQueueView.Unassigned);

            Assert.NotNull(response);
            Assert.Equal(["Zulu", "Yankee"], response!.Data.Select(ticket => ticket.Title));
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LoadTicketsAsync_ActiveStatusFilter_NoMatches_ShowsEmptyState()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data =
                [
                    new Ticket { Id = 1, Title = "A", Status = TicketStatuses.WTrakcie, Priority = TicketPriorities.Low },
                    new Ticket { Id = 2, Title = "B", Status = TicketStatuses.Zamkniete, Priority = TicketPriorities.Low }
                ],
                Total = 2,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.ConfigureQueueViewsForRole(canManageTickets: true);
            panel.SelectedTicketQueueView = FilterLabels.Active;
            panel.SelectedFilterStatus = TicketStatuses.Nowe;

            await panel.LoadTicketsAsync();

            Assert.Empty(panel.Tickets);
            Assert.True(panel.HasNoTickets);
            Assert.Equal(TicketQueueView.Active, tickets.LastQueueView);
            Assert.Equal(TicketStatuses.Nowe, tickets.LastStatus);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LoadTicketsAsync_ActiveStatusFilter_WithMatches_HidesEmptyState()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data =
                [
                    new Ticket { Id = 1, Title = "A", Status = TicketStatuses.Nowe, Priority = TicketPriorities.Low },
                    new Ticket { Id = 2, Title = "B", Status = TicketStatuses.WTrakcie, Priority = TicketPriorities.Low }
                ],
                Total = 2,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.ConfigureQueueViewsForRole(canManageTickets: true);
            panel.SelectedTicketQueueView = FilterLabels.Active;
            panel.SelectedFilterStatus = TicketStatuses.Nowe;

            await panel.LoadTicketsAsync();

            Assert.Single(panel.Tickets);
            Assert.False(panel.HasNoTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    private static string EmptyPageJson() =>
        """
        {
          "current_page": 1,
          "data": [],
          "last_page": 1,
          "per_page": 15,
          "total": 0
        }
        """;

    private static string ActiveQueueJson() =>
        """
        {
          "current_page": 1,
          "data": [
            { "id": 1, "title": "Bravo", "status": "nowe", "priority": "niski", "user_id": 1 },
            { "id": 2, "title": "Alpha", "status": "w trakcie", "priority": "wysoki", "user_id": 1 }
          ],
          "last_page": 1,
          "per_page": 100,
          "total": 2
        }
        """;

    private static string UnassignedQueueJson() =>
        """
        {
          "current_page": 1,
          "data": [
            { "id": 10, "title": "Yankee", "status": "nowe", "priority": "niski", "user_id": 1 },
            { "id": 20, "title": "Zulu", "status": "w trakcie", "priority": "średni", "user_id": 1 }
          ],
          "last_page": 1,
          "per_page": 100,
          "total": 2
        }
        """;

    private static (TicketsPanelViewModel Panel, FakeTicketService Tickets, string TempDir) CreatePanel(
        FakeTicketService tickets)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var panel = new TicketsPanelViewModel(
            tickets,
            new LocalTicketCacheService(tempDir),
            new TicketsPanelCallbacks
            {
                ShowToastKey = TestToastCallbacks.NoopKey,
            ShowToastRaw = TestToastCallbacks.NoopRaw,
                SetIsOffline = _ => { },
                GetIsOffline = () => false,
                NotifyStatistics = (_, _) => { },
                NotifyTicketsLoadingChanged = () => { },
                NotifyOnlineActionsChanged = () => { },
                GetApiErrorMessage = ex => ex.Message,
                TicketSelected = _ => { },
                RefreshPaginationSideEffects = () => { },
                LogAuditAsync = (_, _, _, _) => Task.CompletedTask,
                ExecuteApiAsyncCore = async (action, _, _, _, _, _, _, _) =>
                {
                    await action();
                    return true;
                }
            });

        return (panel, tickets, tempDir);
    }
}
