namespace ZgrzytDesktop.Uninstaller;

public interface IRegistryUninstallReader
{
    string? TryGetUninstallString();
}
