using ZgrzytDesktop.Diagnostics;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Helpers;

public class SafeFireAndForgetDiagnosticTests
{
    private static readonly TimeSpan LogWaitTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Run_FaultedTask_LogsToDiagnosticService()
    {
        var directory = TestDirectoryHelper.CreateTempDirectory();
        var logPath = Path.Combine(directory, "diagnostic.log");
        var previous = DiagnosticLogBridge.Service;

        try
        {
            DiagnosticLogBridge.Service = new LocalDiagnosticLogService(logPath);

            SafeFireAndForget.Run(Task.FromException(new InvalidOperationException("fire-and-forget boom")));

            await WaitForLogFileAsync(logPath, LogWaitTimeout);

            var content = await File.ReadAllTextAsync(logPath);
            Assert.Contains("Unhandled fire-and-forget task failure", content);
            Assert.Contains("InvalidOperationException", content);
            Assert.Contains("fire-and-forget boom", content);
        }
        finally
        {
            DiagnosticLogBridge.Service = previous;
            TestDirectoryHelper.DeleteDirectory(directory);
        }
    }

    private static async Task WaitForLogFileAsync(string logPath, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!File.Exists(logPath))
            await Task.Delay(25, cts.Token);
    }
}
