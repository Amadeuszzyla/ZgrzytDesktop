using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketCategoryFilterTests
{
    public TicketCategoryFilterTests() => ViewModelTestSetup.EnsureAppStrings();

    private static Ticket SoftwareTicket() =>
        new() { Id = 1, Title = "CRM login issue", Category = "Software" };

    private static Ticket NetworkTicket() =>
        new() { Id = 2, Title = "VPN nie działa" };

    private static Ticket HardwareTicket() =>
        new() { Id = 3, Title = "Drukarka w biurze", Description = "Hardware failure" };

    private static Ticket UncategorizedTicket() =>
        new() { Id = 4, Title = "Inne zgłoszenie", Description = "bez słów kluczowych" };

    private static IEnumerable<Ticket> SampleTickets() =>
        [SoftwareTicket(), NetworkTicket(), HardwareTicket(), UncategorizedTicket()];

    [Fact]
    public void CategoryFilter_All_ShowsAllTickets()
    {
        foreach (var ticket in SampleTickets())
            Assert.True(TicketCategoryFilter.Matches(ticket, TicketCategoryFilterKeys.All));
    }

    [Fact]
    public void CategoryFilter_Software_FiltersSoftwareTickets()
    {
        Assert.True(TicketCategoryFilter.Matches(SoftwareTicket(), TicketCategoryFilterKeys.Software));
        Assert.False(TicketCategoryFilter.Matches(NetworkTicket(), TicketCategoryFilterKeys.Software));
        Assert.False(TicketCategoryFilter.Matches(HardwareTicket(), TicketCategoryFilterKeys.Software));
    }

    [Fact]
    public void CategoryFilter_Network_FiltersNetworkTickets()
    {
        Assert.True(TicketCategoryFilter.Matches(NetworkTicket(), TicketCategoryFilterKeys.Network));
        Assert.False(TicketCategoryFilter.Matches(SoftwareTicket(), TicketCategoryFilterKeys.Network));
    }

    [Fact]
    public void CategoryFilter_Hardware_FiltersHardwareTickets()
    {
        Assert.True(TicketCategoryFilter.Matches(HardwareTicket(), TicketCategoryFilterKeys.Hardware));
        Assert.False(TicketCategoryFilter.Matches(SoftwareTicket(), TicketCategoryFilterKeys.Hardware));
    }

    [Fact]
    public void CategoryFilter_UsesTicketCategory_WhenAvailable()
    {
        var ticket = new Ticket
        {
            Id = 10,
            Title = "neutral title",
            Description = "neutral description",
            Category = "Sieć"
        };

        Assert.Equal(TicketCategoryFilterKeys.Network, TicketCategoryFilter.ResolveCategoryKey(ticket));
        Assert.True(TicketCategoryFilter.Matches(ticket, TicketCategoryFilterKeys.Network));
        Assert.False(TicketCategoryFilter.Matches(ticket, TicketCategoryFilterKeys.Software));
    }

    [Fact]
    public void CategoryFilter_FallbacksToTitleAndDescription_WhenCategoryMissing()
    {
        var ticket = new Ticket
        {
            Id = 11,
            Title = "Problem z logowaniem",
            Description = "Nie mogę się zalogować do systemu CRM"
        };

        Assert.Equal(TicketCategoryFilterKeys.Software, TicketCategoryFilter.ResolveCategoryKey(ticket));
        Assert.True(TicketCategoryFilter.Matches(ticket, TicketCategoryFilterKeys.Software));
    }

    [Fact]
    public void CategoryFilter_IsCaseInsensitive()
    {
        var ticket = new Ticket { Id = 12, Title = "WIFI ROUTER", Category = "SOFTWARE" };

        Assert.True(TicketCategoryFilter.Matches(ticket, TicketCategoryFilterKeys.Software));
        Assert.True(TicketCategoryFilter.Matches(new Ticket { Id = 13, Title = "LAPTOP battery" }, TicketCategoryFilterKeys.Hardware));
    }

    [Fact]
    public void CategoryFilter_DoesNotCrashOnNullTitleDescription()
    {
        var ticket = new Ticket { Id = 14, Title = null!, Description = null!, Category = null };

        var resolved = TicketCategoryFilter.ResolveCategoryKey(ticket);

        Assert.False(TicketCategoryFilter.Matches(ticket, TicketCategoryFilterKeys.Software));
        Assert.False(TicketCategoryFilter.Matches(ticket, TicketCategoryFilterKeys.Network));
        Assert.False(TicketCategoryFilter.Matches(ticket, TicketCategoryFilterKeys.Hardware));
        Assert.Equal(string.Empty, resolved);
    }

    [Fact]
    public void CategoryFilter_Localizes_PL_EN()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal("Wszystkie", TicketCategoryFilterOption.GetLabel(TicketCategoryFilterKeys.All));
        Assert.Equal("Sieć", TicketCategoryFilterOption.GetLabel(TicketCategoryFilterKeys.Network));

        AppStrings.ApplyCulture("en");
        Assert.Equal("All", TicketCategoryFilterOption.GetLabel(TicketCategoryFilterKeys.All));
        Assert.Equal("Network", TicketCategoryFilterOption.GetLabel(TicketCategoryFilterKeys.Network));
    }

    [Fact]
    public void NotifyLocalization_RefreshesCategoryLabels_WithoutLosingSelectedCategory()
    {
        var tickets = new FakeTicketService();
        var (panel, _, tempDir) = TicketsPanelViewModelTests.CreatePanel(tickets);

        try
        {
            panel.SelectedCategoryFilterOption = panel.FilterCategoryOptions
                .First(option => option.Key == TicketCategoryFilterKeys.Network);

            AppStrings.ApplyCulture("pl");
            panel.NotifyLocalization();

            Assert.Equal(TicketCategoryFilterKeys.Network, panel.SelectedCategoryFilterKey);
            Assert.Equal("Sieć", panel.SelectedCategoryFilterOption?.Label);

            AppStrings.ApplyCulture("en");
            panel.NotifyLocalization();

            Assert.Equal(TicketCategoryFilterKeys.Network, panel.SelectedCategoryFilterKey);
            Assert.Equal("Network", panel.SelectedCategoryFilterOption?.Label);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
