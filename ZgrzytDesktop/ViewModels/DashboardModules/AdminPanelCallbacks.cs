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

    public required Func<Func<Task>, DashboardApiExecutionOptions?, Task<bool>> ExecuteApiAsyncCore { get; init; }

    public Func<string, string?, Task<bool>> ConfirmAsync { get; init; } =
        static (_, _) => Task.FromResult(true);

    public Task<bool> ExecuteApiAsync(
        Func<Task> action,
        DashboardApiExecutionOptions? options = null) =>
        ExecuteApiAsyncCore(action, options);
}
