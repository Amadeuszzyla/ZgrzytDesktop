using System;
using System.Threading.Tasks;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Shared dashboard services exposed to child panel view models (settings, statistics, …).
/// </summary>
public interface IDashboardContext
{
    bool IsOffline { get; set; }

    string CurrentSection { get; }

    Task<bool> ExecuteApiAsync(
        Func<Task> operation,
        DashboardApiExecutionOptions? options = null);

    void ShowToastKey(string resourceKey, string toastType, params object[] formatArgs);

    void ShowToast(string message, string toastType);

    Task LogAuditAsync(
        string actionKey,
        int? ticketId,
        string? detailsKey,
        object?[]? formatParameters = null);

    void NotifyLocalization();
}
