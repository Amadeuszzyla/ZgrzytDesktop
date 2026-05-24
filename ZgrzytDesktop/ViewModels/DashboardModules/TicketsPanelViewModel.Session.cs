using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketsPanelViewModel
{
    internal void ClearSessionState()
    {
        _allTickets.Clear();
        Tickets.Clear();
        SelectedTicket = null;
        StatusMessage = AppStrings.Get("Tickets_StatusReady");
    }
}
