using Microsoft.Win32;

namespace ZgrzytDesktop.Uninstaller;

public sealed class WindowsRegistryUninstallReader : IRegistryUninstallReader
{
    private const string UninstallSubKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private static readonly RegistryHive[] Hives = [RegistryHive.CurrentUser, RegistryHive.LocalMachine];

    public string? TryGetUninstallString()
    {
        foreach (var hive in Hives)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            var uninstallString = TryGetUninstallStringFromHive(baseKey);
            if (!string.IsNullOrWhiteSpace(uninstallString))
                return uninstallString;
        }

        return null;
    }

    private static string? TryGetUninstallStringFromHive(RegistryKey hiveBase)
    {
        using var uninstallKey = hiveBase.OpenSubKey(UninstallSubKeyPath);
        if (uninstallKey is null)
            return null;

        var appIdKeyName = $"{UninstallerConstants.AppId}{UninstallerConstants.InnoRegistryKeySuffix}";
        var fromAppId = ReadUninstallString(uninstallKey, appIdKeyName);
        if (!string.IsNullOrWhiteSpace(fromAppId))
            return fromAppId;

        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
        {
            using var subKey = uninstallKey.OpenSubKey(subKeyName);
            if (subKey is null)
                continue;

            var displayName = subKey.GetValue("DisplayName") as string;
            if (!string.Equals(displayName, UninstallerConstants.DisplayName, StringComparison.OrdinalIgnoreCase))
                continue;

            var uninstallString = subKey.GetValue("UninstallString") as string;
            if (!string.IsNullOrWhiteSpace(uninstallString))
                return uninstallString;
        }

        return null;
    }

    private static string? ReadUninstallString(RegistryKey uninstallKey, string subKeyName)
    {
        using var appKey = uninstallKey.OpenSubKey(subKeyName);
        return appKey?.GetValue("UninstallString") as string;
    }
}
