using System;
using System.IO;
using System.Threading.Tasks;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Storage;

public class TokenStorage : ITokenStorage
{
    private readonly string _filePath;

    public TokenStorage()
        : this(null)
    {
    }

    public TokenStorage(string? customDirectory)
    {
        if (string.IsNullOrWhiteSpace(customDirectory))
        {
            AppDataPaths.EnsureDirectoryForFile(AppDataPaths.TokenFilePath);
            _filePath = AppDataPaths.TokenFilePath;
            return;
        }

        Directory.CreateDirectory(customDirectory);
        _filePath = Path.Combine(customDirectory, "token.txt");
    }

    public async Task SaveTokenAsync(string token)
    {
        await SecureLocalFileStorage.WriteEncryptedAsync(_filePath, token);
    }

    public string? LoadTokenSync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var stored = File.ReadAllText(_filePath);
            return ParseStoredToken(stored, migrateLegacyPlaintext: true);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var stored = await File.ReadAllTextAsync(_filePath);
            return ParseStoredToken(stored, migrateLegacyPlaintext: true);
        }
        catch
        {
            return null;
        }
    }

    private string? ParseStoredToken(string stored, bool migrateLegacyPlaintext)
    {
        var trimmed = stored.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        var protectedToken = LocalDataProtector.UnprotectString(trimmed);

        if (!string.IsNullOrWhiteSpace(protectedToken))
            return protectedToken;

        if (!IsLikelyLegacyPlaintextToken(trimmed))
            return null;

        if (migrateLegacyPlaintext)
        {
            try
            {
                var protectedValue = LocalDataProtector.ProtectString(trimmed);

                if (!string.IsNullOrWhiteSpace(protectedValue))
                    File.WriteAllText(_filePath, protectedValue);
            }
            catch
            {
                // Migracja nie może zablokować logowania.
            }
        }

        return trimmed;
    }

    private static bool IsLikelyLegacyPlaintextToken(string value)
    {
        if (value.Length < 8)
            return false;

        try
        {
            var bytes = Convert.FromBase64String(value);

            // DPAPI blob jest zwykle dłuższy niż sam token JWT w plaintext.
            return bytes.Length < 48;
        }
        catch
        {
            return true;
        }
    }

    public Task ClearTokenAsync()
    {
        SecureLocalFileStorage.TryDelete(_filePath);
        return Task.CompletedTask;
    }
}
