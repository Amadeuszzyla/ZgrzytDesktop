using System.Reflection;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Tests.Infrastructure.Fakes;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.Tests.ViewModels;

public class AuditPanelViewModelTests
{
    [Fact]
    public async Task RefreshAsync_LoadsEntriesNewestFirst()
    {
        var audit = new FakeAuditLogService();
        audit.Entries.Add(new AuditLogEntry
        {
            Timestamp = new DateTime(2026, 1, 1, 10, 0, 0),
            UserLogin = "older",
            Action = "A",
            Description = "first"
        });
        audit.Entries.Add(new AuditLogEntry
        {
            Timestamp = new DateTime(2026, 1, 2, 10, 0, 0),
            UserLogin = "newer",
            Action = "B",
            Description = "second"
        });

        var panel = new AuditPanelViewModel(audit);

        await panel.RefreshAsync();

        Assert.Equal(2, panel.AuditLogEntries.Count);
        Assert.Equal("newer", panel.AuditLogEntries[0].UserLogin);
        Assert.False(panel.HasNoAuditLogEntries);
    }

    [Fact]
    public void AuditPanelViewModel_DoesNotExposeClearAuditCommandToUi()
    {
        var panel = new AuditPanelViewModel(new FakeAuditLogService());

        Assert.Null(panel.GetType().GetProperty("ClearAuditLogsCommand", BindingFlags.Instance | BindingFlags.Public));
    }
}
