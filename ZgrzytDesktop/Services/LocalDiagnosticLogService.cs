using System;
using System.IO;
using System.Text;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

public sealed class LocalDiagnosticLogService : ILocalDiagnosticLogService
{
    private readonly string _filePath;
    private readonly object _sync = new();
    private readonly Func<DateTime>? _utcNow;

    public LocalDiagnosticLogService(string? customLogFilePath = null, Func<DateTime>? utcNow = null)
    {
        _filePath = string.IsNullOrWhiteSpace(customLogFilePath)
            ? AppDataPaths.DiagnosticLogFilePath
            : customLogFilePath;
        _utcNow = utcNow;
    }

    internal string LogFilePath => _filePath;

    public void LogInfo(string message, Exception? exception = null) =>
        Write(DiagnosticLogLevel.Info, message, exception);

    public void LogWarning(string message, Exception? exception = null) =>
        Write(DiagnosticLogLevel.Warning, message, exception);

    public void LogError(string message, Exception? exception = null) =>
        Write(DiagnosticLogLevel.Error, message, exception);

    private void Write(DiagnosticLogLevel level, string message, Exception? exception)
    {
        try
        {
            var entry = FormatEntry(_utcNow?.Invoke() ?? DateTime.UtcNow, level, message, exception);

            lock (_sync)
            {
                AppDataPaths.EnsureDirectoryForFile(_filePath);
                File.AppendAllText(_filePath, entry, Encoding.UTF8);
            }
        }
        catch
        {
            // Diagnostic logging must never crash the application.
        }
    }

    internal static string FormatEntry(
        DateTime timestampUtc,
        DiagnosticLogLevel level,
        string message,
        Exception? exception)
    {
        var sanitizedMessage = SensitiveDataMasker.Mask(message);
        var exceptionType = exception?.GetType().Name ?? "-";
        var exceptionMessage = exception is null
            ? "-"
            : SensitiveDataMasker.Mask(exception.Message);

        var builder = new StringBuilder();
        builder.Append(timestampUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"));
        builder.Append(" | ");
        builder.Append(level);
        builder.Append(" | ");
        builder.Append(sanitizedMessage);
        builder.Append(" | ");
        builder.Append(exceptionType);
        builder.Append(" | ");
        builder.AppendLine(exceptionMessage);

        if (!string.IsNullOrWhiteSpace(exception?.StackTrace))
        {
            builder.Append(SensitiveDataMasker.Mask(exception.StackTrace));
            builder.AppendLine();
        }

        builder.AppendLine();
        return builder.ToString();
    }
}
