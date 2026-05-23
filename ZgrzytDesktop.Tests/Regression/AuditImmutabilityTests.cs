using System.Reflection;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.Tests.ViewModels;
using ZgrzytDesktop.ViewModels;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.Regression;

public class AuditImmutabilityTests
{
    public AuditImmutabilityTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void DashboardViewModel_DoesNotExposeClearAuditLogsCommand()
    {
        Assert.Null(typeof(DashboardViewModel).GetProperty(
            "ClearAuditLogsCommand",
            BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public async Task LogAuditAsync_NewEntry_UsesDetailsKey_NotPreTranslatedText()
    {
        AppStrings.ApplyCulture("pl");

        var (_, _, _, tempDir) = ViewModelTestFactory.CreateDashboard("it");

        try
        {
            var audit = new LocalAuditLogService(tempDir);
            await audit.AddAsync(AuditLogEntryFactory.Create(
                "BanUser",
                "admin",
                ticketId: null,
                "Audit_Desc_UserBanned",
                "jan.kowalski"));

            var loaded = await audit.LoadAsync();

            Assert.Single(loaded);
            Assert.Equal("Audit_Desc_UserBanned", loaded[0].DetailsKey);
            Assert.Empty(loaded[0].Description);

            AppStrings.ApplyCulture("en");
            Assert.Equal("Banned user: jan.kowalski.", loaded[0].DisplayDescription);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task AuditPanel_NotifyLocalization_RefreshesKeyedEntryDisplay()
    {
        var audit = new FakeAuditLogService();
        audit.Entries.Add(new AuditLogEntry
        {
            Action = "Logout",
            DetailsKey = "Audit_Desc_LogoutDesktop"
        });

        var panel = new AuditPanelViewModel(audit);
        await panel.RefreshAsync();

        AppStrings.ApplyCulture("pl");
        Assert.Contains("Wylogowano", panel.AuditLogEntries[0].DisplayDescription, StringComparison.Ordinal);

        panel.NotifyLocalization();

        AppStrings.ApplyCulture("en");
        Assert.Contains("signed out", panel.AuditLogEntries[0].DisplayDescription, StringComparison.OrdinalIgnoreCase);
    }
}
