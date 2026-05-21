using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Headless.Tests.Views;

[Collection(AvaloniaHeadlessCollection.Name)]
public class HeadlessViewsTests
{
    public HeadlessViewsTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void LoginView_CreatesWithoutException()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var view = HeadlessViewTestHelper.CreateLoginView(
                ViewModelTestFactory.CreateLoginViewModel());

            var window = HeadlessViewTestHelper.ShowInWindow(view, 500, 700);

            Assert.NotNull(window.Content);
            Assert.Contains(view, HeadlessViewTestHelper.EnumerateDescendants(window));
        });
    }

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
    public void DashboardView_ThemeChange_DoesNotCrash()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("user");

            try
            {
                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                vm.CurrentSection = AppSections.Settings;

                foreach (var theme in new[] { "Light", "Dark", "System" })
                {
                    vm.SelectedThemeMode = theme;
                    SettingsService.ApplyThemeMode(theme);
                }

                Assert.Contains(vm.SelectedThemeMode, vm.ThemeModes);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_SettingsPage_ContainsThemeAndLocalAuditSections()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");

            try
            {
                vm.CurrentSection = AppSections.Settings;

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblSettingsTheme));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Lokalny audyt aplikacji"));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Odśwież audyt"));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Wyczyść audyt"));
                Assert.True(HeadlessViewTestHelper.CountDescendantsByTypeName(window, "DataGrid") >= 1);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_TicketsPage_ContainsFiltersAndDataGrid()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Tickets;

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblTicketsFiltersTitle));
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(window) >= 2);
                Assert.True(HeadlessViewTestHelper.CountDescendantsByTypeName(window, "DataGrid") >= 1);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_DetailsPage_ContainsMessagesAndLocalAuditHistory()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Details;
                vm.TicketDetails = new Ticket
                {
                    Id = 42,
                    Title = "Headless ticket",
                    Description = "Opis testowy",
                    Status = TicketStatuses.WTrakcie,
                    Priority = TicketPriorities.Medium,
                    UserId = 1,
                    User = new User { Id = 1, Name = "Tester", Login = "tester" },
                    AssignedTo = new User { Id = 2, Name = "IT", Login = "it-user" }
                };

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Wiadomości"));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Historia lokalnych zmian"));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Wpisz wiadomość..."));
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ItemsControl>(window) >= 1);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }
}
