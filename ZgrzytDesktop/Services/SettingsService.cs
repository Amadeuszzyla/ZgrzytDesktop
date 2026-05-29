using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

/// <summary>
/// Persists non-sensitive UI preferences (<see cref="AppSettings"/>).
/// Stored as plaintext JSON — no passwords or tokens. Sensitive data uses
/// <see cref="SecureLocalFileStorage"/> elsewhere (token, cache, audit).
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsService(string? customDirectory = null)
    {
        var directory = customDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            AppDataPaths.EnsureDirectoryForFile(AppDataPaths.SettingsFilePath);
            _filePath = AppDataPaths.SettingsFilePath;
            return;
        }

        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "settings.json");
    }

    public AppSettings LoadSync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                var defaultSettings = new AppSettings();
                SaveSync(defaultSettings);
                return defaultSettings;
            }

            var json = File.ReadAllText(_filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                var defaultSettings = new AppSettings();
                SaveSync(defaultSettings);
                return defaultSettings;
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
                           ?? new AppSettings();

            settings.ApiBaseUrl = NormalizeApiBaseUrl(settings.ApiBaseUrl);
            settings.ThemeMode = NormalizeThemeMode(settings.ThemeMode);
            settings.UiCulture = NormalizeUiCulture(settings.UiCulture);
            settings.AutoLogoutEnabled = settings.AutoLogoutEnabled;
            settings.AutoLogoutTimeoutMinutes = SessionInactivityMonitor.NormalizeTimeout(settings.AutoLogoutTimeoutMinutes);

            return settings;
        }
        catch
        {
            return new AppSettings
            {
                ApiBaseUrl = ApiDefaults.ProductionApiBaseUrl,
                ThemeMode = "Light"
            };
        }
    }

    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                var defaultSettings = new AppSettings();
                await SaveAsync(defaultSettings);
                return defaultSettings;
            }

            var json = await File.ReadAllTextAsync(_filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                var defaultSettings = new AppSettings();
                await SaveAsync(defaultSettings);
                return defaultSettings;
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
                           ?? new AppSettings();

            settings.ApiBaseUrl = NormalizeApiBaseUrl(settings.ApiBaseUrl);
            settings.ThemeMode = NormalizeThemeMode(settings.ThemeMode);
            settings.UiCulture = NormalizeUiCulture(settings.UiCulture);
            settings.AutoLogoutEnabled = settings.AutoLogoutEnabled;
            settings.AutoLogoutTimeoutMinutes = SessionInactivityMonitor.NormalizeTimeout(settings.AutoLogoutTimeoutMinutes);

            return settings;
        }
        catch
        {
            return new AppSettings
            {
                ApiBaseUrl = ApiDefaults.ProductionApiBaseUrl,
                ThemeMode = "Light"
            };
        }
    }

    public void SaveSync(AppSettings settings)
    {
        try
        {
            settings.ApiBaseUrl = NormalizeApiBaseUrl(settings.ApiBaseUrl);
            settings.ThemeMode = NormalizeThemeMode(settings.ThemeMode);
            settings.UiCulture = NormalizeUiCulture(settings.UiCulture);
            settings.AutoLogoutTimeoutMinutes = SessionInactivityMonitor.NormalizeTimeout(settings.AutoLogoutTimeoutMinutes);

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Brak zapisu ustawień nie powinien blokować aplikacji.
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            settings.ApiBaseUrl = NormalizeApiBaseUrl(settings.ApiBaseUrl);
            settings.ThemeMode = NormalizeThemeMode(settings.ThemeMode);
            settings.UiCulture = NormalizeUiCulture(settings.UiCulture);
            settings.AutoLogoutTimeoutMinutes = SessionInactivityMonitor.NormalizeTimeout(settings.AutoLogoutTimeoutMinutes);

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
            // Brak zapisu ustawień nie powinien blokować aplikacji.
        }
    }

    public string NormalizeApiBaseUrl(string apiBaseUrl)
    {
        if (ApiDefaults.ShouldMigrateToProduction(apiBaseUrl))
            return ApiDefaults.ProductionApiBaseUrl;

        var normalized = apiBaseUrl.Trim();

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "https://" + normalized;
        }

        if (!normalized.EndsWith('/'))
            normalized += "/";

        if (!normalized.EndsWith("api/", StringComparison.OrdinalIgnoreCase))
            normalized += "api/";

        normalized = normalized.Replace("api/api/", "api/", StringComparison.OrdinalIgnoreCase);

        return ApiUrlSecurityHelper.EnsureSecureApiBaseUrl(normalized);
    }

    public string NormalizeThemeMode(string? themeMode) => "Light";

    public static string NormalizeUiCulture(string? uiCulture)
    {
        if (string.Equals(uiCulture, "en", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(uiCulture, "en-US", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        return "pl";
    }

    public static void ApplyThemeMode(string? themeMode)
    {
        if (Application.Current is null)
            return;

        void Apply() =>
            Application.Current!.RequestedThemeVariant = ThemeVariant.Light;

        if (Dispatcher.UIThread.CheckAccess())
            Apply();
        else
            Dispatcher.UIThread.Invoke(Apply);
    }
}