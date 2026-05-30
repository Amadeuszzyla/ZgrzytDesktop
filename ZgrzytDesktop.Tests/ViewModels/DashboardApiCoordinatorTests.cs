using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class DashboardApiCoordinatorTests
{
    public DashboardApiCoordinatorTests() => ViewModelTestSetup.EnsureAppStrings();

    private sealed class ApiCoordinatorHarness
    {
        public bool IsOffline { get; private set; }

        public List<(string Message, string Type)> RawToasts { get; } = [];

        public List<(string Key, string Type)> KeyToasts { get; } = [];

        public RecordingLocalDiagnosticLogService DiagnosticLog { get; } = new();

        public void SetOffline(bool value) => IsOffline = value;

        public void ShowToastRaw(string message, string type) =>
            RawToasts.Add((message, type));

        public void ShowToastKey(string key, string type, params object[] args) =>
            KeyToasts.Add((key, type));

        public DashboardApiCoordinator CreateCoordinator() =>
            new(
                SetOffline,
                ShowToastRaw,
                ShowToastKey,
                DiagnosticLog);
    }

    [Fact]
    public async Task ExecuteApiAsync_OnSuccess_ReturnsTrue()
    {
        var harness = new ApiCoordinatorHarness();
        var coordinator = harness.CreateCoordinator();
        var executed = false;

        var result = await coordinator.ExecuteApiAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        Assert.True(result);
        Assert.True(executed);
        Assert.False(harness.IsOffline);
        Assert.Empty(harness.RawToasts);
        Assert.Empty(harness.DiagnosticLog.Entries);
    }

    [Fact]
    public async Task ExecuteApiAsync_OnUnauthorized_SetsStatusAndShowsErrorToast()
    {
        var harness = new ApiCoordinatorHarness();
        var coordinator = harness.CreateCoordinator();
        string? statusMessage = null;

        var result = await coordinator.ExecuteApiAsync(
            () => throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized"),
            new DashboardApiExecutionOptions
            {
                SetStatusMessage = message => statusMessage = message
            });

        Assert.False(result);
        Assert.Equal(AppStrings.Get("Api_Unauthorized"), statusMessage);
        Assert.Single(harness.RawToasts);
        Assert.Equal(AppStrings.Get("Api_Unauthorized"), harness.RawToasts[0].Message);
        Assert.Equal(ToastTypes.Error, harness.RawToasts[0].Type);
        Assert.Contains(
            harness.DiagnosticLog.Entries,
            e => e.Level == DiagnosticLogLevel.Warning &&
                 e.Message.Contains("401", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteApiAsync_OnServiceUnavailable_EntersOfflineMode()
    {
        var harness = new ApiCoordinatorHarness();
        var coordinator = harness.CreateCoordinator();
        string? statusMessage = null;

        var result = await coordinator.ExecuteApiAsync(
            () => throw new ApiException(HttpStatusCode.ServiceUnavailable, "offline"),
            new DashboardApiExecutionOptions
            {
                SetStatusMessage = message => statusMessage = message,
                OfflineToastMessageKey = "Api_ServiceUnavailable"
            });

        Assert.False(result);
        Assert.True(harness.IsOffline);
        Assert.Null(statusMessage);
        Assert.Single(harness.KeyToasts);
        Assert.Equal("Api_ServiceUnavailable", harness.KeyToasts[0].Key);
        Assert.Equal(ToastTypes.Warning, harness.KeyToasts[0].Type);
    }

    [Fact]
    public async Task ExecuteApiAsync_OnUnexpectedException_LogsErrorAndSetsStatus()
    {
        var harness = new ApiCoordinatorHarness();
        var coordinator = harness.CreateCoordinator();
        string? statusMessage = null;

        var result = await coordinator.ExecuteApiAsync(
            () => throw new InvalidOperationException("boom"),
            new DashboardApiExecutionOptions
            {
                SetStatusMessage = message => statusMessage = message,
                UnexpectedStatusMessageKey = "Api_UnexpectedError",
                UnexpectedToastMessageKey = "Api_UnexpectedError"
            });

        Assert.False(result);
        Assert.Equal(AppStrings.Get("Api_UnexpectedError"), statusMessage);
        Assert.Single(harness.KeyToasts);
        Assert.Equal("Api_UnexpectedError", harness.KeyToasts[0].Key);
        Assert.Equal(ToastTypes.Error, harness.KeyToasts[0].Type);
        Assert.Contains(
            harness.DiagnosticLog.Entries,
            e => e.Level == DiagnosticLogLevel.Error &&
                 e.Message.Contains("Unexpected error", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteApiAsync_WhenShowApiErrorToastFalse_DoesNotShowErrorToast()
    {
        var harness = new ApiCoordinatorHarness();
        var coordinator = harness.CreateCoordinator();

        var result = await coordinator.ExecuteApiAsync(
            () => throw new ApiException(HttpStatusCode.Forbidden, "Forbidden"),
            new DashboardApiExecutionOptions
            {
                ShowApiErrorToast = false,
                SetStatusMessage = _ => { }
            });

        Assert.False(result);
        Assert.Empty(harness.RawToasts);
    }

    [Fact]
    public async Task ExecuteApiAsync_WhenSetOfflineOnServiceUnavailableFalse_DoesNotEnterOfflineMode()
    {
        var harness = new ApiCoordinatorHarness();
        var coordinator = harness.CreateCoordinator();

        var result = await coordinator.ExecuteApiAsync(
            () => throw new ApiException(HttpStatusCode.ServiceUnavailable, "offline"),
            new DashboardApiExecutionOptions
            {
                SetOfflineOnServiceUnavailable = false,
                OfflineToastMessageKey = "Api_ServiceUnavailable"
            });

        Assert.False(result);
        Assert.False(harness.IsOffline);
        Assert.Empty(harness.KeyToasts);
    }

    [Fact]
    public async Task ExecuteApiAsync_WithCustomOnServiceUnavailableAsync_SkipsDefaultOfflineHandling()
    {
        var harness = new ApiCoordinatorHarness();
        var coordinator = harness.CreateCoordinator();
        var customHandlerCalled = false;

        var result = await coordinator.ExecuteApiAsync(
            () => throw new ApiException(HttpStatusCode.ServiceUnavailable, "offline"),
            new DashboardApiExecutionOptions
            {
                OnServiceUnavailableAsync = _ =>
                {
                    customHandlerCalled = true;
                    return Task.CompletedTask;
                },
                OfflineToastMessageKey = "Api_ServiceUnavailable"
            });

        Assert.False(result);
        Assert.True(customHandlerCalled);
        Assert.False(harness.IsOffline);
        Assert.Empty(harness.KeyToasts);
    }

    [Fact]
    public void GetApiErrorMessage_UsesSanitizerForKnownStatusCodes()
    {
        var message = DashboardApiCoordinator.GetApiErrorMessage(
            new ApiException(HttpStatusCode.Unauthorized, "raw"));

        Assert.Equal(AppStrings.Get("Api_Unauthorized"), message);
    }

    [Fact]
    public async Task DashboardViewModel_ExecuteApiAsync_DelegatesToCoordinator()
    {
        var auth = new FakeAuthService
        {
            RefreshException = new ApiException(HttpStatusCode.Unauthorized, "Unauthorized")
        };
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard(auth: auth);

        try
        {
            await vm.RefreshSessionCommand.ExecuteAsync(null);

            Assert.Equal(AppStrings.Get("Api_Unauthorized"), vm.SettingsStatusMessage);
            Assert.True(vm.IsToastVisible);
            Assert.Equal(AppStrings.Get("Api_Unauthorized"), vm.ToastMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
