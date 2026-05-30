namespace ZgrzytDesktop.Uninstaller;

public sealed class InnoUninstallerLocator
{
    private readonly Func<string> _getLocalApplicationData;
    private readonly Func<string, bool> _fileExists;
    private readonly IRegistryUninstallReader _registryReader;

    public InnoUninstallerLocator(
        Func<string>? getLocalApplicationData = null,
        Func<string, bool>? fileExists = null,
        IRegistryUninstallReader? registryReader = null)
    {
        _getLocalApplicationData = getLocalApplicationData
            ?? (() => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        _fileExists = fileExists ?? File.Exists;
        _registryReader = registryReader ?? new WindowsRegistryUninstallReader();
    }

    public string? FindUninstallerPath()
    {
        var fromRegistry = TryGetPathFromRegistry();
        if (fromRegistry is not null)
            return fromRegistry;

        var fallbackPath = UninstallerPaths.GetDefaultInnoUninstallerPath(_getLocalApplicationData());
        return _fileExists(fallbackPath) ? fallbackPath : null;
    }

    private string? TryGetPathFromRegistry()
    {
        var uninstallString = _registryReader.TryGetUninstallString();
        if (!UninstallStringParser.TryParseExecutablePath(uninstallString, out var executablePath)
            || string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        return _fileExists(executablePath) ? executablePath : null;
    }
}
