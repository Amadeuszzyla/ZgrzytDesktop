using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Regression;

public class StatisticsPanelUiRegressionTests
{
    public StatisticsPanelUiRegressionTests() => ViewModelTestSetup.EnsureAppStrings();

    [Theory]
    [InlineData("pl")]
    [InlineData("en")]
    public void AppStrings_StatsKeys_DoNotContainResponseTimeLabels(string culture)
    {
        AppStrings.ApplyCulture(culture);

        var keys = new[]
        {
            "Stats_Title",
            "Stats_LoadAll",
            "Stats_KpiAll",
            "Stats_KpiNew",
            "Stats_KpiInProgress",
            "Stats_KpiClosed",
            "Stats_KpiHighPriority",
            "Stats_ChartByStatus",
            "Stats_ChartByPriority",
            "Stats_ChartAssignment",
            "Stats_Assigned",
            "Stats_Unassigned",
            "Stats_PriorityLow",
            "Stats_PriorityMedium",
            "Stats_PriorityHigh",
            "Stats_Scope_NoData",
            "Stats_Scope_NoTickets",
            "Stats_Scope_CurrentPage",
            "Stats_Scope_AllPages",
            "Stats_LoadingAllPages"
        };

        foreach (var key in keys)
        {
            var text = AppStrings.Get(key);
            Assert.DoesNotContain("reakcj", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("response time", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("first response", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("czas pierwszej", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SLA", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
