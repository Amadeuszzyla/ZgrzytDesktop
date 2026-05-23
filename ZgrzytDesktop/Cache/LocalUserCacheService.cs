using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Cache;

public class LocalUserCacheService : ILocalUserCacheService
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public LocalUserCacheService(string? customDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(customDirectory))
        {
            AppDataPaths.EnsureDirectoryForFile(AppDataPaths.UserCacheFilePath);
            _filePath = AppDataPaths.UserCacheFilePath;
            return;
        }

        Directory.CreateDirectory(customDirectory);
        _filePath = Path.Combine(customDirectory, "user-cache.json");
    }

    public async Task SaveUserAsync(User user)
    {
        try
        {
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            await SecureLocalFileStorage.WriteEncryptedAsync(_filePath, json);
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
            var json = await SecureLocalFileStorage.ReadDecryptedAsync(
                _filePath,
                SecureLocalFileStorage.LooksLikeJsonDocument);

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
        SecureLocalFileStorage.TryDelete(_filePath);
        return Task.CompletedTask;
    }
}
