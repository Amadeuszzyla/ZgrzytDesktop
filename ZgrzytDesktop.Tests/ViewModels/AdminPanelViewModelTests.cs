using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class AdminPanelViewModelTests
{
    public AdminPanelViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task LoadUsersAsync_Success_PopulatesUsers()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 1, Login = "alpha", Email = "a@test.pl", Role = AppRoles.User, Active = true },
                new User { Id = 2, Login = "beta", Email = "b@test.pl", Role = AppRoles.It, Active = true }
            ]
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(2, panel.AdminUsers.Count);
        Assert.Equal("alpha", panel.AdminUsers[0].Login);
        Assert.Equal(
            AppStrings.GetFormat("Admin_StatusCount", 2, AppStrings.Get("Admin_Filter_All")),
            panel.AdminStatusMessage);
    }

    [Fact]
    public async Task LoadUsersAsync_StatusMessage_UsesEnglishFormat()
    {
        AppStrings.ApplyCulture("en");

        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 1, Login = "alpha", Active = true, Ban = false }
            ]
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(
            AppStrings.GetFormat("Admin_StatusCount", 1, AppStrings.Get("Admin_Filter_All")),
            panel.AdminStatusMessage);
    }

    [Fact]
    public async Task LoadUsersAsync_On404Fallback_ShowsSuccessStatusNotError()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 1, Login = "alpha", Active = true, Ban = false }
            ],
            NextUsedLocalFilterFallback = true,
            NextInfoKind = UserAdminListInfoKind.LocalFilterFallback
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Single(panel.AdminUsers);
        Assert.Equal(
            AppStrings.GetFormat("Admin_StatusCount", 1, AppStrings.Get("Admin_Filter_All")),
            panel.AdminStatusMessage);
        Assert.NotEqual(AppStrings.Get("Api_Forbidden"), panel.AdminStatusMessage);
        Assert.NotEqual(AppStrings.Get("Api_NotFound"), panel.AdminStatusMessage);
    }

    [Fact]
    public async Task LoadUsersAsync_On403_ShowsAdminListForbiddenMessage()
    {
        var userAdmin = new FakeUserAdminService
        {
            GetUsersApiException = new ApiException(
                HttpStatusCode.Forbidden,
                AppStrings.Get("Admin_ListForbidden"))
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Admin_ListForbidden"), panel.AdminStatusMessage);
        Assert.Empty(panel.AdminUsers);
    }

    [Fact]
    public async Task LoadUsersAsync_FilterAll_DoesNotUseFilteredEndpoints()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers = [new User { Id = 1, Login = "alpha", Active = true, Ban = false }]
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(UserAdminListFilter.All, userAdmin.LastFilter);
    }


    [Fact]
    public async Task ActivateUserAsync_OnSuccess_RefreshesListAndAudits()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 5, Login = "inactive", Active = false, Ban = false }
            ]
        };

        var ctx = new AdminTestContext();
        var panel = CreatePanel(userAdmin, isAdminRole: true, context: ctx);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);
        panel.SelectedAdminUser = panel.AdminUsers[0];

        await panel.ActivateAdminUserCommand.ExecuteAsync(null);

        Assert.Equal(1, userAdmin.ActivateCallCount);
        Assert.Equal(5, userAdmin.LastActivateUserId);
        Assert.Equal(2, userAdmin.GetUsersCallCount);
        Assert.Contains(ctx.AuditCalls, c => c.action == "ActivateUser");
    }

    [Fact]
    public async Task BanUserAsync_OnSuccess_RefreshesList()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 7, Login = "target", Active = true, Ban = false }
            ]
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);
        panel.SelectedAdminUser = panel.AdminUsers[0];

        await panel.BanAdminUserCommand.ExecuteAsync(null);

        Assert.Equal(1, userAdmin.BanCallCount);
        Assert.Equal(7, userAdmin.LastBanUserId);
        Assert.Equal(2, userAdmin.GetUsersCallCount);
    }

    [Fact]
    public async Task UnbanUserAsync_WithoutPassword_ShowsWarning()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 9, Login = "banned", Active = false, Ban = true }
            ]
        };

        var ctx = new AdminTestContext();
        var panel = CreatePanel(userAdmin, isAdminRole: true, context: ctx);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);
        panel.SelectedAdminUser = panel.AdminUsers[0];
        panel.AdminUnbanPassword = "   ";

        await panel.UnbanAdminUserCommand.ExecuteAsync(null);

        Assert.Equal(0, userAdmin.UnbanCallCount);
        Assert.Contains("hasło", ctx.LastToastMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnbanUserAsync_OnSuccess_ClearsPasswordAndRefreshes()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 10, Login = "banned", Active = false, Ban = true }
            ]
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);
        panel.SelectedAdminUser = panel.AdminUsers[0];
        panel.AdminUnbanPassword = "secret";

        await panel.UnbanAdminUserCommand.ExecuteAsync(null);

        Assert.Equal(1, userAdmin.UnbanCallCount);
        Assert.Equal("secret", userAdmin.LastUnbanPassword);
        Assert.Equal(string.Empty, panel.AdminUnbanPassword);
        Assert.Equal(2, userAdmin.GetUsersCallCount);
    }

    [Fact]
    public async Task Admin_FilterActiveUsers_403_ShowsPermissionMessageInHeader()
    {
        var userAdmin = new FakeUserAdminService
        {
            GetUsersApiException = new ApiException(HttpStatusCode.Forbidden, "forbidden")
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Admin_ListForbidden"), panel.AdminStatusMessage);
        Assert.Empty(panel.AdminUsers);
    }

    [Fact]
    public async Task BanUserAsync_On404_ShowsNotFoundMessage()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers = [new User { Id = 11, Login = "u", Active = true, Ban = false }],
            BanApiException = new ApiException(
                HttpStatusCode.NotFound,
                AppStrings.Get("Admin_ActionNotSupported"))
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);
        panel.SelectedAdminUser = panel.AdminUsers[0];

        await panel.BanAdminUserCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Admin_ActionNotSupported"), panel.AdminStatusMessage);
    }

    [Fact]
    public async Task LoadUsersAsync_OnHtmlError_DoesNotExposeRawHtml()
    {
        var userAdmin = new FakeUserAdminService
        {
            GetUsersApiException = new ApiException(
                HttpStatusCode.InternalServerError,
                "<!DOCTYPE html><html><body>error</body></html>")
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Admin_UsersLoadServerError"), panel.AdminStatusMessage);
        Assert.DoesNotContain("<html", panel.AdminStatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadUsersAsync_OnServiceUnavailable_ShowsOfflineMessage()
    {
        var userAdmin = new FakeUserAdminService
        {
            GetUsersApiException = new ApiException(HttpStatusCode.ServiceUnavailable, "offline")
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Api_ServiceUnavailable"), panel.AdminStatusMessage);
    }

    [Fact]
    public async Task IT_OpenAdminPage_DoesNotLoadUsers()
    {
        var userAdmin = new FakeUserAdminService();
        var panel = CreatePanel(userAdmin, isAdminRole: false, isStaffRole: true);

        panel.PrepareAdminPage(isAdminRole: false);
        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(0, userAdmin.GetUsersCallCount);
        Assert.Empty(panel.AdminUsers);
        Assert.Equal(string.Empty, panel.AdminStatusMessage);
    }

    [Fact]
    public void IT_OpenAdminPage_ShowsOnlyRegisterTab()
    {
        var panel = CreatePanel(new FakeUserAdminService(), isAdminRole: false, isStaffRole: true);

        panel.PrepareAdminPage(isAdminRole: false);

        Assert.Equal(AdminTabs.NewAccount, panel.AdminTab);
        Assert.True(panel.IsAdminNewAccountTabActive);
        Assert.False(panel.IsAdminUsersTabActive);
        Assert.False(panel.IsAdminUsersPanelVisible);
        Assert.True(panel.IsAdminNewAccountPanelVisible);
    }

    [Fact]
    public void IT_OpenAdminPage_HidesUserFilters()
    {
        var panel = CreatePanel(new FakeUserAdminService(), isAdminRole: false, isStaffRole: true);

        panel.PrepareAdminPage(isAdminRole: false);

        Assert.False(panel.IsAdminUsersManagementVisible);
        Assert.False(panel.IsAdminUsersPanelVisible);
    }

    [Fact]
    public void IT_OpenAdminPage_HidesBanActivateUnbanActions()
    {
        var panel = CreatePanel(new FakeUserAdminService(), isAdminRole: false, isStaffRole: true);
        panel.AdminUsers.Add(new User { Id = 1, Login = "u", Active = true, Ban = false });
        panel.SelectedAdminUser = panel.AdminUsers[0];

        Assert.False(panel.CanBanAdminUser);
        Assert.False(panel.CanActivateAdminUser);
        Assert.False(panel.CanUnbanAdminUser);
    }

    [Fact]
    public void Admin_OpenAdminPage_ShowsUsersAndRegisterTabs()
    {
        var panel = CreatePanel(new FakeUserAdminService(), isAdminRole: true, isStaffRole: true);

        panel.PrepareAdminPage(isAdminRole: true);

        Assert.Equal(AdminTabs.Users, panel.AdminTab);
        Assert.True(panel.IsAdminUsersPanelVisible);
        Assert.True(panel.IsAdminUsersManagementVisible);

        panel.AdminTab = AdminTabs.NewAccount;
        Assert.True(panel.IsAdminNewAccountPanelVisible);
    }

    [Fact]
    public async Task Admin_LoadUsers_DefaultFilterUsesAllEndpoint()
    {
        var userAdmin = new FakeUserAdminService();
        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(UserAdminListFilter.All, userAdmin.LastFilter);
    }

    [Fact]
    public async Task Admin_ChangingFilterToActive_UsesActiveEndpoint()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers = [new User { Id = 1, Login = "alpha", Active = true, Ban = false }]
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);
        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        panel.SelectedAdminUserFilter = panel.AdminUserFilterOptions
            .First(option => option.Filter == UserAdminListFilter.Active);

        while (panel.IsLoadingAdminUsers)
            await Task.Delay(10);

        Assert.Equal(UserAdminListFilter.Active, userAdmin.LastFilter);
        Assert.Contains(AppStrings.Get("Admin_Filter_Active"), panel.AdminStatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_FilterActiveUsers_404_FallbacksToUsers()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 1, Login = "alpha", Active = true, Ban = false }
            ],
            NextUsedLocalFilterFallback = true,
            NextInfoKind = UserAdminListInfoKind.LocalFilterFallback
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);
        panel.SelectedAdminUserFilter = panel.AdminUserFilterOptions
            .First(option => option.Filter == UserAdminListFilter.Active);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Single(panel.AdminUsers);
        Assert.Equal(
            AppStrings.GetFormat("Admin_StatusCount", 1, AppStrings.Get("Admin_Filter_Active")),
            panel.AdminStatusMessage);
    }

    [Fact]
    public void IT_ApplyDefaultFilter_DoesNotTriggerUserLoad()
    {
        var userAdmin = new FakeUserAdminService();
        var panel = CreatePanel(userAdmin, isAdminRole: false, isStaffRole: true);

        panel.ApplyDefaultFilter();

        Assert.Equal(0, userAdmin.GetUsersCallCount);
    }

    [Fact]
    public async Task LoadUsersAsync_WithUsers_HasAdminUsersAndNoEmptyState()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers = [new User { Id = 1, Login = "alpha", Active = true, Ban = false }]
        };

        var panel = CreatePanel(userAdmin, isAdminRole: true);
        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.True(panel.HasAdminUsers);
        Assert.False(panel.HasNoAdminUsers);
        Assert.NotEqual(panel.LblAdminNoUsersFound, panel.AdminStatusMessage);
        Assert.Contains("1", panel.AdminStatusMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("Znaleziono", panel.LblAdminNoUsersFound, StringComparison.Ordinal);
        Assert.DoesNotContain("Found:", panel.LblAdminNoUsersFound, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadUsersAsync_EmptyList_ShowsEmptyStateAndCountInHeader()
    {
        var userAdmin = new FakeUserAdminService { NextUsers = [] };

        var panel = CreatePanel(userAdmin, isAdminRole: true);
        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.True(panel.HasNoAdminUsers);
        Assert.False(panel.HasAdminUsers);
        Assert.Equal(AppStrings.Get("Admin_NoUsersFound"), panel.LblAdminNoUsersFound);
        Assert.Equal(
            AppStrings.GetFormat("Admin_StatusCount", 0, AppStrings.Get("Admin_Filter_All")),
            panel.AdminStatusMessage);
        Assert.DoesNotContain(panel.AdminStatusMessage, panel.LblAdminNoUsersFound, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadUsersAsync_EmptyList_StatusUsesEnglishInHeader()
    {
        AppStrings.ApplyCulture("en");

        var userAdmin = new FakeUserAdminService { NextUsers = [] };
        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Contains("Found:", panel.AdminStatusMessage, StringComparison.Ordinal);
        Assert.Equal("No users found.", panel.LblAdminNoUsersFound);
        Assert.True(panel.HasNoAdminUsers);
    }

    [Fact]
    public async Task LoadUsersAsync_EmptyList_StatusUsesPolishInHeader()
    {
        AppStrings.ApplyCulture("pl");

        var userAdmin = new FakeUserAdminService { NextUsers = [] };
        var panel = CreatePanel(userAdmin, isAdminRole: true);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Contains("Znaleziono:", panel.AdminStatusMessage, StringComparison.Ordinal);
        Assert.Equal("Nie znaleziono użytkowników.", panel.LblAdminNoUsersFound);
    }

    private sealed class AdminTestContext
    {
        public bool IsOffline { get; set; }

        public string? LastToastMessage;

        public List<(string action, int? ticketId)> AuditCalls { get; } = new();
    }

    private static AdminPanelViewModel CreatePanel(
        FakeUserAdminService userAdmin,
        bool isAdminRole = true,
        bool isStaffRole = true,
        AdminTestContext? context = null)
    {
        context ??= new AdminTestContext();

        return new AdminPanelViewModel(
            userAdmin,
            new AdminPanelCallbacks
            {
                ShowToastKey = TestToastCallbacks.ResolveKeyTo(m => context.LastToastMessage = m),
                ShowToastRaw = (message, _) => context.LastToastMessage = message,
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
                ExecuteApiAsyncCore = CreateTestExecuteApiAsync(context)
            });
    }

    private static Func<Func<Task>, Action<string>?, string?, string?, string?, bool, bool, Func<ApiException, Task>?, Task<bool>>
        CreateTestExecuteApiAsync(AdminTestContext context) =>
        async (action, setStatusMessage, unexpectedStatusMessage, unexpectedToastMessage, offlineToastMessage,
            showApiErrorToast, setOfflineOnServiceUnavailable, onServiceUnavailableAsync) =>
        {
            try
            {
                await action();
                return true;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                if (onServiceUnavailableAsync is not null)
                {
                    await onServiceUnavailableAsync(ex);
                    return false;
                }

                setStatusMessage?.Invoke(ApiErrorSanitizer.SanitizeApiErrorMessage(
                    ex.ResponseContent ?? ex.Message,
                    ex.StatusCode));
                return false;
            }
            catch (ApiException ex)
            {
                setStatusMessage?.Invoke(ApiErrorSanitizer.SanitizeApiErrorMessage(
                    ex.ResponseContent ?? ex.Message,
                    ex.StatusCode));
                return false;
            }
            catch
            {
                setStatusMessage?.Invoke(
                    unexpectedStatusMessage is not null
                        ? AppStrings.Get(unexpectedStatusMessage)
                        : AppStrings.Get("Api_UnexpectedError"));
                return false;
            }
        };
}
