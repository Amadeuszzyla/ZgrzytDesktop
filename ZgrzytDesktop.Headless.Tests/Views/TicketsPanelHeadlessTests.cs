using System;
using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.Views.DashboardParts;

namespace ZgrzytDesktop.Headless.Tests.Views;

public class TicketsPanelHeadlessTests : HeadlessViewTestsBase
{
    [Fact]
    public void DashboardView_TicketsPage_ContainsFiltersAndTicketCardList()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Tickets;

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var ticketsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(ticketsPanel);
                Assert.False(HeadlessViewTestHelper.ContainsText(ticketsPanel, vm.LblTicketsFiltersTitle));
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(ticketsPanel) >= 7);
                Assert.Equal(0, HeadlessViewTestHelper.CountDescendantsByTypeName(ticketsPanel, "DataGrid"));
                Assert.True(HeadlessViewTestHelper.CountDescendantsByTypeName(ticketsPanel, "ListBox") >= 1);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_TicketsPage_UsesTicketsPanelBindings()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Tickets;
                vm.TicketsPanel.SearchText = "headless-smoke";
                vm.TicketsPanel.StatusMessage = "Status headless test";

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                var ticketsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(ticketsPanel);

                Assert.Same(vm.TicketsPanel, vm.TicketsPanel);

                var searchBox = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.TextBox>(ticketsPanel)
                    .FirstOrDefault(tb => tb.Text == "headless-smoke");

                Assert.NotNull(searchBox);

                Assert.True(HeadlessViewTestHelper.ContainsText(ticketsPanel, vm.TicketsPanel.StatusMessage));
                Assert.Equal(0, HeadlessViewTestHelper.CountDescendantsByTypeName(ticketsPanel, "DataGrid"));
                Assert.True(HeadlessViewTestHelper.CountDescendantsByTypeName(ticketsPanel, "ListBox") >= 1);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_TicketsPage_ShowsLocalizedStatusAndPriorityBadges()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                AppStrings.ApplyCulture("en");
                vm.CurrentSection = AppSections.Tickets;
                vm.TicketsPanel.Tickets.Add(new Ticket
                {
                    Id = 42,
                    Title = "Badge localization",
                    Description = "Desc",
                    Status = TicketStatuses.Nowe,
                    Priority = TicketPriorities.Low,
                    User = new User { Id = 1, Name = "Reporter", Login = "rep" }
                });
                vm.TicketsPanel.NotifyLocalization();

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);
                var ticketsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(ticketsPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(ticketsPanel!, "New"));
                Assert.True(HeadlessViewTestHelper.ContainsText(ticketsPanel!, "Low"));
                Assert.False(HeadlessViewTestHelper.ContainsText(ticketsPanel!, "Nowe"));
                Assert.False(HeadlessViewTestHelper.ContainsText(ticketsPanel!, "Niski"));
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void TicketsPanel_DoesNotShowAdminTabs_PL() =>
        RunTicketsPanelToolbarTest(
            culture: "pl",
            forbiddenButtonLabels: ["Nowe konto", "Użytkownicy"],
            expectedSearchPlaceholder: "Szukaj po tytule lub opisie...",
            expectedStatusFilterLabel: "Wszystkie statusy");

    [Fact]
    public void TicketsPanel_DoesNotShowAdminTabs_EN() =>
        RunTicketsPanelToolbarTest(
            culture: "en",
            forbiddenButtonLabels: ["New account", "Users"],
            expectedSearchPlaceholder: "Search by title or description...",
            expectedStatusFilterLabel: "All statuses");

    [Fact]
    public void TicketsPanel_ShowsSearchAndTicketFilters_PL() =>
        RunTicketsPanelToolbarTest(
            culture: "pl",
            forbiddenButtonLabels: ["Nowe konto", "Użytkownicy"],
            expectedSearchPlaceholder: "Szukaj po tytule lub opisie...",
            expectedStatusFilterLabel: "Wszystkie statusy");

    [Fact]
    public void TicketsPanel_ShowsSearchAndTicketFilters_EN() =>
        RunTicketsPanelToolbarTest(
            culture: "en",
            forbiddenButtonLabels: ["New account", "Users"],
            expectedSearchPlaceholder: "Search by title or description...",
            expectedStatusFilterLabel: "All statuses");

    [Fact]
    public void TicketsPanel_ShowsCategoryFilter_PL() =>
        RunTicketsPanelToolbarTest(
            culture: "pl",
            forbiddenButtonLabels: ["Nowe konto", "Użytkownicy"],
            expectedSearchPlaceholder: "Szukaj po tytule lub opisie...",
            expectedStatusFilterLabel: "Wszystkie statusy",
            expectedCategoryFilterLabel: "Sieć",
            categoryFilterKey: TicketCategoryFilterKeys.Network);

    [Fact]
    public void TicketsPanel_ShowsCategoryFilter_EN() =>
        RunTicketsPanelToolbarTest(
            culture: "en",
            forbiddenButtonLabels: ["New account", "Users"],
            expectedSearchPlaceholder: "Search by title or description...",
            expectedStatusFilterLabel: "All statuses",
            expectedCategoryFilterLabel: "Network",
            categoryFilterKey: TicketCategoryFilterKeys.Network);

    [Fact]
    public void TicketsPanelView_WithNoTickets_ShowsEmptyState()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Tickets;
                vm.TicketsPanel.IsLoading = false;
                vm.TicketsPanel.Tickets.Clear();

                var panelView = new TicketsPanelView { DataContext = vm };
                HeadlessViewTestHelper.ShowInWindow(panelView);

                Assert.True(vm.TicketsPanel.HasNoTickets);

                var emptyState = HeadlessViewTestHelper.FindTextBlockWithExactText(
                    panelView,
                    AppStrings.Get("Tickets_EmptyList"));

                Assert.NotNull(emptyState);
                Assert.True(emptyState!.IsVisible);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void TicketsPanelView_WithTickets_HidesEmptyStateAndShowsList()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Tickets;
                vm.TicketsPanel.IsLoading = false;
                vm.TicketsPanel.Tickets.Clear();
                vm.TicketsPanel.Tickets.Add(new Ticket
                {
                    Id = 42,
                    Title = "Headless ticket card",
                    Description = "Visible card",
                    Status = TicketStatuses.Nowe,
                    Priority = TicketPriorities.Medium,
                    User = new User { Name = "Tester" },
                    CreatedAt = DateTime.UtcNow
                });

                var panelView = new TicketsPanelView { DataContext = vm };
                HeadlessViewTestHelper.ShowInWindow(panelView);

                var listBox = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.ListBox>(panelView)
                    .FirstOrDefault();

                Assert.NotNull(listBox);
                Assert.True(listBox!.IsVisible);

                var emptyState = HeadlessViewTestHelper.FindTextBlockWithExactText(
                    panelView,
                    AppStrings.Get("Tickets_EmptyList"));

                Assert.NotNull(emptyState);
                Assert.False(emptyState!.IsVisible);
                Assert.True(HeadlessViewTestHelper.ContainsText(panelView, "Headless ticket card"));
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    private static void RunTicketsPanelToolbarTest(
        string culture,
        string[] forbiddenButtonLabels,
        string expectedSearchPlaceholder,
        string expectedStatusFilterLabel,
        string? expectedCategoryFilterLabel = null,
        string? categoryFilterKey = null)
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            using var cultureScope = new TestCultureScope(culture);
            HeadlessViewTestHelper.ApplyUiCulture(culture);

            var settings = new FakeSettingsService();
            settings.Settings.UiCulture = culture;

            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin", settings: settings);
            Avalonia.Controls.Window? window = null;

            try
            {
                HeadlessViewTestHelper.ApplyUiCulture(culture);
                vm.TicketsPanel.NotifyLocalization();
                vm.CurrentSection = AppSections.Tickets;
                vm.TicketsPanel.StatusMessage = AppStrings.GetFormat("Tickets_Loaded", 0, 0);

                var panelView = new TicketsPanelView { DataContext = vm };
                window = HeadlessViewTestHelper.ShowInWindow(panelView);
                HeadlessViewTestHelper.WaitForUiIdle(panelView);

                foreach (var label in forbiddenButtonLabels)
                {
                    Assert.False(
                        HeadlessViewTestHelper.ContainsText(panelView, label),
                        $"Tickets panel must not show admin toolbar label: {label}");
                }

                HeadlessViewTestHelper.ApplyUiCulture(culture);
                Assert.Equal(expectedSearchPlaceholder, AppStrings.Get("Tickets_SearchPlaceholder"));
                Assert.True(
                    HeadlessViewTestHelper.FindDescendants<Avalonia.Controls.TextBox>(panelView).Any(),
                    "Tickets search field should be present.");

                Assert.True(
                    HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(panelView) >= 7,
                    "Expected status, priority, assignment, queue, category, sort field and sort direction filters.");

                Assert.Equal(expectedStatusFilterLabel, vm.TicketsPanel.SelectedFilterStatusOption?.Label);
                Assert.NotNull(vm.TicketsPanel.SelectedFilterPriorityOption);
                Assert.NotNull(vm.TicketsPanel.SelectedFilterAssignmentOption);
                Assert.NotNull(vm.TicketsPanel.SelectedTicketSortField);
                Assert.NotNull(vm.TicketsPanel.SelectedTicketSortDirection);
                Assert.Contains(
                    vm.TicketsPanel.FilterCategoryOptions,
                    option => option.Key == TicketCategoryFilterKeys.Software);

                if (categoryFilterKey is not null)
                {
                    vm.TicketsPanel.SelectedCategoryFilterOption = vm.TicketsPanel.FilterCategoryOptions
                        .First(option => option.Key == categoryFilterKey);

                    HeadlessViewTestHelper.WaitForCondition(
                        () => !vm.TicketsPanel.IsLoading,
                        timeoutMs: 5000);
                    HeadlessViewTestHelper.ApplyUiCulture(culture);
                    HeadlessViewTestHelper.WaitForUiIdle(panelView);
                }

                if (expectedCategoryFilterLabel is not null && categoryFilterKey is not null)
                {
                    HeadlessViewTestHelper.ApplyUiCulture(culture);
                    Assert.Equal(expectedCategoryFilterLabel, vm.TicketsPanel.SelectedCategoryFilterOption?.Label);
                    Assert.True(
                        HeadlessViewTestHelper.ComboBoxHasSelectedCategory(
                            panelView,
                            categoryFilterKey,
                            expectedCategoryFilterLabel),
                        "Category filter ComboBox should expose the selected localized label.");
                }

                Assert.True(HeadlessViewTestHelper.ContainsText(panelView, vm.TicketsPanel.StatusMessage));
                Assert.False(HeadlessViewTestHelper.ContainsText(panelView, AppStrings.Get("Tickets_FiltersTitle")));
            }
            finally
            {
                HeadlessViewTestHelper.CloseWindow(window);
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }
}
