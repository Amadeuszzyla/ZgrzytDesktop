using System.Threading.Tasks;
using ZgrzytDesktop.Constants;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private async Task LoadTicketDetailsAndOpenAsync(int ticketId)
    {
        await TicketDetailsPanel.LoadAsync(ticketId);
        CurrentSection = AppSections.Details;
    }
}
