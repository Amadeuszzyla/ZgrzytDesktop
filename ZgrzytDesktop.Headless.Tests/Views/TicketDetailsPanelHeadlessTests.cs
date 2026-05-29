using System;
using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.Views.DashboardParts;

namespace ZgrzytDesktop.Headless.Tests.Views;

public class TicketDetailsPanelHeadlessTests : HeadlessViewTestsBase
{
    [Fact]
    public void DashboardView_DetailsPage_ShowsEnglishStatusBadgesAfterCultureSwitch()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Details;
                vm.TicketDetailsPanel.TicketDetails = new Ticket
                {
                    Id = 8,
                    Title = "Details EN badges",
                    Description = "Plain",
                    Status = TicketStatuses.WTrakcie,
                    Priority = TicketPriorities.Medium,
                    UserId = 1,
                    User = new User { Id = 1, Name = "User", Login = "user" }
                };
                AppStrings.ApplyCulture("en");
                vm.TicketDetailsPanel.NotifyLocalization();

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);
                var detailsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketDetailsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(detailsPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "In progress"));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Medium"));
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void TicketDetails_Admin_ShowsAssignToComboBox_PL()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var settings = new FakeSettingsService();
            settings.Settings.UiCulture = "pl";
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin", settings: settings);

            try
            {
                vm.CurrentSection = AppSections.Details;
                vm.TicketDetailsPanel.TicketDetails = new Ticket
                {
                    Id = 20,
                    Title = "Assign admin",
                    Status = TicketStatuses.Nowe,
                    Priority = TicketPriorities.Low,
                    UserId = 1
                };
                vm.TicketDetailsPanel.AssignableUsers.Clear();
                vm.TicketDetailsPanel.AssignableUsers.Add(
                    AssignableUserOption.FromUser(new User { Id = 2, Name = "IT", Login = "it1", Role = AppRoles.It, Active = true }));
                vm.TicketDetailsPanel.SelectedAssignedUser = vm.TicketDetailsPanel.AssignableUsers[0];
                vm.TicketDetailsPanel.NotifyLocalization();

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);
                var detailsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketDetailsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(detailsPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.LblTicketAssignTo));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.LblTicketSaveAssignment));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.LblTicketAssignToMe));
                Assert.True(HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(detailsPanel!) >= 3);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void TicketDetails_IT_ShowsAssignToMeOnly_EN()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            using var cultureScope = new TestCultureScope("en");
            var settings = new FakeSettingsService();
            settings.Settings.UiCulture = "en";
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it", settings: settings);

            try
            {
                vm.CurrentSection = AppSections.Details;
                vm.TicketDetailsPanel.TicketDetails = new Ticket
                {
                    Id = 21,
                    Title = "Assign IT",
                    Status = TicketStatuses.Nowe,
                    Priority = TicketPriorities.Low,
                    UserId = 1
                };
                HeadlessViewTestHelper.ApplyUiCulture("en");
                vm.TicketDetailsPanel.NotifyLocalization();

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);
                var detailsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketDetailsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(detailsPanel);
                HeadlessViewTestHelper.WaitForCondition(
                    () => HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.LblTicketAssignToMe),
                    timeoutMs: 5000);
                Assert.False(vm.IsAdminRole);
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.LblTicketAssignToMe));
                Assert.Empty(vm.TicketDetailsPanel.AssignableUsers);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void TicketDetails_DoesNotShowMessageEditDeleteButtons()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");

            try
            {
                vm.CurrentSection = AppSections.Details;
                vm.TicketDetailsPanel.TicketDetails = new Ticket { Id = 22, Title = "Msg", Status = TicketStatuses.Nowe };
                vm.TicketDetailsPanel.Messages.Add(new Models.Message
                {
                    Id = 1,
                    Content = "Hello",
                    Sender = new User { Id = 1, Name = "U", Login = "u" }
                });

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);
                var detailsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketDetailsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(detailsPanel);
                Assert.False(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Edytuj wiadomość"));
                Assert.False(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Usuń wiadomość"));
                Assert.False(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Edit message"));
                Assert.False(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Delete message"));
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
            using var cultureScope = new TestCultureScope("pl");
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Details;
                vm.TicketDetailsPanel.TicketDetails = new Ticket
                {
                    Id = 7,
                    Title = "Details binding smoke",
                    Description = "<p>Opis headless</p>",
                    Status = TicketStatuses.Zamkniete,
                    Priority = TicketPriorities.High,
                    UserId = 1,
                    User = new User { Id = 1, Name = "User", Login = "user" }
                };
                HeadlessViewTestHelper.ApplyUiCulture("pl");
                vm.TicketDetailsPanel.NotifyLocalization();
                vm.TicketDetailsPanel.DetailsStatusMessage = "Status details headless";
                vm.TicketDetailsPanel.Messages.Add(new Models.Message
                {
                    Id = 1,
                    Content = "<p>Wiadomość testowa</p>",
                    TicketId = 7,
                    CreatedAt = new DateTime(2026, 5, 20, 14, 30, 0),
                    Sender = new User { Id = 1, Name = "User", Login = "user" }
                });

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                var detailsPanel = HeadlessViewTestHelper
                    .FindDescendants<TicketDetailsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(detailsPanel);
                HeadlessViewTestHelper.WaitForCondition(
                    () => HeadlessViewTestHelper.ContainsText(detailsPanel!, "Details binding smoke"),
                    timeoutMs: 5000);

                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Details binding smoke"));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Opis headless"));
                Assert.False(HeadlessViewTestHelper.ContainsText(detailsPanel!, "<p>"));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.LblDetailsBackToList));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, vm.TicketDetailsPanel.DetailsStatusMessage));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Wiadomość testowa"));
                Assert.False(HeadlessViewTestHelper.ContainsText(detailsPanel!, "<p>Wiadomość"));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Zamknięte"));
                Assert.True(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Wysoki"));
                Assert.False(HeadlessViewTestHelper.ContainsText(detailsPanel!, "Closed"));
                Assert.False(HeadlessViewTestHelper.ContainsText(detailsPanel!, "High"));

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
}
