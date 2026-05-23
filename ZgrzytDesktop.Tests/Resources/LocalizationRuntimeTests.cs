using ZgrzytDesktop.Cache;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Resources;

public class LocalizationRuntimeTests
{
    public LocalizationRuntimeTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void CultureEn_StatusPriorityFiltersAndStats_UseEnglish()
    {
        AppStrings.ApplyCulture("en");

        Assert.Equal("New", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.Nowe));
        Assert.Equal("Low", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Low));
        Assert.Equal("All statuses", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Status));
        Assert.Equal("All", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Queue));
        Assert.Equal("Active", new AdminListFilterOption(UserAdminListFilter.Active).Label);

        var stats = new StatisticsPanelViewModel(
            new FakeTicketService(),
            CreateBridge(),
            () => true);
        stats.ApplyFromTickets([], 0, fromCurrentPageOnly: false);
        Assert.Equal("No tickets to analyze.", stats.StatsScopeMessage);

        var settings = CreateSettingsPanel();
        Assert.Equal("Settings ready.", settings.SettingsStatusMessage);
    }

    [Fact]
    public void CulturePl_StatusPriorityFiltersAndStats_UsePolish()
    {
        AppStrings.ApplyCulture("pl");

        Assert.Equal("Nowe", StatusDisplayHelper.ToDisplayStatus(TicketStatuses.Nowe));
        Assert.Equal("Niski", PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Low));
        Assert.Equal("Wszystkie statusy", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Status));
        Assert.Equal("Wszystkie", TicketFilterDisplayHelper.GetLabel(FilterLabels.All, TicketFilterOptionKind.Queue));
        Assert.Equal("Aktywni", new AdminListFilterOption(UserAdminListFilter.Active).Label);

        var stats = new StatisticsPanelViewModel(
            new FakeTicketService(),
            CreateBridge(),
            () => true);
        stats.ApplyFromTickets([], 0, fromCurrentPageOnly: false);
        Assert.Equal("Brak zgłoszeń do analizy.", stats.StatsScopeMessage);

        var settings = CreateSettingsPanel();
        Assert.Equal("Ustawienia gotowe.", settings.SettingsStatusMessage);
    }

    [Fact]
    public void NotifyLocalization_RefreshesFilterLabelsAfterCultureChange()
    {
        var (panel, _, tempDir) = CreateTicketsPanel();

        try
        {
            AppStrings.ApplyCulture("pl");
            panel.NotifyLocalization();
            var plLabel = panel.FilterStatusOptions.First(option => FilterLabels.IsAll(option.Value)).Label;

            AppStrings.ApplyCulture("en");
            panel.NotifyLocalization();
            var enLabel = panel.FilterStatusOptions.First(option => FilterLabels.IsAll(option.Value)).Label;

            Assert.Equal("Wszystkie statusy", plLabel);
            Assert.Equal("All statuses", enLabel);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void ToastSampleAction_UsesCurrentCulture()
    {
        AppStrings.ApplyCulture("en");
        Assert.Equal("Settings saved", AppStrings.Get("Toast_SettingsSaved"));

        AppStrings.ApplyCulture("pl");
        Assert.Equal("Ustawienia zapisane", AppStrings.Get("Toast_SettingsSaved"));
    }

    public static TheoryData<string> NewI18nKeys => new()
    {
        "Status_New",
        "Priority_Low",
        "Filter_All",
        "Tickets_Filter_StatusAll",
        "Stats_Scope_AllPages",
        "Settings_StatusReady",
        "Toast_TicketSaved",
        "Audit_Action_Login",
        "Category_Network",
        "Details_SelectFromList"
    };

    [Theory]
    [MemberData(nameof(NewI18nKeys))]
    public void NewKeys_ExistInPolishAndEnglish(string key)
    {
        AppStrings.ApplyCulture("pl");
        var polish = AppStrings.Get(key);
        Assert.NotEqual(key, polish);

        AppStrings.ApplyCulture("en");
        var english = AppStrings.Get(key);
        Assert.NotEqual(key, english);
        Assert.NotEqual(polish, english);
    }

    private static DashboardVmBridge CreateBridge() =>
        new()
        {
            ShowToast = (_, _) => { },
            LogAuditAsync = (_, _, _, _) => Task.CompletedTask,
            NotifyLocalization = () => { },
            GetIsOffline = () => false,
            SetIsOffline = _ => { },
            GetCurrentSection = () => AppSections.Statistics,
            ExecuteApiAsyncCore = async (action, _, _, _, _, _, _, _) =>
            {
                await action();
                return true;
            }
        };

    private static SettingsPanelViewModel CreateSettingsPanel()
    {
        var tempDir = TestDirectoryHelper.CreateTempDirectory();
        return new SettingsPanelViewModel(
            new SettingsService(tempDir),
            new FakeAuthService(),
            CreateBridge(),
            () => Task.CompletedTask);
    }

    private static (TicketsPanelViewModel Panel, FakeTicketService Tickets, string TempDir) CreateTicketsPanel()
    {
        var tempDir = TestDirectoryHelper.CreateTempDirectory();
        var tickets = new FakeTicketService();
        var panel = new TicketsPanelViewModel(
            tickets,
            new LocalTicketCacheService(tempDir),
            new TicketsPanelCallbacks
            {
                ShowToast = (_, _) => { },
                SetIsOffline = _ => { },
                GetIsOffline = () => false,
                NotifyStatistics = (_, _) => { },
                NotifyTicketsLoadingChanged = () => { },
                NotifyOnlineActionsChanged = () => { },
                GetApiErrorMessage = ex => ex.Message,
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
