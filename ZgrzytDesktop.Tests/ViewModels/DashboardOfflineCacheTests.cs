using System.Net;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels;

namespace ZgrzytDesktop.Tests.ViewModels;

public class DashboardOfflineCacheTests
{
    public DashboardOfflineCacheTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task ServiceUnavailable_WithCache_ShowsOfflineTickets()
    {
        var tickets = new FakeTicketService
        {
            GetTicketsApiException = new ApiException(HttpStatusCode.ServiceUnavailable, "offline")
        };

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("user", tickets: tickets);

        try
        {
            var cache = new LocalTicketCacheService(tempDir);
            await cache.SaveTicketsAsync(
            [
                new Ticket
                {
                    Id = 99,
                    Title = "Offline ticket",
                    Description = "Cached",
                    Status = TicketStatuses.Nowe,
                    Priority = TicketPriorities.Low
                }
            ]);

            await vm.RefreshTicketsNowCommand.ExecuteAsync(null);

            Assert.True(vm.IsOffline);
            Assert.Single(vm.Tickets);
            Assert.Equal(99, vm.Tickets[0].Id);
            Assert.Contains("offline", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task ServiceUnavailable_WithEmptyCache_ShowsMessageWithoutCrash()
    {
        var tickets = new FakeTicketService
        {
            GetTicketsApiException = new ApiException(HttpStatusCode.ServiceUnavailable, "offline")
        };

        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("user", tickets: tickets);

        try
        {
            await vm.RefreshTicketsNowCommand.ExecuteAsync(null);

            Assert.True(vm.IsOffline);
            Assert.Empty(vm.Tickets);
            Assert.Equal("Brak połączenia z API i brak zapisanych danych offline.", vm.StatusMessage);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
