namespace ZgrzytDesktop.Tests.Infrastructure;

internal static class TestDirectoryHelper
{
    public static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "ZgrzytDesktopTests", Guid.NewGuid().ToString("N"));

    public static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
