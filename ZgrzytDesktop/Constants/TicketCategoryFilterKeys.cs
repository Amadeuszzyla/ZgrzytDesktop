using System;

namespace ZgrzytDesktop.Constants;

public static class TicketCategoryFilterKeys
{
    public const string All = "all";
    public const string Software = "software";
    public const string Network = "network";
    public const string Hardware = "hardware";

    public static readonly string[] AllKeys = [All, Software, Network, Hardware];

    public static bool IsAll(string? key) =>
        string.IsNullOrWhiteSpace(key) ||
        string.Equals(key, All, StringComparison.OrdinalIgnoreCase);
}
