using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;

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
        var json = JsonSerializer.Serialize(user, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<User?> LoadUserAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        var json = await File.ReadAllTextAsync(_filePath);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<User>(json, _jsonOptions);
    }

    public Task ClearAsync()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);

        return Task.CompletedTask;
    }
}