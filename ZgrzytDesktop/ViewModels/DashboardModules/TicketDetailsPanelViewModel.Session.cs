using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketDetailsPanelViewModel
{
    internal void ClearSessionState()
    {
        TicketDetails = null;
        NewMessageText = string.Empty;
        Messages.Clear();
        TicketAuditLogEntries.Clear();
        ClearAssignableUsers();
        SelectedStatus = null;
        SelectedPriority = null;
        DetailsStatusMessage = AppStrings.Get("Details_SelectFromList");
    }
}
