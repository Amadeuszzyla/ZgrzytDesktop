using System;

using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

/// <summary>
/// No-op diagnostic logger for tests and optional wiring.
/// </summary>
public sealed class NullLocalDiagnosticLogService : ILocalDiagnosticLogService
{
    public static readonly NullLocalDiagnosticLogService Instance = new();

    public void LogInfo(string message, Exception? exception = null)
    {
    }

    public void LogWarning(string message, Exception? exception = null)
    {
    }

    public void LogError(string message, Exception? exception = null)
    {
    }
}
