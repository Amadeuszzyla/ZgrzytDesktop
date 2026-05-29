using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.ViewModels.DashboardModules;
namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private void ShowTicketsPage()
    {
        CurrentSection = AppSections.Tickets;
    }

    private void ShowSettingsPage()
    {
        CurrentSection = AppSections.Settings;
        SafeFireAndForget.Run(AuditPanel.RefreshAsync());
    }

    private void ShowRequestAccountPage()
    {
        CurrentSection = AppSections.RequestAccount;
    }

    private void ShowStatisticsPage()
    {
        CurrentSection = AppSections.Statistics;
    }

    private void ShowAdminPage()
    {
        CurrentSection = AppSections.Admin;
        AdminPanel.PrepareAdminPage(IsAdminRole);
    }

    private async Task LogoutAsync()
    {
        _ticketPolling?.Stop();

        await LogAuditAsync("Logout", null, "Audit_Desc_LogoutDesktop", null);

        ShowToastKey("Toast_LoggedOut", ToastTypes.Info);

        await _onLogoutRequested();
    }
}
