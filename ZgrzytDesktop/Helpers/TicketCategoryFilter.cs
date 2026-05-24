using System;
using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

public static class TicketCategoryFilter
{
    private static readonly string[] SoftwareKeywords =
    [
        "software", "aplikacja", "program", "system", "crm", "konto", "logowanie", "dostęp", "hasło", "haslo"
    ];

    private static readonly string[] NetworkKeywords =
    [
        "sieć", "siec", "network", "internet", "wifi", "wi-fi", "vpn", "router", "połączenie", "polaczenie",
        "folder sieciowy", "serwer"
    ];

    private static readonly string[] HardwareKeywords =
    [
        "hardware", "sprzęt", "sprzet", "drukarka", "komputer", "laptop", "monitor", "klawiatura", "mysz", "dysk",
        "bateria"
    ];

    public static bool Matches(Ticket ticket, string? filterKey)
    {
        if (TicketCategoryFilterKeys.IsAll(filterKey))
            return true;

        var resolved = ResolveCategoryKey(ticket);

        return !string.IsNullOrWhiteSpace(resolved) &&
               string.Equals(resolved, filterKey, StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveCategoryKey(Ticket ticket)
    {
        if (!string.IsNullOrWhiteSpace(ticket.Category))
        {
            var filterKey = ToFilterKey(TicketCategoryHelper.NormalizeCategoryName(ticket.Category));

            if (!string.IsNullOrWhiteSpace(filterKey))
                return filterKey;
        }

        var extracted = TicketCategoryHelper.ExtractCategory(ticket.Title, ticket.Description);

        if (!string.IsNullOrWhiteSpace(extracted))
        {
            var filterKey = ToFilterKey(extracted);

            if (!string.IsNullOrWhiteSpace(filterKey))
                return filterKey;
        }

        return ResolveFromKeywords(ticket.Title, ticket.Description);
    }

    public static string ToFilterKey(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return string.Empty;

        var normalized = TicketCategoryHelper.NormalizeCategoryName(category);

        if (string.Equals(normalized, TicketCategoryHelper.Software, StringComparison.OrdinalIgnoreCase))
            return TicketCategoryFilterKeys.Software;

        if (string.Equals(normalized, TicketCategoryHelper.Network, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "Network", StringComparison.OrdinalIgnoreCase))
            return TicketCategoryFilterKeys.Network;

        if (string.Equals(normalized, TicketCategoryHelper.Hardware, StringComparison.OrdinalIgnoreCase))
            return TicketCategoryFilterKeys.Hardware;

        return string.Empty;
    }

    private static string ResolveFromKeywords(string? title, string? description)
    {
        var text = $"{title} {description}";

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.ToLowerInvariant();

        if (ContainsAnyKeyword(text, NetworkKeywords))
            return TicketCategoryFilterKeys.Network;

        if (ContainsAnyKeyword(text, HardwareKeywords))
            return TicketCategoryFilterKeys.Hardware;

        if (ContainsAnyKeyword(text, SoftwareKeywords))
            return TicketCategoryFilterKeys.Software;

        return string.Empty;
    }

    private static bool ContainsAnyKeyword(string text, string[] keywords) =>
        keywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal));
}
