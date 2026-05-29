using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.Views.DashboardParts;

namespace ZgrzytDesktop.Headless.Tests.Views;

public class DashboardShellHeadlessTests : HeadlessViewTestsBase
{
    [Fact]
    public void DashboardView_CreatesWithoutException_WithFakeViewModel()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                Assert.NotNull(window.Content);
                Assert.Same(vm, view.DataContext);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_StatisticsPage_ContainsKpiAndCharts()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Statistics;

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var statisticsPanel = HeadlessViewTestHelper
                    .FindDescendants<StatisticsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(statisticsPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.StatisticsPanel.LblStatsTitle));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.StatisticsPanel.LblStatsChartByStatus));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.StatisticsPanel.LblStatsChartByPriority));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.StatisticsPanel.LblStatsChartAssignment));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.StatisticsPanel.LblStatsKpiAll));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.StatisticsPanel.LblStatsKpiHighPriority));
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ProgressBar>(statisticsPanel!) >= 8);
                Assert.False(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "czas pierwszej"));
                Assert.False(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "first response"));
                Assert.False(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "reakcji"));
                Assert.False(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "response time"));
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_StatisticsPage_ShowsPolishLabelsAfterCultureSwitch()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                AppStrings.ApplyCulture("pl");
                vm.CurrentSection = AppSections.Statistics;
                vm.StatisticsPanel.NotifyLocalization();

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                var statisticsPanel = HeadlessViewTestHelper
                    .FindDescendants<StatisticsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(statisticsPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "Statystyki zgłoszeń"));
                Assert.True(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "Według statusu"));
                Assert.True(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "Przypisanie"));
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_StatisticsPage_ShowsEnglishLabelsAfterCultureSwitch()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                AppStrings.ApplyCulture("en");
                vm.CurrentSection = AppSections.Statistics;
                vm.StatisticsPanel.NotifyLocalization();

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                var statisticsPanel = HeadlessViewTestHelper
                    .FindDescendants<StatisticsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(statisticsPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "Ticket statistics"));
                Assert.True(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "By status"));
                Assert.True(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "Assignment"));
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_StatisticsPage_UsesStatisticsPanelBindings()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Statistics;
                vm.StatisticsPanel.ApplyFromTickets(
                    new[]
                    {
                        new Ticket { Id = 1, Status = TicketStatuses.Nowe, Priority = TicketPriorities.Low },
                        new Ticket { Id = 2, Status = TicketStatuses.WTrakcie, Priority = TicketPriorities.Medium, AssignedItId = 5 }
                    },
                    totalInSystem: 2,
                    fromCurrentPageOnly: false);

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var statisticsPanel = HeadlessViewTestHelper
                    .FindDescendants<StatisticsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(statisticsPanel);
                Assert.NotNull(vm.StatisticsPanel);

                Assert.True(HeadlessViewTestHelper.ContainsText(statisticsPanel!, vm.StatisticsPanel.StatsScopeMessage));
                Assert.True(HeadlessViewTestHelper.ContainsText(statisticsPanel!, "2"));

                var loadAllButton = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(statisticsPanel!)
                    .FirstOrDefault(b => b.Content as string == vm.StatisticsPanel.LblStatsLoadAll);

                Assert.NotNull(loadAllButton);
                Assert.NotNull(loadAllButton!.Command);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_RequestAccountPage_ContainsRegistrationForm()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("user");

            try
            {
                vm.CurrentSection = AppSections.RequestAccount;

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var requestPanel = HeadlessViewTestHelper
                    .FindDescendants<RequestAccountPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(requestPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblRequestAccountTitle));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblRequestAccountSubtitle));
                Assert.True(HeadlessViewTestHelper.FindDescendants<RequestAccountFormView>(requestPanel!).Any());
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.TextBox>(requestPanel!) >= 5);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_RequestAccountPage_UsesRequestAccountBindings()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("user");

            try
            {
                vm.CurrentSection = AppSections.RequestAccount;
                vm.RequestName = "Jan Headless";
                vm.RequestLogin = "jan.headless";
                vm.RequestAccountStatusMessage = "Status headless request";

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                var requestPanel = HeadlessViewTestHelper
                    .FindDescendants<RequestAccountPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(requestPanel);

                var nameBox = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.TextBox>(requestPanel!)
                    .FirstOrDefault(tb => tb.Text == "Jan Headless");

                Assert.NotNull(nameBox);

                var loginBox = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.TextBox>(requestPanel!)
                    .FirstOrDefault(tb => tb.Text == "jan.headless");

                Assert.NotNull(loginBox);

                Assert.True(HeadlessViewTestHelper.ContainsText(requestPanel!, vm.RequestAccountStatusMessage));

                var submitButton = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(requestPanel!)
                    .FirstOrDefault(b => b.Content as string == vm.LblRequestAccountSubmit);

                Assert.NotNull(submitButton);
                Assert.NotNull(submitButton!.Command);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }
}
