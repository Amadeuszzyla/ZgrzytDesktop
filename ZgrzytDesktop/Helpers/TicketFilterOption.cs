using System;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public sealed class TicketFilterOption
{
    public TicketFilterOption(string value, TicketFilterOptionKind kind)
    {
        Value = value;
        Kind = kind;
    }

    public string Value { get; }

    public TicketFilterOptionKind Kind { get; }

    public string Label => TicketFilterDisplayHelper.GetLabel(Value, Kind);

    public override string ToString() => Label;

    public override bool Equals(object? obj) =>
        obj is TicketFilterOption other &&
        string.Equals(Value, other.Value, StringComparison.Ordinal) &&
        Kind == other.Kind;

    public override int GetHashCode() => HashCode.Combine(Value, Kind);
}

public enum TicketFilterOptionKind
{
    Queue,
    Status,
    Priority
}

public static class TicketFilterDisplayHelper
{
    public static string GetLabel(string value, TicketFilterOptionKind kind) =>
        kind switch
        {
            TicketFilterOptionKind.Queue => value switch
            {
                FilterLabels.All => AppStrings.Get("Filter_All"),
                FilterLabels.Active => AppStrings.Get("Filter_Active"),
                FilterLabels.Unassigned => AppStrings.Get("Filter_Unassigned"),
                _ => value
            },
            TicketFilterOptionKind.Status => FilterLabels.IsAll(value)
                ? AppStrings.Get("Tickets_Filter_StatusAll")
                : StatusDisplayHelper.ToDisplayStatus(value),
            TicketFilterOptionKind.Priority => FilterLabels.IsAll(value)
                ? AppStrings.Get("Tickets_Filter_PriorityAll")
                : PriorityDisplayHelper.ToDisplayPriority(value),
            _ => value
        };
}
