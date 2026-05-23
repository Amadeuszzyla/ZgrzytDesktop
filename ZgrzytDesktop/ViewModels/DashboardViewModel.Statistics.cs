namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private void UpdateTicketStatistics() =>
        StatisticsPanel.ApplyFromTickets(TicketsPanel.AllTickets, TicketsPanel.TotalTickets, fromCurrentPageOnly: true);
}
