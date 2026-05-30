using ZgrzytDesktop.Uninstaller;

namespace ZgrzytDesktop.Tests.Uninstaller;

public class InnoUninstallerServiceTests
{
    [Fact]
    public void GetDefaultInnoUninstallerPath_UsesLocalAppDataProgramsFolder()
    {
        var path = UninstallerPaths.GetDefaultInnoUninstallerPath(@"C:\Users\test\AppData\Local");

        Assert.Equal(
            @"C:\Users\test\AppData\Local\Programs\ZgrzytDesktop\unins000.exe",
            path);
    }

    [Fact]
    public void GetDefaultInnoUninstallerPath_RejectsEmptyLocalAppData()
    {
        Assert.Throws<ArgumentException>(() => UninstallerPaths.GetDefaultInnoUninstallerPath(""));
    }

    [Theory]
    [InlineData(@"""D:\Custom\ZgrzytDesktop\unins000.exe""", @"D:\Custom\ZgrzytDesktop\unins000.exe")]
    [InlineData(@"""D:\Custom\ZgrzytDesktop\unins000.exe"" /SILENT", @"D:\Custom\ZgrzytDesktop\unins000.exe")]
    [InlineData(@"C:\Apps\ZgrzytDesktop\unins000.exe /VERYSILENT", @"C:\Apps\ZgrzytDesktop\unins000.exe")]
    public void UninstallStringParser_ParsesQuotedAndUnquotedPaths(string uninstallString, string expectedPath)
    {
        var parsed = UninstallStringParser.TryParseExecutablePath(uninstallString, out var executablePath);

        Assert.True(parsed);
        Assert.Equal(expectedPath, executablePath);
    }

    [Fact]
    public void FindUninstallerPath_UsesRegistryPathWhenFileExists()
    {
        const string registryPath = @"D:\Custom\ZgrzytDesktop\unins000.exe";

        var locator = new InnoUninstallerLocator(
            getLocalApplicationData: () => @"C:\Users\test\AppData\Local",
            fileExists: path => string.Equals(path, registryPath, StringComparison.OrdinalIgnoreCase),
            registryReader: new FakeRegistryUninstallReader(
                $@"""{registryPath}"" /SILENT"));

        var path = locator.FindUninstallerPath();

        Assert.Equal(registryPath, path);
    }

    [Fact]
    public void FindUninstallerPath_FallsBackToLocalAppDataWhenRegistryMissing()
    {
        const string fallbackPath = @"C:\Users\test\AppData\Local\Programs\ZgrzytDesktop\unins000.exe";

        var locator = new InnoUninstallerLocator(
            getLocalApplicationData: () => @"C:\Users\test\AppData\Local",
            fileExists: path => string.Equals(path, fallbackPath, StringComparison.OrdinalIgnoreCase),
            registryReader: new FakeRegistryUninstallReader(null));

        var path = locator.FindUninstallerPath();

        Assert.Equal(fallbackPath, path);
    }

    [Fact]
    public void FindUninstallerPath_FallsBackWhenRegistryPathDoesNotExist()
    {
        const string registryPath = @"D:\Missing\ZgrzytDesktop\unins000.exe";
        const string fallbackPath = @"C:\Users\test\AppData\Local\Programs\ZgrzytDesktop\unins000.exe";

        var locator = new InnoUninstallerLocator(
            getLocalApplicationData: () => @"C:\Users\test\AppData\Local",
            fileExists: path => string.Equals(path, fallbackPath, StringComparison.OrdinalIgnoreCase),
            registryReader: new FakeRegistryUninstallReader($@"""{registryPath}"""));

        var path = locator.FindUninstallerPath();

        Assert.Equal(fallbackPath, path);
    }

    [Fact]
    public void FindUninstallerPath_ReturnsNullWhenRegistryAndFallbackMissing()
    {
        var locator = new InnoUninstallerLocator(
            getLocalApplicationData: () => @"C:\Users\test\AppData\Local",
            fileExists: _ => false,
            registryReader: new FakeRegistryUninstallReader(null));

        var path = locator.FindUninstallerPath();

        Assert.Null(path);
    }

    [Fact]
    public void Run_WhenUninstallerMissing_ShowsMessageAndReturnsNotFound()
    {
        var messages = new List<string>();
        var processStarts = 0;

        var service = new InnoUninstallerService(
            locator: new InnoUninstallerLocator(
                getLocalApplicationData: () => @"C:\Missing\AppData\Local",
                fileExists: _ => false,
                registryReader: new FakeRegistryUninstallReader(null)),
            startProcess: _ => processStarts++,
            notifier: new RecordingUserNotifier(messages));

        var exitCode = service.Run();

        Assert.Equal(InnoUninstallerService.ExitNotFound, exitCode);
        Assert.Equal(NativeMessageBox.NotFoundMessage, Assert.Single(messages));
        Assert.Equal(0, processStarts);
    }

    [Fact]
    public void Run_WhenUninstallerExists_StartsProcessAndReturnsSuccess()
    {
        const string fallbackPath = @"C:\Users\test\AppData\Local\Programs\ZgrzytDesktop\unins000.exe";
        var startedPaths = new List<string>();
        var messages = new List<string>();

        var service = new InnoUninstallerService(
            locator: new InnoUninstallerLocator(
                getLocalApplicationData: () => @"C:\Users\test\AppData\Local",
                fileExists: path => string.Equals(path, fallbackPath, StringComparison.OrdinalIgnoreCase),
                registryReader: new FakeRegistryUninstallReader(null)),
            startProcess: startedPaths.Add,
            notifier: new RecordingUserNotifier(messages));

        var exitCode = service.Run();

        Assert.Equal(InnoUninstallerService.ExitSuccess, exitCode);
        Assert.Equal(fallbackPath, Assert.Single(startedPaths));
        Assert.Empty(messages);
    }

    private sealed class FakeRegistryUninstallReader(string? uninstallString) : IRegistryUninstallReader
    {
        public string? TryGetUninstallString() => uninstallString;
    }

    private sealed class RecordingUserNotifier(List<string> messages) : IUserNotifier
    {
        public void ShowNotFound() => messages.Add(NativeMessageBox.NotFoundMessage);
    }
}
