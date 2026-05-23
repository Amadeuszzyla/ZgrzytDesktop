using System;
using System.Net;
using System.Threading.Tasks;
using ZgrzytDesktop.Exceptions;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed class AdminPanelCallbacks
{
    public required Action<string, string> ShowToast { get; init; }

    public required Func<bool> GetIsOffline { get; init; }

    public required Func<bool> GetIsAdminRole { get; init; }

    public required Func<bool> GetIsStaffRole { get; init; }

    public required Func<bool> GetCanUseOnlineActions { get; init; }

    public required Func<ApiException, string> GetApiErrorMessage { get; init; }

    public required Func<string, int?, string?, object?[]?, Task> LogAuditAsync { get; init; }

    public required Func<Func<Task>, Action<string>?, string?, string?, string?, bool, bool, Func<ApiException, Task>?, Task<bool>> ExecuteApiAsyncCore { get; init; }

    public Task<bool> ExecuteApiAsync(
        Func<Task> action,
        Action<string>? setStatusMessage = null,
        string? unexpectedStatusMessage = null,
        string? unexpectedToastMessage = null,
        string? offlineToastMessage = null,
        bool showApiErrorToast = true,
        bool setOfflineOnServiceUnavailable = true,
        Func<ApiException, Task>? onServiceUnavailableAsync = null) =>
        ExecuteApiAsyncCore(
            action,
            setStatusMessage,
            unexpectedStatusMessage,
            unexpectedToastMessage,
            offlineToastMessage,
            showApiErrorToast,
            setOfflineOnServiceUnavailable,
            onServiceUnavailableAsync);
}
