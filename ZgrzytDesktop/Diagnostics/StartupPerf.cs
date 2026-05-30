using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Diagnostics;

/// <summary>
/// Lightweight startup timing probes written to <see cref="ILocalDiagnosticLogService"/> as Info.
/// </summary>
public static class StartupPerf
{
    private const string Prefix = "[StartupPerf]";

    private static readonly Stopwatch Total = Stopwatch.StartNew();
    private static readonly object Sync = new();
    private static readonly List<string> Buffer = [];

    private static ILocalDiagnosticLogService? _log;
    private static bool _startupComplete;
    private static bool _autoLoginFinished;
    private static bool _dashboardCreated;
    private static bool _initialTicketsLoadFinished;
    private static int _apiRequestCount;
    private const int MaxTrackedApiRequests = 12;

    public static bool IsTrackingApi => !_startupComplete;

    public static bool IsActive => !_startupComplete;

    public static void AttachLogger(ILocalDiagnosticLogService log)
    {
        lock (Sync)
        {
            _log = log;

            foreach (var message in Buffer)
                _log.LogInfo(message);

            Buffer.Clear();
        }

        Write("Logger attached");
    }

    public static IDisposable Measure(string step) => new Scope(step);

    public static void Log(string step, long elapsedMs, string? detail = null)
    {
        if (_startupComplete)
            return;
        var message = detail is null
            ? $"{Prefix} {step}: {elapsedMs} ms (total {Total.ElapsedMilliseconds} ms)"
            : $"{Prefix} {step}: {elapsedMs} ms — {detail} (total {Total.ElapsedMilliseconds} ms)";

        Write(message);
    }

    public static void TrackApiRequest(string method, string endpoint, long elapsedMs, int statusCode)
    {
        if (_startupComplete || _apiRequestCount >= MaxTrackedApiRequests)
            return;

        _apiRequestCount++;
        Log($"API {method} {SanitizeEndpoint(endpoint)}", elapsedMs, $"status={statusCode}");
    }

    public static void MarkDashboardCreated() => _dashboardCreated = true;

    public static void NotifyAutoLoginFinished()
    {
        _autoLoginFinished = true;

        if (!_dashboardCreated)
            TryComplete("auto-login finished (login screen)");
    }

    public static void NotifyInitialTicketsLoadFinished()
    {
        _initialTicketsLoadFinished = true;
        TryComplete("initial tickets load finished");
    }

    private static void TryComplete(string reason)
    {
        if (_startupComplete)
            return;

        if (!_autoLoginFinished)
            return;

        if (_dashboardCreated && !_initialTicketsLoadFinished)
            return;

        _startupComplete = true;
        Write($"{Prefix} Startup tracking complete: {Total.ElapsedMilliseconds} ms — {reason}");
    }

    private static string SanitizeEndpoint(string endpoint)
    {
        var normalized = endpoint.TrimStart('/').Split('?')[0];

        if (normalized.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[4..];

        return normalized;
    }

    private static void Write(string message)
    {
        lock (Sync)
        {
            if (_log is null)
            {
                Buffer.Add(message);
                return;
            }

            _log.LogInfo(message);
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly string _step;

        public Scope(string step) => _step = step;

        public void Dispose() => Log(_step, _stopwatch.ElapsedMilliseconds);
    }
}
