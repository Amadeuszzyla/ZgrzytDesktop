namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private void UpdateTicketStatistics() =>
        StatisticsPanel.ApplyFromTickets(_allTickets, TotalTickets, fromCurrentPageOnly: true);
}
