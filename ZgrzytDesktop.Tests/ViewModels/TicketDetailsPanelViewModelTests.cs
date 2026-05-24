using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;
using TicketMessage = ZgrzytDesktop.Models.Message;

namespace ZgrzytDesktop.Tests.ViewModels;

public class TicketDetailsPanelViewModelTests
{
    public TicketDetailsPanelViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task LoadTicketDetailsAsync_Success_LoadsTicketDetails()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[1] = new Ticket
        {
            Id = 1,
            Title = "Test ticket",
            Description = "Opis",
            Status = TicketStatuses.Nowe,
            Priority = TicketPriorities.Low,
            UserId = 10
        };

        var (panel, _, _) = CreatePanel(tickets);

        await panel.LoadTicketDetailsAsync(1);

        Assert.NotNull(panel.TicketDetails);
        Assert.Equal(1, panel.TicketDetails!.Id);
        Assert.Equal("Test ticket", panel.TicketDetails.Title);
        Assert.Contains("Wybrano zgłoszenie #1", panel.DetailsStatusMessage);
        Assert.False(panel.IsLoadingDetails);
    }

    [Fact]
    public async Task LoadTicketDetailsAsync_LoadsMessages()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[2] = new Ticket { Id = 2, Title = "With messages", Status = TicketStatuses.Nowe };
        tickets.MessagesByTicketId[2] =
        [
            new TicketMessage { Id = 1, Content = "Hello", TicketId = 2 },
            new TicketMessage { Id = 2, Content = "World", TicketId = 2 }
        ];

        var (panel, _, _) = CreatePanel(tickets);

        await panel.LoadTicketDetailsAsync(2);

        Assert.Equal(2, panel.Messages.Count);
        Assert.False(panel.HasNoMessages);
        Assert.Equal(1, tickets.GetTicketMessagesCallCount);
    }

    [Fact]
    public async Task LoadTicketDetailsAsync_RefreshesTicketAuditLog()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[3] = new Ticket { Id = 3, Title = "Audit", Status = TicketStatuses.Nowe };

        var audit = new FakeAuditLogService();
        await audit.AddAsync(new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            UserLogin = "user",
            Action = "CreateTicket",
            TicketId = 3,
            Description = "Utworzono"
        });

        var (panel, _, _) = CreatePanel(tickets, audit);

        await panel.LoadTicketDetailsAsync(3);

        Assert.Single(panel.TicketAuditLogEntries);
        Assert.False(panel.HasNoTicketAuditLogEntries);
        Assert.Equal("CreateTicket", panel.TicketAuditLogEntries[0].Action);
    }

    [Fact]
    public async Task LoadTicketDetailsAsync_HtmlError_DoesNotExposeRawHtml()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[4] = new Ticket
        {
            Id = 4,
            Title = "<!DOCTYPE html><html><body>Error</body></html>",
            Description = "desc",
            Status = TicketStatuses.Nowe
        };

        var previous = new Ticket { Id = 4, Title = "Safe", Description = "ok", Status = TicketStatuses.Nowe };
        var (panel, _, _) = CreatePanel(tickets);
        panel.TicketDetails = previous;

        await panel.LoadTicketDetailsAsync(4);

        Assert.Equal("Safe", panel.TicketDetails!.Title);
        Assert.DoesNotContain("<html", panel.DetailsStatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadTicketDetailsAsync_Forbidden_ShowsShortMessage()
    {
        var tickets = new FakeTicketService
        {
            GetTicketApiException = new ApiException(HttpStatusCode.Forbidden, "forbidden")
        };

        var (panel, _, _) = CreatePanel(tickets);

        await panel.LoadTicketDetailsAsync(9);

        Assert.Equal(AppStrings.Get("Api_Forbidden"), panel.DetailsStatusMessage);
        Assert.Null(panel.TicketDetails);
    }

    [Fact]
    public async Task LoadTicketDetailsAsync_ServiceUnavailable_UsesCachedTicketIfAvailable()
    {
        var tickets = new FakeTicketService
        {
            GetTicketApiException = new ApiException(HttpStatusCode.ServiceUnavailable, "offline")
        };

        var cached = new Ticket
        {
            Id = 7,
            Title = "Cached",
            Status = TicketStatuses.WTrakcie,
            Priority = TicketPriorities.Medium,
            Messages = [new TicketMessage { Id = 1, Content = "Cached msg", TicketId = 7 }]
        };

        var (panel, _, _) = CreatePanel(
            tickets,
            findCached: _ => cached);

        await panel.LoadTicketDetailsAsync(7);

        Assert.NotNull(panel.TicketDetails);
        Assert.Equal(7, panel.TicketDetails!.Id);
        Assert.Single(panel.Messages);
        Assert.Contains("offline", panel.DetailsStatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HasNoMessages_TrueWhenMessagesEmpty()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[5] = new Ticket { Id = 5, Title = "No msgs", Status = TicketStatuses.Nowe };

        var (panel, _, _) = CreatePanel(tickets);

        await panel.LoadTicketDetailsAsync(5);

        Assert.Empty(panel.Messages);
        Assert.True(panel.HasNoMessages);
    }

    [Fact]
    public async Task HasNoTicketAuditLogEntries_TrueWhenAuditEmpty()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[6] = new Ticket { Id = 6, Title = "No audit", Status = TicketStatuses.Nowe };

        var (panel, _, _) = CreatePanel(tickets, new FakeAuditLogService());

        await panel.LoadTicketDetailsAsync(6);

        Assert.Empty(panel.TicketAuditLogEntries);
        Assert.True(panel.HasNoTicketAuditLogEntries);
    }

    [Fact]
    public async Task SendMessageAsync_WithValidMessage_AddsMessageAndClearsInput()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[10] = new Ticket { Id = 10, Title = "Msg", Status = TicketStatuses.Nowe };

        var (panel, _, _) = CreatePanel(tickets);

        await panel.LoadTicketDetailsAsync(10);
        panel.NewMessageText = "Nowa wiadomość";

        await panel.SendMessageCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal(string.Empty, panel.NewMessageText);
        Assert.Equal(1, tickets.SendMessageCallCount);
        Assert.Contains(panel.Messages, m => m.Content == "Nowa wiadomość");
        Assert.Contains(panel.Messages, m => m.DisplayBody == "Nowa wiadomość");
    }

    [Fact]
    public async Task LoadMessagesAsync_HtmlBody_ShowsPlainDisplayBody()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[20] = new Ticket { Id = 20, Title = "Chat", Status = TicketStatuses.Nowe };
        tickets.MessagesByTicketId[20] =
        [
            new TicketMessage
            {
                Id = 1,
                Content = "<p>Widoczna treść</p>",
                Sender = new User { Id = 1, Name = "User", Login = "user" }
            }
        ];

        var (panel, _, _) = CreatePanel(tickets);

        await panel.LoadTicketDetailsAsync(20);

        Assert.Single(panel.Messages);
        Assert.Equal("Widoczna treść", panel.Messages[0].DisplayBody);
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyMessage_ShowsValidation()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[11] = new Ticket { Id = 11, Title = "X", Status = TicketStatuses.Nowe };

        var (panel, _, _) = CreatePanel(tickets);

        await panel.LoadTicketDetailsAsync(11);
        panel.NewMessageText = "   ";

        await panel.SendMessageCommand.ExecuteAsync(null);

        Assert.Equal("Treść wiadomości nie może być pusta.", panel.DetailsStatusMessage);
        Assert.Equal(0, tickets.SendMessageCallCount);
    }

    [Fact]
    public async Task UpdateTicketAsync_UpdatesStatusAndPriority()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[12] = new Ticket
        {
            Id = 12,
            Title = "Upd",
            Status = TicketStatuses.Nowe,
            Priority = TicketPriorities.Low
        };

        var (panel, _, _) = CreatePanel(tickets, canManageTickets: true);

        await panel.LoadTicketDetailsAsync(12);
        panel.SelectedStatus = StatusDisplayHelper.ToDisplayStatus(TicketStatuses.WTrakcie);
        panel.SelectedPriority = TicketPriorities.High;

        await panel.UpdateTicketCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal(TicketStatuses.WTrakcie, tickets.LastUpdateRequest!.Status);
        Assert.Equal(TicketPriorities.High, tickets.LastUpdateRequest.Priority);
    }

    [Fact]
    public async Task AssignToMeAsync_AssignsCurrentUser()
    {
        var user = new User { Id = 99, Login = "it.user", Name = "IT", Role = AppRoles.It };
        var tickets = new FakeTicketService();
        tickets.TicketsById[13] = new Ticket { Id = 13, Title = "Assign", Status = TicketStatuses.Nowe };

        var (panel, _, _) = CreatePanel(tickets, canManageTickets: true, currentUser: user);

        await panel.LoadTicketDetailsAsync(13);

        await panel.AssignToMeCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal(99, tickets.LastUpdateRequest!.AssignedItId);
    }

    [Fact]
    public async Task CloseTicketAsync_On403_DoesNotOverwriteTicketDetails()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[14] = new Ticket
        {
            Id = 14,
            Title = "Close me",
            Status = TicketStatuses.Nowe,
            Priority = TicketPriorities.Medium,
            UserId = 1
        };
        tickets.UpdateTicketApiException = new ApiException(HttpStatusCode.Forbidden, "forbidden");

        var (panel, _, _) = CreatePanel(tickets, canManageTickets: true);

        await panel.LoadTicketDetailsAsync(14);
        var titleBefore = panel.TicketDetails!.Title;
        var statusBefore = panel.SelectedStatus;

        await panel.CloseTicketCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal(titleBefore, panel.TicketDetails!.Title);
        Assert.Equal(statusBefore, panel.SelectedStatus);
        Assert.Equal(AppStrings.Get("Api_Forbidden"), panel.DetailsStatusMessage);
    }

    [Fact]
    public async Task CloseTicketAsync_OnHtmlError_DoesNotExposeRawHtml()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[15] = new Ticket
        {
            Id = 15,
            Title = "Safe",
            Status = TicketStatuses.Nowe,
            Priority = TicketPriorities.Low
        };
        tickets.NextUpdatedTicket = new Ticket
        {
            Id = 15,
            Title = "<!DOCTYPE html><html><body>x</body></html>",
            Description = "x",
            Status = TicketStatuses.Zamkniete
        };

        var (panel, _, _) = CreatePanel(tickets, canManageTickets: true);

        await panel.LoadTicketDetailsAsync(15);

        await panel.CloseTicketCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal("Safe", panel.TicketDetails!.Title);
        Assert.DoesNotContain("<html", panel.DetailsStatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteTicketAsync_OnSuccess_NavigatesToTickets()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[16] = new Ticket { Id = 16, Title = "Del", Status = TicketStatuses.Nowe };

        var ctx = new DetailsTestContext();
        var (panel, _, _) = CreatePanel(tickets, canManageTickets: true, context: ctx);

        await panel.LoadTicketDetailsAsync(16);

        await panel.DeleteTicketCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Null(panel.TicketDetails);
        Assert.True(ctx.NavigatedToTickets);
        Assert.True(ctx.ClearedSelection);
        Assert.Equal(1, ctx.RefreshTicketsCount);
    }

    [Fact]
    public async Task DeleteTicketAsync_OnApiError_KeepsDetails()
    {
        var tickets = new FakeTicketService
        {
            DeleteTicketApiException = new ApiException(HttpStatusCode.InternalServerError, "fail")
        };
        tickets.TicketsById[17] = new Ticket { Id = 17, Title = "Keep", Status = TicketStatuses.Nowe };

        var (panel, _, _) = CreatePanel(tickets, canManageTickets: true);

        await panel.LoadTicketDetailsAsync(17);

        await panel.DeleteTicketCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.NotNull(panel.TicketDetails);
        Assert.Equal(17, panel.TicketDetails!.Id);
    }

    [Fact]
    public async Task Mutations_OnSuccess_RefreshTicketsCallbackCalled()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[18] = new Ticket { Id = 18, Title = "R", Status = TicketStatuses.Nowe };

        var ctx = new DetailsTestContext();
        var (panel, _, _) = CreatePanel(tickets, canManageTickets: true, context: ctx);

        await panel.LoadTicketDetailsAsync(18);
        panel.SelectedStatus = StatusDisplayHelper.ToDisplayStatus(TicketStatuses.WTrakcie);
        panel.SelectedPriority = TicketPriorities.Medium;

        await panel.UpdateTicketCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.True(ctx.RefreshTicketsCount >= 1);
    }

    [Fact]
    public async Task Mutations_OnSuccess_LogAuditCalled()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[19] = new Ticket { Id = 19, Title = "A", Status = TicketStatuses.Nowe };

        var ctx = new DetailsTestContext();
        var (panel, _, _) = CreatePanel(tickets, context: ctx);

        await panel.LoadTicketDetailsAsync(19);
        panel.NewMessageText = "audit test";

        await panel.SendMessageCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Contains(ctx.AuditCalls, c => c.action == "SendMessage" && c.ticketId == 19);
    }

    internal sealed class DetailsTestContext
    {
        public int RefreshTicketsCount;

        public bool NavigatedToTickets;

        public bool ClearedSelection;

        public List<(string action, int? ticketId)> AuditCalls { get; } = new();
    }

    internal static (TicketDetailsPanelViewModel Panel, FakeTicketService Tickets, FakeUserAdminService Users) CreatePanel(
        FakeTicketService tickets,
        FakeAuditLogService? audit = null,
        FakeUserAdminService? users = null,
        Func<int, Ticket?>? findCached = null,
        bool canManageTickets = false,
        bool isAdminRole = false,
        bool isRegularUser = true,
        User? currentUser = null,
        DetailsTestContext? context = null)
    {
        audit ??= new FakeAuditLogService();
        users ??= new FakeUserAdminService();
        currentUser ??= new User { Id = 1, Login = "user", Name = "User", Role = AppRoles.User };

        var panel = new TicketDetailsPanelViewModel(
            tickets,
            users,
            audit,
            CreateCallbacks(findCached, canManageTickets, isAdminRole, isRegularUser, currentUser, context));

        return (panel, tickets, users);
    }

    private static TicketDetailsPanelCallbacks CreateCallbacks(
        Func<int, Ticket?>? findCached = null,
        bool canManageTickets = false,
        bool isAdminRole = false,
        bool isRegularUser = true,
        User? currentUser = null,
        DetailsTestContext? context = null)
    {
        currentUser ??= new User { Id = 1, Login = "user", Name = "User", Role = AppRoles.User };
        context ??= new DetailsTestContext();

        return new TicketDetailsPanelCallbacks
        {
            ShowToastKey = TestToastCallbacks.NoopKey,
            ShowToastRaw = TestToastCallbacks.NoopRaw,
            SetIsOffline = _ => { },
            GetIsOffline = () => false,
            GetApiErrorMessage = ex => ApiErrorSanitizer.SanitizeApiErrorMessage(
                ex.ResponseContent ?? ex.Message,
                ex.StatusCode),
            FindCachedTicket = findCached ?? (_ => null),
            NotifyDetailsSideEffects = () => { },
            NotifyDetailsLoadingChanged = () => { },
            GetCurrentUser = () => currentUser,
            GetCanManageTickets = () => canManageTickets,
            GetIsAdminRole = () => isAdminRole,
            GetIsRegularUser = () => isRegularUser,
            LogAuditAsync = (action, ticketId, _, _) =>
            {
                context.AuditCalls.Add((action, ticketId));
                return Task.CompletedTask;
            },
            RefreshTicketsAsync = () =>
            {
                context.RefreshTicketsCount++;
                return Task.CompletedTask;
            },
            NavigateToTickets = () => context.NavigatedToTickets = true,
            ClearSelectedTicket = () => context.ClearedSelection = true,
            ExecuteApiAsyncCore = CreateTestExecuteApiAsync()
        };
    }

    private static Func<Func<Task>, Action<string>?, string?, string?, string?, bool, bool, Func<ApiException, Task>?, Task<bool>>
        CreateTestExecuteApiAsync() =>
        async (action, setStatusMessage, unexpectedStatusMessage, unexpectedToastMessage, offlineToastMessage,
            showApiErrorToast, setOfflineOnServiceUnavailable, onServiceUnavailableAsync) =>
        {
            try
            {
                await action();
                return true;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                if (onServiceUnavailableAsync is not null)
                {
                    await onServiceUnavailableAsync(ex);
                    return false;
                }

                setStatusMessage?.Invoke(
                    offlineToastMessage is not null
                        ? AppStrings.Get(offlineToastMessage)
                        : ex.Message);
                return false;
            }
            catch (ApiException ex)
            {
                setStatusMessage?.Invoke(ApiErrorSanitizer.SanitizeApiErrorMessage(
                    ex.ResponseContent ?? ex.Message,
                    ex.StatusCode));
                return false;
            }
            catch
            {
                setStatusMessage?.Invoke(
                    unexpectedStatusMessage is not null
                        ? AppStrings.Get(unexpectedStatusMessage)
                        : AppStrings.Get("Api_UnexpectedError"));
                return false;
            }
        };
}
