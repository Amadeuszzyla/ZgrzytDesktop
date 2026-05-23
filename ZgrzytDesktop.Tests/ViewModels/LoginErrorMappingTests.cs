using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.ViewModels;

public class LoginErrorMappingTests
{
    public LoginErrorMappingTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task Login_401_ShowsInvalidCredentials_NotServerError()
    {
        var (vm, tempDir) = await LoginWithApiStatusAsync(
            HttpStatusCode.Unauthorized,
            """{"message":"Nieprawidłowe dane logowania."}""");

        try
        {
            Assert.Equal(AppStrings.Get("Login_InvalidCredentials"), vm.ErrorMessage);
            Assert.DoesNotContain(AppStrings.Get("Api_InternalServerError"), vm.ErrorMessage, StringComparison.Ordinal);
            Assert.DoesNotContain("Błąd serwera", vm.ErrorMessage, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Login_403_ShowsNoDesktopAccess()
    {
        var (vm, tempDir) = await LoginWithApiStatusAsync(
            HttpStatusCode.Forbidden,
            """{"message":"Forbidden"}""");

        try
        {
            Assert.Equal(AppStrings.Get("Login_NoDesktopAccess"), vm.ErrorMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Login_422_ShowsValidationMessage()
    {
        const string json = """
            {
              "message": "The given data was invalid.",
              "errors": {
                "login": ["Pole login jest wymagane."]
              }
            }
            """;

        var (vm, tempDir) = await LoginWithApiStatusAsync(HttpStatusCode.UnprocessableEntity, json);

        try
        {
            Assert.Equal(AppStrings.Get("Login_CheckCredentials"), vm.ErrorMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Login_500_ShowsApiServerError()
    {
        var (vm, tempDir) = await LoginWithApiStatusAsync(
            HttpStatusCode.InternalServerError,
            """{"message":"Server failure"}""");

        try
        {
            Assert.Equal(AppStrings.Get("Login_ApiServerError"), vm.ErrorMessage);
            Assert.DoesNotContain(AppStrings.Get("Api_InternalServerError"), vm.ErrorMessage, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task Login_502_503_504_ShowApiServerError(HttpStatusCode statusCode)
    {
        var (vm, tempDir) = await LoginWithApiStatusAsync(statusCode, """{"message":"Unavailable"}""");

        try
        {
            Assert.Equal(AppStrings.Get("Login_ApiServerError"), vm.ErrorMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Login_NoHttpResponse_ShowsConnectionError()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueException(new HttpRequestException("No such host is known."));

            var vm = CreateLoginViewModel(api, tempDir);

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.Equal(AppStrings.Get("Login_ConnectionError"), vm.ErrorMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Login_InvalidJson_ShowsInvalidApiResponse()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.Enqueue(HttpStatusCode.OK, "not-json", "application/json");

            var vm = CreateLoginViewModel(api, tempDir);

            await vm.LoginCommand.ExecuteAsync(null);

            Assert.Equal(AppStrings.Get("Login_InvalidApiResponse"), vm.ErrorMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Theory]
    [InlineData("pl", "Login_InvalidCredentials", "Nieprawidłowy login lub hasło.")]
    [InlineData("en", "Login_InvalidCredentials", "Invalid login or password.")]
    [InlineData("pl", "Login_NoDesktopAccess", "To konto nie ma dostępu do aplikacji desktopowej.")]
    [InlineData("en", "Login_NoDesktopAccess", "This account does not have access to the desktop application.")]
    [InlineData("pl", "Login_CheckCredentials", "Sprawdź login i hasło.")]
    [InlineData("en", "Login_CheckCredentials", "Check your login and password.")]
    [InlineData("pl", "Login_ApiServerError", "Serwer API zwrócił błąd. Spróbuj ponownie później.")]
    [InlineData("en", "Login_ApiServerError", "The API server returned an error. Try again later.")]
    [InlineData("pl", "Login_ConnectionError", "Nie można połączyć się z API. Sprawdź internet lub adres serwera.")]
    [InlineData("en", "Login_ConnectionError", "Could not connect to the API. Check your internet connection or server URL.")]
    [InlineData("pl", "Login_InvalidApiResponse", "API zwróciło nieprawidłową odpowiedź.")]
    [InlineData("en", "Login_InvalidApiResponse", "The API returned an invalid response.")]
    public void Login_ErrorMessages_AreLocalized_PL_EN(string culture, string key, string expected)
    {
        AppStrings.ApplyCulture(culture);
        Assert.Equal(expected, AppStrings.Get(key));
    }

    [Fact]
    public async Task Login_DoesNotLogPasswordOrToken()
    {
        const string password = "SuperSecret99!";
        var audit = new FakeAuditLogService();
        var auth = new FakeAuthService
        {
            LoginException = new ApiException(HttpStatusCode.Unauthorized, "Unauthorized")
        };

        var vm = new LoginViewModel(auth, audit, _ => { })
        {
            Login = "jan",
            Password = password
        };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Empty(audit.Entries);
        Assert.Equal(("jan", password), auth.LastLoginCredentials);

        var mapperMessage = LoginErrorMapper.GetErrorMessage(
            new ApiException(HttpStatusCode.Unauthorized, "token=abc password=secret"));
        Assert.Equal(AppStrings.Get("Login_InvalidCredentials"), mapperMessage);
    }

    [Fact]
    public void LoginErrorMapper_401_MapsToInvalidCredentials_EvenWhenApiMessageIsServerErrorText()
    {
        AppStrings.ApplyCulture("pl");

        var message = LoginErrorMapper.GetErrorMessage(
            new ApiException(
                HttpStatusCode.Unauthorized,
                AppStrings.Get("Api_InternalServerError"),
                """{"message":"Nieprawidłowe dane logowania."}"""));

        Assert.Equal(AppStrings.Get("Login_InvalidCredentials"), message);
    }

    private static async Task<(LoginViewModel Vm, string TempDir)> LoginWithApiStatusAsync(
        HttpStatusCode statusCode,
        string json)
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        handler.EnqueueJson(statusCode, json);
        var vm = CreateLoginViewModel(api, tempDir);
        await vm.LoginCommand.ExecuteAsync(null);
        return (vm, tempDir);
    }

    private static LoginViewModel CreateLoginViewModel(ApiService api, string tempDir)
    {
        var auth = TestApiFactory.CreateAuth(api, tempDir);
        return new LoginViewModel(auth, new FakeAuditLogService(), _ => { })
        {
            Login = "jan",
            Password = "bad-password"
        };
    }
}
