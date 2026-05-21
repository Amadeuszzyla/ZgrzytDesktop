using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Security;

namespace ZgrzytDesktop.Services;

public class LocalAuditLogService
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public LocalAuditLogService(string? customDirectory = null)
    {
        var directory = customDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            directory = Path.Combine(appData, "ZgrzytDesktop");
        }

        Directory.CreateDirectory(directory);

        _filePath = Path.Combine(directory, "audit-log.json");
    }

    public async Task AddAsync(AuditLogEntry entry)
    {
        try
        {
            var entries = await LoadAsync();
            entries.Add(entry);
            await SaveAsync(entries);
        }
        catch
        {
            // Brak zapisu audytu nie powinien blokować aplikacji.
        }
    }

    public async Task<List<AuditLogEntry>> LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<AuditLogEntry>();

            var stored = await File.ReadAllTextAsync(_filePath);
            var json = LocalDataProtector.UnprotectString(stored);

            if (string.IsNullOrWhiteSpace(json))
                return new List<AuditLogEntry>();

            return JsonSerializer.Deserialize<List<AuditLogEntry>>(json, _jsonOptions)
                   ?? new List<AuditLogEntry>();
        }
        catch
        {
            return new List<AuditLogEntry>();
        }
    }

    public async Task<List<AuditLogEntry>> LoadForTicketAsync(int ticketId)
    {
        try
        {
            var entries = await LoadAsync();

            return entries
                .Where(entry => entry.TicketId == ticketId)
                .OrderByDescending(entry => entry.Timestamp)
                .ToList();
        }
        catch
        {
            return new List<AuditLogEntry>();
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        catch
        {
            // Czyszczenie audytu nie może zatrzymać aplikacji.
        }

        await Task.CompletedTask;
    }

    private async Task SaveAsync(List<AuditLogEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, _jsonOptions);
        var protectedJson = LocalDataProtector.ProtectString(json);
        await File.WriteAllTextAsync(_filePath, protectedJson);
    }
}
