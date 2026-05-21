using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Security;

namespace ZgrzytDesktop.Cache;

public class LocalUserCacheService
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public LocalUserCacheService(string? customDirectory = null)
    {
        var directory = customDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            directory = Path.Combine(appData, "ZgrzytDesktop", "Cache");
        }

        Directory.CreateDirectory(directory);

        _filePath = Path.Combine(directory, "user-cache.json");
    }

    public async Task SaveUserAsync(User user)
    {
        try
        {
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var protectedJson = LocalDataProtector.ProtectString(json);
            await File.WriteAllTextAsync(_filePath, protectedJson);
        }
        catch
        {
            // Brak zapisu cache nie powinien blokować aplikacji.
        }
    }

    public async Task<User?> LoadUserAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var stored = await File.ReadAllTextAsync(_filePath);
            var json = LocalDataProtector.UnprotectString(stored);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<User>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public Task ClearAsync()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        catch
        {
            // Czyszczenie cache nie może zatrzymać aplikacji.
        }

        return Task.CompletedTask;
    }
}
