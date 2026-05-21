using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.ViewModels;

public class LoginViewModelTests
{
    public LoginViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task LoginAsync_Success_InvokesCallbackAndClearsError()
    {
        var auth = new FakeAuthService
        {
            LoginResult = new User { Id = 1, Login = "jan", Name = "Jan", Role = "user", Active = true }
        };
        var audit = new FakeAuditLogService();
        User? loggedIn = null;

        var vm = new LoginViewModel(auth, audit, user => loggedIn = user)
        {
            Login = "jan",
            Password = "secret123"
        };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.NotNull(loggedIn);
        Assert.Equal("jan", loggedIn!.Login);
        Assert.False(vm.HasError);
        Assert.True(vm.IsNotLoading);
        Assert.Single(audit.Entries);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ShowsErrorMessage()
    {
        var auth = new FakeAuthService
        {
            LoginException = new ApiException(HttpStatusCode.Unauthorized, "Unauthorized")
        };
        var vm = new LoginViewModel(auth, new FakeAuditLogService(), _ => { })
        {
            Login = "jan",
            Password = "bad"
        };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Login_InvalidCredentials"), vm.ErrorMessage);
        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task LoginAsync_NullUser_ShowsFailureMessage()
    {
        var auth = new FakeAuthService { LoginResult = null };
        var vm = new LoginViewModel(auth, new FakeAuditLogService(), _ => { })
        {
            Login = "jan",
            Password = "secret"
        };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Nie udało się zalogować. Sprawdź login i hasło.", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_DoesNotStorePasswordInAuditLog()
    {
        const string password = "SuperSecret99!";
        var auth = new FakeAuthService
        {
            LoginResult = new User { Id = 2, Login = "admin", Role = "admin", Active = true }
        };
        var audit = new FakeAuditLogService();
        var vm = new LoginViewModel(auth, audit, _ => { })
        {
            Login = "admin",
            Password = password
        };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(("admin", password), auth.LastLoginCredentials);
        Assert.DoesNotContain(password, audit.Entries[0].Description, StringComparison.Ordinal);
        Assert.DoesNotContain(password, audit.Entries[0].Action, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginAsync_EmptyLogin_ShowsValidationMessage()
    {
        var auth = new FakeAuthService();
        var vm = new LoginViewModel(auth, new FakeAuditLogService(), _ => { })
        {
            Login = "  ",
            Password = "x"
        };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Login_ProvideLogin"), vm.ErrorMessage);
        Assert.Null(auth.LastLoginCredentials);
    }

    [Fact]
    public async Task LoginAsync_EmptyPassword_ShowsValidationMessage()
    {
        var auth = new FakeAuthService();
        var vm = new LoginViewModel(auth, new FakeAuditLogService(), _ => { })
        {
            Login = "jan",
            Password = ""
        };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Login_ProvidePassword"), vm.ErrorMessage);
        Assert.Null(auth.LastLoginCredentials);
    }

    [Fact]
    public void IsNotLoading_WhenNotLoading_AllowsInteraction()
    {
        var vm = new LoginViewModel(new FakeAuthService(), new FakeAuditLogService(), _ => { });

        Assert.True(vm.IsNotLoading);
        Assert.Equal("Zaloguj", vm.LoginButtonText);

        vm.IsLoading = true;

        Assert.False(vm.IsNotLoading);
        Assert.Equal("Logowanie...", vm.LoginButtonText);
    }
}
