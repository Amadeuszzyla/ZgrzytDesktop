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
        string? offlineToastMessageKey = null,
        bool showToast = true,
        bool setOfflineOnServiceUnavailable = true)
    {
        if (setOfflineOnServiceUnavailable &&
            ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;

            if (!string.IsNullOrWhiteSpace(offlineToastMessageKey) && showToast)
                ShowToastKey(offlineToastMessageKey, ToastTypes.Warning);

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
        string? statusMessageKey,
        string? toastMessageKey = null,
        bool showToast = true)
    {
        var statusKey = string.IsNullOrWhiteSpace(statusMessageKey)
            ? "Api_UnexpectedError"
            : statusMessageKey;
        setStatusMessage?.Invoke(AppStrings.Get(statusKey));

        if (showToast && !string.IsNullOrWhiteSpace(toastMessageKey))
            ShowToastKey(toastMessageKey, ToastTypes.Error);
    }

    internal async Task<bool> ExecuteApiAsync(
        Func<Task> action,
        Action<string>? setStatusMessage = null,
        string? unexpectedStatusMessageKey = null,
        string? unexpectedToastMessageKey = null,
        string? offlineToastMessageKey = null,
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
                offlineToastMessageKey,
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
                unexpectedStatusMessageKey,
                unexpectedToastMessageKey);
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
