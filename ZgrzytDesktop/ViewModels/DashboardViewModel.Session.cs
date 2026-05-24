namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    internal void ClearSessionData()
    {
        _ticketPollingTimer?.Stop();
        TicketsPanel.ClearSessionState();
        TicketDetailsPanel.ClearSessionState();
    }
}
