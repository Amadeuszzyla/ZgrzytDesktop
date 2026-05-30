using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Services;

public class LocalDiagnosticLogServiceTests
{
    [Fact]
    public void DefaultLogFilePath_IsUnderAppDataLogsDirectory()
    {
        var service = new LocalDiagnosticLogService();

        Assert.Equal(AppDataPaths.DiagnosticLogFilePath, service.LogFilePath);
        Assert.EndsWith(Path.Combine("ZgrzytDesktop", "Logs", "diagnostic.log"), service.LogFilePath);
    }

    [Fact]
    public void LogError_CreatesLogFile()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();
        var logPath = Path.Combine(directory, "diagnostic.log");

        try
        {
            var timestamp = new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc);
            var service = new LocalDiagnosticLogService(logPath, () => timestamp);

            service.LogError("Test diagnostic entry");

            Assert.True(File.Exists(logPath));
            var content = File.ReadAllText(logPath);
            Assert.Contains("2026-05-21T12:00:00.000Z", content);
            Assert.Contains("Error", content);
            Assert.Contains("Test diagnostic entry", content);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    [Fact]
    public void LogError_WritesExceptionDetails()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();
        var logPath = Path.Combine(directory, "diagnostic.log");

        try
        {
            var service = new LocalDiagnosticLogService(logPath);
            var exception = new InvalidOperationException("boom");

            service.LogError("Operation failed", exception);

            var content = File.ReadAllText(logPath);
            Assert.Contains("InvalidOperationException", content);
            Assert.Contains("boom", content);
            Assert.Contains(nameof(InvalidOperationException), content);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    [Fact]
    public void LogError_DoesNotWriteSecrets()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();
        var logPath = Path.Combine(directory, "diagnostic.log");

        try
        {
            var service = new LocalDiagnosticLogService(logPath);
            const string secretToken = "super-secret-token-value";
            const string secretPassword = "MySecretPassword123";
            var message = $"\"password\": \"{secretPassword}\", \"token\": \"{secretToken}\"";

            service.LogError(message, new InvalidOperationException(message));

            var content = File.ReadAllText(logPath);
            Assert.DoesNotContain(secretToken, content, StringComparison.Ordinal);
            Assert.DoesNotContain(secretPassword, content, StringComparison.Ordinal);
            Assert.Contains("[MASKED]", content, StringComparison.Ordinal);
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    [Fact]
    public void LogError_WhenWriteFails_DoesNotThrow()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();

        try
        {
            var service = new LocalDiagnosticLogService(directory);

            service.LogError("Should not crash", new InvalidOperationException("boom"));

            Assert.False(File.Exists(Path.Combine(directory, "diagnostic.log")));
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    [Fact]
    public void FormatEntry_IncludesLevelAndMessage()
    {
        var timestamp = new DateTime(2026, 5, 21, 8, 15, 30, 456, DateTimeKind.Utc);
        var exception = new ApiException(HttpStatusCode.InternalServerError, "Server failed");

        var entry = LocalDiagnosticLogService.FormatEntry(
            timestamp,
            DiagnosticLogLevel.Warning,
            "API request failed (500 InternalServerError)",
            exception);

        Assert.Contains("2026-05-21T08:15:30.456Z | Warning | API request failed (500 InternalServerError) | ApiException | Server failed", entry);
    }
}
