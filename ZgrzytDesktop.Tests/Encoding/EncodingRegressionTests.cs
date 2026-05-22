using System.Text;
using Xunit;

namespace ZgrzytDesktop.Tests.Regression;

public class EncodingRegressionTests
{
    private static readonly char[] MojibakeMarkers = ['Å', 'Ä', 'Ã', '\uFFFD', 'Ĺ'];

    [Fact]
    public void ProjectTextAssets_ShouldNotContainMojibakeMarkers()
    {
        var projectRoot = FindProjectRoot();
        var scanRoots = new[]
        {
            Path.Combine(projectRoot, "ZgrzytDesktop", "Views"),
            Path.Combine(projectRoot, "ZgrzytDesktop", "Resources")
        };

        var offenders = new List<string>();

        foreach (var scanRoot in scanRoots)
        {
            if (!Directory.Exists(scanRoot))
                continue;

            foreach (var file in Directory.EnumerateFiles(scanRoot, "*.*", SearchOption.AllDirectories))
            {
                if (!file.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) &&
                    !file.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file, Encoding.UTF8);
                if (text.IndexOfAny(MojibakeMarkers) >= 0)
                    offenders.Add(Path.GetRelativePath(projectRoot, file));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Mojibake detected in: " + string.Join(", ", offenders));
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ZgrzytDesktop.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate solution root.");
    }
}
