using ZgrzytDesktop.Models;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.ViewModels;

public class MainWindowViewModelTests
{
    public MainWindowViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void Constructor_WithoutSession_ShowsLoginViewModel()
    {
        var auth = new FakeAuthService { CurrentUserResult = null };
        var deps = ViewModelTestFactory.CreateMainWindowDependencies(auth);

        var window = new MainWindowViewModel(deps, runStartup: false);

        Assert.IsType<LoginViewModel>(window.CurrentViewModel);
    }

    [Fact]
    public async Task RunStartup_WithAuthenticatedUser_ShowsDashboardViewModel()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User
            {
                Id = 10,
                Login = "it-user",
                Name = "IT",
                Role = "it",
                Active = true
            }
        };
        var deps = ViewModelTestFactory.CreateMainWindowDependencies(auth);
        var window = new MainWindowViewModel(deps, runStartup: false);

        await window.RunStartupAsync();

        Assert.IsType<DashboardViewModel>(window.CurrentViewModel);
        var dashboard = (DashboardViewModel)window.CurrentViewModel;
        Assert.Equal("it", dashboard.CurrentUser.Role);
    }

    [Fact]
    public async Task Logout_ReturnsToLoginViewModel()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 1, Login = "admin", Role = "admin", Active = true }
        };
        var deps = ViewModelTestFactory.CreateMainWindowDependencies(auth);
        var window = new MainWindowViewModel(deps, runStartup: false);

        await window.RunStartupAsync();
        Assert.IsType<DashboardViewModel>(window.CurrentViewModel);

        await window.LogoutForTestsAsync();

        Assert.IsType<LoginViewModel>(window.CurrentViewModel);
        Assert.Equal(1, auth.LogoutCallCount);
    }

    [Fact]
    public async Task LoginSuccess_FromLoginViewModel_StaffRole_ShowsDashboard()
    {
        var auth = new FakeAuthService
        {
            LoginResult = new User { Id = 3, Login = "it-user", Role = "it", Active = true }
        };
        var deps = ViewModelTestFactory.CreateMainWindowDependencies(auth);
        var window = new MainWindowViewModel(deps, runStartup: false);
        var login = (LoginViewModel)window.CurrentViewModel;

        login.Login = "it-user";
        login.Password = "pass";
        await login.LoginCommand.ExecuteAsync(null);

        for (var attempt = 0; attempt < 50 && window.CurrentViewModel is LoginViewModel; attempt++)
            await Task.Delay(20);

        Assert.IsType<DashboardViewModel>(window.CurrentViewModel);
    }
}
