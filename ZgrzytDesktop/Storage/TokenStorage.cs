using System;
using System.IO;
using System.Threading.Tasks;
using ZgrzytDesktop.Security;

namespace ZgrzytDesktop.Storage;

public class TokenStorage
{
    private readonly string _filePath;

    public TokenStorage()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "ZgrzytDesktop");

        Directory.CreateDirectory(directory);

        _filePath = Path.Combine(directory, "token.txt");
    }

    public async Task SaveTokenAsync(string token)
    {
        var protectedToken = LocalDataProtector.ProtectString(token);
        await File.WriteAllTextAsync(_filePath, protectedToken);
    }

    public string? LoadTokenSync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var stored = File.ReadAllText(_filePath);
            var token = LocalDataProtector.UnprotectString(stored);

            return string.IsNullOrWhiteSpace(token) ? null : token;
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
            var token = LocalDataProtector.UnprotectString(stored);

            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
        catch
        {
            return null;
        }
    }

    public Task ClearTokenAsync()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        catch
        {
            // Usunięcie tokena nie może zatrzymać aplikacji.
        }

        return Task.CompletedTask;
    }
}
