using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed class SettingsPanelViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAuthService _authService;
    private readonly DashboardVmBridge _bridge;
    private readonly Func<Task> _refreshAuditWhenOnSettingsPage;

    private string _settingsStatusMessage = "Ustawienia gotowe.";
    private string _selectedThemeMode = "System";
    private string _selectedUiCulture = "pl";

    public SettingsPanelViewModel(
        ISettingsService settingsService,
        IAuthService authService,
        DashboardVmBridge bridge,
        Func<Task> refreshAuditWhenOnSettingsPage)
    {
        _settingsService = settingsService;
        _authService = authService;
        _bridge = bridge;
        _refreshAuditWhenOnSettingsPage = refreshAuditWhenOnSettingsPage;

        foreach (var mode in new[] { "System", "Light", "Dark" })
            ThemeModes.Add(mode);

        foreach (var culture in new[] { "pl", "en" })
            UiCultures.Add(culture);

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        RefreshSessionCommand = new AsyncRelayCommand(RefreshSessionAsync);
    }

    public ObservableCollection<string> ThemeModes { get; } = new();

    public ObservableCollection<string> UiCultures { get; } = new();

    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => SetProperty(ref _settingsStatusMessage, value);
    }

    public string SelectedThemeMode
    {
        get => _selectedThemeMode;
        set => SetProperty(ref _selectedThemeMode, value);
    }

    public string SelectedUiCulture
    {
        get => _selectedUiCulture;
        set => SetProperty(ref _selectedUiCulture, value);
    }

    public string LblSettingsTitle => AppStrings.Get("Settings_Title");

    public string LblSettingsSubtitle => AppStrings.Get("Settings_Subtitle");

    public string LblSettingsTheme => AppStrings.Get("Settings_Theme");

    public string LblSettingsLanguage => AppStrings.Get("Settings_Language");

    public string LblSettingsSave => AppStrings.Get("Settings_Save");

    public string LblSettingsRefreshSession => AppStrings.Get("Settings_RefreshSession");

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public IAsyncRelayCommand RefreshSessionCommand { get; }

    public void ApplyBootstrapFromSettings()
    {
        var appSettings = _settingsService.LoadSync();
        SelectedThemeMode = appSettings.ThemeMode;
        SelectedUiCulture = SettingsService.NormalizeUiCulture(appSettings.UiCulture);
    }

    public void NotifyLocalization()
    {
        OnPropertyChanged(nameof(LblSettingsTitle));
        OnPropertyChanged(nameof(LblSettingsSubtitle));
        OnPropertyChanged(nameof(LblSettingsTheme));
        OnPropertyChanged(nameof(LblSettingsLanguage));
        OnPropertyChanged(nameof(LblSettingsSave));
        OnPropertyChanged(nameof(LblSettingsRefreshSession));
    }

    private async Task SaveSettingsAsync()
    {
        await _bridge.ExecuteApiAsync(
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
                _bridge.NotifyLocalization();

                SettingsStatusMessage = string.Empty;

                await _bridge.LogAuditAsync("Zapis ustawień", null, "Zmieniono ustawienia aplikacji.");

                _bridge.ShowToast(AppStrings.Get("Toast_SettingsSaved"), ToastTypes.Success);

                if (_bridge.GetCurrentSection() == AppSections.Settings)
                    await _refreshAuditWhenOnSettingsPage();
            },
            unexpectedToastMessage: "Nie udało się zapisać ustawień.",
            showApiErrorToast: false);
    }

    private async Task RefreshSessionAsync()
    {
        SettingsStatusMessage = "Odświeżanie sesji...";

        await _bridge.ExecuteApiAsync(
            async () =>
            {
                var refreshed = await _authService.RefreshTokenAsync();

                if (refreshed)
                {
                    SettingsStatusMessage = "Sesja została odświeżona.";
                    _bridge.ShowToast("Token sesji został odświeżony.", ToastTypes.Success);
                    return;
                }

                SettingsStatusMessage = "Serwer nie zwrócił nowego tokenu (sprawdź POST /api/refresh).";
                _bridge.ShowToast("Nie udało się odświeżyć sesji.", ToastTypes.Warning);
            },
            setStatusMessage: message => SettingsStatusMessage = message,
            unexpectedStatusMessage: "Nie udało się odświeżyć sesji.",
            unexpectedToastMessage: "Nie udało się odświeżyć sesji.");
    }
}
