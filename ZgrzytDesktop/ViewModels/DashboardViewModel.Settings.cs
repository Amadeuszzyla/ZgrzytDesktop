using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private async Task SaveSettingsAsync()
    {
        try
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
        }
        catch
        {
            ShowToast("Nie udało się zapisać ustawień.", "error");
        }
    }
    private async Task RefreshSessionAsync()
    {
        try
        {
            SettingsStatusMessage = "Odświeżanie sesji...";

            var refreshed = await _authService.RefreshTokenAsync();

            if (refreshed)
            {
                SettingsStatusMessage = "Sesja została odświeżona.";
                ShowToast("Token sesji został odświeżony.", "success");
                return;
            }

            SettingsStatusMessage = "Serwer nie zwrócił nowego tokenu (sprawdź POST /api/refresh).";
            ShowToast("Nie udało się odświeżyć sesji.", "warning");
        }
        catch (ApiException ex)
        {
            SettingsStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            SettingsStatusMessage = "Nie udało się odświeżyć sesji.";
            ShowToast("Nie udało się odświeżyć sesji.", "error");
        }
    }
}
