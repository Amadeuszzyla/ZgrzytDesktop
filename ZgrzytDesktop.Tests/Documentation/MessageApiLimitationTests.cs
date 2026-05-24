using Xunit;

namespace ZgrzytDesktop.Tests.Documentation;

public class MessageApiLimitationTests
{
    [Fact]
    public void Documentation_MentionsMessageEditDeleteRequiresBackendEndpoint()
    {
        var requirements = File.ReadAllText(
            Path.Combine(TestPaths.RepoRoot, "REQUIREMENTS.md"));
        var readme = File.ReadAllText(
            Path.Combine(TestPaths.RepoRoot, "README.md"));

        Assert.Contains("edycja", requirements, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("usuwanie", requirements, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("endpoint", requirements, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("messages", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("edycji", readme, StringComparison.OrdinalIgnoreCase);
    }
}

internal static class TestPaths
{
    public static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
