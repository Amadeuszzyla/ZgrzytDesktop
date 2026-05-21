using System;
using System.Text.RegularExpressions;

namespace ZgrzytDesktop.Helpers;

public static class TicketCategoryHelper
{
    public static readonly string[] Categories = ["Hardware", "Software", "Sieć"];

    private static readonly Regex TitleCategoryPrefixRegex = new(
        @"^\[(Hardware|Software|Sieć)\]\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DescriptionCategoryLineRegex = new(
        @"^Kategoria:\s*(Hardware|Software|Sieć)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled);

    public static string FormatTitle(string? category, string title)
    {
        var cleanTitle = title.Trim();

        if (string.IsNullOrWhiteSpace(category))
            return cleanTitle;

        var withoutPrefix = StripTitleCategoryPrefix(cleanTitle);

        return $"[{category.Trim()}] {withoutPrefix}";
    }

    public static string FormatDescription(string? category, string description)
    {
        var cleanDescription = description.Trim();

        if (string.IsNullOrWhiteSpace(category))
            return cleanDescription;

        if (DescriptionCategoryLineRegex.IsMatch(cleanDescription))
            return cleanDescription;

        return $"Kategoria: {category.Trim()}{Environment.NewLine}{Environment.NewLine}{cleanDescription}";
    }

    public static string ExtractCategory(string? title, string? description)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleMatch = TitleCategoryPrefixRegex.Match(title.Trim());

            if (titleMatch.Success)
                return NormalizeCategoryName(titleMatch.Groups[1].Value);
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            var descriptionMatch = DescriptionCategoryLineRegex.Match(description);

            if (descriptionMatch.Success)
                return NormalizeCategoryName(descriptionMatch.Groups[1].Value);
        }

        return string.Empty;
    }

    public static string StripTitleCategoryPrefix(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        return TitleCategoryPrefixRegex.Replace(title.Trim(), string.Empty).Trim();
    }

    private static string NormalizeCategoryName(string value)
    {
        foreach (var category in Categories)
        {
            if (string.Equals(category, value, StringComparison.OrdinalIgnoreCase))
                return category;
        }

        return value.Trim();
    }
}
