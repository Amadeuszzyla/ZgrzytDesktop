using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.Infrastructure;

public static class ViewModelTestFactory
{
    public static LoginViewModel CreateLoginViewModel(
        FakeAuthService? auth = null,
        FakeAuditLogService? audit = null,
        Action<User>? onSuccess = null)
    {
        auth ??= new FakeAuthService();
        audit ??= new FakeAuditLogService();
        return new LoginViewModel(
            auth,
            audit,
            user => onSuccess?.Invoke(user));
    }

    internal static (DashboardViewModel Vm, FakeTicketService Tickets, FakeSettingsService Settings, string TempDir)
        CreateDashboard(
            string role = "user",
            FakeAuthService? auth = null,
            FakeTicketService? tickets = null,
            FakeSettingsService? settings = null,
            FakeUserAdminService? userAdmin = null,
            DashboardViewModel.BootstrapOptions? bootstrap = null)
    {
        auth ??= new FakeAuthService();
        tickets ??= new FakeTicketService();
        settings ??= new FakeSettingsService();
        var audit = new FakeAuditLogService();
        userAdmin ??= new FakeUserAdminService();
        bootstrap ??= DashboardViewModel.BootstrapOptions.Testing;

        var (_, _, tempDir) = TestApiFactory.CreateApi();
        try
        {
            ILocalTicketCacheService ticketCache = new LocalTicketCacheService(tempDir);
            var user = new User
            {
                Id = 1,
                Login = "tester",
                Name = "Tester",
                Email = "tester@test.pl",
                Role = role,
                Active = true,
                Ban = false
            };

            var vm = new DashboardViewModel(
                user,
                auth,
                tickets,
                settings,
                ticketCache,
                audit,
                userAdmin,
                () => Task.CompletedTask,
                bootstrap);

            return (vm, tickets, settings, tempDir);
        }
        catch
        {
            TestApiFactory.Cleanup(tempDir);
            throw;
        }
    }

    internal static MainWindowViewModel.MainWindowDependencies CreateMainWindowDependencies(
        FakeAuthService? auth = null,
        FakeTicketService? tickets = null,
        FakeSettingsService? settings = null,
        FakeAuditLogService? audit = null,
        TimeSpan? autoLoginColdStartHintDelay = null)
    {
        auth ??= new FakeAuthService();
        tickets ??= new FakeTicketService();
        settings ??= new FakeSettingsService();
        audit ??= new FakeAuditLogService();

        var (api, _, tempDir) = TestApiFactory.CreateApi();
        ILocalTicketCacheService ticketCache = new LocalTicketCacheService(tempDir);
        ILocalUserCacheService userCache = new LocalUserCacheService(tempDir);

        return new MainWindowViewModel.MainWindowDependencies(
            auth,
            tickets,
            settings,
            ticketCache,
            userCache,
            audit,
            new FakeUserAdminService(),
            api,
            NullLocalDiagnosticLogService.Instance,
            autoLoginColdStartHintDelay);
    }
}
