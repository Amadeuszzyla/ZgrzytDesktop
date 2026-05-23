using System;
using System.Collections.Generic;
using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

public enum ResponseTimeSampleSource
{
    None,
    FirstResponseAtField,
    EmbeddedStaffMessages,
    Mixed
}

public sealed class ResponseTimeStatistics
{
    public bool IsAvailable { get; init; }

    public TimeSpan? Average { get; init; }

    public int SampleCount { get; init; }

    public ResponseTimeSampleSource Source { get; init; }
}

public sealed class TicketStatisticsSnapshot
{
    public int Total { get; init; }

    public int New { get; init; }

    public int InProgress { get; init; }

    public int Closed { get; init; }

    public int LowPriority { get; init; }

    public int MediumPriority { get; init; }

    public int HighPriority { get; init; }

    public int Assigned { get; init; }

    public int Unassigned { get; init; }

    public ResponseTimeStatistics ResponseTime { get; init; } = new();
}

public static class TicketStatisticsCalculator
{
    public static TicketStatisticsSnapshot Compute(IReadOnlyList<Ticket> tickets)
    {
        var assigned = tickets.Count(ticket => ticket.AssignedItId.HasValue);

        return new TicketStatisticsSnapshot
        {
            Total = tickets.Count,
            New = CountByStatus(tickets, TicketStatuses.Nowe),
            InProgress = CountByStatus(tickets, TicketStatuses.WTrakcie),
            Closed = CountByStatus(tickets, TicketStatuses.Zamkniete),
            LowPriority = CountByPriority(tickets, TicketPriorities.Low),
            MediumPriority = CountByPriority(tickets, TicketPriorities.Medium),
            HighPriority = CountByPriority(tickets, TicketPriorities.High),
            Assigned = assigned,
            Unassigned = Math.Max(0, tickets.Count - assigned),
            ResponseTime = ComputeResponseTime(tickets)
        };
    }

    /// <summary>
    /// Per ticket: prefer API <c>first_response_at</c> minus <c>created_at</c>;
    /// otherwise first embedded staff message (<c>it</c>/<c>admin</c>) minus <c>created_at</c>.
    /// List endpoint usually has neither — then statistics are unavailable (no fabricated values).
    /// </summary>
    public static ResponseTimeStatistics ComputeResponseTime(IReadOnlyList<Ticket> tickets)
    {
        var samples = new List<(TimeSpan Duration, ResponseTimeSampleSource Source)>();

        foreach (var ticket in tickets)
        {
            if (ticket.CreatedAt is null)
                continue;

            if (TryGetResponseTimeFromApiField(ticket, out var fromApi))
            {
                samples.Add((fromApi, ResponseTimeSampleSource.FirstResponseAtField));
                continue;
            }

            if (TryGetResponseTimeFromEmbeddedMessages(ticket, out var fromMessages))
                samples.Add((fromMessages, ResponseTimeSampleSource.EmbeddedStaffMessages));
        }

        if (samples.Count == 0)
        {
            return new ResponseTimeStatistics
            {
                IsAvailable = false,
                Source = ResponseTimeSampleSource.None
            };
        }

        var averageTicks = (long)samples.Average(sample => sample.Duration.Ticks);
        var sources = samples.Select(sample => sample.Source).Distinct().ToList();

        return new ResponseTimeStatistics
        {
            IsAvailable = true,
            Average = TimeSpan.FromTicks(averageTicks),
            SampleCount = samples.Count,
            Source = sources.Count == 1
                ? sources[0]
                : ResponseTimeSampleSource.Mixed
        };
    }

    private static bool TryGetResponseTimeFromApiField(Ticket ticket, out TimeSpan duration)
    {
        duration = default;

        if (ticket.FirstResponseAt is null || ticket.CreatedAt is null)
            return false;

        duration = ticket.FirstResponseAt.Value - ticket.CreatedAt.Value;
        return duration >= TimeSpan.Zero;
    }

    private static bool TryGetResponseTimeFromEmbeddedMessages(Ticket ticket, out TimeSpan duration)
    {
        duration = default;

        if (ticket.CreatedAt is null || ticket.Messages.Count == 0)
            return false;

        var firstStaffMessageAt = ticket.Messages
            .Where(message => message.CreatedAt.HasValue && IsStaffSender(message.Sender))
            .Select(message => message.CreatedAt!.Value)
            .OrderBy(timestamp => timestamp)
            .Cast<DateTime?>()
            .FirstOrDefault();

        if (firstStaffMessageAt is null)
            return false;

        duration = firstStaffMessageAt.Value - ticket.CreatedAt.Value;
        return duration >= TimeSpan.Zero;
    }

    private static bool IsStaffSender(User? sender) =>
        sender is not null &&
        (string.Equals(sender.Role, "it", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(sender.Role, "admin", StringComparison.OrdinalIgnoreCase));

    private static int CountByStatus(IReadOnlyList<Ticket> tickets, string status) =>
        tickets.Count(ticket => string.Equals(ticket.Status, status, StringComparison.OrdinalIgnoreCase));

    private static int CountByPriority(IReadOnlyList<Ticket> tickets, string priority) =>
        tickets.Count(ticket => string.Equals(ticket.Priority, priority, StringComparison.OrdinalIgnoreCase));
}
