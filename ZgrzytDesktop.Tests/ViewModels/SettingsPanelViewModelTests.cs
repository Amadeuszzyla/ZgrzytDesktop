using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class SettingsPanelViewModelTests
{
    public SettingsPanelViewModelTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void Constructor_InitializesCultureCollection()
    {
        var panel = CreatePanel();

        Assert.Equal(2, panel.UiCultures.Count);
        Assert.Contains("en", panel.UiCultures);
        Assert.Equal(SettingsPanelViewModel.LightThemeMode, panel.SelectedThemeMode);
    }

    [Fact]
    public async Task SaveSettingsCommand_PersistsCultureAndLightTheme()
    {
        var settings = new FakeSettingsService();
        var panel = CreatePanel(settings: settings);

        panel.SelectedUiCulture = "en";

        await panel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Equal(1, settings.SaveAsyncCallCount);
        Assert.Equal(SettingsPanelViewModel.LightThemeMode, settings.Settings.ThemeMode);
        Assert.Equal("en", settings.Settings.UiCulture);
        Assert.Equal("en", panel.SelectedUiCulture);
    }

    [Fact]
    public async Task RefreshSession_OnUnauthorized_ShowsApiUnauthorizedMessage()
    {
        var auth = new FakeAuthService
        {
            RefreshException = new ApiException(HttpStatusCode.Unauthorized, "Unauthorized")
        };
        var panel = CreatePanel(auth: auth);

        await panel.RefreshSessionCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Api_Unauthorized"), panel.SettingsStatusMessage);
    }

    private static SettingsPanelViewModel CreatePanel(
        FakeSettingsService? settings = null,
        FakeAuthService? auth = null)
    {
        settings ??= new FakeSettingsService();
        auth ??= new FakeAuthService();

        var context = TestDashboardContext.CreateDefault(AppSections.Settings).WithApiErrorHandling();

        return new SettingsPanelViewModel(
            settings,
            auth,
            context,
            () => Task.CompletedTask);
    }
}
