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
    public void Constructor_ExposesAutoLogoutTimeoutOptions()
    {
        var panel = CreatePanel();

        Assert.Equal([15, 30, 60], panel.AutoLogoutTimeoutOptions);
        Assert.Equal(SettingsPanelViewModel.AutoLogoutTimeoutMinuteOptions.Length, panel.AutoLogoutTimeoutOptions.Count);
    }

    [Fact]
    public void ApplyBootstrapFromSettings_LoadsAutoLogoutValues()
    {
        var settings = new FakeSettingsService();
        settings.Settings.AutoLogoutEnabled = false;
        settings.Settings.AutoLogoutTimeoutMinutes = 60;

        var panel = CreatePanel(settings: settings);
        panel.ApplyBootstrapFromSettings();

        Assert.False(panel.AutoLogoutEnabled);
        Assert.Equal(60, panel.SelectedAutoLogoutTimeoutMinutes);
    }

    [Fact]
    public async Task SaveSettingsCommand_PersistsAutoLogoutEnabled()
    {
        var settings = new FakeSettingsService();
        var applyInvoked = false;
        var panel = CreatePanel(
            settings: settings,
            applyAutoLogoutSettings: (enabled, _) =>
            {
                applyInvoked = true;
                Assert.False(enabled);
            });

        panel.AutoLogoutEnabled = false;

        await panel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.False(settings.Settings.AutoLogoutEnabled);
        Assert.True(applyInvoked);
    }

    [Fact]
    public async Task SaveSettingsCommand_PersistsAutoLogoutTimeoutMinutes()
    {
        var settings = new FakeSettingsService();
        var appliedTimeout = 0;
        var panel = CreatePanel(
            settings: settings,
            applyAutoLogoutSettings: (_, timeout) => appliedTimeout = timeout);

        panel.SelectedAutoLogoutTimeoutMinutes = 15;

        await panel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Equal(15, settings.Settings.AutoLogoutTimeoutMinutes);
        Assert.Equal(15, panel.SelectedAutoLogoutTimeoutMinutes);
        Assert.Equal(15, appliedTimeout);
    }

    [Fact]
    public async Task SaveSettingsCommand_NormalizesInvalidAutoLogoutTimeout()
    {
        var settings = new FakeSettingsService();
        var panel = CreatePanel(settings: settings);

        panel.SelectedAutoLogoutTimeoutMinutes = 45;

        await panel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Equal(30, settings.Settings.AutoLogoutTimeoutMinutes);
        Assert.Equal(30, panel.SelectedAutoLogoutTimeoutMinutes);
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
        FakeAuthService? auth = null,
        Action<bool, int>? applyAutoLogoutSettings = null)
    {
        settings ??= new FakeSettingsService();
        auth ??= new FakeAuthService();

        var context = TestDashboardContext.CreateDefault(AppSections.Settings).WithApiErrorHandling();

        return new SettingsPanelViewModel(
            settings,
            auth,
            context,
            () => Task.CompletedTask,
            applyAutoLogoutSettings);
    }
}
