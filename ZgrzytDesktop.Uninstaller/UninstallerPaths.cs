namespace ZgrzytDesktop.Uninstaller;

public static class UninstallerPaths
{
    public const string InstallFolderName = "ZgrzytDesktop";
    public const string InnoUninstallerFileName = "unins000.exe";

    public static string GetDefaultInnoUninstallerPath(string localApplicationData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);

        return Path.Combine(
            localApplicationData,
            "Programs",
            InstallFolderName,
            InnoUninstallerFileName);
    }
}
