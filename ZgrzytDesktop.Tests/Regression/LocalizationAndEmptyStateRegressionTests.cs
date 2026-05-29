using System.Net;
using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Regression;

/// <summary>
/// Regression suite for localization, tickets empty state, admin panel, and light-only settings.
/// </summary>
public class LocalizationAndEmptyStateRegressionTests
{
    public LocalizationAndEmptyStateRegressionTests() => ViewModelTestSetup.EnsureAppStrings();

    #region 16B — i18n EN

    [Fact]
    public void I18nEn_StatisticsPanel_StatsScopeMessage_IsEnglish()
    {
        using var _ = new CultureScope("en");

        var stats = new StatisticsPanelViewModel(
            new FakeTicketService(),
            CreateStatisticsContext(),
            () => true);
        stats.ApplyFromTickets([], 0, fromCurrentPageOnly: false);

        Assert.Equal(AppStrings.Get("Stats_Scope_NoTickets"), stats.StatsScopeMessage);
    }

    [Fact]
    public void I18nEn_SettingsPanel_StatusLabels_AreEnglish()
    {
        using var _ = new CultureScope("en");

        var panel = CreateSettingsPanel();
        panel.NotifyLocalization();

        Assert.Equal("Settings ready.", panel.SettingsStatusMessage);
        Assert.Equal("Interface language", panel.LblSettingsLanguage);
        Assert.Equal("Save settings", panel.LblSettingsSave);
    }

    [Fact]
    public void I18nEn_AdminFilterLabels_AreEnglish()
    {
        using var _ = new CultureScope("en");

        Assert.Equal("All", new AdminListFilterOption(UserAdminListFilter.All).Label);
        Assert.Equal("Active", new AdminListFilterOption(UserAdminListFilter.Active).Label);
        Assert.Equal("Inactive", new AdminListFilterOption(UserAdminListFilter.Inactive).Label);
        Assert.Equal("Banned", new AdminListFilterOption(UserAdminListFilter.Banned).Label);
    }

    [Fact]
    public void I18nEn_TicketStatusAndPriorityDisplay_AreEnglish()
    {
        using var _ = new CultureScope("en");

        Assert.Equal("New", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.Nowe));
        Assert.Equal("In progress", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.WTrakcie));
        Assert.Equal("Closed", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.Zamkniete));
        Assert.Equal("Low", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Low));
        Assert.Equal("Medium", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Medium));
        Assert.Equal("High", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.High));
    }

    [Fact]
    public void I18nEn_TicketsFilters_AreEnglish()
    {
        using var _ = new CultureScope("en");

        Assert.Equal("All statuses", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Status));
        Assert.Equal("All", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Queue));
        Assert.Equal("All priorities", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Priority));
    }

    [Fact]
    public async Task I18nEn_SaveSettings_Toast_IsEnglish()
    {
        using var _ = new CultureScope("en");

        string? toastMessage = null;
        var settings = new FakeSettingsService();
        var panel = CreateSettingsPanel(
            settings,
            onToast: message => toastMessage = message);

        panel.SelectedUiCulture = "en";
        await panel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Toast_SettingsSaved"), toastMessage);
    }

    #endregion

    #region 16B — i18n PL

    [Fact]
    public void I18nPl_StatisticsPanel_StatsScopeMessage_IsPolish()
    {
        using var _ = new CultureScope("pl");

        var stats = new StatisticsPanelViewModel(
            new FakeTicketService(),
            CreateStatisticsContext(),
            () => true);
        stats.ApplyFromTickets([], 0, fromCurrentPageOnly: false);

        Assert.Equal(AppStrings.Get("Stats_Scope_NoTickets"), stats.StatsScopeMessage);
    }

    [Fact]
    public void I18nPl_TicketStatusAndPriorityDisplay_ArePolish()
    {
        using var _ = new CultureScope("pl");

        Assert.Equal("Nowe", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.Nowe));
        Assert.Equal("W toku", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.WTrakcie));
        Assert.Equal("Zamknięte", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.Zamkniete));
        Assert.Equal("Niski", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Low));
        Assert.Equal("Średni", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Medium));
        Assert.Equal("Wysoki", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.High));
    }

    [Fact]
    public void I18nPl_AdminAndTicketsFilters_ArePolish()
    {
        using var _ = new CultureScope("pl");

        Assert.Equal("Wszyscy", new AdminListFilterOption(UserAdminListFilter.All).Label);
        Assert.Equal("Aktywni", new AdminListFilterOption(UserAdminListFilter.Active).Label);
        Assert.Equal("Wszystkie statusy", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Status));
        Assert.Equal("Wszystkie", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Queue));
    }

    #endregion

    #region Tickets empty state

    [Fact]
    public async Task TicketsEmptyState_HasNoTickets_WhenCountIsZero()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data = [],
                Total = 0,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreateTicketsPanel(tickets);

        try
        {
            await panel.LoadTicketsAsync();

            Assert.Empty(panel.Tickets);
            Assert.False(panel.IsLoading);
            Assert.True(panel.HasNoTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task TicketsEmptyState_HasNoTickets_False_WhenCountGreaterThanZero()
    {
        var tickets = new FakeTicketService
        {
            NextTicketsResponse = new PaginatedResponse<Ticket>
            {
                Data = [new Ticket { Id = 1, Title = "T", Status = TicketStatuses.Nowe, Priority = TicketPriorities.Low }],
                Total = 1,
                LastPage = 1,
                CurrentPage = 1
            }
        };

        var (panel, _, tempDir) = CreateTicketsPanel(tickets);

        try
        {
            await panel.LoadTicketsAsync();

            Assert.Single(panel.Tickets);
            Assert.False(panel.HasNoTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void TicketsEmptyState_HasNoTickets_False_WhileLoading()
    {
        var (panel, _, tempDir) = CreateTicketsPanel(new FakeTicketService());

        try
        {
            panel.IsLoading = true;
            Assert.Empty(panel.Tickets);
            Assert.False(panel.HasNoTickets);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    #endregion

    #region 16D — admin filtering

    [Fact]
    public async Task AdminFiltering_AllFilter_UsesUsersEndpointOnly()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, "[]");
            var service = TestApiFactory.CreateUserAdmin(api);

            await service.GetUsersAsync(UserAdminListFilter.All);

            Assert.Single(handler.Requests);
            Assert.EndsWith("/api/users", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task AdminFiltering_On404Fallback_ShowsSuccessWithoutErrorStatus()
    {
        var userAdmin = new FakeUserAdminService
        {
            NextUsers = [new User { Id = 1, Login = "alpha", Active = true, Ban = false }],
            NextUsedLocalFilterFallback = true,
            NextInfoKind = UserAdminListInfoKind.LocalFilterFallback
        };

        var panel = CreateAdminPanel(userAdmin);

        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Single(panel.AdminUsers);
        Assert.NotEqual(AppStrings.Get("Api_Forbidden"), panel.AdminStatusMessage);
        Assert.NotEqual(AppStrings.Get("Api_NotFound"), panel.AdminStatusMessage);
        Assert.Contains("1", panel.AdminStatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminFiltering_On403_ShowsAdminListForbiddenMessage()
    {
        var userAdmin = new FakeUserAdminService
        {
            GetUsersApiException = new ApiException(
                HttpStatusCode.Forbidden,
                AppStrings.Get("Admin_ListForbidden"))
        };

        var panel = CreateAdminPanel(userAdmin);
        await panel.LoadAdminUsersCommand.ExecuteAsync(null);

        Assert.Equal(AppStrings.Get("Admin_ListForbidden"), panel.AdminStatusMessage);
        Assert.Empty(panel.AdminUsers);
    }

    #endregion

    #region 16E — light-only settings

    [Fact]
    public void Settings_LightOnly_ExposesLanguageButNotThemeModesCollection()
    {
        var panel = CreateSettingsPanel();

        Assert.Equal(2, panel.UiCultures.Count);
        Assert.Equal(SettingsPanelViewModel.LightThemeMode, panel.SelectedThemeMode);
        Assert.DoesNotContain(
            panel.GetType().GetProperties().Select(property => property.Name),
            name => string.Equals(name, "ThemeModes", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Settings_SaveSettings_PersistsCultureAndLightTheme()
    {
        var settings = new FakeSettingsService();
        var panel = CreateSettingsPanel(settings);

        panel.SelectedUiCulture = "en";
        await panel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Equal(1, settings.SaveAsyncCallCount);
        Assert.Equal("en", settings.Settings.UiCulture);
        Assert.Equal(SettingsPanelViewModel.LightThemeMode, settings.Settings.ThemeMode);
    }

    [Fact]
    public void Settings_NormalizeThemeMode_AlwaysReturnsLight()
    {
        var tempDir = TestDirectoryHelper.CreateTempDirectory();
        try
        {
            var service = new SettingsService(tempDir);
            Assert.Equal("Light", service.NormalizeThemeMode("Dark"));
            Assert.Equal("Light", service.NormalizeThemeMode("System"));
        }
        finally
        {
            TestDirectoryHelper.DeleteDirectory(tempDir);
        }
    }

    #endregion

    private sealed class CultureScope : IDisposable
    {
        public CultureScope(string culture) => AppStrings.ApplyCulture(culture);

        public void Dispose() => AppStrings.ApplyCulture("pl");
    }

    private static TestDashboardContext CreateStatisticsContext() =>
        TestDashboardContext.CreateDefault(AppSections.Statistics);

    private static SettingsPanelViewModel CreateSettingsPanel(
        FakeSettingsService? settings = null,
        Action<string>? onToast = null)
    {
        settings ??= new FakeSettingsService();
        var context = new TestDashboardContext
        {
            CurrentSection = AppSections.Settings,
            ShowToastKeyHandler = TestToastCallbacks.ResolveKeyTo(onToast),
            ShowToastRawHandler = (message, _) => onToast?.Invoke(message)
        };

        return new SettingsPanelViewModel(
            settings,
            new FakeAuthService(),
            context,
            () => Task.CompletedTask);
    }

    private static AdminPanelViewModel CreateAdminPanel(FakeUserAdminService userAdmin) =>
        new(
            userAdmin,
            new AdminPanelCallbacks
            {
                ShowToastKey = TestToastCallbacks.NoopKey,
            ShowToastRaw = TestToastCallbacks.NoopRaw,
                GetIsOffline = () => false,
                GetIsAdminRole = () => true,
                GetIsStaffRole = () => true,
                GetCanUseOnlineActions = () => true,
                GetApiErrorMessage = ex => ApiErrorSanitizer.SanitizeApiErrorMessage(
                    ex.ResponseContent ?? ex.Message,
                    ex.StatusCode),
                LogAuditAsync = (_, _, _, _) => Task.CompletedTask,
                ExecuteApiAsyncCore = async (action, setStatusMessage, _, _, _, _, _, _) =>
                {
                    try
                    {
                        await action();
                        return true;
                    }
                    catch (ApiException ex)
                    {
                        setStatusMessage?.Invoke(ApiErrorSanitizer.SanitizeApiErrorMessage(
                            ex.ResponseContent ?? ex.Message,
                            ex.StatusCode));
                        return false;
                    }
                }
            });

    private static (TicketsPanelViewModel Panel, FakeTicketService Tickets, string TempDir) CreateTicketsPanel(
        FakeTicketService tickets)
    {
        var tempDir = TestDirectoryHelper.CreateTempDirectory();
        var panel = new TicketsPanelViewModel(
            tickets,
            new LocalTicketCacheService(tempDir),
            new TicketsPanelCallbacks
            {
                ShowToastKey = TestToastCallbacks.NoopKey,
            ShowToastRaw = TestToastCallbacks.NoopRaw,
                SetIsOffline = _ => { },
                GetIsOffline = () => false,
                NotifyStatistics = (_, _) => { },
                NotifyTicketsLoadingChanged = () => { },
                NotifyOnlineActionsChanged = () => { },
                GetApiErrorMessage = ex => ex.Message,
                GetCurrentUserId = () => 1,
                TicketSelected = _ => { },
                RefreshPaginationSideEffects = () => { },
                LogAuditAsync = (_, _, _, _) => Task.CompletedTask,
                ExecuteApiAsyncCore = async (action, _, _, _, _, _, _, _) =>
                {
                    await action();
                    return true;
                }
            });

        return (panel, tickets, tempDir);
    }
}
