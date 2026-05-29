using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Headless.Tests.Headless;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;
using ZgrzytDesktop.Views.DashboardParts;

namespace ZgrzytDesktop.Headless.Tests.Views;

public class SettingsPanelHeadlessTests : HeadlessViewTestsBase
{
    [Fact]
    public void DashboardView_SettingsPage_UsesLightThemeAndSaveDoesNotCrash()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("user");

            try
            {
                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                HeadlessViewTestHelper.ShowInWindow(view);

                vm.CurrentSection = AppSections.Settings;

                SettingsService.ApplyThemeMode("Dark");
                Assert.Equal(Avalonia.Styling.ThemeVariant.Light, Avalonia.Application.Current!.ActualThemeVariant);
                Assert.Equal(SettingsPanelViewModel.LightThemeMode, vm.SelectedThemeMode);

                vm.SaveSettingsCommand.ExecuteAsync(null).GetAwaiter().GetResult();
                Assert.Equal("pl", vm.SelectedUiCulture);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }

    [Fact]
    public void DashboardView_SettingsPage_ContainsLanguageSaveAndLocalAuditSections()
    {
        AvaloniaHeadlessTestHost.RunOnUiThread(() =>
        {
            var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("admin");

            try
            {
                vm.CurrentSection = AppSections.Settings;

                var view = HeadlessViewTestHelper.CreateDashboardView(vm);
                var window = HeadlessViewTestHelper.ShowInWindow(view);

                var settingsPanel = HeadlessViewTestHelper
                    .FindDescendants<SettingsPanelView>(view)
                    .FirstOrDefault();

                Assert.NotNull(settingsPanel);
                Assert.False(HeadlessViewTestHelper.ContainsText(window, AppStrings.Get("Settings_Theme")));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblSettingsLanguage));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblSettingsSave));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, vm.LblSettingsRefreshSession));
                Assert.False(HeadlessViewTestHelper.ContainsText(window, AppStrings.Get("Settings_ResetApiUrl")));
                Assert.False(HeadlessViewTestHelper.ContainsText(window, AppStrings.Get("Settings_AutoLogout")));
                Assert.Equal(1, HeadlessViewTestHelper.CountDescendants<Avalonia.Controls.ComboBox>(settingsPanel!));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Lokalny audyt aplikacji"));
                Assert.True(HeadlessViewTestHelper.ContainsText(window, "Odśwież audyt"));
                Assert.False(HeadlessViewTestHelper.ContainsText(window, "Wyczyść audyt"));
                Assert.Equal(0, HeadlessViewTestHelper.CountDescendantsByTypeName(settingsPanel!, "DataGrid"));
                Assert.True(HeadlessViewTestHelper.CountDescendantsByTypeName(settingsPanel!, "ItemsControl") >= 1);
            }
            finally
            {
                TestApiFactory.Cleanup(tempDir);
            }
        });
    }
}
