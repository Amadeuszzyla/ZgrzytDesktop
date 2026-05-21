using System;
using System.IO;
using System.Threading.Tasks;
using ZgrzytDesktop.Storage;
using Xunit;

namespace ZgrzytDesktop.Tests.Storage;

public class TokenStorageTests
{
    [Fact]
    public async Task GetTokenAsync_MigratesLegacyPlaintextToken()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var legacyToken = "legacy-plain-token-value";
        await File.WriteAllTextAsync(Path.Combine(directory, "token.txt"), legacyToken);

        var storage = new TokenStorage(directory);
        var token = await storage.GetTokenAsync();

        Assert.Equal(legacyToken, token);

        var storedAfterMigration = await File.ReadAllTextAsync(Path.Combine(directory, "token.txt"));
        Assert.NotEqual(legacyToken, storedAfterMigration.Trim());
        Assert.NotEqual(token, storedAfterMigration.Trim());
    }
}
