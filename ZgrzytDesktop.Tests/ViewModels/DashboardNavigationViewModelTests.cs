using System.ComponentModel;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class DashboardNavigationViewModelTests
{
    public DashboardNavigationViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    private static User CreateUser(string role) =>
        new()
        {
            Id = 1,
            Login = "tester",
            Name = "Tester",
            Role = role,
            Active = true
        };

    [Theory]
    [InlineData(AppSections.Tickets, true, false, false, false, false, false)]
    [InlineData(AppSections.Details, false, true, false, false, false, false)]
    [InlineData(AppSections.Settings, false, false, true, false, false, false)]
    [InlineData(AppSections.Statistics, false, false, false, true, false, false)]
    [InlineData(AppSections.Admin, false, false, false, false, true, false)]
    [InlineData(AppSections.RequestAccount, false, false, false, false, false, true)]
    public void CurrentSection_UpdatesPageVisibility(
        string section,
        bool ticketsVisible,
        bool detailsVisible,
        bool settingsVisible,
        bool statisticsVisible,
        bool adminVisible,
        bool requestAccountVisible)
    {
        var navigation = new DashboardNavigationViewModel(CreateUser("user"));

        navigation.CurrentSection = section;

        Assert.Equal(ticketsVisible, navigation.IsTicketsPageVisible);
        Assert.Equal(detailsVisible, navigation.IsDetailsPageVisible);
        Assert.Equal(settingsVisible, navigation.IsSettingsPageVisible);
        Assert.Equal(statisticsVisible, navigation.IsStatisticsPageVisible);
        Assert.Equal(adminVisible, navigation.IsAdminPageVisible);
        Assert.Equal(requestAccountVisible, navigation.IsRequestAccountPageVisible);
    }

    [Fact]
    public void ShowTicketsPageCommand_SwitchesToTicketsSection()
    {
        var navigation = new DashboardNavigationViewModel(CreateUser("it"));
        navigation.CurrentSection = AppSections.Settings;

        navigation.ShowTicketsPageCommand.Execute(null);

        Assert.Equal(AppSections.Tickets, navigation.CurrentSection);
        Assert.True(navigation.IsTicketsNavActive);
        Assert.True(navigation.IsTicketsPageVisible);
    }

    [Fact]
    public void ShowStatisticsPageCommand_SwitchesToStatisticsSection()
    {
        var navigation = new DashboardNavigationViewModel(CreateUser("user"));

        navigation.ShowStatisticsPageCommand.Execute(null);

        Assert.Equal(AppSections.Statistics, navigation.CurrentSection);
        Assert.True(navigation.IsStatisticsNavActive);
        Assert.True(navigation.IsStatisticsPageVisible);
    }

    [Theory]
    [InlineData("user", true, false, false)]
    [InlineData("it", false, true, false)]
    [InlineData("admin", false, true, true)]
    public void Role_ControlsNavAvailability(string role, bool showRequestAccount, bool showAdministration, bool isAdmin)
    {
        var navigation = new DashboardNavigationViewModel(CreateUser(role));

        Assert.Equal(showRequestAccount, navigation.ShowRequestAccountNav);
        Assert.Equal(showAdministration, navigation.ShowAdministrationNav);
        Assert.Equal(isAdmin, navigation.IsAdminRole);
        Assert.Equal(showAdministration, navigation.IsStaffRole);
    }

    [Fact]
    public void CurrentSection_RaisesPropertyChangedForDependentVisibilityFlags()
    {
        var navigation = new DashboardNavigationViewModel(CreateUser("user"));
        var changed = new List<string>();
        navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changed.Add(e.PropertyName);
        };

        navigation.CurrentSection = AppSections.Settings;

        Assert.Contains(nameof(DashboardNavigationViewModel.CurrentSection), changed);
        Assert.Contains(nameof(DashboardNavigationViewModel.IsSettingsPageVisible), changed);
        Assert.Contains(nameof(DashboardNavigationViewModel.IsSettingsNavActive), changed);
        Assert.Contains(nameof(DashboardNavigationViewModel.CurrentSectionTitle), changed);
        Assert.Contains(nameof(DashboardNavigationViewModel.IsTicketsPageVisible), changed);
    }

    [Fact]
    public void ShowSettingsPageCommand_InvokesSettingsNavigatedCallback()
    {
        var callbackInvoked = false;
        var navigation = new DashboardNavigationViewModel(
            CreateUser("it"),
            onSettingsNavigated: () => callbackInvoked = true);

        navigation.ShowSettingsPageCommand.Execute(null);

        Assert.True(callbackInvoked);
        Assert.Equal(AppSections.Settings, navigation.CurrentSection);
    }

    [Fact]
    public void ShowAdminPageCommand_InvokesAdminNavigatedCallbackWithRole()
    {
        bool? callbackIsAdmin = null;
        var navigation = new DashboardNavigationViewModel(
            CreateUser("admin"),
            onAdminNavigated: isAdmin => callbackIsAdmin = isAdmin);

        navigation.ShowAdminPageCommand.Execute(null);

        Assert.True(callbackIsAdmin);
        Assert.Equal(AppSections.Admin, navigation.CurrentSection);
    }

    [Fact]
    public void NotifyLocalization_RaisesCurrentSectionTitleChanged()
    {
        var navigation = new DashboardNavigationViewModel(CreateUser("user"));
        navigation.CurrentSection = AppSections.Tickets;
        var changed = new List<string>();
        navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changed.Add(e.PropertyName);
        };

        navigation.NotifyLocalization();

        Assert.Contains(nameof(DashboardNavigationViewModel.CurrentSectionTitle), changed);
        Assert.Equal(AppStrings.Get("Section_Tickets"), navigation.CurrentSectionTitle);
    }

    [Fact]
    public void DashboardViewModel_ForwardsNavigationPropertyChanged()
    {
        var (vm, _, _, _) = ViewModelTestFactory.CreateDashboard("user");
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changed.Add(e.PropertyName);
        };

        vm.CurrentSection = AppSections.Statistics;

        Assert.Contains(nameof(DashboardViewModel.CurrentSection), changed);
        Assert.Contains(nameof(DashboardViewModel.IsStatisticsPageVisible), changed);
        Assert.Contains(nameof(DashboardViewModel.IsStatisticsNavActive), changed);
    }
}
