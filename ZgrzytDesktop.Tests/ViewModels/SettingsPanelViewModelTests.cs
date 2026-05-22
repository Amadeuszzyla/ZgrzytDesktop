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
    public void Constructor_InitializesThemeAndCultureCollections()
    {
        var panel = CreatePanel();

        Assert.Equal(3, panel.ThemeModes.Count);
        Assert.Equal(2, panel.UiCultures.Count);
        Assert.Contains("Dark", panel.ThemeModes);
        Assert.Contains("en", panel.UiCultures);
    }

    [Fact]
    public async Task SaveSettingsCommand_PersistsThemeAndCulture()
    {
        var settings = new FakeSettingsService();
        var panel = CreatePanel(settings: settings);

        panel.SelectedThemeMode = "Dark";
        panel.SelectedUiCulture = "en";

        await panel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Equal(1, settings.SaveAsyncCallCount);
        Assert.Equal("Dark", settings.Settings.ThemeMode);
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

        var bridge = new DashboardVmBridge
        {
            ExecuteApiAsyncCore = async (action, setStatusMessage, unexpectedStatusMessage, unexpectedToastMessage,
                offlineToastMessage, showApiErrorToast, setOfflineOnServiceUnavailable, onServiceUnavailableAsync) =>
            {
                try
                {
                    await action();
                    return true;
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
                    setStatusMessage?.Invoke(unexpectedStatusMessage ?? AppStrings.Get("Api_UnexpectedError"));
                    return false;
                }
            },
            ShowToast = (_, _) => { },
            LogAuditAsync = (_, _, _) => Task.CompletedTask,
            GetIsOffline = () => false,
            SetIsOffline = _ => { },
            NotifyLocalization = () => { },
            GetCurrentSection = () => AppSections.Settings
        };

        return new SettingsPanelViewModel(
            settings,
            auth,
            bridge,
            () => Task.CompletedTask);
    }
}
