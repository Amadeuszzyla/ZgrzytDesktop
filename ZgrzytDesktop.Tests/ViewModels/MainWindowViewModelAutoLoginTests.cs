using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;
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

    [Fact]
    public async Task RunStartup_Timeout_EndsAutoLoginAndLogsWarning()
    {
        var diagnosticLog = new RecordingLocalDiagnosticLogService();
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 5, Login = "it-user", Role = "it", Active = true },
            GetCurrentUserDelayMs = 5_000
        };
        var window = new MainWindowViewModel(
            ViewModelTestFactory.CreateMainWindowDependencies(
                auth,
                autoLoginTimeout: TimeSpan.FromMilliseconds(100),
                diagnosticLog: diagnosticLog),
            runStartup: false);

        await window.RunStartupAsync();

        Assert.False(window.IsAutoLoginInProgress);
        Assert.True(string.IsNullOrEmpty(window.AutoLoginStatusMessage));
        Assert.IsType<LoginViewModel>(window.CurrentViewModel);
        Assert.Contains(
            diagnosticLog.Entries,
            e => e.Level == DiagnosticLogLevel.Warning &&
                 e.Message.Contains("Auto-login timed out", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunStartup_Timeout_EnablesManualLoginWithMessage()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 6, Login = "it-user", Role = "it", Active = true },
            GetCurrentUserDelayMs = 5_000
        };
        var window = new MainWindowViewModel(
            ViewModelTestFactory.CreateMainWindowDependencies(
                auth,
                autoLoginTimeout: TimeSpan.FromMilliseconds(100)),
            runStartup: false);

        await window.RunStartupAsync();

        var login = Assert.IsType<LoginViewModel>(window.CurrentViewModel);
        Assert.True(window.IsManualLoginAllowed);
        Assert.Equal(AppStrings.Get("Login_AutoLogin_Timeout"), login.ErrorMessage);
    }

    [Fact]
    public async Task RunStartup_LateResponseAfterTimeout_DoesNotSwitchToDashboard()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 7, Login = "it-user", Role = "it", Active = true },
            GetCurrentUserDelayMs = 2_000
        };
        var window = new MainWindowViewModel(
            ViewModelTestFactory.CreateMainWindowDependencies(
                auth,
                autoLoginTimeout: TimeSpan.FromMilliseconds(100)),
            runStartup: false);

        await window.RunStartupAsync();

        Assert.IsType<LoginViewModel>(window.CurrentViewModel);

        await Task.Delay(2_500);

        Assert.IsType<LoginViewModel>(window.CurrentViewModel);
    }

    [Fact]
    public async Task RunStartup_TimeoutMessage_IsLocalizedInPolishAndEnglish()
    {
        var auth = new FakeAuthService
        {
            CurrentUserResult = new User { Id = 8, Login = "it-user", Role = "it", Active = true },
            GetCurrentUserDelayMs = 5_000
        };

        using (new TestCultureScope("pl"))
        {
            var windowPl = new MainWindowViewModel(
                ViewModelTestFactory.CreateMainWindowDependencies(
                    auth,
                    autoLoginTimeout: TimeSpan.FromMilliseconds(100)),
                runStartup: false);

            await windowPl.RunStartupAsync();

            var loginPl = Assert.IsType<LoginViewModel>(windowPl.CurrentViewModel);
            Assert.Equal(
                "Serwer odpowiada zbyt długo. Spróbuj zalogować się ręcznie lub ponów próbę za chwilę.",
                loginPl.ErrorMessage);
        }

        using (new TestCultureScope("en"))
        {
            var windowEn = new MainWindowViewModel(
                ViewModelTestFactory.CreateMainWindowDependencies(
                    auth,
                    autoLoginTimeout: TimeSpan.FromMilliseconds(100)),
                runStartup: false);

            await windowEn.RunStartupAsync();

            var loginEn = Assert.IsType<LoginViewModel>(windowEn.CurrentViewModel);
            Assert.Equal(
                "The server is taking too long to respond. Try signing in manually or retry in a moment.",
                loginEn.ErrorMessage);
        }
    }
}
