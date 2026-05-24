using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class TicketDisplayLocalizationTests
{
    public TicketDisplayLocalizationTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void Ticket_DisplayStatusAndPriority_FollowCurrentCulture()
    {
        var ticket = new Ticket
        {
            Status = TicketStatuses.Nowe,
            Priority = TicketPriorities.High
        };

        AppStrings.ApplyCulture("pl");
        Assert.Equal("Nowe", ticket.DisplayStatus);
        Assert.Equal("Wysoki", ticket.DisplayPriority);

        AppStrings.ApplyCulture("en");
        Assert.Equal("New", ticket.DisplayStatus);
        Assert.Equal("High", ticket.DisplayPriority);
    }

    [Fact]
    public void TicketsPanel_NotifyLocalization_KeepsApiValuesAndLocalizedDisplay()
    {
        AppStrings.ApplyCulture("en");
        var tempDir = Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var panel = new TicketsPanelViewModel(
                new FakeTicketService(),
                new LocalTicketCacheService(tempDir),
                CreateTicketsCallbacks());

            panel.Tickets.Add(new Ticket
            {
                Id = 1,
                Title = "T",
                Status = TicketStatuses.Zamkniete,
                Priority = TicketPriorities.Low
            });

            Assert.Equal("Closed", panel.Tickets[0].DisplayStatus);

            AppStrings.ApplyCulture("pl");
            panel.NotifyLocalization();

            Assert.Equal(TicketStatuses.Zamkniete, panel.Tickets[0].Status);
            Assert.Equal(TicketPriorities.Low, panel.Tickets[0].Priority);
            Assert.Equal("Zamknięte", panel.Tickets[0].DisplayStatus);
            Assert.Equal("Niski", panel.Tickets[0].DisplayPriority);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task TicketDetailsPanel_NotifyLocalization_RefreshesCombosFromApiValues()
    {
        AppStrings.ApplyCulture("en");
        var tickets = new FakeTicketService();
        tickets.TicketsById[1] = new Ticket
        {
            Id = 1,
            Title = "T",
            Status = TicketStatuses.WTrakcie,
            Priority = TicketPriorities.Medium
        };

        var panel = new TicketDetailsPanelViewModel(
            tickets,
            new FakeUserAdminService(),
            new FakeAuditLogService(),
            CreateDetailsCallbacks());

        await panel.LoadTicketDetailsAsync(1);

        Assert.Equal("In progress", panel.SelectedStatus);
        Assert.Equal("Medium", panel.SelectedPriority);

        AppStrings.ApplyCulture("pl");
        panel.NotifyLocalization();

        Assert.Equal("W toku", panel.SelectedStatus);
        Assert.Equal("Średni", panel.SelectedPriority);
    }

    private static TicketsPanelCallbacks CreateTicketsCallbacks() =>
        new()
        {
            ShowToastKey = TestToastCallbacks.NoopKey,
            ShowToastRaw = TestToastCallbacks.NoopRaw,
            SetIsOffline = _ => { },
            GetIsOffline = () => false,
            NotifyStatistics = (_, _) => { },
            NotifyTicketsLoadingChanged = () => { },
            NotifyOnlineActionsChanged = () => { },
            GetApiErrorMessage = ex => ex.Message,
            GetCurrentUserId = () => 1,
            TicketSelected = _ => { },
            RefreshPaginationSideEffects = () => { },
            LogAuditAsync = (_, _, _, _) => Task.CompletedTask,
            ExecuteApiAsyncCore = async (action, _, _, _, _, _, _, _) =>
            {
                await action();
                return true;
            }
        };

    private static TicketDetailsPanelCallbacks CreateDetailsCallbacks() =>
        new()
        {
            ShowToastKey = TestToastCallbacks.NoopKey,
            ShowToastRaw = TestToastCallbacks.NoopRaw,
            SetIsOffline = _ => { },
            GetIsOffline = () => false,
            GetApiErrorMessage = ex => ex.Message,
            FindCachedTicket = _ => null,
            NotifyDetailsSideEffects = () => { },
            NotifyDetailsLoadingChanged = () => { },
            GetCurrentUser = () => new User { Id = 1, Login = "it", Role = AppRoles.It },
            GetCanManageTickets = () => true,
            GetIsAdminRole = () => false,
            GetIsRegularUser = () => false,
            LogAuditAsync = (_, _, _, _) => Task.CompletedTask,
            RefreshTicketsAsync = () => Task.CompletedTask,
            NavigateToTickets = () => { },
            ClearSelectedTicket = () => { },
            ExecuteApiAsyncCore = async (action, _, _, _, _, _, _, _) =>
            {
                await action();
                return true;
            }
        };
}
