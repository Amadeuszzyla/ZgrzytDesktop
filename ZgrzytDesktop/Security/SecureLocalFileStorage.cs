using System;
using System.IO;
using System.Threading.Tasks;
using ZgrzytDesktop.Diagnostics;

namespace ZgrzytDesktop.Security;

public static class SecureLocalFileStorage
{
    public static async Task WriteEncryptedAsync(string filePath, string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Cannot encrypt empty plaintext.", nameof(plainText));

        AppDataPaths.EnsureDirectoryForFile(filePath);

        try
        {
            var protectedText = LocalDataProtector.ProtectString(plainText);

            if (string.IsNullOrWhiteSpace(protectedText))
                throw new LocalDataProtectionException("Data protection produced no output.");

            await File.WriteAllTextAsync(filePath, protectedText);
        }
        catch (LocalDataProtectionException ex)
        {
            DiagnosticLogBridge.LogError("Local data protection failed during encrypted file write", ex);
            throw;
        }
        catch (Exception ex)
        {
            DiagnosticLogBridge.LogError("Encrypted local file write failed", ex);
            throw;
        }
    }

    public static async Task<string?> ReadDecryptedAsync(
        string filePath,
        Func<string, bool>? isLegacyPlaintext = null)
    {
        if (!File.Exists(filePath))
            return null;

        var stored = (await File.ReadAllTextAsync(filePath)).Trim();

        if (string.IsNullOrWhiteSpace(stored))
            return null;

        var decrypted = LocalDataProtector.UnprotectString(stored);

        if (!string.IsNullOrWhiteSpace(decrypted))
            return decrypted;

        if (isLegacyPlaintext?.Invoke(stored) == true)
        {
            try
            {
                await WriteEncryptedAsync(filePath, stored);
            }
            catch (Exception ex)
            {
                DiagnosticLogBridge.LogWarning("Failed to migrate legacy plaintext local file to encrypted storage", ex);
            }

            return stored;
        }

        DiagnosticLogBridge.LogWarning("Unable to decrypt local protected file");
        return null;
    }

    public static bool LooksLikeJsonDocument(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    public static void TryDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            // Usunięcie pliku nie może zatrzymać aplikacji.
        }
    }
}
