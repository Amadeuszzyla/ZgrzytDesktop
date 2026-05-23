using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Regression;

public class Phase16HStragglersTests
{
    public Phase16HStragglersTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void CultureEn_TicketsPaginationLabels_UseAppStrings()
    {
        var (vm, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

        try
        {
            AppStrings.ApplyCulture("en");

            Assert.Equal("← Previous", AppStrings.Get("Tickets_PagePrevious"));
            Assert.Equal("Next →", AppStrings.Get("Tickets_PageNext"));
            Assert.Equal(vm.LblTicketsPagePrevious, AppStrings.Get("Tickets_PagePrevious"));
            Assert.Equal(vm.LblTicketsPageNext, AppStrings.Get("Tickets_PageNext"));
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void CultureEn_StatsKpiClosed_MatchesStatusClosedTerminology()
    {
        AppStrings.ApplyCulture("en");

        Assert.Equal("Closed", AppStrings.Get("Stats_KpiClosed"));
        Assert.Equal(AppStrings.Get("Status_Closed"), AppStrings.Get("Stats_KpiClosed"));
    }

    [Fact]
    public void CulturePl_StatsKpiClosed_MatchesStatusClosedTerminology()
    {
        AppStrings.ApplyCulture("pl");

        Assert.Equal("Zamknięte", AppStrings.Get("Stats_KpiClosed"));
        Assert.Equal(AppStrings.Get("Status_Closed"), AppStrings.Get("Stats_KpiClosed"));
    }

    [Fact]
    public void AuditDescriptions_LoginAutoAndLogout_UseLocalizedKeys()
    {
        AppStrings.ApplyCulture("en");
        Assert.Equal("Signed in automatically at application startup.", AppStrings.Get("Audit_Desc_LoginAuto"));
        Assert.Equal("User signed out from desktop application.", AppStrings.Get("Audit_Desc_LogoutDesktop"));

        AppStrings.ApplyCulture("pl");
        Assert.Contains("Automatyczne", AppStrings.Get("Audit_Desc_LoginAuto"), StringComparison.Ordinal);
        Assert.Contains("Wylogowano", AppStrings.Get("Audit_Desc_LogoutDesktop"), StringComparison.Ordinal);
    }
}
