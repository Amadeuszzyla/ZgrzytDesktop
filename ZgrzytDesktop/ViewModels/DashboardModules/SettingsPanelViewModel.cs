using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed class SettingsPanelViewModel : ViewModelBase
{
    public const string LightThemeMode = "Light";

    private readonly ISettingsService _settingsService;
    private readonly IAuthService _authService;
    private readonly IDashboardContext _context;
    private readonly Func<Task> _refreshAuditWhenOnSettingsPage;
    private readonly Action<bool, int>? _applyAutoLogoutSettings;

    private string _settingsStatusMessage = AppStrings.Get("Settings_StatusReady");
    private string _selectedUiCulture = "pl";

    public SettingsPanelViewModel(
        ISettingsService settingsService,
        IAuthService authService,
        IDashboardContext context,
        Func<Task> refreshAuditWhenOnSettingsPage,
        Action<bool, int>? applyAutoLogoutSettings = null)
    {
        _settingsService = settingsService;
        _authService = authService;
        _context = context;
        _refreshAuditWhenOnSettingsPage = refreshAuditWhenOnSettingsPage;
        _applyAutoLogoutSettings = applyAutoLogoutSettings;

        foreach (var culture in new[] { "pl", "en" })
            UiCultures.Add(culture);

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        RefreshSessionCommand = new AsyncRelayCommand(RefreshSessionAsync);
    }

    public ObservableCollection<string> UiCultures { get; } = new();

    public string SelectedThemeMode => LightThemeMode;

    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => SetProperty(ref _settingsStatusMessage, value);
    }

    public string SelectedUiCulture
    {
        get => _selectedUiCulture;
        set => SetProperty(ref _selectedUiCulture, value);
    }

    public string LblSettingsTitle => AppStrings.Get("Settings_Title");

    public string LblSettingsSubtitle => AppStrings.Get("Settings_Subtitle");

    public string LblSettingsLanguage => AppStrings.Get("Settings_Language");

    public string LblSettingsSave => AppStrings.Get("Settings_Save");

    public string LblSettingsRefreshSession => AppStrings.Get("Settings_RefreshSession");

    public string LblAuditUserFormat => AppStrings.Get("Audit_User");

    public string LblAuditTicketFormat => AppStrings.Get("Audit_Ticket");

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public IAsyncRelayCommand RefreshSessionCommand { get; }

    public void ApplyBootstrapFromSettings()
    {
        var appSettings = _settingsService.LoadSync();
        SelectedUiCulture = SettingsService.NormalizeUiCulture(appSettings.UiCulture);
        SettingsService.ApplyThemeMode(LightThemeMode);
        _applyAutoLogoutSettings?.Invoke(
            appSettings.AutoLogoutEnabled,
            SessionInactivityMonitor.NormalizeTimeout(appSettings.AutoLogoutTimeoutMinutes));
    }

    public void NotifyLocalization()
    {
        OnPropertyChanged(nameof(LblSettingsTitle));
        OnPropertyChanged(nameof(LblSettingsSubtitle));
        OnPropertyChanged(nameof(LblSettingsLanguage));
        OnPropertyChanged(nameof(LblSettingsSave));
        OnPropertyChanged(nameof(LblSettingsRefreshSession));
        OnPropertyChanged(nameof(LblAuditUserFormat));
        OnPropertyChanged(nameof(LblAuditTicketFormat));

        if (string.IsNullOrWhiteSpace(SettingsStatusMessage))
            SettingsStatusMessage = AppStrings.Get("Settings_StatusReady");
    }

    private async Task SaveSettingsAsync()
    {
        await _context.ExecuteApiAsync(
            async () =>
            {
                var existing = await _settingsService.LoadAsync();

                var settings = new AppSettings
                {
                    ApiBaseUrl = existing.ApiBaseUrl,
                    ThemeMode = LightThemeMode,
                    UiCulture = SettingsService.NormalizeUiCulture(SelectedUiCulture),
                    AutoLogoutEnabled = existing.AutoLogoutEnabled,
                    AutoLogoutTimeoutMinutes = SessionInactivityMonitor.NormalizeTimeout(
                        existing.AutoLogoutTimeoutMinutes)
                };

                await _settingsService.SaveAsync(settings);

                SelectedUiCulture = settings.UiCulture;
                SettingsService.ApplyThemeMode(LightThemeMode);
                AppStrings.ApplyCulture(settings.UiCulture);
                _context.NotifyLocalization();

                SettingsStatusMessage = string.Empty;

                await _context.LogAuditAsync(
                    "SettingsSaved",
                    null,
                    "Audit_Desc_SettingsSaved",
                    null);

                _context.ShowToastKey("Toast_SettingsSaved", ToastTypes.Success);

                if (_context.CurrentSection == AppSections.Settings)
                    await _refreshAuditWhenOnSettingsPage();
            },
            unexpectedToastMessageKey: "Toast_SettingsSaveFailed",
            showApiErrorToast: false);
    }

    private async Task RefreshSessionAsync()
    {
        SettingsStatusMessage = AppStrings.Get("Settings_StatusSessionRefreshing");

        await _context.ExecuteApiAsync(
            async () =>
            {
                var refreshed = await _authService.RefreshTokenAsync();

                if (refreshed)
                {
                    SettingsStatusMessage = AppStrings.Get("Settings_StatusSessionRefreshed");
                    _context.ShowToastKey("Toast_SessionRefreshed", ToastTypes.Success);
                    return;
                }

                SettingsStatusMessage = AppStrings.Get("Settings_StatusSessionNoToken");
                _context.ShowToastKey("Toast_SessionRefreshFailed", ToastTypes.Warning);
            },
            setStatusMessage: message => SettingsStatusMessage = message,
            unexpectedStatusMessageKey: "Settings_StatusSessionRefreshFailed",
            unexpectedToastMessageKey: "Toast_SessionRefreshFailed");
    }

}
