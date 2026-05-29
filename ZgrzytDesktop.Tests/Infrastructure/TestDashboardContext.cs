using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Infrastructure;

public sealed class TestDashboardContext : IDashboardContext
{
    public bool IsOffline { get; set; }

    public string CurrentSection { get; set; } = AppSections.Settings;

    public ToastKeyCallback ShowToastKeyHandler { get; init; } = TestToastCallbacks.NoopKey;

    public Action<string, string> ShowToastRawHandler { get; init; } = TestToastCallbacks.NoopRaw;

    public Action NotifyLocalizationHandler { get; init; } = () => { };

    public Func<string, int?, string?, object?[]?, Task> LogAuditAsyncHandler { get; init; } =
        (_, _, _, _) => Task.CompletedTask;

    public Func<Func<Task>, Action<string>?, string?, string?, string?, bool, bool, Func<ApiException, Task>?, Task<bool>>?
        ExecuteApiAsyncHandler { get; init; }

    public static TestDashboardContext CreateDefault(string currentSection = AppSections.Settings) =>
        new() { CurrentSection = currentSection };

    public Task<bool> ExecuteApiAsync(
        Func<Task> action,
        Action<string>? setStatusMessage = null,
        string? unexpectedStatusMessageKey = null,
        string? unexpectedToastMessageKey = null,
        string? offlineToastMessageKey = null,
        bool showApiErrorToast = true,
        bool setOfflineOnServiceUnavailable = true,
        Func<ApiException, Task>? onServiceUnavailableAsync = null)
    {
        if (ExecuteApiAsyncHandler is not null)
        {
            return ExecuteApiAsyncHandler(
                action,
                setStatusMessage,
                unexpectedStatusMessageKey,
                unexpectedToastMessageKey,
                offlineToastMessageKey,
                showApiErrorToast,
                setOfflineOnServiceUnavailable,
                onServiceUnavailableAsync);
        }

        return RunPassThroughAsync(action);
    }

    public void ShowToastKey(string resourceKey, string toastType, params object[] formatArgs) =>
        ShowToastKeyHandler(resourceKey, toastType, formatArgs);

    public void ShowToast(string message, string toastType) =>
        ShowToastRawHandler(message, toastType);

    public Task LogAuditAsync(
        string actionKey,
        int? ticketId,
        string? detailsKey,
        object?[]? formatParameters = null) =>
        LogAuditAsyncHandler(actionKey, ticketId, detailsKey, formatParameters);

    public void NotifyLocalization() => NotifyLocalizationHandler();

    public TestDashboardContext WithApiErrorHandling() =>
        new()
        {
            IsOffline = IsOffline,
            CurrentSection = CurrentSection,
            ShowToastKeyHandler = ShowToastKeyHandler,
            ShowToastRawHandler = ShowToastRawHandler,
            NotifyLocalizationHandler = NotifyLocalizationHandler,
            LogAuditAsyncHandler = LogAuditAsyncHandler,
            ExecuteApiAsyncHandler = async (action, setStatusMessage, unexpectedStatusMessage, _, _, _, _, _) =>
            {
                try
                {
                    await action();
                    return true;
                }
                catch (ApiException ex)
                {
                    setStatusMessage?.Invoke(ApiErrorSanitizer.SanitizeApiErrorMessage(
                        ex.ResponseContent ?? ex.Message,
                        ex.StatusCode));
                    return false;
                }
                catch
                {
                    setStatusMessage?.Invoke(
                        unexpectedStatusMessage ?? AppStrings.Get("Api_UnexpectedError"));
                    return false;
                }
            }
        };

    private static async Task<bool> RunPassThroughAsync(Func<Task> action)
    {
        await action();
        return true;
    }
}
