using System.Net;
using System.Text.Json;
using Xunit;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Regression;

public class RegisterUserTests
{
    public RegisterUserTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task RegisterUserAsync_ShouldPostToRegisterWithRole()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                { "message": "ok", "user": { "id": 1, "login": "new.user", "role": "it" } }
                """);
            var service = TestApiFactory.CreateUserAdmin(api);

            var request = new RegisterUserRequest
            {
                Name = "Jan Kowalski",
                Login = "new.user",
                Email = "new@test.pl",
                Password = "secret123",
                PasswordConfirmation = "secret123",
                Role = AppRoles.It
            };

            var response = await service.RegisterUserAsync(request);

            Assert.Equal("new.user", response.User?.Login);
            Assert.EndsWith("/api/register", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);

            var body = TestApiFactory.LastRequestBody(handler);
            using var document = JsonDocument.Parse(body!);
            Assert.Equal(AppRoles.It, document.RootElement.GetProperty("role").GetString());
            Assert.Equal("new.user", document.RootElement.GetProperty("login").GetString());
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Theory]
    [InlineData(AppRoles.User)]
    [InlineData(AppRoles.It)]
    [InlineData(AppRoles.Admin)]
    public async Task RegisterUserCommand_AsStaff_SendsRole(string role)
    {
        var userAdmin = new FakeUserAdminService();
        var context = new RegisterTestContext();
        var panel = CreatePanel(userAdmin, context, isStaffRole: true);

        panel.NewUserName = "Test User";
        panel.NewUserLogin = "test.user";
        panel.NewUserEmail = "test@test.pl";
        panel.NewUserPassword = "password1";
        panel.NewUserPasswordConfirmation = "password1";
        panel.SelectedNewUserRole = panel.NewUserRoles.First(option => option.Role == role);

        await panel.RegisterUserCommand.ExecuteAsync(null);

        Assert.Equal(1, userAdmin.RegisterUserCallCount);
        Assert.Equal(role, userAdmin.LastRegisterUserRequest?.Role);
        Assert.Contains(("RegisterUser", null), context.AuditCalls);
    }

    [Fact]
    public async Task RegisterUserCommand_AsAdmin_CanCreateAllRoles()
    {
        foreach (var role in new[] { AppRoles.User, AppRoles.It, AppRoles.Admin })
        {
            var userAdmin = new FakeUserAdminService();
            var panel = CreatePanel(userAdmin, isAdminRole: true, isStaffRole: true);
            FillValidRegisterForm(panel);
            panel.SelectedNewUserRole = panel.NewUserRoles.First(option => option.Role == role);

            await panel.RegisterUserCommand.ExecuteAsync(null);

            Assert.Equal(role, userAdmin.LastRegisterUserRequest?.Role);
        }
    }

    [Fact]
    public async Task RegisterUserCommand_MissingRole_ShowsValidationMessage()
    {
        var panel = CreatePanel(new FakeUserAdminService(), isStaffRole: true);
        FillValidRegisterForm(panel);
        panel.SelectedNewUserRole = null;

        await panel.RegisterUserCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("RegisterUser_ValidationRole"), panel.RegisterUserStatusMessage);
    }

    [Fact]
    public async Task RegisterUserCommand_PasswordMismatch_ShowsValidationMessage()
    {
        var panel = CreatePanel(new FakeUserAdminService(), isStaffRole: true);
        FillValidRegisterForm(panel);
        panel.NewUserPasswordConfirmation = "different";

        await panel.RegisterUserCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("RequestAccount_ValidationPasswordMismatch"), panel.RegisterUserStatusMessage);
    }

    [Fact]
    public async Task RegisterUserCommand_On422_ShowsUserFriendlyValidationMessage()
    {
        const string json = """
            {
              "message": "The given data was invalid.",
              "errors": {
                "login": ["Login jest już zajęty."]
              }
            }
            """;

        var userAdmin = new FakeUserAdminService
        {
            RegisterUserApiException = new ApiException(HttpStatusCode.UnprocessableEntity, "validation", json)
        };

        var panel = CreatePanel(userAdmin, isStaffRole: true);
        FillValidRegisterForm(panel);

        await panel.RegisterUserCommand.ExecuteAsync(null);

        Assert.Contains("login:", panel.RegisterUserStatusMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("<html", panel.RegisterUserStatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("pl", "Użytkownik", "IT", "Administrator")]
    [InlineData("en", "User", "IT", "Administrator")]
    public void RegisterUserRoleOption_Labels_AreLocalized(string culture, string userLabel, string itLabel, string adminLabel)
    {
        AppStrings.ApplyCulture(culture);

        var labels = RegisterUserRoleOption.All.Select(option => option.Label).ToList();

        Assert.Equal(userLabel, labels[0]);
        Assert.Equal(itLabel, labels[1]);
        Assert.Equal(adminLabel, labels[2]);
    }

    [Fact]
    public void RegisterUserValidator_IsAllowedRole_AcceptsOnlySwaggerRoles()
    {
        Assert.True(RegisterUserRoleOption.IsAllowedRole(AppRoles.User));
        Assert.True(RegisterUserRoleOption.IsAllowedRole(AppRoles.It));
        Assert.True(RegisterUserRoleOption.IsAllowedRole(AppRoles.Admin));
        Assert.False(RegisterUserRoleOption.IsAllowedRole("superadmin"));
        Assert.False(RegisterUserRoleOption.IsAllowedRole(null));
    }

    private static void FillValidRegisterForm(AdminPanelViewModel panel)
    {
        panel.NewUserName = "Jan Kowalski";
        panel.NewUserLogin = "jan.kowalski";
        panel.NewUserEmail = "jan@test.pl";
        panel.NewUserPassword = "Haslo123!";
        panel.NewUserPasswordConfirmation = "Haslo123!";
        panel.SelectedNewUserRole = panel.NewUserRoles.First(option => option.Role == AppRoles.User);
    }

    private sealed class RegisterTestContext
    {
        public bool IsOffline { get; set; }

        public List<(string action, int? ticketId)> AuditCalls { get; } = new();
    }

    private static AdminPanelViewModel CreatePanel(
        FakeUserAdminService userAdmin,
        RegisterTestContext? context = null,
        bool isAdminRole = true,
        bool isStaffRole = true)
    {
        context ??= new RegisterTestContext();

        return new AdminPanelViewModel(
            userAdmin,
            new AdminPanelCallbacks
            {
                ShowToastKey = TestToastCallbacks.NoopKey,
            ShowToastRaw = TestToastCallbacks.NoopRaw,
                GetIsOffline = () => context.IsOffline,
                GetIsAdminRole = () => isAdminRole,
                GetIsStaffRole = () => isStaffRole,
                GetCanUseOnlineActions = () => !context.IsOffline,
                GetApiErrorMessage = ex => ApiErrorSanitizer.SanitizeApiErrorMessage(
                    ex.ResponseContent ?? ex.Message,
                    ex.StatusCode),
                LogAuditAsync = (action, ticketId, _, _) =>
                {
                    context.AuditCalls.Add((action, ticketId));
                    return Task.CompletedTask;
                },
                ExecuteApiAsyncCore = async (action, options) =>
                {
                    try
                    {
                        await action();
                        return true;
                    }
                    catch (ApiException ex)
                    {
                        options?.SetStatusMessage?.Invoke(ApiErrorSanitizer.SanitizeApiErrorMessage(
                            ex.ResponseContent ?? ex.Message,
                            ex.StatusCode));
                        return false;
                    }
                    catch
                    {
                        options?.SetStatusMessage?.Invoke(AppStrings.Get("RegisterUser_Failed"));
                        return false;
                    }
                }
            });
    }
}
