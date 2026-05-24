using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Storage;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.ViewModels;

public class MainWindowDesktopAccessTests
{
    public MainWindowDesktopAccessTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task Login_AdminRole_ShowsDashboard()
    {
        var auth = new FakeAuthService
        {
            LoginResult = new User { Id = 1, Login = "admin", Role = "admin", Active = true }
        };
        var window = await LoginAndWaitAsync(auth);

        Assert.IsType<DashboardViewModel>(window.CurrentViewModel);
        Assert.Equal(0, auth.LogoutCallCount);
    }

    [Fact]
    public async Task Login_ItRole_ShowsDashboard()
    {
        var auth = new FakeAuthService
        {
            LoginResult = new User { Id = 2, Login = "it-user", Role = "it", Active = true }
        };
        var window = await LoginAndWaitAsync(auth);

        Assert.IsType<DashboardViewModel>(window.CurrentViewModel);
        Assert.Equal(0, auth.LogoutCallCount);
    }

    [Fact]
    public async Task User_Login_IsDeniedBeforeDashboard()
    {
        var auth = new FakeAuthService
        {
            LoginResult = new User { Id = 3, Login = "user1", Role = "user", Active = true }
        };
        var (window, userCache, _) = CreateWindow(auth);

        await ExecuteLoginAsync(window);

        Assert.IsType<LoginViewModel>(window.CurrentViewModel);
        var login = (LoginViewModel)window.CurrentViewModel;
        Assert.Equal(AppStrings.Get("Login_DesktopAccessDenied"), login.ErrorMessage);
        Assert.Equal(1, auth.LogoutCallCount);
        Assert.Null(await userCache.LoadUserAsync());
    }

    [Fact]
    public async Task AutoLogin_UserRole_DeniesAccess_ClearsSession_StaysOnLogin()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 4, Login = "user2", Role = "user", Active = true }
        };
        var (window, userCache, _) = CreateWindow(auth);

        await window.RunStartupAsync();

        Assert.IsType<LoginViewModel>(window.CurrentViewModel);
        var login = (LoginViewModel)window.CurrentViewModel;
        Assert.Equal(AppStrings.Get("Login_DesktopAccessDenied"), login.ErrorMessage);
        Assert.Equal(1, auth.LogoutCallCount);
        Assert.Null(await userCache.LoadUserAsync());
    }

    [Fact]
    public void DesktopAccessDeniedMessage_CultureEn_IsEnglish()
    {
        AppStrings.ApplyCulture("en");
        Assert.Equal(
            "The desktop application is available only for IT staff and administrators.",
            AppStrings.Get("Login_DesktopAccessDenied"));
    }

    [Fact]
    public void DesktopAccessDeniedMessage_CulturePl_IsPolish()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal(
            "Aplikacja desktopowa jest dostępna tylko dla pracowników IT i administratorów.",
            AppStrings.Get("Login_DesktopAccessDenied"));
    }

    private static async Task<MainWindowViewModel> LoginAndWaitAsync(FakeAuthService auth)
    {
        var (window, _, _) = CreateWindow(auth);
        await ExecuteLoginAsync(window);
        return window;
    }

    private static async Task ExecuteLoginAsync(MainWindowViewModel window)
    {
        var login = (LoginViewModel)window.CurrentViewModel;
        login.Login = "tester";
        login.Password = "secret";
        await login.LoginCommand.ExecuteAsync(null);
        await WaitForLoginResultAsync(window);
    }

    private static async Task WaitForLoginResultAsync(MainWindowViewModel window)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (window.CurrentViewModel is DashboardViewModel)
                return;

            if (window.CurrentViewModel is LoginViewModel login && login.HasError && !login.IsLoading)
                return;

            await Task.Delay(20);
        }
    }

    private static (MainWindowViewModel Window, LocalUserCacheService UserCache, string TempDir) CreateWindow(
        FakeAuthService auth)
    {
        var (api, _, tempDir) = TestApiFactory.CreateApi();
        var userCache = new LocalUserCacheService(tempDir);
        var ticketCache = new LocalTicketCacheService(tempDir);
        var deps = new MainWindowViewModel.MainWindowDependencies(
            auth,
            new FakeTicketService(),
            new FakeSettingsService(),
            ticketCache,
            userCache,
            new FakeAuditLogService(),
            new FakeUserAdminService(),
            api);

        var window = new MainWindowViewModel(deps, runStartup: false);
        return (window, userCache, tempDir);
    }
}
