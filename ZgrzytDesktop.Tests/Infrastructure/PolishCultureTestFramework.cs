using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("ZgrzytDesktop.Tests.Infrastructure.PolishCultureTestFramework", "ZgrzytDesktop.Tests")]

namespace ZgrzytDesktop.Tests.Infrastructure;

public sealed class PolishCultureTestFramework : XunitTestFramework
{
    public PolishCultureTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new PolishCultureTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}

public sealed class PolishCultureTestFrameworkExecutor : XunitTestFrameworkExecutor
{
    public PolishCultureTestFrameworkExecutor(
        AssemblyName assemblyName,
        ISourceInformationProvider sourceInformationProvider,
        IMessageSink diagnosticMessageSink)
        : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
    {
    }

    protected override void RunTestCases(
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions)
    {
        TestCultureBootstrap.ApplyPolishDefaults();
        base.RunTestCases(testCases, executionMessageSink, executionOptions);
    }
}
