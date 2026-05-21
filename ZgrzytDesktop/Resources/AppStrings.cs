using System.Globalization;
using System.Resources;

namespace ZgrzytDesktop.Resources;

public static class AppStrings
{
    private static readonly ResourceManager Manager = new(
        "ZgrzytDesktop.Resources.AppStrings",
        typeof(AppStrings).Assembly);

    public static string Get(string name) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
