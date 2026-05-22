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

        var panel = new AuditPanelViewModel(audit, (_, _) => { });

        await panel.RefreshAsync();

        Assert.Equal(2, panel.AuditLogEntries.Count);
        Assert.Equal("newer", panel.AuditLogEntries[0].UserLogin);
        Assert.False(panel.HasNoAuditLogEntries);
    }

    [Fact]
    public async Task ClearAuditLogsCommand_ClearsEntriesAndShowsToast()
    {
        var audit = new FakeAuditLogService();
        audit.Entries.Add(new AuditLogEntry { UserLogin = "u", Action = "Test", Description = "d" });

        string? toastMessage = null;
        var panel = new AuditPanelViewModel(audit, (message, _) => toastMessage = message);

        await panel.ClearAuditLogsCommand.ExecuteAsync(null);

        Assert.Empty(panel.AuditLogEntries);
        Assert.True(panel.HasNoAuditLogEntries);
        Assert.Equal("Lokalny audyt został wyczyszczony.", toastMessage);
    }
}
