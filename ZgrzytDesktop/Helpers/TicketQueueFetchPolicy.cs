namespace ZgrzytDesktop.Helpers;

/// <summary>
/// Safety limits when aggregating active/unassigned queue pages for local filter/sort (phase 16I fallback).
/// </summary>
public static class TicketQueueFetchPolicy
{
    public const int PageSize = 100;

    /// <summary>Maximum API pages fetched per local queue operation (50 × 100 = 5000 items cap).</summary>
    public const int MaxPages = 50;

    public const int MaxItems = 5000;
}
