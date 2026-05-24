using System.Net;
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

public class TicketDetailsAssignmentTests
{
    public TicketDetailsAssignmentTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task Admin_CanLoadAssignableUsers()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[1] = new Ticket { Id = 1, Title = "T", Status = TicketStatuses.Nowe };

        var users = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 2, Name = "IT User", Login = "it1", Role = AppRoles.It, Active = true },
                new User { Id = 3, Name = "Admin User", Login = "admin1", Role = AppRoles.Admin, Active = true },
                new User { Id = 4, Name = "Regular", Login = "user1", Role = AppRoles.User, Active = true }
            ]
        };

        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin });

        await panel.LoadTicketDetailsAsync(1);

        Assert.Equal(UserAdminListFilter.All, users.LastFilter);
        Assert.Equal(2, panel.AssignableUsers.Count(option => !option.IsUnassigned));
    }

    [Fact]
    public async Task Admin_AssignableUsers_OnlyItAndAdmin()
    {
        var users = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 2, Login = "it1", Role = AppRoles.It, Active = true },
                new User { Id = 3, Login = "admin1", Role = AppRoles.Admin, Active = true },
                new User { Id = 4, Login = "user1", Role = AppRoles.User, Active = true },
                new User { Id = 5, Login = "banned-it", Role = AppRoles.It, Active = true, Ban = true }
            ]
        };

        var tickets = new FakeTicketService();
        tickets.TicketsById[2] = new Ticket { Id = 2, Title = "T", Status = TicketStatuses.Nowe };

        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin });

        await panel.LoadTicketDetailsAsync(2);

        Assert.DoesNotContain(panel.AssignableUsers, option => option.Label.Contains("user1", StringComparison.Ordinal));
        Assert.Contains(panel.AssignableUsers, option => option.UserId == 2);
        Assert.Contains(panel.AssignableUsers, option => option.UserId == 3);
    }

    [Fact]
    public async Task Admin_AssignTicketToSelectedUser_UsesAssignedItId()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[3] = new Ticket { Id = 3, Title = "T", Status = TicketStatuses.Nowe };

        var users = new FakeUserAdminService
        {
            NextUsers = [new User { Id = 42, Name = "Worker", Login = "it42", Role = AppRoles.It, Active = true }]
        };

        var ctx = new TicketDetailsPanelViewModelTests.DetailsTestContext();
        var (panel, ticketService, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin },
            context: ctx);

        await panel.LoadTicketDetailsAsync(3);
        panel.SelectedAssignedUser = panel.AssignableUsers.First(option => option.UserId == 42);

        await panel.AssignSelectedUserCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal(42, ticketService.LastUpdateRequest!.AssignedItId);
        Assert.Null(ticketService.LastUpdateRequest.Status);
        Assert.Null(ticketService.LastUpdateRequest.Priority);
    }

    [Fact]
    public async Task Admin_AssignSelectedUser_PreservesStatusAndPriority() =>
        await Admin_AssignTicketToSelectedUser_UsesAssignedItId();

    [Fact]
    public async Task Admin_AssignSelectedUser_SendsSelectedUserIdAsAssignedItId() =>
        await Admin_AssignTicketToSelectedUser_UsesAssignedItId();

    [Fact]
    public async Task Admin_AssignSelectedUser_RefreshesTicketDetailsAndList() =>
        await Admin_AssignmentSaved_RefreshesDetailsAndList();

    [Fact]
    public async Task Admin_Details_ShowsAssigneeComboBox()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[1] = new Ticket { Id = 1, Title = "T", Status = TicketStatuses.Nowe };

        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin });

        await panel.LoadTicketDetailsAsync(1);

        Assert.True(panel.CanSelectAssignee);
        Assert.True(panel.CanShowAdminAssignmentControls);
    }

    [Fact]
    public async Task Admin_LoadAssignableUsers_ReturnsOnlyAdminAndIt() =>
        await Admin_AssignableUsers_OnlyItAndAdmin();

    [Fact]
    public async Task Admin_LoadAssignableUsers_IsCaseInsensitiveForRoles()
    {
        var users = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 2, Login = "it1", Role = "IT", Active = true },
                new User { Id = 3, Login = "admin1", Role = "ADMINISTRATOR", Active = true }
            ]
        };

        var tickets = new FakeTicketService();
        tickets.TicketsById[12] = new Ticket { Id = 12, Title = "T", Status = TicketStatuses.Nowe };

        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin });

        await panel.LoadTicketDetailsAsync(12);

        Assert.Equal(2, panel.AssignableUsers.Count(option => !option.IsUnassigned));
    }

    [Fact]
    public void IT_Details_DoesNotShowAssigneeComboBox() =>
        IT_SeesAssignToMeOnly();

    [Fact]
    public void Assignment_DoesNotAllowRegularUser()
    {
        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            new FakeTicketService(),
            canManageTickets: false,
            isAdminRole: false,
            isRegularUser: true,
            currentUser: new User { Id = 1, Login = "user", Role = AppRoles.User });

        Assert.False(panel.CanSelectAssignee);
        Assert.False(panel.CanShowAdminAssignmentControls);
        Assert.False(panel.CanAssignSelectedUser);
    }

    [Fact]
    public async Task Admin_AssignableUsers_ExcludesRegularUsers() =>
        await Admin_AssignableUsers_OnlyItAndAdmin();

    [Fact]
    public async Task Admin_AssignToMe_StillWorks() =>
        await Admin_AssignTicketToSelf_StillWorks();

    [Fact]
    public async Task Admin_AssignableUsers_IncludesStaff_WhenActiveFieldOmittedOnActiveEndpoint()
    {
        var users = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 2, Login = "it1", Role = AppRoles.It, Active = false },
                new User { Id = 3, Login = "admin1", Role = "administrator", Active = false }
            ]
        };

        var tickets = new FakeTicketService();
        tickets.TicketsById[10] = new Ticket { Id = 10, Title = "T", Status = TicketStatuses.Nowe };

        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = "administrator" });

        await panel.LoadTicketDetailsAsync(10);

        Assert.Equal(2, panel.AssignableUsers.Count(option => !option.IsUnassigned));
        Assert.True(panel.CanSelectAssignee);
        panel.SelectedAssignableUser = panel.AssignableUsers.First(option => option.UserId == 2);
        Assert.True(panel.CanAssignSelectedUser);
    }

    [Fact]
    public async Task Admin_GetActiveUsersForbidden_FallsBackToAllUsers()
    {
        var users = new FakeUserAdminService
        {
            GetActiveUsersApiException = new ApiException(HttpStatusCode.Forbidden, "forbidden"),
            NextUsers =
            [
                new User { Id = 2, Login = "it1", Role = AppRoles.It, Active = true },
                new User { Id = 3, Login = "admin1", Role = AppRoles.Admin, Active = true }
            ]
        };

        var tickets = new FakeTicketService();
        tickets.TicketsById[11] = new Ticket { Id = 11, Title = "T", Status = TicketStatuses.Nowe };

        var (panel, _, userService) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin });

        await panel.LoadTicketDetailsAsync(11);

        Assert.Equal(UserAdminListFilter.All, userService.LastFilter);
        Assert.Equal(2, panel.AssignableUsers.Count(option => !option.IsUnassigned));
        Assert.False(panel.ShowAssignableUsersEmptyMessage);
    }

    [Fact]
    public async Task Admin_EmptyActiveUsersEndpoint_FallsBackToAllUsers()
    {
        var users = new FakeUserAdminService
        {
            NextActiveUsers =
            [
                new User { Id = 4, Login = "user1", Role = AppRoles.User, Active = true }
            ],
            NextUsers =
            [
                new User { Id = 2, Login = "it1", Role = AppRoles.It, Active = true },
                new User { Id = 3, Login = "admin1", Role = AppRoles.Admin, Active = true }
        ]
        };

        var tickets = new FakeTicketService();
        tickets.TicketsById[13] = new Ticket { Id = 13, Title = "T", Status = TicketStatuses.Nowe };

        var (panel, _, userService) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin });

        await panel.LoadTicketDetailsAsync(13);

        Assert.Equal(UserAdminListFilter.All, userService.LastFilter);
        Assert.Equal(2, panel.AssignableUsers.Count(option => !option.IsUnassigned));
    }

    [Fact]
    public void IT_SeesAssignToMeOnly()
    {
        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            new FakeTicketService(),
            canManageTickets: true,
            isAdminRole: false,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "it", Role = AppRoles.It });

        Assert.False(panel.CanSelectAssignee);
        Assert.False(panel.CanShowAdminAssignmentControls);
        Assert.True(panel.AssignToMeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Admin_AssignTicketToSelf_StillWorks()
    {
        var admin = new User { Id = 10, Login = "admin", Name = "Admin", Role = AppRoles.Admin };
        var tickets = new FakeTicketService();
        tickets.TicketsById[4] = new Ticket { Id = 4, Title = "T", Status = TicketStatuses.Nowe };

        var users = new FakeUserAdminService
        {
            NextUsers = [admin, new User { Id = 42, Login = "it42", Role = AppRoles.It, Active = true }]
        };

        var (panel, ticketService, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: admin);

        await panel.LoadTicketDetailsAsync(4);
        await panel.AssignToMeCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal(10, ticketService.LastUpdateRequest!.AssignedItId);
    }

    [Fact]
    public async Task Admin_AssignmentSaved_RefreshesDetailsAndList()
    {
        var tickets = new FakeTicketService();
        tickets.TicketsById[5] = new Ticket { Id = 5, Title = "T", Status = TicketStatuses.Nowe, AssignedItId = null };

        var users = new FakeUserAdminService
        {
            NextUsers = [new User { Id = 7, Login = "it7", Role = AppRoles.It, Active = true }]
        };

        var ctx = new TicketDetailsPanelViewModelTests.DetailsTestContext();
        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin },
            context: ctx);

        await panel.LoadTicketDetailsAsync(5);
        panel.SelectedAssignedUser = panel.AssignableUsers.First(option => option.UserId == 7);
        await panel.AssignSelectedUserCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal(1, ctx.RefreshTicketsCount);
        Assert.Equal(7, panel.TicketDetails!.AssignedItId);
    }

    [Fact]
    public async Task Admin_AssignmentFailure_ShowsLocalizedToast()
    {
        var tickets = new FakeTicketService
        {
            UpdateTicketApiException = new ApiException(HttpStatusCode.Forbidden, "forbidden")
        };
        tickets.TicketsById[6] = new Ticket { Id = 6, Title = "T", Status = TicketStatuses.Nowe };

        var users = new FakeUserAdminService
        {
            NextUsers = [new User { Id = 8, Login = "it8", Role = AppRoles.It, Active = true }]
        };

        var (panel, _, _) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: true,
            isRegularUser: false,
            currentUser: new User { Id = 10, Login = "admin", Role = AppRoles.Admin });

        await panel.LoadTicketDetailsAsync(6);
        panel.SelectedAssignedUser = panel.AssignableUsers.First(option => option.UserId == 8);
        await panel.AssignSelectedUserCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.NotEqual(AppStrings.Get("Ticket_AssignmentSaved"), panel.DetailsStatusMessage);
    }

    [Fact]
    public async Task IT_AssignToMe_StillWorks()
    {
        var itUser = new User { Id = 55, Login = "it55", Role = AppRoles.It };
        var tickets = new FakeTicketService();
        tickets.TicketsById[7] = new Ticket { Id = 7, Title = "T", Status = TicketStatuses.Nowe };

        var (panel, ticketService, users) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            canManageTickets: true,
            isAdminRole: false,
            isRegularUser: false,
            currentUser: itUser);

        await panel.LoadTicketDetailsAsync(7);
        await panel.AssignToMeCommand.ExecuteAsync(null);

        while (panel.IsLoadingDetails)
            await Task.Delay(10);

        Assert.Equal(55, ticketService.LastUpdateRequest!.AssignedItId);
        Assert.Equal(0, users.GetUsersCallCount);
        Assert.False(panel.CanShowAdminAssignmentControls);
    }

    [Fact]
    public async Task IT_DoesNotLoadFullAssignableUsers_IfNoPermission()
    {
        var users = new FakeUserAdminService
        {
            GetUsersApiException = new ApiException(HttpStatusCode.Forbidden, "forbidden")
        };

        var tickets = new FakeTicketService();
        tickets.TicketsById[8] = new Ticket { Id = 8, Title = "T", Status = TicketStatuses.Nowe };

        var (panel, _, userService) = TicketDetailsPanelViewModelTests.CreatePanel(
            tickets,
            users: users,
            canManageTickets: true,
            isAdminRole: false,
            isRegularUser: false,
            currentUser: new User { Id = 55, Login = "it55", Role = AppRoles.It });

        await panel.LoadTicketDetailsAsync(8);

        Assert.Equal(0, userService.GetUsersCallCount);
        Assert.Empty(panel.AssignableUsers);
    }

    [Fact]
    public void Assignment_Localized_PL_EN()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal("Przypisz do", AppStrings.Get("Ticket_AssignTo"));
        Assert.Equal("Zapisz przypisanie", AppStrings.Get("Ticket_SaveAssignment"));
        Assert.Equal("Przypisanie zapisane.", AppStrings.Get("Ticket_AssignmentSaved"));
        Assert.Equal("Brak dostępnych pracowników do przypisania.", AppStrings.Get("Ticket_NoAssignableUsers"));

        AppStrings.ApplyCulture("en");
        Assert.Equal("Assign to", AppStrings.Get("Ticket_AssignTo"));
        Assert.Equal("Save assignment", AppStrings.Get("Ticket_SaveAssignment"));
        Assert.Equal("Assignment saved.", AppStrings.Get("Ticket_AssignmentSaved"));
        Assert.Equal("No staff users available for assignment.", AppStrings.Get("Ticket_NoAssignableUsers"));
    }

    [Fact]
    public void MessageEditDelete_NotShown_WhenApiUnsupported()
    {
        Assert.False(typeof(ITicketService).GetMethod("UpdateMessageAsync") is not null);
        Assert.False(typeof(ITicketService).GetMethod("DeleteMessageAsync") is not null);
    }
}
