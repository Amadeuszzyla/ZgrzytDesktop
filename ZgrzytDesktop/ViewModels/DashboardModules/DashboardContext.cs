using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Production adapter from <see cref="DashboardViewModel"/> to <see cref="IDashboardContext"/>.
/// </summary>
public sealed class DashboardContext : IDashboardContext
{
    private readonly Func<Func<Task>, DashboardApiExecutionOptions?, Task<bool>> _executeApiAsync;
    private readonly ToastKeyCallback _showToastKey;
    private readonly Action<string, string> _showToastRaw;
    private readonly Func<string, int?, string?, object?[]?, Task> _logAuditAsync;
    private readonly Func<bool> _getIsOffline;
    private readonly Action<bool> _setIsOffline;
    private readonly Action _notifyLocalization;
    private readonly Func<string> _getCurrentSection;

    public DashboardContext(
        Func<Func<Task>, DashboardApiExecutionOptions?, Task<bool>> executeApiAsync,
        ToastKeyCallback showToastKey,
        Action<string, string> showToastRaw,
        Func<string, int?, string?, object?[]?, Task> logAuditAsync,
        Func<bool> getIsOffline,
        Action<bool> setIsOffline,
        Action notifyLocalization,
        Func<string> getCurrentSection)
    {
        _executeApiAsync = executeApiAsync;
        _showToastKey = showToastKey;
        _showToastRaw = showToastRaw;
        _logAuditAsync = logAuditAsync;
        _getIsOffline = getIsOffline;
        _setIsOffline = setIsOffline;
        _notifyLocalization = notifyLocalization;
        _getCurrentSection = getCurrentSection;
    }

    public bool IsOffline
    {
        get => _getIsOffline();
        set => _setIsOffline(value);
    }

    public string CurrentSection => _getCurrentSection();

    public Task<bool> ExecuteApiAsync(
        Func<Task> operation,
        DashboardApiExecutionOptions? options = null) =>
        _executeApiAsync(operation, options);

    public void ShowToastKey(string resourceKey, string toastType, params object[] formatArgs) =>
        _showToastKey(resourceKey, toastType, formatArgs);

    public void ShowToast(string message, string toastType) =>
        _showToastRaw(message, toastType);

    public Task LogAuditAsync(
        string actionKey,
        int? ticketId,
        string? detailsKey,
        object?[]? formatParameters = null) =>
        _logAuditAsync(actionKey, ticketId, detailsKey, formatParameters);

    public void NotifyLocalization() => _notifyLocalization();
}
