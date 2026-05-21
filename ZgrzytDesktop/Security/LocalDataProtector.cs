using System;
using System.Security.Cryptography;
using System.Text;

namespace ZgrzytDesktop.Security;

public static class LocalDataProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ZgrzytDesktop.LocalData.v1");

    public static string ProtectString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = ProtectedData.Protect(
                plainBytes,
                Entropy,
                DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(protectedBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string? UnprotectString(string protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
            return null;

        try
        {
            var protectedBytes = Convert.FromBase64String(protectedText);
            var plainBytes = ProtectedData.Unprotect(
                protectedBytes,
                Entropy,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return null;
        }
    }
}
