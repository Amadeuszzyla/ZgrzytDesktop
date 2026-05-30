using System;
using System.Net;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Central API error handling for dashboard panels: offline mode, toasts, status messages, diagnostic logs.
/// </summary>
public sealed class DashboardApiCoordinator
{
    private readonly ILocalDiagnosticLogService? _diagnosticLogService;
    private readonly Action<bool> _setIsOffline;
    private readonly Action<string, string> _showToastRaw;
    private readonly ToastKeyCallback _showToastKey;

    public DashboardApiCoordinator(
        Action<bool> setIsOffline,
        Action<string, string> showToastRaw,
        ToastKeyCallback showToastKey,
        ILocalDiagnosticLogService? diagnosticLogService = null)
    {
        _setIsOffline = setIsOffline;
        _showToastRaw = showToastRaw;
        _showToastKey = showToastKey;
        _diagnosticLogService = diagnosticLogService;
    }

    public static string GetApiErrorMessage(ApiException ex) =>
        ApiErrorSanitizer.SanitizeApiErrorMessage(
            ex.ResponseContent ?? ex.Message,
            ex.StatusCode);

    public async Task<bool> ExecuteApiAsync(
        Func<Task> action,
        DashboardApiExecutionOptions? options = null)
    {
        options ??= new DashboardApiExecutionOptions();

        try
        {
            await action();
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _diagnosticLogService?.LogWarning(
                $"API request failed ({(int)ex.StatusCode} {ex.StatusCode})",
                ex);

            if (options.OnServiceUnavailableAsync is not null)
            {
                await options.OnServiceUnavailableAsync(ex);
                return false;
            }

            if (options.SetOfflineOnServiceUnavailable)
                _setIsOffline(true);

            HandleApiError(
                ex,
                options.SetStatusMessage,
                options.OfflineToastMessageKey,
                options.ShowApiErrorToast,
                options.SetOfflineOnServiceUnavailable);
            return false;
        }
        catch (ApiException ex)
        {
            _diagnosticLogService?.LogWarning(
                $"API request failed ({(int)ex.StatusCode} {ex.StatusCode})",
                ex);

            HandleApiError(ex, options.SetStatusMessage, showToast: options.ShowApiErrorToast);
            return false;
        }
        catch (Exception ex)
        {
            _diagnosticLogService?.LogError("Unexpected error during API operation", ex);

            HandleUnexpectedError(
                options.SetStatusMessage,
                options.UnexpectedStatusMessageKey,
                options.UnexpectedToastMessageKey);
            return false;
        }
    }

    private void ShowApiErrorToast(ApiException ex) =>
        _showToastRaw(GetApiErrorMessage(ex), ToastTypes.Error);

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
            _setIsOffline(true);

            if (!string.IsNullOrWhiteSpace(offlineToastMessageKey) && showToast)
                _showToastKey(offlineToastMessageKey, ToastTypes.Warning);

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
            _showToastKey(toastMessageKey, ToastTypes.Error);
    }
}
