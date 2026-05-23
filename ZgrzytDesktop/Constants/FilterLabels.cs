using System;

namespace ZgrzytDesktop.Constants;

public static class FilterLabels
{
    public const string All = "__FILTER_ALL__";
    public const string Active = "__FILTER_ACTIVE__";
    public const string Unassigned = "__FILTER_UNASSIGNED__";

    public static bool IsAll(string? value) =>
        string.Equals(value, All, StringComparison.Ordinal);
}
