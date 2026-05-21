using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private async Task SaveSettingsAsync()
    {
        await ExecuteApiAsync(
            async () =>
            {
                var existing = await _settingsService.LoadAsync();

                var settings = new AppSettings
                {
                    ApiBaseUrl = existing.ApiBaseUrl,
                    ThemeMode = SelectedThemeMode,
                    UiCulture = SettingsService.NormalizeUiCulture(SelectedUiCulture)
                };

                await _settingsService.SaveAsync(settings);

                SelectedThemeMode = settings.ThemeMode;
                SelectedUiCulture = settings.UiCulture;
                SettingsService.ApplyThemeMode(settings.ThemeMode);
                AppStrings.ApplyCulture(settings.UiCulture);
                NotifyLocalizationProperties();

                SettingsStatusMessage = string.Empty;

                await LogAuditAsync("Zapis ustawień", null, "Zmieniono ustawienia aplikacji.");

                ShowToast(AppStrings.Get("Toast_SettingsSaved"), ToastTypes.Success);

                if (CurrentSection == AppSections.Settings)
                    await RefreshSettingsAuditLogAsync();
            },
            unexpectedToastMessage: "Nie udało się zapisać ustawień.",
            showApiErrorToast: false);
    }

    private async Task RefreshSessionAsync()
    {
        SettingsStatusMessage = "Odświeżanie sesji...";

        await ExecuteApiAsync(
            async () =>
            {
                var refreshed = await _authService.RefreshTokenAsync();

                if (refreshed)
                {
                    SettingsStatusMessage = "Sesja została odświeżona.";
                    ShowToast("Token sesji został odświeżony.", ToastTypes.Success);
                    return;
                }

                SettingsStatusMessage = "Serwer nie zwrócił nowego tokenu (sprawdź POST /api/refresh).";
                ShowToast("Nie udało się odświeżyć sesji.", ToastTypes.Warning);
            },
            setStatusMessage: message => SettingsStatusMessage = message,
            unexpectedStatusMessage: "Nie udało się odświeżyć sesji.",
            unexpectedToastMessage: "Nie udało się odświeżyć sesji.");
    }
}
