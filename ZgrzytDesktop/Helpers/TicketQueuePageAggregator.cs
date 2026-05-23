using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

public sealed class QueuePageAggregationResult
{
    public List<Ticket> Tickets { get; init; } = [];

    public bool Truncated { get; init; }

    public int PagesFetched { get; init; }

    public int? ApiReportedTotal { get; init; }
}

public static class TicketQueuePageAggregator
{
    public static async Task<QueuePageAggregationResult> FetchAllPagesAsync(
        Func<int, int, Task<PaginatedResponse<Ticket>?>> fetchPageAsync,
        int pageSize = TicketQueueFetchPolicy.PageSize,
        int maxPages = TicketQueueFetchPolicy.MaxPages,
        int maxItems = TicketQueueFetchPolicy.MaxItems)
    {
        var allTickets = new List<Ticket>();
        var currentPage = 1;
        var pagesFetched = 0;
        PaginatedResponse<Ticket>? lastResponse = null;
        var truncated = false;

        while (currentPage <= maxPages && allTickets.Count < maxItems)
        {
            var response = await fetchPageAsync(currentPage, pageSize);
            lastResponse = response;
            pagesFetched++;

            if (response?.Data is null || response.Data.Count == 0)
                break;

            var remainingCapacity = maxItems - allTickets.Count;
            if (response.Data.Count > remainingCapacity)
            {
                allTickets.AddRange(response.Data.Take(remainingCapacity));
                truncated = true;
                break;
            }

            allTickets.AddRange(response.Data);

            if (response.Data.Count < pageSize)
                break;

            var reportedLastPage = response.LastPage > 0 ? response.LastPage : currentPage;
            if (currentPage >= reportedLastPage)
                break;

            currentPage++;
        }

        if (!truncated && lastResponse?.Data is { Count: > 0 } lastPageData)
        {
            var reportedLastPage = lastResponse.LastPage > 0 ? lastResponse.LastPage : currentPage;
            var lastFetchWasFullPage = lastPageData.Count >= pageSize;

            if (lastFetchWasFullPage && reportedLastPage > currentPage)
                truncated = true;
        }

        return new QueuePageAggregationResult
        {
            Tickets = allTickets,
            Truncated = truncated,
            PagesFetched = pagesFetched,
            ApiReportedTotal = lastResponse?.Total > 0 ? lastResponse.Total : null
        };
    }
}
