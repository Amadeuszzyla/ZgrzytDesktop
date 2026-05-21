using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services;

public class SettingsService
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
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            directory = Path.Combine(appData, "ZgrzytDesktop", "Settings");
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

            return settings;
        }
        catch
        {
            return new AppSettings
            {
                ApiBaseUrl = "http://127.0.0.1:9000/api/",
                ThemeMode = "System"
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

            return settings;
        }
        catch
        {
            return new AppSettings
            {
                ApiBaseUrl = "http://127.0.0.1:9000/api/",
                ThemeMode = "System"
            };
        }
    }

    public void SaveSync(AppSettings settings)
    {
        try
        {
            settings.ApiBaseUrl = NormalizeApiBaseUrl(settings.ApiBaseUrl);
            settings.ThemeMode = NormalizeThemeMode(settings.ThemeMode);

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
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return "http://127.0.0.1:9000/api/";

        var normalized = apiBaseUrl.Trim();

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "http://" + normalized;
        }

        if (!normalized.EndsWith('/'))
            normalized += "/";

        if (!normalized.EndsWith("api/", StringComparison.OrdinalIgnoreCase))
        {
            normalized += "api/";
        }

        return normalized;
    }

    public string NormalizeThemeMode(string? themeMode)
    {
        if (string.Equals(themeMode, "Light", StringComparison.OrdinalIgnoreCase))
            return "Light";

        if (string.Equals(themeMode, "Dark", StringComparison.OrdinalIgnoreCase))
            return "Dark";

        return "System";
    }

    public static void ApplyThemeMode(string? themeMode)
    {
        if (Application.Current is null)
            return;

        Application.Current.RequestedThemeVariant = themeMode switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}