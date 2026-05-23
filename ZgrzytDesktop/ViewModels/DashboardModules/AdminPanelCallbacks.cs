using System;
using System.Net;
using System.Threading.Tasks;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed class AdminPanelCallbacks
{
    public required ToastKeyCallback ShowToastKey { get; init; }

    public required Action<string, string> ShowToastRaw { get; init; }

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
        string? unexpectedStatusMessageKey = null,
        string? unexpectedToastMessageKey = null,
        string? offlineToastMessageKey = null,
        bool showApiErrorToast = true,
        bool setOfflineOnServiceUnavailable = true,
        Func<ApiException, Task>? onServiceUnavailableAsync = null) =>
        ExecuteApiAsyncCore(
            action,
            setStatusMessage,
            unexpectedStatusMessageKey,
            unexpectedToastMessageKey,
            offlineToastMessageKey,
            showApiErrorToast,
            setOfflineOnServiceUnavailable,
            onServiceUnavailableAsync);
}
