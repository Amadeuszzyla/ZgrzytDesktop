using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;
using ZgrzytDesktop.Views.DashboardParts;

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
            var vm = ViewModelTestFactory.CreateLoginViewModel();
            var view = HeadlessViewTestHelper.CreateLoginView(vm);

            var window = HeadlessViewTestHelper.ShowInWindow(view, 500, 700);

            Assert.NotNull(window.Content);
            Assert.Contains(view, HeadlessViewTestHelper.EnumerateDescendants(window));
            Assert.True(HeadlessViewTestHelper.ContainsText(view, vm.LblLoginTitle));
            Assert.True(HeadlessViewTestHelper.ContainsText(view, vm.LblAppSubtitle));
            Assert.True(HeadlessViewTestHelper.ContainsText(view, "ZGRZYT"));
            Assert.NotNull(HeadlessViewTestHelper.FindDescendants<Avalonia.Controls.Image>(view).FirstOrDefault());
            Assert.NotNull(
                HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(view)
                    .FirstOrDefault(b => b.Command == vm.LoginCommand));
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
    public void DashboardView_SettingsPage_UsesLightThemeAndSaveDoesNotCrash()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("user");

            try
            {
                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                vm.CurrentSection = AppSections.Settings;

                SettingsService.ApplyThemeMode("Dark");
                Assert.Equal(Avalonia.Styling.ThemeVariant.Light, Avalonia.Application.Current!.ActualThemeVariant);
                Assert.Equal(SettingsPanelViewModel.LightThemeMode, vm.SelectedThemeMode);

                vm.SaveSettingsCommand.ExecuteAsync(null).GetAwaiter().GetResult();
                Assert.Equal("pl", vm.SelectedUiCulture);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_SettingsPage_ContainsLanguageSaveAndLocalAuditSections()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");

            try
            {
                vm.CurrentSection = AppSections.Settings;

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var settingsPanel = HeadlessViewTestHelper
                    .FindDescendants<SettingsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(settingsPanel);
                Assert.False(HeadlessViewTestHelper.ContainsText(window, AppStrings.Get("Settings_Theme")));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblSettingsLanguage));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblSettingsSave));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblSettingsRefreshSession));
                Assert.Equal(1, HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(settingsPanel!));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Lokalny audyt aplikacji"));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Odśwież audyt"));
                Assert.False(HeadlessViewTestHelper.ContainsText(window, "Wyczyść audyt"));
                Assert.Equal(0, HeadlessViewTestHelper.CountDescendantsByTypeName(settingsPanel!, "DataGrid"));
                Assert.True(HeadlessViewTestHelper.CountDescendantsByTypeName(settingsPanel!, "ItemsControl") >= 1);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

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
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblTicketsFiltersTitle));
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(window) >= 2);
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
                var window = HeadlessViewTestHelper.ShowInWindow(view);

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

    [Fact]
    public void DashboardView_DetailsPage_ContainsMessagesAndLocalAuditHistory()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Details;
                vm.TicketDetailsPanel.TicketDetails = new Ticket
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

                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblDetailsBackToList));
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

    [Fact]
    public void DashboardView_DetailsPage_UsesTicketDetailsPanelBindings()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Details;
                vm.TicketDetailsPanel.TicketDetails = new Ticket
                {
                    Id = 7,
                    Title = "Details binding smoke",
                    Description = "Opis headless",
                    Status = TicketStatuses.Nowe,
                    Priority = TicketPriorities.Low,
                    UserId = 1,
                    User = new User { Id = 1, Name = "User", Login = "user" }
                };
                vm.TicketDetailsPanel.DetailsStatusMessage = "Status details headless";
                vm.TicketDetailsPanel.Messages.Add(new Models.Message
                {
                    Id = 1,
                    Content = "Wiadomość testowa",
                    TicketId = 7,
                    Sender = new User { Id = 1, Name = "User", Login = "user" }
                });

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var detailsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketDetailsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(detailsPanel);

                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Details binding smoke"));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.LblDetailsBackToList));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.TicketDetailsPanel.DetailsStatusMessage));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Wiadomość testowa"));

                var sendButton = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(detailsPanel!)
                    .FirstOrDefault(b => b.Content as string == "Wyślij");

                Assert.NotNull(sendButton);
                Assert.NotNull(sendButton!.Command);

                var updateButton = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(detailsPanel!)
                    .FirstOrDefault(b => b.Content as string == "Zapisz zmiany");

                Assert.NotNull(updateButton);
                Assert.NotNull(updateButton!.Command);
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
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ProgressBar>(statisticsPanel!) >= 8);
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
    public void DashboardView_AdminPage_ContainsUsersListAndActions()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");

            try
            {
                vm.CurrentSection = AppSections.Admin;
                vm.AdminPanel.PrepareAdminPage(isAdminRole: true);

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var adminPanel = HeadlessViewTestHelper
                    .FindDescendants<AdminPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(adminPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblAdminUsersTitle));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblAdminUsersSubtitle));
                Assert.Equal(0, HeadlessViewTestHelper.CountDescendantsByTypeName(adminPanel!, "DataGrid"));
                Assert.True(HeadlessViewTestHelper.CountDescendantsByTypeName(adminPanel!, "ListBox") >= 1);
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(adminPanel!) >= 1);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_AdminPage_NewAccountTab_ContainsRegistrationForm()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Admin;
                vm.AdminPanel.PrepareAdminPage(isAdminRole: false);

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var adminPanel = HeadlessViewTestHelper
                    .FindDescendants<AdminPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(adminPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblRegisterUserTitle));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblRegisterUserSubtitle));
                Assert.True(HeadlessViewTestHelper.FindDescendants<RegisterUserFormView>(adminPanel!).Any());
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.TextBox>(adminPanel!) >= 5);
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(adminPanel!) >= 1);

                var submitButton = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(adminPanel!)
                    .FirstOrDefault(b => b.Content as string == vm.LblRegisterUserSubmit);

                Assert.NotNull(submitButton);
                Assert.Equal(vm.RegisterUserCommand, submitButton.Command);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_AdminPage_UsesAdminPanelBindings()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");

            try
            {
                vm.CurrentSection = AppSections.Admin;
                vm.AdminPanel.PrepareAdminPage(isAdminRole: true);
                vm.AdminPanel.AdminUnbanPassword = "headless-unban";
                vm.AdminPanel.AdminUsers.Add(new User
                {
                    Id = 99,
                    Login = "headless-admin",
                    Email = "headless@test.pl",
                    Role = AppRoles.User,
                    Active = true,
                    Ban = false
                });

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var adminPanel = HeadlessViewTestHelper
                    .FindDescendants<AdminPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(adminPanel);

                var unbanPasswordBox = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.TextBox>(adminPanel!)
                    .FirstOrDefault(tb => tb.Text == "headless-unban");

                Assert.NotNull(unbanPasswordBox);
                Assert.True(HeadlessViewTestHelper.ContainsText(adminPanel!, "headless-admin"));

                var refreshButton = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(adminPanel!)
                    .FirstOrDefault(b => b.Content as string == vm.LblAdminRefreshList);

                Assert.NotNull(refreshButton);
                Assert.NotNull(refreshButton!.Command);

                var banButton = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(adminPanel!)
                    .FirstOrDefault(b => b.Content as string == vm.LblAdminBan);

                Assert.NotNull(banButton);
                Assert.Equal(0, HeadlessViewTestHelper.CountDescendantsByTypeName(adminPanel!, "DataGrid"));
                Assert.True(HeadlessViewTestHelper.CountDescendantsByTypeName(adminPanel!, "ListBox") >= 1);
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
                var window = HeadlessViewTestHelper.ShowInWindow(view);

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
