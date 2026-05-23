using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeSettingsService : ISettingsService
{
    public AppSettings Settings { get; } = new()
    {
        ApiBaseUrl = "http://127.0.0.1:9000/api/",
        ThemeMode = "Light",
        UiCulture = "pl"
    };

    public int SaveAsyncCallCount { get; private set; }

    public AppSettings LoadSync() => Settings;

    public Task<AppSettings> LoadAsync() => Task.FromResult(Settings);

    public void SaveSync(AppSettings settings)
    {
        Settings.ApiBaseUrl = settings.ApiBaseUrl;
        Settings.ThemeMode = settings.ThemeMode;
        Settings.UiCulture = settings.UiCulture;
    }

    public Task SaveAsync(AppSettings settings)
    {
        SaveAsyncCallCount++;
        SaveSync(settings);
        return Task.CompletedTask;
    }

    public string NormalizeApiBaseUrl(string apiBaseUrl) =>
        new SettingsService(Guid.NewGuid().ToString()).NormalizeApiBaseUrl(apiBaseUrl);

    public string NormalizeThemeMode(string? themeMode) =>
        new SettingsService(Guid.NewGuid().ToString()).NormalizeThemeMode(themeMode);
}
