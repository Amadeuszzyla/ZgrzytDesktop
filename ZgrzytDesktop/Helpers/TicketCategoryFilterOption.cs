using System;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public sealed class TicketCategoryFilterOption
{
    public TicketCategoryFilterOption(string key)
    {
        Key = key;
    }

    public string Key { get; }

    public string Label => GetLabel(Key);

    public static string GetLabel(string key) =>
        key switch
        {
            TicketCategoryFilterKeys.Software => AppStrings.Get("Tickets_CategorySoftware"),
            TicketCategoryFilterKeys.Network => AppStrings.Get("Tickets_CategoryNetwork"),
            TicketCategoryFilterKeys.Hardware => AppStrings.Get("Tickets_CategoryHardware"),
            _ => AppStrings.Get("Tickets_CategoryAll")
        };

    public override string ToString() => Label;

    public override bool Equals(object? obj) =>
        obj is TicketCategoryFilterOption other &&
        string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Key);
}
