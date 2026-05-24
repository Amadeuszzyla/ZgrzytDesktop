using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class ToastLocalizationTests
{
    public ToastLocalizationTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task SettingsSave_Toast_IsEnglish_WhenCultureEn()
    {
        using var _ = new TestCultureScope("en");
        var (vm, settings, _, tempDir) = ViewModelTestFactory.CreateDashboard();
        try
        {
            vm.CurrentSection = AppSections.Settings;
            vm.SelectedUiCulture = "en";
            await vm.SaveSettingsCommand.ExecuteAsync(null);

            Assert.True(vm.IsToastVisible);
            AppStrings.ApplyCulture("en");
            Assert.Equal(AppStrings.Get("Toast_SettingsSaved"), vm.ToastMessage);
            Assert.Contains("saved", vm.ToastMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task SettingsSave_Toast_IsPolish_WhenCulturePl()
    {
        using var _ = new TestCultureScope("pl");
        var (vm, settings, _, tempDir) = ViewModelTestFactory.CreateDashboard();
        try
        {
            vm.CurrentSection = AppSections.Settings;
            vm.SelectedUiCulture = "pl";
            await vm.SaveSettingsCommand.ExecuteAsync(null);

            Assert.True(vm.IsToastVisible);
            AppStrings.ApplyCulture("pl");
            Assert.Equal(AppStrings.Get("Toast_SettingsSaved"), vm.ToastMessage);
            Assert.Contains("zapisane", vm.ToastMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task AdminBan_Toast_IsEnglish_WhenCultureEn()
    {
        using var _ = new TestCultureScope("en");
        var previousConfirmation = ConfirmationServiceHolder.Instance;
        ConfirmationServiceHolder.Instance = new FakeUserConfirmationService { NextResult = true };
        var userAdmin = new FakeUserAdminService
        {
            NextUsers =
            [
                new User { Id = 7, Login = "target", Active = true, Ban = false }
            ]
        };
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin", userAdmin: userAdmin);
        try
        {
            AppStrings.ApplyCulture("en");
            await vm.LoadAdminUsersCommand.ExecuteAsync(null);
            vm.SelectedAdminUser = vm.AdminUsers.First(u => u.Active && !u.Ban);
            await vm.BanAdminUserCommand.ExecuteAsync(null);

            Assert.True(vm.IsToastVisible);
            AppStrings.ApplyCulture("en");
            Assert.Equal(AppStrings.Get("Toast_UserBanned"), vm.ToastMessage);
            Assert.Contains("banned", vm.ToastMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            ConfirmationServiceHolder.Instance = previousConfirmation;
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task TicketUpdate_Toast_IsEnglish_WhenCultureEn()
    {
        using var _ = new TestCultureScope("en");
        string? toastMessage = null;
        var tickets = new FakeTicketService();
        tickets.TicketsById[12] = new Ticket
        {
            Id = 12,
            Title = "Issue",
            Status = TicketStatuses.Nowe,
            Priority = TicketPriorities.Low
        };

        var panel = new TicketDetailsPanelViewModel(
            tickets,
            new FakeUserAdminService(),
            new FakeAuditLogService(),
            new TicketDetailsPanelCallbacks
            {
                ShowToastKey = TestToastCallbacks.ResolveKeyTo(m => toastMessage = m),
                ShowToastRaw = TestToastCallbacks.NoopRaw,
                SetIsOffline = _ => { },
                GetIsOffline = () => false,
                GetApiErrorMessage = ex => ex.Message,
                FindCachedTicket = _ => null,
                NotifyDetailsSideEffects = () => { },
                NotifyDetailsLoadingChanged = () => { },
                GetCurrentUser = () => new User { Id = 99, Login = "it", Name = "IT", Role = AppRoles.It },
                GetCanManageTickets = () => true,
                GetIsAdminRole = () => false,
                GetIsRegularUser = () => false,
                LogAuditAsync = (_, _, _, _) => Task.CompletedTask,
                RefreshTicketsAsync = () => Task.CompletedTask,
                NavigateToTickets = () => { },
                ClearSelectedTicket = () => { },
                ExecuteApiAsyncCore = async (action, _, _, _, _, _, _, _) =>
                {
                    await action();
                    return true;
                }
            });

        await panel.LoadTicketDetailsAsync(12);
        panel.SelectedStatus = StatusDisplayHelper.ToDisplayStatus(TicketStatuses.WTrakcie);
        await panel.UpdateTicketCommand.ExecuteAsync(null);

        AppStrings.ApplyCulture("en");
        Assert.Equal(AppStrings.Get("Toast_TicketSaved"), toastMessage);
        Assert.Contains("saved", toastMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SessionRefreshUnauthorized_Toast_IsEnglish_WhenCultureEn()
    {
        using var _ = new TestCultureScope("en");
        var auth = new FakeAuthService
        {
            RefreshException = new ApiException(HttpStatusCode.Unauthorized, "Unauthorized")
        };
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard(auth: auth);
        try
        {
            AppStrings.ApplyCulture("en");
            await vm.RefreshSessionCommand.ExecuteAsync(null);

            Assert.True(vm.IsToastVisible);
            AppStrings.ApplyCulture("en");
            Assert.Equal(AppStrings.Get("Api_Unauthorized"), vm.ToastMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void NotifyLocalization_RefreshesVisibleLocalizedToast()
    {
        using var pl = new TestCultureScope("pl");
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard(
            bootstrap: DashboardViewModel.BootstrapOptions.Testing);
        try
        {
            vm.ShowToastKey("Toast_SettingsSaved", ToastTypes.Success);
            var plMessage = vm.ToastMessage;

            AppStrings.ApplyCulture("en");
            vm.GetType().GetMethod(
                    "NotifyLocalizationProperties",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(vm, null);

            Assert.True(vm.IsToastVisible);
            Assert.NotEqual(plMessage, vm.ToastMessage);
            Assert.Equal(AppStrings.Get("Toast_SettingsSaved"), vm.ToastMessage);
            Assert.Contains("saved", vm.ToastMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void ViewModels_DoNotPassHardcodedPolishToShowToast()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "ZgrzytDesktop",
            "ViewModels"));

        var polishInShowToast = new Regex(
            @"ShowToast\s*\(\s*""[^""]*[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]",
            RegexOptions.CultureInvariant);

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (polishInShowToast.IsMatch(text))
                violations.Add(Path.GetRelativePath(root, file));
        }

        Assert.Empty(violations);
    }
}
