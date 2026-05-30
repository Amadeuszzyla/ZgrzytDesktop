using System.Net;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class TicketsPanelViewModelTests
{
    public TicketsPanelViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task LoadTicketsAsync_Success_PopulatesTickets()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data =
                [
                    new Ticket { Id = 1, Title = "A", Status = TicketStatuses.Nowe, Priority = TicketPriorities.Low },
                    new Ticket { Id = 2, Title = "B", Status = TicketStatuses.WTrakcie, Priority = TicketPriorities.High }
                ],
                Total = 2,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            await panel.LoadTicketsAsync();

            Assert.Equal(2, panel.Tickets.Count);
            Assert.Equal(2, panel.TotalTickets);
            Assert.Contains("Pobrano", panel.StatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(panel.IsLoading);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task SearchTicketsAsync_PassesSearchAndFilterParameters()
    {
        var tickets = new FakeTicketService();
        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.SearchText = "awaria";
            panel.SelectedFilterStatus = TicketStatuses.Nowe;
            panel.SelectedFilterPriority = TicketPriorities.High;
            panel.SelectedTicketSortField = TicketSortHelper.Fields.First(f => f.SortBy == "title");
            panel.SelectedTicketSortDirection = TicketSortHelper.Directions.First(d => d.Direction == "asc");

            await panel.SearchTicketsCommand.ExecuteAsync(null);

            Assert.Equal(1, tickets.LastPage);
            Assert.Equal("awaria", tickets.LastSearch);
            Assert.Equal(TicketStatuses.Nowe, tickets.LastStatus);
            Assert.Equal(TicketPriorities.High, tickets.LastPriority);
            Assert.Equal("title", tickets.LastSortBy);
            Assert.Equal("asc", tickets.LastSortDirection);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task LoadTicketsAsync_PassesCategoryFilterAndFiltersResults()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data =
                [
                    new Ticket { Id = 1, Title = "CRM issue", Category = "Software" },
                    new Ticket { Id = 2, Title = "VPN offline" },
                    new Ticket { Id = 3, Title = "Printer jam", Description = "drukarka" }
                ],
                Total = 3,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.SelectedCategoryFilterOption = panel.FilterCategoryOptions
                .First(option => option.Key == TicketCategoryFilterKeys.Network);

            await panel.LoadTicketsAsync();

            Assert.Equal(TicketCategoryFilterKeys.Network, tickets.LastCategoryFilter);
            Assert.Single(panel.Tickets);
            Assert.Equal(2, panel.Tickets[0].Id);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Pagination_UpdatesPagePositionAndNavigationFlags()
    {
        var tickets = new FakeTicketService
        {
            PagedResponses =
            {
                [1] = new PaginatedResponse<Ticket>
                {
                    Data = [new Ticket { Id = 1, Title = "P1" }],
                    Total = 25,
                    LastPage = 3,
                    CurrentPage = 1
                },
                [2] = new PaginatedResponse<Ticket>
                {
                    Data = [new Ticket { Id = 2, Title = "P2" }],
                    Total = 25,
                    LastPage = 3,
                    CurrentPage = 2
                }
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            await panel.LoadTicketsAsync();

            Assert.Equal("Strona 1 z 3", panel.PagePositionText);
            Assert.False(panel.CanGoPreviousPage);
            Assert.True(panel.CanGoNextPage);

            await panel.NextPageCommand.ExecuteAsync(null);

            while (panel.IsLoading)
                await Task.Delay(10);

            Assert.Equal(2, panel.CurrentPage);
            Assert.Equal("Strona 2 z 3", panel.PagePositionText);
            Assert.True(panel.CanGoPreviousPage);
            Assert.True(panel.CanGoNextPage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task ClearFilters_ResetsSearchStatusAndPriority()
    {
        var tickets = new FakeTicketService();
        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.SearchText = "test";
            panel.SelectedFilterStatus = TicketStatuses.Zamkniete;
            panel.SelectedFilterPriority = TicketPriorities.High;

            panel.ClearFiltersCommand.Execute(null);

            while (panel.IsLoading)
                await Task.Delay(10);

            Assert.Equal(string.Empty, panel.SearchText);
            Assert.Equal(FilterLabels.All, panel.SelectedFilterStatus);
            Assert.Equal(FilterLabels.All, panel.SelectedFilterPriority);
            Assert.Equal(1, panel.CurrentPage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task ServiceUnavailable_WithCache_LoadsOfflineTickets()
    {
        var tickets = new FakeTicketService
        {
            GetTicketsApiException = new ApiException(HttpStatusCode.ServiceUnavailable, "offline")
        };

        var (_, _, tempDir) = TestApiFactory.CreateApi();

        try
        {
            var cache = new LocalTicketCacheService(tempDir);
            await cache.SaveTicketsAsync(
            [
                new Ticket
                {
                    Id = 42,
                    Title = "Offline",
                    Status = TicketStatuses.Nowe,
                    Priority = TicketPriorities.Low
                }
            ]);

            var panel = CreatePanel(tickets, cache, tempDir).Panel;
            var offlineSet = false;

            panel = new TicketsPanelViewModel(
                tickets,
                cache,
                CreateCallbacks(
                    tempDir,
                    setOffline: value => offlineSet = value,
                    showToastKey: null));

            await panel.LoadTicketsAsync();

            Assert.True(offlineSet);
            Assert.Single(panel.Tickets);
            Assert.Equal(42, panel.Tickets[0].Id);
            Assert.Contains("offline", panel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_WithValidData_CreatesTicket()
    {
        var tickets = new FakeTicketService
        {
            NextCreatedTicket = new Ticket { Id = 10, Title = "[Hardware] Awaria", Status = TicketStatuses.Nowe },
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data = [new Ticket { Id = 10, Title = "[Hardware] Awaria", Status = TicketStatuses.Nowe }],
                Total = 1,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.NewTicketTitle = "Awaria";
            panel.NewTicketDescription = "Opis problemu";
            panel.SelectedNewTicketCategory = "Hardware";
            panel.NewTicketPriority = TicketPriorities.High;

            await panel.CreateTicketCommand.ExecuteAsync(null);

            while (panel.IsLoading)
                await Task.Delay(10);

            Assert.Equal(1, tickets.CreateTicketCallCount);
            Assert.NotNull(tickets.LastCreateRequest);
            Assert.Equal(TicketPriorities.High, tickets.LastCreateRequest.Priority);
            Assert.Equal(10, panel.SelectedTicket?.Id);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_WithEmptyTitle_ShowsValidationMessage()
    {
        var tickets = new FakeTicketService();
        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.NewTicketTitle = "  ";
            panel.NewTicketDescription = "Opis";

            await panel.CreateTicketCommand.ExecuteAsync(null);

            Assert.Equal("Podaj tytuł zgłoszenia.", panel.CreateTicketStatusMessage);
            Assert.Equal(0, tickets.CreateTicketCallCount);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_WithEmptyDescription_ShowsValidationMessage()
    {
        var (panel, _, tempDir) = CreatePanel(new FakeTicketService());

        try
        {
            panel.NewTicketTitle = "Tytuł";
            panel.NewTicketDescription = "";

            await panel.CreateTicketCommand.ExecuteAsync(null);

            Assert.Equal("Podaj opis zgłoszenia.", panel.CreateTicketStatusMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_AddsCategoryPrefixOrDescription()
    {
        var tickets = new FakeTicketService
        {
            NextCreatedTicket = new Ticket { Id = 1, Title = "[Software] Bug" },
            NextTicketsResponse = new PaginatedResponse<Ticket> { Data = [], Total = 0, LastPage = 1, CurrentPage = 1 }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.NewTicketTitle = "Bug";
            panel.NewTicketDescription = "Szczegóły";
            panel.SelectedNewTicketCategory = "Software";

            await panel.CreateTicketCommand.ExecuteAsync(null);

            while (panel.IsLoading)
                await Task.Delay(10);

            Assert.Equal("[Software] Bug", tickets.LastCreateRequest!.Title);
            Assert.Contains("Kategoria: Software", tickets.LastCreateRequest.Description);
            Assert.Contains("Szczegóły", tickets.LastCreateRequest.Description);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_OnSuccess_RefreshesList()
    {
        var tickets = new FakeTicketService
        {
            NextCreatedTicket = new Ticket { Id = 5, Title = "Nowe" },
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data = [new Ticket { Id = 5, Title = "Nowe" }],
                Total = 1,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.NewTicketTitle = "Nowe";
            panel.NewTicketDescription = "Opis";
            var callsBefore = tickets.GetTicketsCallCount;

            await panel.CreateTicketCommand.ExecuteAsync(null);

            while (panel.IsLoading)
                await Task.Delay(10);

            Assert.True(tickets.GetTicketsCallCount > callsBefore);
            Assert.Single(panel.Tickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_OnSuccess_ClearsForm()
    {
        var tickets = new FakeTicketService
        {
            NextCreatedTicket = new Ticket { Id = 1, Title = "X" },
            NextTicketsResponse = new PaginatedResponse<Ticket> { Data = [], Total = 0, LastPage = 1, CurrentPage = 1 }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.NewTicketTitle = "X";
            panel.NewTicketDescription = "Y";
            panel.NewTicketPriority = TicketPriorities.High;
            panel.SelectedNewTicketCategory = "Sieć";

            await panel.CreateTicketCommand.ExecuteAsync(null);

            while (panel.IsLoading)
                await Task.Delay(10);

            Assert.Equal(string.Empty, panel.NewTicketTitle);
            Assert.Equal(string.Empty, panel.NewTicketDescription);
            Assert.Equal(PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Low), panel.NewTicketPriority);
            Assert.Equal("Hardware", panel.SelectedNewTicketCategory);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_OnSuccess_LogsAudit()
    {
        var tickets = new FakeTicketService
        {
            NextCreatedTicket = new Ticket { Id = 3, Title = "[Hardware] Audit" },
            NextTicketsResponse = new PaginatedResponse<Ticket> { Data = [], Total = 0, LastPage = 1, CurrentPage = 1 }
        };

        string? auditAction = null;
        int? auditTicketId = null;

        var (panel, _, tempDir) = CreatePanel(
            tickets,
            logAudit: (action, ticketId, _, _) =>
            {
                auditAction = action;
                auditTicketId = ticketId;
                return Task.CompletedTask;
            });

        try
        {
            panel.NewTicketTitle = "Audit";
            panel.NewTicketDescription = "Opis";

            await panel.CreateTicketCommand.ExecuteAsync(null);

            while (panel.IsLoading)
                await Task.Delay(10);

            Assert.Equal("CreateTicket", auditAction);
            Assert.Equal(3, auditTicketId);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_OnApiError_DoesNotShowRawHtml()
    {
        const string html = "<!DOCTYPE html><html><body>Error</body></html>";
        var tickets = new FakeTicketService
        {
            CreateTicketApiException = new ApiException(HttpStatusCode.InternalServerError, html, html)
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            panel.NewTicketTitle = "T";
            panel.NewTicketDescription = "D";

            await panel.CreateTicketCommand.ExecuteAsync(null);

            while (panel.IsLoading)
                await Task.Delay(10);

            Assert.DoesNotContain("<html", panel.CreateTicketStatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<!DOCTYPE", panel.CreateTicketStatusMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(AppStrings.Get("Api_HtmlResponse"), panel.CreateTicketStatusMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void SelectedTicket_InvokesTicketSelectedCallback()
    {
        var tickets = new FakeTicketService();
        var tempDir = Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var selectedId = 0;

        try
        {
            var panel = new TicketsPanelViewModel(
                tickets,
                new LocalTicketCacheService(tempDir),
                CreateCallbacks(
                    tempDir,
                    ticketSelected: id => selectedId = id));

            panel.SelectedTicket = new Ticket { Id = 77, Title = "Pick me" };

            Assert.Equal(77, selectedId);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task HasNoTickets_TrueAfterEmptyLoad()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data = [],
                Total = 0,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            await panel.LoadTicketsAsync();

            Assert.Empty(panel.Tickets);
            Assert.True(panel.HasNoTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task HasNoTickets_FalseAfterLoadWithTickets()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data =
                [
                    new Ticket { Id = 1, Title = "A", Status = TicketStatuses.Nowe, Priority = TicketPriorities.Low }
                ],
                Total = 1,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreatePanel(tickets);

        try
        {
            await panel.LoadTicketsAsync();

            Assert.Single(panel.Tickets);
            Assert.False(panel.HasNoTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void HasNoTickets_FalseWhileLoadingEvenWhenEmpty()
    {
        var (panel, _, tempDir) = CreatePanel(new FakeTicketService());

        try
        {
            panel.IsLoading = true;

            Assert.Empty(panel.Tickets);
            Assert.False(panel.HasNoTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    internal static (TicketsPanelViewModel Panel, FakeTicketService Tickets, string TempDir) CreatePanel(
        FakeTicketService tickets,
        ILocalTicketCacheService? cache = null,
        string? tempDir = null,
        Func<string, int?, string?, object?[]?, Task>? logAudit = null)
    {
        tempDir ??= Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        cache ??= new LocalTicketCacheService(tempDir);

        var panel = new TicketsPanelViewModel(
            tickets,
            cache,
            CreateCallbacks(tempDir, logAudit: logAudit));

        return (panel, tickets, tempDir);
    }

    private static TicketsPanelCallbacks CreateCallbacks(
        string tempDir,
        Action<bool>? setOffline = null,
        ToastKeyCallback? showToastKey = null,
        Action<string, string>? showToastRaw = null,
        Action<int>? ticketSelected = null,
        Func<string, int?, string?, object?[]?, Task>? logAudit = null)
    {
        var isOffline = false;

        return new TicketsPanelCallbacks
        {
            ShowToastKey = showToastKey ?? TestToastCallbacks.NoopKey, ShowToastRaw = showToastRaw ?? TestToastCallbacks.NoopRaw,
            SetIsOffline = value =>
            {
                isOffline = value;
                setOffline?.Invoke(value);
            },
            GetIsOffline = () => isOffline,
            NotifyStatistics = (_, _) => { },
            NotifyTicketsLoadingChanged = () => { },
            NotifyOnlineActionsChanged = () => { },
            GetApiErrorMessage = ex => ApiErrorSanitizer.SanitizeApiErrorMessage(
                ex.ResponseContent ?? ex.Message,
                ex.StatusCode),
            GetCurrentUserId = () => 1,
            TicketSelected = ticketSelected ?? (_ => { }),
            RefreshPaginationSideEffects = () => { },
            LogAuditAsync = logAudit ?? ((_, _, _, _) => Task.CompletedTask),
            ExecuteApiAsyncCore = CreateTestExecuteApiAsync()
        };
    }

    private static Func<Func<Task>, DashboardApiExecutionOptions?, Task<bool>> CreateTestExecuteApiAsync() =>
        async (action, options) =>
        {
            options ??= new DashboardApiExecutionOptions();

            try
            {
                await action();
                return true;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                if (options.OnServiceUnavailableAsync is not null)
                {
                    await options.OnServiceUnavailableAsync(ex);
                    return false;
                }

                if (options.SetOfflineOnServiceUnavailable)
                    options.SetStatusMessage?.Invoke(
                        options.OfflineToastMessageKey is not null
                            ? AppStrings.Get(options.OfflineToastMessageKey)
                            : ex.Message);

                return false;
            }
            catch (ApiException ex)
            {
                options.SetStatusMessage?.Invoke(ApiErrorSanitizer.SanitizeApiErrorMessage(
                    ex.ResponseContent ?? ex.Message,
                    ex.StatusCode));
                return false;
            }
            catch
            {
                options.SetStatusMessage?.Invoke(
                    options.UnexpectedStatusMessageKey is not null
                        ? AppStrings.Get(options.UnexpectedStatusMessageKey)
                        : AppStrings.Get("Api_UnexpectedError"));
                return false;
            }
        };
}
