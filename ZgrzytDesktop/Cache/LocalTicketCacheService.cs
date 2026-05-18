using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;

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
        var json = JsonSerializer.Serialize(tickets, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<List<Ticket>> LoadTicketsAsync()
    {
        if (!File.Exists(_filePath))
            return new List<Ticket>();

        var json = await File.ReadAllTextAsync(_filePath);

        if (string.IsNullOrWhiteSpace(json))
            return new List<Ticket>();

        return JsonSerializer.Deserialize<List<Ticket>>(json, _jsonOptions)
               ?? new List<Ticket>();
    }

    public Task ClearAsync()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);

        return Task.CompletedTask;
    }
}