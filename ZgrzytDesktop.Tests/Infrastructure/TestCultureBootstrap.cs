using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Tests.Infrastructure;

/// <summary>
/// Pins Polish UI culture for deterministic localization in tests (CI uses en-US by default).
/// </summary>
public static class TestCultureBootstrap
{
    private static readonly CultureInfo PolishCulture = new("pl-PL");

    [ModuleInitializer]
    internal static void Initialize() => ApplyPolishDefaults();

    public static void ApplyPolishDefaults()
    {
        CultureInfo.DefaultThreadCurrentCulture = PolishCulture;
        CultureInfo.DefaultThreadCurrentUICulture = PolishCulture;
        Thread.CurrentThread.CurrentCulture = PolishCulture;
        Thread.CurrentThread.CurrentUICulture = PolishCulture;
        AppStrings.ApplyCulture("pl");
    }
}
