using System;

namespace ZgrzytDesktop.Services.Interfaces;

public enum DiagnosticLogLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Technical diagnostic log for developers/support — separate from business audit trail.
/// </summary>
public interface ILocalDiagnosticLogService
{
    void LogInfo(string message, Exception? exception = null);

    void LogWarning(string message, Exception? exception = null);

    void LogError(string message, Exception? exception = null);
}
