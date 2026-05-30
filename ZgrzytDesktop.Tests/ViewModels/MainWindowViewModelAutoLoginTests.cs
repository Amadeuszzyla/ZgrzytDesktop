using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.ViewModels;

public class MainWindowViewModelAutoLoginTests
{
    public MainWindowViewModelAutoLoginTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task RunStartup_SetsLoadingStatusWhileWaitingForApi()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 1, Login = "it-user", Role = "it", Active = true },
            GetCurrentUserDelayMs = 2_000
        };
        var deps = ViewModelTestFactory.CreateMainWindowDependencies(auth);
        var window = new MainWindowViewModel(deps, runStartup: false);

        var startupTask = window.RunStartupAsync();
        await Task.Delay(50);

        Assert.True(window.IsAutoLoginInProgress);
        Assert.Equal(AppStrings.Get("Login_AutoLogin_CheckingSession"), window.AutoLoginStatusMessage);
        Assert.False(window.IsManualLoginAllowed);

        await startupTask;

        Assert.False(window.IsAutoLoginInProgress);
        Assert.True(string.IsNullOrEmpty(window.AutoLoginStatusMessage));
        Assert.IsType<DashboardViewModel>(window.CurrentViewModel);
    }

    [Fact]
    public async Task RunStartup_ClearsStatusAfterSuccessfulLogin()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 2, Login = "admin", Role = "admin", Active = true }
        };
        var window = new MainWindowViewModel(
            ViewModelTestFactory.CreateMainWindowDependencies(auth),
            runStartup: false);

        await window.RunStartupAsync();

        Assert.False(window.IsAutoLoginInProgress);
        Assert.True(string.IsNullOrEmpty(window.AutoLoginStatusMessage));
        Assert.IsType<DashboardViewModel>(window.CurrentViewModel);
    }

    [Fact]
    public async Task RunStartup_ClearsStatusAndAllowsManualLoginAfterFailure()
    {
        var auth = new FakeAuthService { CurrentUserResult = null };
        var window = new MainWindowViewModel(
            ViewModelTestFactory.CreateMainWindowDependencies(auth),
            runStartup: false);

        await window.RunStartupAsync();

        Assert.False(window.IsAutoLoginInProgress);
        Assert.True(string.IsNullOrEmpty(window.AutoLoginStatusMessage));
        Assert.IsType<LoginViewModel>(window.CurrentViewModel);
        Assert.True(window.IsManualLoginAllowed);
    }

    [Fact]
    public async Task RunStartup_ShowsColdStartHintAfterDelay()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 3, Login = "it-user", Role = "it", Active = true },
            GetCurrentUserDelayMs = 2_000
        };
        var deps = ViewModelTestFactory.CreateMainWindowDependencies(
            auth,
            autoLoginColdStartHintDelay: TimeSpan.FromMilliseconds(80));
        var window = new MainWindowViewModel(deps, runStartup: false);

        var startupTask = window.RunStartupAsync();
        await Task.Delay(150);

        Assert.True(window.IsAutoLoginInProgress);
        Assert.Equal(AppStrings.Get("Login_AutoLogin_ColdStartHint"), window.AutoLoginStatusMessage);

        await startupTask;
    }

    [Fact]
    public async Task CancelAutoLogin_ReturnsToManualLoginWithoutDashboard()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 4, Login = "it-user", Role = "it", Active = true },
            GetCurrentUserDelayMs = 10_000
        };
        var window = new MainWindowViewModel(
            ViewModelTestFactory.CreateMainWindowDependencies(auth),
            runStartup: false);

        var startupTask = window.RunStartupAsync();
        await Task.Delay(50);

        Assert.True(window.CanCancelAutoLogin);
        window.CancelAutoLoginCommand.Execute(null);

        Assert.False(window.IsAutoLoginInProgress);
        Assert.True(string.IsNullOrEmpty(window.AutoLoginStatusMessage));
        Assert.True(window.IsManualLoginAllowed);
        Assert.IsType<LoginViewModel>(window.CurrentViewModel);

        await startupTask;
        Assert.IsType<LoginViewModel>(window.CurrentViewModel);
    }
}
