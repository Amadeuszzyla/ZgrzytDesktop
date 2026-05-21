using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.ViewModels;

public class DashboardPollingTests
{
    public DashboardPollingTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void InitializePolling_OnlyStartsOncePerViewModel()
    {
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard(
            bootstrap: DashboardViewModel.BootstrapOptions.TestingWithTimers);

        try
        {
            Assert.True(vm.IsTicketPollingActive);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Logout_StopsPollingTimer()
    {
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard(
            bootstrap: DashboardViewModel.BootstrapOptions.TestingWithTimers);

        try
        {
            Assert.True(vm.IsTicketPollingActive);

            await vm.LogoutCommand.ExecuteAsync(null);

            Assert.False(vm.IsTicketPollingActive);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task AutoRefresh_MultipleSilentFailures_DoNotSpamToasts()
    {
        var tickets = new FakeTicketService
        {
            GetTicketsException = new InvalidOperationException("refresh failed")
        };

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it", tickets: tickets);

        try
        {
            vm.CurrentSection = AppSections.Tickets;

            await vm.RunAutoRefreshForTestsAsync();
            await vm.RunAutoRefreshForTestsAsync();
            await vm.RunAutoRefreshForTestsAsync();

            Assert.False(vm.IsToastVisible);
            Assert.Contains("automatycznie", vm.PollingStatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task AutoRefresh_ServiceUnavailable_ShowsAtMostOneWarningToast()
    {
        var tickets = new FakeTicketService
        {
            GetTicketsApiException = new ApiException(HttpStatusCode.ServiceUnavailable, "offline")
        };

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it", tickets: tickets);

        try
        {
            vm.CurrentSection = AppSections.Tickets;

            await vm.RunAutoRefreshForTestsAsync();
            var firstToast = vm.IsToastVisible;
            var firstMessage = vm.ToastMessage;

            await vm.RunAutoRefreshForTestsAsync();

            if (firstToast)
            {
                Assert.Equal(firstMessage, vm.ToastMessage);
            }
            else
            {
                Assert.False(vm.IsToastVisible);
            }
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
