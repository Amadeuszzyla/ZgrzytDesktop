using System;
using System.IO;

namespace ZgrzytDesktop.Security;

/// <summary>
/// Per-user application data under %AppData% — never the publish folder or current working directory.
/// </summary>
public static class AppDataPaths
{
    public static string RootDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZgrzytDesktop");

    public static string CacheDirectory => Path.Combine(RootDirectory, "Cache");

    public static string SettingsDirectory => Path.Combine(RootDirectory, "Settings");

    public static string TokenFilePath => Path.Combine(RootDirectory, "token.txt");

    public static string AuditLogFilePath => Path.Combine(RootDirectory, "audit-log.json");

    public static string TicketsCacheFilePath => Path.Combine(CacheDirectory, "tickets-cache.json");

    public static string UserCacheFilePath => Path.Combine(CacheDirectory, "user-cache.json");

    public static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

    public static void EnsureDirectoryForFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}
