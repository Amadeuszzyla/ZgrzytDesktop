using System;
using System.IO;
using System.Threading.Tasks;

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
        await File.WriteAllTextAsync(_filePath, token);
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        var token = await File.ReadAllTextAsync(_filePath);

        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public Task ClearTokenAsync()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);

        return Task.CompletedTask;
    }
}