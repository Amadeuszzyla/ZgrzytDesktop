using System;
using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.Views.DashboardParts;

namespace ZgrzytDesktop.Headless.Tests.Views;

public class AdminPanelHeadlessTests : HeadlessViewTestsBase
{
    [Fact]
    public void AdminPanel_StillShowsUsersAndNewAccount_ForAdmin()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");

            try
            {
                vm.CurrentSection = AppSections.Admin;
                vm.AdminPanel.PrepareAdminPage(isAdminRole: true);

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                var adminPanel = HeadlessViewTestHelper
                    .FindDescendants<AdminPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(adminPanel);
                Assert.True(HeadlessViewTestHelper.ContainsText(adminPanel!, vm.LblAdminTabUsers));
                Assert.True(HeadlessViewTestHelper.ContainsText(adminPanel!, vm.LblAdminTabNewAccount));
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void AdminPanel_NewAccountStillWorks()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                vm.CurrentSection = AppSections.Admin;
                vm.AdminPanel.PrepareAdminPage(isAdminRole: false);

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                var adminPanel = HeadlessViewTestHelper
                    .FindDescendants<AdminPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(adminPanel);
                Assert.True(HeadlessViewTestHelper.FindDescendants<RegisterUserFormView>(adminPanel!).Any());
                Assert.Equal(vm.RegisterUserCommand,
                    HeadlessViewTestHelper
                        .FindDescendants<Avalonia.Controls.Button>(adminPanel!)
                        .FirstOrDefault(b => b.Content as string == vm.LblRegisterUserSubmit)
                        ?.Command);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_AdminPage_AdminRole_ShowsUsersListAndFilters()
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

                var refreshButton = HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(adminPanel!)
                    .FirstOrDefault(b => b.Content as string == vm.LblAdminRefreshList);

                Assert.NotNull(refreshButton);
                Assert.Equal(1, HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(adminPanel!));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, AppStrings.Get("Admin_Filter_All")));
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_AdminPage_EmptyUsers_ShowsEmptyStateInListCenterOnly()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");

            try
            {
                vm.CurrentSection = AppSections.Admin;
                vm.AdminPanel.PrepareAdminPage(isAdminRole: true);
                vm.AdminPanel.AdminUsers.Clear();
                vm.AdminPanel.NotifyLocalization();

                var panelView = new AdminPanelView { DataContext = vm };
                HeadlessViewTestHelper.ShowInWindow(panelView);

                Assert.True(vm.HasNoAdminUsers);
                Assert.True(HeadlessViewTestHelper.ContainsText(panelView, vm.LblAdminNoUsersFound));
                Assert.DoesNotContain(
                    vm.LblAdminNoUsersFound,
                    vm.AdminStatusMessage,
                    StringComparison.Ordinal);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_AdminPage_ItRole_ShowsRegisterForm_HidesUserListAndFilters()
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
                Assert.False(vm.IsAdminUsersPanelVisible);
                Assert.True(vm.IsAdminNewAccountPanelVisible);
                Assert.False(vm.IsAdminRole);
                Assert.Equal(AdminTabs.NewAccount, vm.AdminTab);

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
                HeadlessViewTestHelper.ShowInWindow(view);

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
}
