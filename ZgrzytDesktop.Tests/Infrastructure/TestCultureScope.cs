using System.Globalization;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Tests.Infrastructure;

public sealed class TestCultureScope : IDisposable
{
    private readonly CultureInfo? _previousDefaultCulture;
    private readonly CultureInfo? _previousDefaultUiCulture;
    private readonly CultureInfo _previousThreadCulture;
    private readonly CultureInfo _previousThreadUiCulture;

    public TestCultureScope(string culture)
    {
        _previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        _previousDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        _previousThreadCulture = CultureInfo.CurrentCulture;
        _previousThreadUiCulture = CultureInfo.CurrentUICulture;

        AppStrings.ApplyCulture(culture);
    }

    public void Dispose()
    {
        CultureInfo.DefaultThreadCurrentCulture = _previousDefaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _previousDefaultUiCulture;
        CultureInfo.CurrentCulture = _previousThreadCulture;
        CultureInfo.CurrentUICulture = _previousThreadUiCulture;
    }
}
