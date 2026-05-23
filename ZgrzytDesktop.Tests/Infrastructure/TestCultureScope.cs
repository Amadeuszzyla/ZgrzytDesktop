using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Tests.Infrastructure;

public sealed class TestCultureScope : IDisposable
{
    public TestCultureScope(string culture) => AppStrings.ApplyCulture(culture);

    public void Dispose() => AppStrings.ApplyCulture("pl");
}
