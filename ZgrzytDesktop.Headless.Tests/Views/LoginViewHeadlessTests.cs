using System.Linq;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Headless.Tests.Views;

public class LoginViewHeadlessTests : HeadlessViewTestsBase
{
    public LoginViewHeadlessTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void LoginView_CreatesWithoutException()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var vm = ViewModelTestFactory.CreateLoginViewModel();
            var view = HeadlessViewTestHelper.CreateLoginView(vm);

            var window = HeadlessViewTestHelper.ShowInWindow(view, 500, 700);

            Assert.NotNull(window.Content);
            Assert.Contains(view, HeadlessViewTestHelper.EnumerateDescendants(window));
            Assert.True(HeadlessViewTestHelper.ContainsText(view, vm.LblLoginTitle));
            Assert.True(HeadlessViewTestHelper.ContainsText(view, vm.LblAppSubtitle));
            Assert.True(HeadlessViewTestHelper.ContainsText(view, "ZGRZYT"));
            Assert.NotNull(HeadlessViewTestHelper.FindDescendants<Avalonia.Controls.Image>(view).FirstOrDefault());
            Assert.NotNull(
                HeadlessViewTestHelper
                    .FindDescendants<Avalonia.Controls.Button>(view)
                    .FirstOrDefault(b => b.Command == vm.LoginCommand));
        });
    }

    [Fact]
    public void LoginView_DoesNotRenderAutoLoginStatusPanel()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var vm = ViewModelTestFactory.CreateLoginViewModel();
            var view = HeadlessViewTestHelper.CreateLoginView(vm);
            HeadlessViewTestHelper.ShowInWindow(view, 500, 700);

            Assert.Empty(HeadlessViewTestHelper.FindDescendants<Avalonia.Controls.ProgressBar>(view));
            Assert.False(HeadlessViewTestHelper.ContainsText(
                view,
                AppStrings.Get("Login_AutoLogin_CheckingSession")));
            Assert.False(HeadlessViewTestHelper.ContainsText(
                view,
                AppStrings.Get("Login_AutoLogin_Cancel")));
        });
    }
}
