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

    public Func<Func<Task>, DashboardApiExecutionOptions?, Task<bool>>? ExecuteApiAsyncHandler { get; init; }

    public static TestDashboardContext CreateDefault(string currentSection = AppSections.Settings) =>
        new() { CurrentSection = currentSection };

    public Task<bool> ExecuteApiAsync(
        Func<Task> action,
        DashboardApiExecutionOptions? options = null)
    {
        if (ExecuteApiAsyncHandler is not null)
            return ExecuteApiAsyncHandler(action, options);

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
            ExecuteApiAsyncHandler = async (action, options) =>
            {
                options ??= new DashboardApiExecutionOptions();

                try
                {
                    await action();
                    return true;
                }
                catch (ApiException ex)
                {
                    options.SetStatusMessage?.Invoke(ApiErrorSanitizer.SanitizeApiErrorMessage(
                        ex.ResponseContent ?? ex.Message,
                        ex.StatusCode));
                    return false;
                }
                catch
                {
                    options.SetStatusMessage?.Invoke(AppStrings.Get(
                        options.UnexpectedStatusMessageKey ?? "Api_UnexpectedError"));
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
