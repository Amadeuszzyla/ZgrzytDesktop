using System.Diagnostics;

namespace ZgrzytDesktop.Uninstaller;

public sealed class InnoUninstallerService
{
    public const int ExitSuccess = 0;
    public const int ExitNotFound = 2;

    private readonly InnoUninstallerLocator _locator;
    private readonly Action<string> _startProcess;
    private readonly IUserNotifier _notifier;

    public InnoUninstallerService(
        InnoUninstallerLocator? locator = null,
        Action<string>? startProcess = null,
        IUserNotifier? notifier = null)
    {
        _locator = locator ?? new InnoUninstallerLocator();
        _startProcess = startProcess ?? StartUninstallerProcess;
        _notifier = notifier ?? new NativeMessageBox();
    }

    public int Run()
    {
        var uninstallerPath = _locator.FindUninstallerPath();
        if (uninstallerPath is null)
        {
            _notifier.ShowNotFound();
            return ExitNotFound;
        }

        _startProcess(uninstallerPath);
        return ExitSuccess;
    }

    private static void StartUninstallerProcess(string uninstallerPath)
    {
        _ = Process.Start(new ProcessStartInfo
        {
            FileName = uninstallerPath,
            UseShellExecute = true
        }) ?? throw new InvalidOperationException($"Could not start uninstaller: {uninstallerPath}");
    }
}
