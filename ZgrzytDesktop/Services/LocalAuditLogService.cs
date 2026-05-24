using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using System.Text.Json;

using System.Threading.Tasks;

using ZgrzytDesktop.Models;

using ZgrzytDesktop.Security;

using ZgrzytDesktop.Services.Interfaces;



namespace ZgrzytDesktop.Services;



public class LocalAuditLogService : ILocalAuditLogService

{

    private readonly string _filePath;



    private readonly JsonSerializerOptions _jsonOptions = new()

    {

        WriteIndented = true,

        PropertyNameCaseInsensitive = true

    };



    public LocalAuditLogService(string? customDirectory = null)

    {

        if (string.IsNullOrWhiteSpace(customDirectory))

        {

            AppDataPaths.EnsureDirectoryForFile(AppDataPaths.AuditLogFilePath);

            _filePath = AppDataPaths.AuditLogFilePath;

            return;

        }



        Directory.CreateDirectory(customDirectory);

        _filePath = Path.Combine(customDirectory, "audit-log.json");

    }



    public async Task AddAsync(AuditLogEntry entry)

    {

        try

        {

            entry = SanitizeEntry(entry);

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

            var json = await SecureLocalFileStorage.ReadDecryptedAsync(

                _filePath,

                SecureLocalFileStorage.LooksLikeJsonDocument);



            if (string.IsNullOrWhiteSpace(json))

                return [];



            return JsonSerializer.Deserialize<List<AuditLogEntry>>(json, _jsonOptions) ?? [];

        }

        catch

        {

            return [];

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

            return [];

        }

    }



    /// <summary>Test/service cleanup only — not exposed through UI.</summary>

    public async Task ClearAsync()

    {

        SecureLocalFileStorage.TryDelete(_filePath);

        await Task.CompletedTask;

    }



    private static AuditLogEntry SanitizeEntry(AuditLogEntry entry) =>
        new()
        {
            Timestamp = entry.Timestamp,
            Action = SensitiveDataMasker.Mask(entry.Action),
            UserLogin = SensitiveDataMasker.Mask(entry.UserLogin),
            TicketId = entry.TicketId,
            DetailsKey = entry.DetailsKey,
            ParametersJson = entry.ParametersJson is null
                ? null
                : SensitiveDataMasker.Mask(entry.ParametersJson),
            Description = SensitiveDataMasker.Mask(entry.Description)
        };

    private async Task SaveAsync(List<AuditLogEntry> entries)

    {

        var json = JsonSerializer.Serialize(entries, _jsonOptions);

        await SecureLocalFileStorage.WriteEncryptedAsync(_filePath, json);

    }

}


