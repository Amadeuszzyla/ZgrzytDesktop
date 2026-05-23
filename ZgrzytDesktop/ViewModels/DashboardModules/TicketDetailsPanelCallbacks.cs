using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed class TicketDetailsPanelCallbacks
{
    public required Action<string, string> ShowToast { get; init; }

    public required Action<bool> SetIsOffline { get; init; }

    public required Func<bool> GetIsOffline { get; init; }

    public required Func<ApiException, string> GetApiErrorMessage { get; init; }

    public required Func<int, Ticket?> FindCachedTicket { get; init; }

    public required Action NotifyDetailsSideEffects { get; init; }

    public required Action NotifyDetailsLoadingChanged { get; init; }

    public required Func<User> GetCurrentUser { get; init; }

    public required Func<bool> GetCanManageTickets { get; init; }

    public required Func<bool> GetIsRegularUser { get; init; }

    public required Func<string, int?, string?, object?[]?, Task> LogAuditAsync { get; init; }

    public required Func<Task> RefreshTicketsAsync { get; init; }

    public required Action NavigateToTickets { get; init; }

    public required Action ClearSelectedTicket { get; init; }

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
