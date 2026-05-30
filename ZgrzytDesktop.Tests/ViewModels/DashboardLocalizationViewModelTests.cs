using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class DashboardLocalizationViewModelTests
{
    public DashboardLocalizationViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void Labels_ReturnPolishStrings_WhenCultureIsPolish()
    {
        using var _ = new TestCultureScope("pl");
        var labels = new DashboardLocalizationViewModel();

        Assert.Equal("Zgłoszenia", labels.LblNavTickets);
        Assert.Equal("Lista zgłoszeń", labels.LblTicketsListTitle);
        Assert.Equal("Ustawienia", labels.LblNavSettings);
    }

    [Fact]
    public void Labels_ReturnEnglishStrings_WhenCultureIsEnglish()
    {
        using var _ = new TestCultureScope("en");
        var labels = new DashboardLocalizationViewModel();

        Assert.Equal("Tickets", labels.LblNavTickets);
        Assert.Equal("Ticket list", labels.LblTicketsListTitle);
        Assert.Equal("Settings", labels.LblNavSettings);
    }

    [Fact]
    public void NotifyLabels_RaisesPropertyChangedForKeyLabels()
    {
        var labels = new DashboardLocalizationViewModel();
        var changed = new List<string>();
        labels.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changed.Add(e.PropertyName);
        };

        labels.NotifyLabels();

        Assert.Contains(nameof(DashboardLocalizationViewModel.LblNavTickets), changed);
        Assert.Contains(nameof(DashboardLocalizationViewModel.LblNavSettings), changed);
        Assert.Contains(nameof(DashboardLocalizationViewModel.LblTicketsListTitle), changed);
        Assert.Contains(nameof(DashboardLocalizationViewModel.LblDetailsBackToList), changed);
        Assert.Contains(nameof(DashboardLocalizationViewModel.LblAuditTitle), changed);
        Assert.Equal(117, changed.Count);
    }

    [Fact]
    public void DashboardViewModel_ForwardsLblNavTickets()
    {
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard();

        try
        {
            Assert.Equal(AppStrings.Get("Nav_Tickets"), vm.LblNavTickets);
            Assert.Equal(AppStrings.Get("Tickets_ListTitle"), vm.LblTicketsListTitle);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void DashboardViewModel_NotifyLocalizationProperties_RefreshesForwardedLabels()
    {
        using var pl = new TestCultureScope("pl");
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard(
            bootstrap: DashboardViewModel.BootstrapOptions.Testing);

        try
        {
            var plNav = vm.LblNavTickets;

            AppStrings.ApplyCulture("en");
            vm.GetType().GetMethod(
                    "NotifyLocalizationProperties",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(vm, null);

            Assert.Equal("Tickets", vm.LblNavTickets);
            Assert.NotEqual(plNav, vm.LblNavTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void DashboardViewModel_NotifyLocalizationProperties_ForwardsPropertyChangedForLabels()
    {
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard(
            bootstrap: DashboardViewModel.BootstrapOptions.Testing);
        var changed = new List<string>();

        try
        {
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is not null)
                    changed.Add(e.PropertyName);
            };

            vm.GetType().GetMethod(
                    "NotifyLocalizationProperties",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(vm, null);

            Assert.Contains(nameof(DashboardViewModel.LblNavTickets), changed);
            Assert.Contains(nameof(DashboardViewModel.LblTicketsListTitle), changed);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
