using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Tests.ViewModels;

public static class ViewModelTestSetup
{
    private static bool _initialized;

    public static void EnsureAppStrings()
    {
        if (_initialized)
            return;

        AppStrings.ApplyCulture("pl");
        _initialized = true;
    }
}
