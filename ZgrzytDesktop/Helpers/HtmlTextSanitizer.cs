using System.Net;
using System.Text.RegularExpressions;

namespace ZgrzytDesktop.Helpers;

public static class HtmlTextSanitizer
{
    private static readonly Regex ScriptTagRegex = new(
        @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]+>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new(
        @"\s+",
        RegexOptions.Compiled);

    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = ScriptTagRegex.Replace(html, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = HtmlTagRegex.Replace(text, " ");
        text = text.Replace('\u00A0', ' ');
        text = WhitespaceRegex.Replace(text, " ").Trim();
        return text;
    }
}
