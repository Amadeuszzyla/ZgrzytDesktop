using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Security;

namespace ZgrzytDesktop.Cache;

public class LocalTicketCacheService
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public LocalTicketCacheService(string? customDirectory = null)
    {
        var directory = customDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            directory = Path.Combine(appData, "ZgrzytDesktop", "Cache");
        }

        Directory.CreateDirectory(directory);

        _filePath = Path.Combine(directory, "tickets-cache.json");
    }

    public async Task SaveTicketsAsync(IEnumerable<Ticket> tickets)
    {
        try
        {
            var json = JsonSerializer.Serialize(tickets, _jsonOptions);
            var protectedJson = LocalDataProtector.ProtectString(json);
            await File.WriteAllTextAsync(_filePath, protectedJson);
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
            if (!File.Exists(_filePath))
                return new List<Ticket>();

            var stored = await File.ReadAllTextAsync(_filePath);
            var json = LocalDataProtector.UnprotectString(stored);

            if (string.IsNullOrWhiteSpace(json))
                return new List<Ticket>();

            return JsonSerializer.Deserialize<List<Ticket>>(json, _jsonOptions)
                   ?? new List<Ticket>();
        }
        catch
        {
            return new List<Ticket>();
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
