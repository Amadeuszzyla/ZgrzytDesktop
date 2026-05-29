using ZgrzytDesktop.Security;

namespace ZgrzytDesktop.Tests.Security;

public class SecureLocalFileStorageTests : IDisposable
{
    public void Dispose() => LocalDataProtector.SimulateProtectFailureForTests = false;

    [Fact]
    public async Task WriteEncryptedAsync_RoundTrip_PreservesData()
    {
        var directory = CreateTempDirectory();
        var filePath = Path.Combine(directory, "protected.txt");

        try
        {
            const string plainText = "secret-token-or-cache-payload";

            await SecureLocalFileStorage.WriteEncryptedAsync(filePath, plainText);

            var raw = await File.ReadAllTextAsync(filePath);
            Assert.False(string.IsNullOrWhiteSpace(raw));
            Assert.DoesNotContain(plainText, raw, StringComparison.Ordinal);

            var decrypted = await SecureLocalFileStorage.ReadDecryptedAsync(filePath);
            Assert.Equal(plainText, decrypted);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task WriteEncryptedAsync_WhenProtectionFails_DoesNotCreateEmptyFile()
    {
        var directory = CreateTempDirectory();
        var filePath = Path.Combine(directory, "protected.txt");

        try
        {
            LocalDataProtector.SimulateProtectFailureForTests = true;

            await Assert.ThrowsAsync<LocalDataProtectionException>(
                () => SecureLocalFileStorage.WriteEncryptedAsync(filePath, "secret-data"));

            Assert.False(File.Exists(filePath));
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task WriteEncryptedAsync_WhenProtectionFails_DoesNotOverwriteExistingFile()
    {
        var directory = CreateTempDirectory();
        var filePath = Path.Combine(directory, "protected.txt");

        try
        {
            await SecureLocalFileStorage.WriteEncryptedAsync(filePath, "original-data");
            var originalProtected = await File.ReadAllTextAsync(filePath);
            Assert.False(string.IsNullOrWhiteSpace(originalProtected));

            LocalDataProtector.SimulateProtectFailureForTests = true;

            await Assert.ThrowsAsync<LocalDataProtectionException>(
                () => SecureLocalFileStorage.WriteEncryptedAsync(filePath, "replacement-data"));

            var afterFailedWrite = await File.ReadAllTextAsync(filePath);
            Assert.Equal(originalProtected, afterFailedWrite);

            var decrypted = await SecureLocalFileStorage.ReadDecryptedAsync(filePath);
            Assert.Equal("original-data", decrypted);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static string CreateTempDirectory() =>
        Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Tests", Guid.NewGuid().ToString("N"));

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
