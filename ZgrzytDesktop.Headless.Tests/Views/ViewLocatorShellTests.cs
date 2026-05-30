using Avalonia.Controls;
using Avalonia.VisualTree;
using ZgrzytDesktop;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.Views;

namespace ZgrzytDesktop.Headless.Tests.Views;

[Collection(AvaloniaHeadlessCollection.Name)]
public class ViewLocatorShellTests : HeadlessViewTestsBase
{
    [Fact]
    public void ViewLocator_Build_ReturnsLoginView_ForLoginViewModel()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var locator = new ViewLocator();
            var viewModel = ViewModelTestFactory.CreateLoginViewModel();

            Assert.True(locator.Match(viewModel));

            var control = locator.Build(viewModel);

            Assert.IsType<LoginView>(control);
            Assert.Same(viewModel, control!.DataContext);
        });
    }

    [Fact]
    public void ViewLocator_Build_ReturnsDashboardView_ForDashboardViewModel()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var locator = new ViewLocator();
            var (dashboard, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

            try
            {
                Assert.True(locator.Match(dashboard));

                var control = locator.Build(dashboard);

                Assert.IsType<DashboardView>(control);
                Assert.Same(dashboard, control!.DataContext);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void MainWindow_ContentControl_BindsToCurrentViewModel()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var deps = ViewModelTestFactory.CreateMainWindowDependencies();
            var shell = new MainWindowViewModel(deps, runStartup: false);
            Window? window = null;

            try
            {
                window = new MainWindow
                {
                    DataContext = shell,
                    Width = 1200,
                    Height = 750
                };

                window.Show();
                HeadlessViewTestHelper.WaitForUiIdle(window);

                var contentControl = window.GetVisualDescendants()
                    .OfType<ContentControl>()
                    .FirstOrDefault();

                Assert.NotNull(contentControl);
                Assert.Same(shell.CurrentViewModel, contentControl!.Content);

                shell.CurrentViewModel = ViewModelTestFactory.CreateLoginViewModel();
                HeadlessViewTestHelper.WaitForUiIdle(window);
                Assert.Same(shell.CurrentViewModel, contentControl.Content);
            }
            finally
            {
                HeadlessViewTestHelper.CloseWindow(window);
            }
        });
    }

    [Fact]
    public void MainWindow_WithShellViewModel_RendersLoginViewThroughViewLocator()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var deps = ViewModelTestFactory.CreateMainWindowDependencies();
            var shell = new MainWindowViewModel(deps, runStartup: false);
            Window? window = null;

            try
            {
                Assert.IsType<LoginViewModel>(shell.CurrentViewModel);

                window = new MainWindow
                {
                    DataContext = shell,
                    Width = 1200,
                    Height = 750
                };

                window.Show();
                HeadlessViewTestHelper.WaitForUiIdle(window);

                var loginView = window.GetVisualDescendants()
                    .OfType<LoginView>()
                    .FirstOrDefault();

                Assert.NotNull(loginView);
                Assert.Same(shell.CurrentViewModel, loginView!.DataContext);
            }
            finally
            {
                HeadlessViewTestHelper.CloseWindow(window);
            }
        });
    }

    [Fact]
    public void MainWindow_WithShellViewModel_RendersDashboardViewAfterNavigation()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var deps = ViewModelTestFactory.CreateMainWindowDependencies();
            var shell = new MainWindowViewModel(deps, runStartup: false);
            var (dashboard, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");
            Window? window = null;

            try
            {
                shell.CurrentViewModel = dashboard;

                window = new MainWindow
                {
                    DataContext = shell,
                    Width = 1200,
                    Height = 750
                };

                window.Show();
                HeadlessViewTestHelper.WaitForUiIdle(window);

                var dashboardView = window.GetVisualDescendants()
                    .OfType<DashboardView>()
                    .FirstOrDefault();

                Assert.NotNull(dashboardView);
                Assert.Same(dashboard, dashboardView!.DataContext);
            }
            finally
            {
                HeadlessViewTestHelper.CloseWindow(window);
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }
}
