using System;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace ZgrzytDesktop.Resources;

public static class AppStrings
{
    private static readonly ResourceManager Manager = new(
        "ZgrzytDesktop.Resources.AppStrings",
        typeof(AppStrings).Assembly);

    public static string Get(string name) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    public static void ApplyCulture(string? uiCulture)
    {
        var normalized = string.Equals(uiCulture, "en", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(uiCulture, "en-US", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "pl";
        var culture = normalized == "en"
            ? new CultureInfo("en-US")
            : new CultureInfo("pl-PL");

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
    }
}
