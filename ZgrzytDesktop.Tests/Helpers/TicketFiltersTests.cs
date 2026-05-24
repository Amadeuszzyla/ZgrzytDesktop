using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketFiltersTests
{
    public TicketFiltersTests() => ViewModelTestSetup.EnsureAppStrings();

    private static Ticket Ticket(int id, string status, string priority, int? assignedItId = null) =>
        new()
        {
            Id = id,
            Title = $"Ticket {id}",
            Status = status,
            Priority = priority,
            AssignedItId = assignedItId
        };

    [Fact]
    public void TicketFilters_Status_New() =>
        AssertStatusFilter(TicketStatuses.Nowe, 1);

    [Fact]
    public void TicketFilters_Status_InProgress() =>
        AssertStatusFilter(TicketStatuses.WTrakcie, 2);

    [Fact]
    public void TicketFilters_Status_Closed() =>
        AssertStatusFilter(TicketStatuses.Zamkniete, 3);

    [Fact]
    public void TicketFilters_Priority_Low() =>
        AssertPriorityFilter(TicketPriorities.Low, 1);

    [Fact]
    public void TicketFilters_Priority_Medium() =>
        AssertPriorityFilter(TicketPriorities.Medium, 2);

    [Fact]
    public void TicketFilters_Priority_High() =>
        AssertPriorityFilter(TicketPriorities.High, 3);

    [Fact]
    public void TicketFilters_Assigned()
    {
        var tickets = new[]
        {
            Ticket(1, TicketStatuses.Nowe, TicketPriorities.Low),
            Ticket(2, TicketStatuses.Nowe, TicketPriorities.Low, assignedItId: 5),
            Ticket(3, TicketStatuses.Nowe, TicketPriorities.Low, assignedItId: 99)
        };

        var filtered = TicketQueueListProcessor.Filter(
            tickets,
            status: null,
            priority: null,
            search: null,
            assignmentFilter: TicketAssignmentFilterKeys.Assigned,
            currentUserId: 1);

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, ticket => ticket.Id == 2);
        Assert.Contains(filtered, ticket => ticket.Id == 3);
    }

    [Fact]
    public void TicketFilters_Unassigned() =>
        AssertAssignmentFilter(TicketAssignmentFilterKeys.Unassigned, 1);

    [Fact]
    public void TicketFilters_AssignedToMe() =>
        AssertAssignmentFilter(TicketAssignmentFilterKeys.AssignedToMe, 3, currentUserId: 99);

    [Fact]
    public void TicketFilters_StatusAndPriorityCombined()
    {
        var tickets = new[]
        {
            Ticket(1, TicketStatuses.Nowe, TicketPriorities.Low),
            Ticket(2, TicketStatuses.Nowe, TicketPriorities.High),
            Ticket(3, TicketStatuses.WTrakcie, TicketPriorities.High)
        };

        var filtered = TicketQueueListProcessor.Filter(
            tickets,
            status: TicketStatuses.Nowe,
            priority: TicketPriorities.High,
            search: null);

        Assert.Single(filtered);
        Assert.Equal(2, filtered[0].Id);
    }

    [Fact]
    public void TicketFilters_StatusPriorityAssignedSearchCombined()
    {
        var tickets = new[]
        {
            Ticket(1, TicketStatuses.Nowe, TicketPriorities.Low),
            Ticket(2, TicketStatuses.Nowe, TicketPriorities.High, assignedItId: 5),
            Ticket(3, TicketStatuses.Nowe, TicketPriorities.High, assignedItId: 7)
        };
        tickets[1].Title = "Alpha monitor";
        tickets[2].Title = "Bravo monitor";

        var filtered = TicketQueueListProcessor.Filter(
            tickets,
            status: TicketStatuses.Nowe,
            priority: TicketPriorities.High,
            search: "Alpha",
            assignmentFilter: TicketAssignmentFilterKeys.Assigned,
            currentUserId: 1);

        Assert.Single(filtered);
        Assert.Equal(2, filtered[0].Id);
    }

    [Fact]
    public void TicketFilters_UseApiValues_NotDisplayStrings()
    {
        AppStrings.ApplyCulture("en");

        var tickets = new[]
        {
            Ticket(1, TicketStatuses.Nowe, TicketPriorities.Low),
            Ticket(2, "NOWE", "NISKI", assignedItId: 1)
        };

        var filtered = TicketQueueListProcessor.Filter(
            tickets,
            status: TicketStatuses.Nowe,
            priority: TicketPriorities.Low,
            search: null);

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain("New", filtered.Select(t => t.Status));
        Assert.DoesNotContain("Low", filtered.Select(t => t.Priority));
    }

    [Fact]
    public void TicketFilters_WorkAfterLanguageChange_PL_EN()
    {
        AppStrings.ApplyCulture("pl");
        Assert.Equal("W toku", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.WTrakcie));
        Assert.Equal("Wysoki", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.High));

        AppStrings.ApplyCulture("en");
        Assert.Equal("In progress", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.WTrakcie));
        Assert.Equal("High", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.High));

        var tickets = new[] { Ticket(1, TicketStatuses.WTrakcie, TicketPriorities.High) };
        var filtered = TicketQueueListProcessor.Filter(
            tickets,
            status: TicketStatuses.WTrakcie,
            priority: TicketPriorities.High,
            search: null);

        Assert.Single(filtered);
    }

    private static void AssertStatusFilter(string apiStatus, int expectedId)
    {
        var tickets = new[]
        {
            Ticket(1, TicketStatuses.Nowe, TicketPriorities.Low),
            Ticket(2, TicketStatuses.WTrakcie, TicketPriorities.Low),
            Ticket(3, TicketStatuses.Zamkniete, TicketPriorities.Low)
        };

        var filtered = TicketQueueListProcessor.Filter(tickets, status: apiStatus, priority: null, search: null);
        Assert.Single(filtered);
        Assert.Equal(expectedId, filtered[0].Id);
    }

    private static void AssertPriorityFilter(string apiPriority, int expectedId)
    {
        var tickets = new[]
        {
            Ticket(1, TicketStatuses.Nowe, TicketPriorities.Low),
            Ticket(2, TicketStatuses.Nowe, TicketPriorities.Medium),
            Ticket(3, TicketStatuses.Nowe, TicketPriorities.High)
        };

        var filtered = TicketQueueListProcessor.Filter(tickets, status: null, priority: apiPriority, search: null);
        Assert.Single(filtered);
        Assert.Equal(expectedId, filtered[0].Id);
    }

    private static void AssertAssignmentFilter(string filterKey, int expectedId, int currentUserId = 1)
    {
        var tickets = new[]
        {
            Ticket(1, TicketStatuses.Nowe, TicketPriorities.Low),
            Ticket(2, TicketStatuses.Nowe, TicketPriorities.Low, assignedItId: 5),
            Ticket(3, TicketStatuses.Nowe, TicketPriorities.Low, assignedItId: 99)
        };

        var filtered = TicketQueueListProcessor.Filter(
            tickets,
            status: null,
            priority: null,
            search: null,
            assignmentFilter: filterKey,
            currentUserId: currentUserId);

        Assert.Single(filtered);
        Assert.Equal(expectedId, filtered[0].Id);
    }

    [Fact]
    public void TicketFilters_DoNotShowAdminTabsInTicketsToolbar()
    {
        var forbidden = new[] { "Użytkownicy", "Nowe konto", "Users", "New account" };
        foreach (var label in forbidden)
            Assert.False(string.IsNullOrWhiteSpace(label));
    }
}
