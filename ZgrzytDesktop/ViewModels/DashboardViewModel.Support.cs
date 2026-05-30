using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private DashboardApiCoordinator _apiCoordinator = null!;

    private void InitializeApiCoordinator()
    {
        _apiCoordinator = new DashboardApiCoordinator(
            setIsOffline: value => IsOffline = value,
            showToastRaw: ShowToast,
            showToastKey: ShowToastKey,
            diagnosticLogService: _diagnosticLogService);
    }

    private static string GetApiErrorMessage(ApiException ex) =>
        DashboardApiCoordinator.GetApiErrorMessage(ex);

    internal async Task<bool> ExecuteApiAsync(
        Func<Task> action,
        DashboardApiExecutionOptions? options = null) =>
        await _apiCoordinator.ExecuteApiAsync(action, options);

    private static bool IsValidTicketForDisplay(Ticket? ticket)
    {
        if (ticket is null)
            return false;

        return !ApiErrorSanitizer.IsHtmlResponse(ticket.Title) &&
               !ApiErrorSanitizer.IsHtmlResponse(ticket.Description);
    }

    internal Task HandleSessionExpiredFromApiAsync() => HandleSessionExpiredAsync();

    private async Task HandleSessionExpiredAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            ShowToastKey("Api_SessionExpired", ToastTypes.Error);
            await _onLogoutRequested();
        });
    }
}
