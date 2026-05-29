namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    internal void ClearSessionData()
    {
        _ticketPolling?.Stop();
        TicketsPanel.ClearSessionState();
        TicketDetailsPanel.ClearSessionState();
    }
}
