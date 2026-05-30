using Xunit;

namespace ZgrzytDesktop.Tests.Documentation;

public class MessageApiLimitationTests
{
    [Fact]
    public void Documentation_MentionsMessageEditDeleteRequiresBackendEndpoint()
    {
        var readme = File.ReadAllText(
            Path.Combine(TestPaths.RepoRoot, "README.md"));

        Assert.Contains("edycja", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("usuwanie", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("endpoint", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("messages", readme, StringComparison.OrdinalIgnoreCase);
    }
}

internal static class TestPaths
{
    public static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
