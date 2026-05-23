using System;
using System.Text.RegularExpressions;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class TicketCategoryHelper
{
    public const string Hardware = "Hardware";
    public const string Software = "Software";
    public const string Network = "Sieć";

    public static readonly string[] Categories = [Hardware, Software, Network];

    private static readonly Regex TitleCategoryPrefixRegex = new(
        @"^\[(Hardware|Software|Sieć|Network)\]\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DescriptionCategoryLineRegex = new(
        @"^(Kategoria:|Category:)\s*(Hardware|Software|Sieć|Network)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled);

    public static string ToDisplayCategory(string? storageCategory)
    {
        if (string.IsNullOrWhiteSpace(storageCategory))
            return string.Empty;

        return NormalizeCategoryName(storageCategory) switch
        {
            Hardware => AppStrings.Get("Category_Hardware"),
            Software => AppStrings.Get("Category_Software"),
            Network => AppStrings.Get("Category_Network"),
            _ => storageCategory.Trim()
        };
    }

    public static string FormatTitle(string? category, string title)
    {
        var cleanTitle = title.Trim();

        if (string.IsNullOrWhiteSpace(category))
            return cleanTitle;

        var storageCategory = NormalizeCategoryName(category.Trim());
        var withoutPrefix = StripTitleCategoryPrefix(cleanTitle);

        return $"[{storageCategory}] {withoutPrefix}";
    }

    public static string FormatDescription(string? category, string description)
    {
        var cleanDescription = description.Trim();

        if (string.IsNullOrWhiteSpace(category))
            return cleanDescription;

        if (DescriptionCategoryLineRegex.IsMatch(cleanDescription))
            return cleanDescription;

        var storageCategory = NormalizeCategoryName(category.Trim());
        return $"{AppStrings.Get("Category_DescriptionPrefix")} {storageCategory}{Environment.NewLine}{Environment.NewLine}{cleanDescription}";
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
                return NormalizeCategoryName(descriptionMatch.Groups[2].Value);
        }

        return string.Empty;
    }

    public static string StripTitleCategoryPrefix(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        return TitleCategoryPrefixRegex.Replace(title.Trim(), string.Empty).Trim();
    }

    public static string NormalizeCategoryName(string value)
    {
        if (string.Equals(value, Hardware, StringComparison.OrdinalIgnoreCase))
            return Hardware;

        if (string.Equals(value, Software, StringComparison.OrdinalIgnoreCase))
            return Software;

        if (string.Equals(value, Network, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "Network", StringComparison.OrdinalIgnoreCase))
            return Network;

        return value.Trim();
    }
}
