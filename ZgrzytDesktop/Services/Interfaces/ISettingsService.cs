using System.Threading.Tasks;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services.Interfaces;

public interface ISettingsService
{
    AppSettings LoadSync();

    Task<AppSettings> LoadAsync();

    void SaveSync(AppSettings settings);

    Task SaveAsync(AppSettings settings);

    string NormalizeApiBaseUrl(string apiBaseUrl);

    string NormalizeThemeMode(string? themeMode);
}
