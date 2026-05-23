using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Cache;

public class LocalTicketCacheService : ILocalTicketCacheService
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public LocalTicketCacheService(string? customDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(customDirectory))
        {
            AppDataPaths.EnsureDirectoryForFile(AppDataPaths.TicketsCacheFilePath);
            _filePath = AppDataPaths.TicketsCacheFilePath;
            return;
        }

        Directory.CreateDirectory(customDirectory);
        _filePath = Path.Combine(customDirectory, "tickets-cache.json");
    }

    public async Task SaveTicketsAsync(IEnumerable<Ticket> tickets)
    {
        try
        {
            var json = JsonSerializer.Serialize(tickets, _jsonOptions);
            await SecureLocalFileStorage.WriteEncryptedAsync(_filePath, json);
        }
        catch
        {
            // Brak zapisu cache nie powinien blokować aplikacji.
        }
    }

    public async Task<List<Ticket>> LoadTicketsAsync()
    {
        try
        {
            var json = await SecureLocalFileStorage.ReadDecryptedAsync(
                _filePath,
                SecureLocalFileStorage.LooksLikeJsonDocument);

            if (string.IsNullOrWhiteSpace(json))
                return [];

            return JsonSerializer.Deserialize<List<Ticket>>(json, _jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public Task ClearAsync()
    {
        SecureLocalFileStorage.TryDelete(_filePath);
        return Task.CompletedTask;
    }
}
