using System.Diagnostics.CodeAnalysis;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Tests.Infrastructure;

internal sealed class IntegrationTestSettings
{
    public required string ApiBaseUrl { get; init; }

    public required string Login { get; init; }

    public required string Password { get; init; }
}

internal static class IntegrationTestEnvironment
{
    public const string SkipReason =
        "Set environment variables ZGRZYT_API_URL, ZGRZYT_LOGIN, and ZGRZYT_PASSWORD to run live API integration tests.";

    public static bool IsConfigured => TryGetSettings(out _);

    public static bool TryGetSettings([NotNullWhen(true)] out IntegrationTestSettings? settings)
    {
        var apiUrl = Environment.GetEnvironmentVariable("ZGRZYT_API_URL");
        var login = Environment.GetEnvironmentVariable("ZGRZYT_LOGIN");
        var password = Environment.GetEnvironmentVariable("ZGRZYT_PASSWORD");

        if (string.IsNullOrWhiteSpace(apiUrl) ||
            string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(password))
        {
            settings = null;
            return false;
        }

        var normalizer = new SettingsService(
            Path.Combine(Path.GetTempPath(), "ZgrzytDesktop.Integration", Guid.NewGuid().ToString("N")));

        settings = new IntegrationTestSettings
        {
            ApiBaseUrl = normalizer.NormalizeApiBaseUrl(apiUrl),
            Login = login.Trim(),
            Password = password
        };

        return true;
    }
}
