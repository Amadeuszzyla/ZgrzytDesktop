using System.Net;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Storage;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Security;

public class DesktopSecurityHardeningTests
{
    public DesktopSecurityHardeningTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void AutoLogout_ExpiresSessionAfterInactivity()
    {
        var now = new DateTime(2026, 5, 23, 12, 0, 0, DateTimeKind.Utc);
        var monitor = new SessionInactivityMonitor(() => now);
        var expired = false;

        monitor.Configure(true, 30);
        monitor.Start(() =>
        {
            expired = true;
            return Task.CompletedTask;
        });

        now = now.AddMinutes(31);
        monitor.CheckExpirationForTests();

        Assert.True(expired);
    }

    [Fact]
    public void AutoLogout_DoesNotExpireBeforeTimeout()
    {
        var now = new DateTime(2026, 5, 23, 12, 0, 0, DateTimeKind.Utc);
        var monitor = new SessionInactivityMonitor(() => now);
        var expired = false;

        monitor.Configure(true, 30);
        monitor.RecordActivity();
        monitor.Start(() =>
        {
            expired = true;
            return Task.CompletedTask;
        });

        now = now.AddMinutes(29);
        monitor.CheckExpirationForTests();

        Assert.False(expired);
        Assert.True(monitor.IsRunningForTests());
    }

    [Fact]
    public void ApiUrlValidation_RejectsHttpRemote()
    {
        var error = ApiUrlValidator.Validate("http://zgrzyt-api.onrender.com/api/", allowLocalHttpInDevMode: true);
        Assert.Equal(AppStrings.Get("Security_ApiUrlMustBeHttps"), error);
    }

    [Fact]
    public void ApiUrlValidation_AllowsHttps()
    {
        Assert.Null(ApiUrlValidator.Validate("https://zgrzyt-api.onrender.com/api/"));
    }

    [Fact]
    public void ApiUrlValidation_AllowsLocalhostHttpOnlyInDevMode()
    {
        Assert.Null(ApiUrlValidator.Validate("http://127.0.0.1:9000/api/", allowLocalHttpInDevMode: true));
        Assert.NotNull(ApiUrlValidator.Validate("http://127.0.0.1:9000/api/", allowLocalHttpInDevMode: false));
    }

    [Fact]
    public async Task Logout_ClearsTokenAndCache()
    {
        var (api, _, tempDir) = TestApiFactory.CreateApi();
        var storage = new TokenStorage(tempDir);
        await storage.SaveTokenAsync("eyJhbGciOiJIUzI1NiJ9.test.signature");

        var auth = new AuthService(api, storage);
        var ticketCache = new LocalTicketCacheService(tempDir);
        var userCache = new LocalUserCacheService(tempDir);
        await ticketCache.SaveTicketsAsync([new Ticket { Id = 1, Title = "Cached" }]);
        await userCache.SaveUserAsync(new User { Id = 1, Login = "it1", Role = AppRoles.It });

        var vm = new MainWindowViewModel(new MainWindowViewModel.MainWindowDependencies(
            auth,
            new FakeTicketService(),
            new FakeSettingsService(),
            ticketCache,
            userCache,
            new FakeAuditLogService(),
            new FakeUserAdminService(),
            api,
            NullLocalDiagnosticLogService.Instance));

        await vm.LogoutForTestsAsync();

        Assert.Null(await storage.GetTokenAsync());
        Assert.Empty(await ticketCache.LoadTicketsAsync());
        Assert.Null(await userCache.LoadUserAsync());

        TestApiFactory.Cleanup(tempDir);
    }

    [Fact]
    public void SensitiveDataMasker_MasksPasswordTokenEmail()
    {
        const string input = "\"password\": \"secret\", \"token\": \"abc\", user@test.com Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.a.b";
        var masked = SensitiveDataMasker.Mask(input);

        Assert.DoesNotContain("secret", masked, StringComparison.Ordinal);
        Assert.DoesNotContain("user@test.com", masked, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJ", masked, StringComparison.Ordinal);
        Assert.Contains("[MASKED]", masked, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SensitiveDataMasker_DoesNotMaskBenignText()
    {
        const string input = "Ticket saved successfully.";
        var masked = SensitiveDataMasker.Mask(input);
        Assert.Equal(input, masked);
    }

    [Fact]
    public async Task RiskyAction_Cancel_DoesNotCallApi()
    {
        var confirmation = new FakeUserConfirmationService { NextResult = false };
        ConfirmationServiceHolder.Instance = confirmation;

        var userAdmin = new FakeUserAdminService();
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin", userAdmin: userAdmin);
        vm.AdminPanel.AdminUsers.Add(new User { Id = 2, Login = "user2", Active = true, Ban = false, Role = AppRoles.User });
        vm.AdminPanel.SelectedAdminUser = vm.AdminPanel.AdminUsers[0];

        await vm.AdminPanel.BanAdminUserCommand.ExecuteAsync(null);

        Assert.Equal(1, confirmation.ConfirmCallCount);
        Assert.Equal(0, userAdmin.BanCallCount);
        TestApiFactory.Cleanup(tempDir);
    }

    [Fact]
    public async Task RiskyAction_Confirm_CallsApi()
    {
        var confirmation = new FakeUserConfirmationService { NextResult = true };
        ConfirmationServiceHolder.Instance = confirmation;

        var userAdmin = new FakeUserAdminService();
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin", userAdmin: userAdmin);
        vm.AdminPanel.AdminUsers.Add(new User { Id = 2, Login = "user2", Active = true, Ban = false, Role = AppRoles.User });
        vm.AdminPanel.SelectedAdminUser = vm.AdminPanel.AdminUsers[0];

        await vm.AdminPanel.BanAdminUserCommand.ExecuteAsync(null);

        Assert.Equal(1, confirmation.ConfirmCallCount);
        Assert.Equal(1, userAdmin.BanCallCount);
        TestApiFactory.Cleanup(tempDir);
    }

    [Fact]
    public void RegisterValidation_RejectsInvalidEmail()
    {
        var error = RegisterUserValidator.Validate(
            "Name",
            "login1",
            "not-an-email",
            "password",
            "password",
            RegisterUserRoleOption.All[0]);

        Assert.Equal(AppStrings.Get("Validation_InvalidEmail"), error);
    }

    [Fact]
    public void RegisterValidation_RejectsPasswordMismatch()
    {
        var error = RegisterUserValidator.Validate(
            "Name",
            "login1",
            "user@example.com",
            "password",
            "different",
            RegisterUserRoleOption.All[0]);

        Assert.Equal(AppStrings.Get("RequestAccount_ValidationPasswordMismatch"), error);
    }

    [Fact]
    public void MessageValidation_RejectsEmptyOrTooLongBody()
    {
        Assert.Equal(AppStrings.Get("Details_EmptyMessage"), TicketMessageValidator.ValidateBody("   "));
        Assert.Equal(
            AppStrings.Get("Validation_MessageBodyTooLong"),
            TicketMessageValidator.ValidateBody(new string('x', ValidationLimits.MessageBodyMaxLength + 1)));
    }

    [Fact]
    public void RoleBasedUI_IT_DoesNotShowUserManagement()
    {
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");
        Assert.False(vm.IsAdminUsersPanelVisible);
        Assert.False(vm.AdminPanel.IsAdminUsersManagementVisible);
        TestApiFactory.Cleanup(tempDir);
    }

    [Fact]
    public void RoleBasedUI_Admin_ShowsUserManagement()
    {
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");
        vm.AdminPanel.AdminTab = AdminTabs.Users;
        Assert.True(vm.IsAdminUsersPanelVisible);
        Assert.True(vm.AdminPanel.IsAdminUsersManagementVisible);
        TestApiFactory.Cleanup(tempDir);
    }

    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));
}
