using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.ViewModels;

public static class ViewModelTestSetup
{
    public static void EnsureAppStrings() => TestCultureBootstrap.ApplyPolishDefaults();
}
