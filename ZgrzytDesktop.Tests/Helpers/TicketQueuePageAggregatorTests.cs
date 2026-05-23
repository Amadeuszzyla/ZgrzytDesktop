using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketQueuePageAggregatorTests
{
    [Fact]
    public async Task FetchAllPages_StopsAtMaxPages()
    {
        var requestedPages = new List<int>();

        var result = await TicketQueuePageAggregator.FetchAllPagesAsync(
            async (page, size) =>
            {
                requestedPages.Add(page);
                await Task.CompletedTask;

                return new PaginatedResponse<Ticket>
                {
                    CurrentPage = page,
                    LastPage = 100,
                    PerPage = size,
                    Total = 10_000,
                    Data = Enumerable.Range(1, size)
                        .Select(index => new Ticket { Id = page * 1000 + index, Title = $"T{page}-{index}" })
                        .ToList()
                };
            },
            pageSize: 10,
            maxPages: 3,
            maxItems: 5000);

        Assert.Equal(3, requestedPages.Count);
        Assert.Equal(30, result.Tickets.Count);
        Assert.True(result.Truncated);
        Assert.Equal(3, result.PagesFetched);
    }

    [Fact]
    public async Task FetchAllPages_EmptyFirstPage_EndsImmediately()
    {
        var fetchCount = 0;

        var result = await TicketQueuePageAggregator.FetchAllPagesAsync(
            async (_, _) =>
            {
                fetchCount++;
                await Task.CompletedTask;
                return new PaginatedResponse<Ticket>
                {
                    Data = [],
                    LastPage = 5,
                    Total = 0
                };
            });

        Assert.Equal(1, fetchCount);
        Assert.Empty(result.Tickets);
        Assert.False(result.Truncated);
    }

    [Fact]
    public async Task FetchAllPages_PartialPage_EndsWithoutExtraRequests()
    {
        var fetchCount = 0;

        var result = await TicketQueuePageAggregator.FetchAllPagesAsync(
            async (page, size) =>
            {
                fetchCount++;
                await Task.CompletedTask;

                return new PaginatedResponse<Ticket>
                {
                    CurrentPage = 1,
                    LastPage = 99,
                    Data =
                    [
                        new Ticket { Id = 1, Title = "Only" }
                    ],
                    PerPage = size
                };
            },
            pageSize: 100);

        Assert.Equal(1, fetchCount);
        Assert.Single(result.Tickets);
        Assert.False(result.Truncated);
    }

    [Fact]
    public async Task FetchAllPages_StopsAtMaxItems()
    {
        var result = await TicketQueuePageAggregator.FetchAllPagesAsync(
            async (page, size) =>
            {
                await Task.CompletedTask;
                return new PaginatedResponse<Ticket>
                {
                    CurrentPage = page,
                    LastPage = 10,
                    Data = Enumerable.Range(1, size)
                        .Select(index => new Ticket { Id = index, Title = "T" })
                        .ToList()
                };
            },
            pageSize: 100,
            maxPages: 50,
            maxItems: 150);

        Assert.Equal(150, result.Tickets.Count);
        Assert.True(result.Truncated);
    }
}
