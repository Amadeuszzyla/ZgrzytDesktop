using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Headless.Tests.Views;

[Collection(AvaloniaHeadlessCollection.Name)]
public abstract class HeadlessViewTestsBase : IDisposable
{
    protected HeadlessViewTestsBase()
    {
        ViewModelTestSetup.EnsureAppStrings();
        AvaloniaHeadlessTestHost.RunOnUiThread(HeadlessViewTestHelper.ResetSharedTestState);
    }

    public void Dispose() =>
        AvaloniaHeadlessTestHost.RunOnUiThread(HeadlessViewTestHelper.ResetSharedTestState);
}
