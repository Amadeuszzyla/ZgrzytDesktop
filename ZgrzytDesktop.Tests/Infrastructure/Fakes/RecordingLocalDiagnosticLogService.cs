using System.Collections.Generic;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class RecordingLocalDiagnosticLogService : ILocalDiagnosticLogService
{
    public List<(DiagnosticLogLevel Level, string Message)> Entries { get; } = [];

    public void LogInfo(string message, Exception? exception = null) =>
        Entries.Add((DiagnosticLogLevel.Info, message));

    public void LogWarning(string message, Exception? exception = null) =>
        Entries.Add((DiagnosticLogLevel.Warning, message));

    public void LogError(string message, Exception? exception = null) =>
        Entries.Add((DiagnosticLogLevel.Error, message));
}
