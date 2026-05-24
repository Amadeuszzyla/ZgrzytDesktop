using System;

namespace ZgrzytDesktop.Constants;

public static class TicketAssignmentFilterKeys
{
    public const string All = "__ASSIGNMENT_ALL__";
    public const string Assigned = "__ASSIGNMENT_ASSIGNED__";
    public const string Unassigned = "__ASSIGNMENT_UNASSIGNED__";
    public const string AssignedToMe = "__ASSIGNMENT_TO_ME__";

    public static readonly string[] AllKeys = [All, Assigned, Unassigned, AssignedToMe];

    public static bool IsAll(string? key) =>
        string.IsNullOrWhiteSpace(key) ||
        string.Equals(key, All, StringComparison.Ordinal);
}
