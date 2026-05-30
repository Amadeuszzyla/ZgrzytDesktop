using System;

using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Diagnostics;

/// <summary>
/// Static bridge for components that cannot receive DI (e.g. fire-and-forget helpers).
/// </summary>
internal static class DiagnosticLogBridge
{
    public static ILocalDiagnosticLogService? Service { get; set; }

    public static void LogInfo(string message, Exception? exception = null)
    {
        try
        {
            Service?.LogInfo(message, exception);
        }
        catch
        {
            // Bridge must not crash callers.
        }
    }

    public static void LogWarning(string message, Exception? exception = null)
    {
        try
        {
            Service?.LogWarning(message, exception);
        }
        catch
        {
            // Bridge must not crash callers.
        }
    }

    public static void LogError(string message, Exception? exception = null)
    {
        try
        {
            Service?.LogError(message, exception);
        }
        catch
        {
            // Bridge must not crash callers.
        }
    }
}
