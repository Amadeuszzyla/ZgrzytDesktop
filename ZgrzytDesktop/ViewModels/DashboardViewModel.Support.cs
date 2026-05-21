using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Threading;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private static string GetApiErrorMessage(ApiException ex) =>
        ApiErrorSanitizer.SanitizeApiErrorMessage(
            ex.ResponseContent ?? ex.Message,
            ex.StatusCode);

    private void ShowApiErrorToast(ApiException ex) =>
        ShowToast(GetApiErrorMessage(ex), ToastTypes.Error);

    private bool HandleApiError(
        ApiException ex,
        Action<string>? setStatusMessage = null,
        string? offlineToastMessage = null,
        bool showToast = true,
        bool setOfflineOnServiceUnavailable = true)
    {
        if (setOfflineOnServiceUnavailable &&
            ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;

            if (!string.IsNullOrWhiteSpace(offlineToastMessage) && showToast)
                ShowToast(offlineToastMessage, ToastTypes.Warning);

            return true;
        }

        var message = GetApiErrorMessage(ex);
        setStatusMessage?.Invoke(message);

        if (showToast)
            ShowApiErrorToast(ex);

        return true;
    }

    private void HandleUnexpectedError(
        Action<string>? setStatusMessage,
        string statusMessage,
        string? toastMessage = null,
        bool showToast = true)
    {
        setStatusMessage?.Invoke(statusMessage);

        if (showToast && !string.IsNullOrWhiteSpace(toastMessage))
            ShowToast(toastMessage, ToastTypes.Error);
    }

    private async Task<bool> ExecuteApiAsync(
        Func<Task> action,
        Action<string>? setStatusMessage = null,
        string? unexpectedStatusMessage = null,
        string? unexpectedToastMessage = null,
        string? offlineToastMessage = null,
        bool showApiErrorToast = true,
        bool setOfflineOnServiceUnavailable = true,
        Func<ApiException, Task>? onServiceUnavailableAsync = null)
    {
        try
        {
            await action();
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            if (onServiceUnavailableAsync is not null)
            {
                await onServiceUnavailableAsync(ex);
                return false;
            }

            if (setOfflineOnServiceUnavailable)
                IsOffline = true;

            HandleApiError(
                ex,
                setStatusMessage,
                offlineToastMessage,
                showApiErrorToast,
                setOfflineOnServiceUnavailable);
            return false;
        }
        catch (ApiException ex)
        {
            HandleApiError(ex, setStatusMessage, showToast: showApiErrorToast);
            return false;
        }
        catch
        {
            HandleUnexpectedError(
                setStatusMessage,
                unexpectedStatusMessage ?? AppStrings.Get("Api_UnexpectedError"),
                unexpectedToastMessage);
            return false;
        }
    }

    private static bool IsValidTicketForDisplay(Ticket? ticket)
    {
        if (ticket is null)
            return false;

        return !ApiErrorSanitizer.IsHtmlResponse(ticket.Title) &&
               !ApiErrorSanitizer.IsHtmlResponse(ticket.Description);
    }

    private async Task HandleSessionExpiredAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            ShowToast(AppStrings.Get("Api_SessionExpired"), ToastTypes.Error);
            await _onLogoutRequested();
        });
    }

}
