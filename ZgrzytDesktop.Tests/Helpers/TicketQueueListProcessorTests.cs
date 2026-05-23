using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketQueueListProcessorTests
{
    private static readonly Ticket[] SampleTickets =
    [
        new()
        {
            Id = 1,
            Title = "Bravo",
            Status = TicketStatuses.Nowe,
            Priority = TicketPriorities.Low,
            CreatedAt = new DateTime(2026, 1, 2)
        },
        new()
        {
            Id = 2,
            Title = "Alpha",
            Status = TicketStatuses.WTrakcie,
            Priority = TicketPriorities.High,
            CreatedAt = new DateTime(2026, 1, 3)
        },
        new()
        {
            Id = 3,
            Title = "Charlie",
            Status = TicketStatuses.Nowe,
            Priority = TicketPriorities.Medium,
            CreatedAt = new DateTime(2026, 1, 1)
        }
    ];

    [Fact]
    public void Filter_ByStatus_ReturnsMatchingTicketsOnly()
    {
        var filtered = TicketQueueListProcessor.Filter(SampleTickets, TicketStatuses.Nowe, null, null);

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, ticket => Assert.Equal(TicketStatuses.Nowe, ticket.Status));
    }

    [Fact]
    public void Filter_ByPriority_ReturnsMatchingTicketsOnly()
    {
        var filtered = TicketQueueListProcessor.Filter(SampleTickets, null, TicketPriorities.High, null);

        Assert.Single(filtered);
        Assert.Equal(TicketPriorities.High, filtered[0].Priority);
    }

    [Fact]
    public void Sort_ByTitleAscending_OrdersAlphabetically()
    {
        var sorted = TicketQueueListProcessor.Sort(SampleTickets, "title", "asc");

        Assert.Equal(["Alpha", "Bravo", "Charlie"], sorted.Select(ticket => ticket.Title));
    }

    [Fact]
    public void Paginate_EmptyList_ReturnsEmptyPage()
    {
        var page = TicketQueueListProcessor.Paginate([], page: 1, perPage: 15);

        Assert.Empty(page.Data);
        Assert.Equal(0, page.Total);
        Assert.Equal(1, page.LastPage);
    }
}
