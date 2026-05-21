using System.Collections.Generic;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public sealed class TicketSortFieldOption
{
    public required string SortBy { get; init; }

    public required string ResourceKey { get; init; }

    public string Label => AppStrings.Get(ResourceKey);
}

public sealed class TicketSortDirectionOption
{
    public required string Direction { get; init; }

    public required string ResourceKey { get; init; }

    public string Label => AppStrings.Get(ResourceKey);
}

public static class TicketSortHelper
{
    public static IReadOnlyList<TicketSortFieldOption> Fields { get; } =
    [
        new() { SortBy = "created_at", ResourceKey = "SortField_CreatedAt" },
        new() { SortBy = "title", ResourceKey = "SortField_Title" },
        new() { SortBy = "status", ResourceKey = "SortField_Status" },
        new() { SortBy = "priority", ResourceKey = "SortField_Priority" }
    ];

    public static IReadOnlyList<TicketSortDirectionOption> Directions { get; } =
    [
        new() { Direction = "asc", ResourceKey = "SortDirection_Asc" },
        new() { Direction = "desc", ResourceKey = "SortDirection_Desc" }
    ];

    public static TicketSortFieldOption DefaultField => Fields[0];

    public static TicketSortDirectionOption DefaultDirection => Directions[1];
}
