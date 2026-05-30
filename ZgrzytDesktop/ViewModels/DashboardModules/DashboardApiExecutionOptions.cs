using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Exceptions;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Optional behavior for <see cref="IDashboardContext.ExecuteApiAsync"/>.
/// </summary>
public sealed class DashboardApiExecutionOptions
{
    /// <summary>Updates panel status text when an API or unexpected error occurs.</summary>
    public Action<string>? SetStatusMessage { get; init; }

    /// <summary>AppStrings key for unexpected-error status text.</summary>
    public string? UnexpectedStatusMessageKey { get; init; }

    /// <summary>AppStrings key for unexpected-error toast.</summary>
    public string? UnexpectedToastMessageKey { get; init; }

    /// <summary>AppStrings key for offline / service-unavailable toast.</summary>
    public string? OfflineToastMessageKey { get; init; }

    /// <summary>When true, sets offline mode on HTTP 503 (unless overridden by <see cref="OnServiceUnavailableAsync"/>).</summary>
    public bool SetOfflineOnServiceUnavailable { get; init; } = true;

    /// <summary>Whether API errors should show an error toast.</summary>
    public bool ShowApiErrorToast { get; init; } = true;

    /// <summary>Custom handler for HTTP 503; when set, default offline handling is skipped.</summary>
    public Func<ApiException, Task>? OnServiceUnavailableAsync { get; init; }
}
