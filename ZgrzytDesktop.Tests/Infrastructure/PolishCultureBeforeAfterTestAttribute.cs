using System.Reflection;
using Xunit.Sdk;

namespace ZgrzytDesktop.Tests.Infrastructure;

/// <summary>
/// Resets thread and default cultures to Polish before and after each test method.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class PolishCultureBeforeAfterTestAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest) => TestCultureBootstrap.ApplyPolishDefaults();

    public override void After(MethodInfo methodUnderTest) => TestCultureBootstrap.ApplyPolishDefaults();
}
