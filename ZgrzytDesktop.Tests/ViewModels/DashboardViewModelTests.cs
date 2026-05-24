using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class DashboardViewModelTests
{
    public DashboardViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Theory]
    [InlineData(AppSections.Tickets, true, false, false, false, false, false)]
    [InlineData(AppSections.Details, false, true, false, false, false, false)]
    [InlineData(AppSections.Settings, false, false, true, false, false, false)]
    [InlineData(AppSections.Statistics, false, false, false, true, false, false)]
    [InlineData(AppSections.Admin, false, false, false, false, true, false)]
    public void CurrentSection_UpdatesPageVisibility(
        string section,
        bool ticketsVisible,
        bool detailsVisible,
        bool settingsVisible,
        bool statisticsVisible,
        bool adminVisible,
        bool requestAccountVisible)
    {
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard();

        vm.CurrentSection = section;

        Assert.Equal(ticketsVisible, vm.IsTicketsPageVisible);
        Assert.Equal(detailsVisible, vm.IsDetailsPageVisible);
        Assert.Equal(settingsVisible, vm.IsSettingsPageVisible);
        Assert.Equal(statisticsVisible, vm.IsStatisticsPageVisible);
        Assert.Equal(adminVisible, vm.IsAdminPageVisible);
        Assert.Equal(requestAccountVisible, vm.IsRequestAccountPageVisible);
    }

    [Theory]
    [InlineData("user", false, false)]
    [InlineData("it", true, true)]
    [InlineData("admin", true, true)]
    [InlineData("administrator", true, true)]
    public void Role_SetsManageAndStaffFlags(string role, bool canManage, bool isStaff)
    {
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard(role);

        Assert.Equal(canManage, vm.CanManageTickets);
        Assert.Equal(isStaff, vm.IsStaffRole);
        Assert.Equal(ZgrzytDesktop.Helpers.AppRoleHelper.IsAdmin(role), vm.IsAdminRole);
    }

    [Fact]
    public async Task SelectedTicketQueueView_Active_UsesActiveTicketsEndpoint()
    {
        var tickets = new FakeTicketService();
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard("it", tickets: tickets);

        vm.TicketsPanel.SelectedTicketQueueView = FilterLabels.Active;
        await Task.Delay(100);

        Assert.Equal(TicketQueueView.Active, tickets.LastQueueView);
    }

    [Fact]
    public async Task SelectedTicketQueueView_Unassigned_UsesUnassignedEndpoint()
    {
        var tickets = new FakeTicketService();
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard("it", tickets: tickets);

        vm.TicketsPanel.SelectedTicketQueueView = FilterLabels.Unassigned;
        await Task.Delay(100);

        Assert.Equal(TicketQueueView.Unassigned, tickets.LastQueueView);
    }

    [Fact]
    public async Task SaveSettingsCommand_PersistsCultureAndLightTheme()
    {
        var settings = new FakeSettingsService();
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard(settings: settings);

        vm.SelectedUiCulture = "en";

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Equal(1, settings.SaveAsyncCallCount);
        Assert.Equal(SettingsPanelViewModel.LightThemeMode, settings.Settings.ThemeMode);
        Assert.Equal("en", settings.Settings.UiCulture);
        Assert.Equal("en", vm.SelectedUiCulture);
    }

    [Fact]
    public void ShowToast_SetsMessageAndVisibility()
    {
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard();

        vm.ShowToast("Test komunikat", ToastTypes.Success);

        Assert.True(vm.IsToastVisible);
        Assert.Equal("Test komunikat", vm.ToastMessage);
    }

    [Fact]
    public async Task RefreshSession_OnUnauthorized_ShowsApiUnauthorizedMessage()
    {
        var auth = new FakeAuthService
        {
            RefreshException = new ApiException(HttpStatusCode.Unauthorized, "Unauthorized")
        };
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard(auth: auth);

        await vm.RefreshSessionCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Api_Unauthorized"), vm.SettingsStatusMessage);
        Assert.True(vm.IsToastVisible);
        Assert.Equal(AppStrings.Get("Api_Unauthorized"), vm.ToastMessage);
    }

    [Fact]
    public async Task RefreshSession_OnForbidden_ShowsApiForbiddenMessage()
    {
        var auth = new FakeAuthService
        {
            RefreshException = new ApiException(HttpStatusCode.Forbidden, "Forbidden")
        };
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard(auth: auth);

        await vm.RefreshSessionCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Api_Forbidden"), vm.SettingsStatusMessage);
        Assert.Equal(AppStrings.Get("Api_Forbidden"), vm.ToastMessage);
    }

    [Fact]
    public async Task IT_OpenAdminPage_DoesNotLoadUsers_FromDashboard()
    {
        var userAdmin = new FakeUserAdminService();
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard("it", userAdmin: userAdmin);

        vm.AdminPanel.PrepareAdminPage(vm.IsAdminRole);
        await vm.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(0, userAdmin.GetUsersCallCount);
        Assert.Equal(string.Empty, vm.AdminStatusMessage);
    }

    [Fact]
    public void UserRole_ShowsRequestAccountNav_NotStaffAdminNav()
    {
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard("user");

        Assert.True(vm.ShowRequestAccountNav);
        Assert.False(vm.ShowAdministrationNav);
    }

    [Fact]
    public void ItRole_ShowsAdministrationNav_WithoutUsersPanel()
    {
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard("it");

        Assert.True(vm.ShowAdministrationNav);
        Assert.False(vm.IsAdminRole);
        Assert.True(vm.IsStaffRole);

        vm.AdminPanel.PrepareAdminPage(vm.IsAdminRole);

        Assert.False(vm.IsAdminUsersPanelVisible);
        Assert.True(vm.IsAdminNewAccountPanelVisible);
    }

    [Fact]
    public void AdminRole_ShowsUsersPanelOnAdminPage()
    {
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard("admin");

        Assert.True(vm.ShowAdministrationNav);
        Assert.True(vm.IsAdminRole);

        vm.AdminPanel.PrepareAdminPage(vm.IsAdminRole);

        Assert.True(vm.IsAdminUsersPanelVisible);
        Assert.Equal(AdminTabs.Users, vm.AdminTab);
    }
}
