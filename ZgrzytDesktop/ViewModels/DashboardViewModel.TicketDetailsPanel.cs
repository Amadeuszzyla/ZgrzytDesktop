using ZgrzytDesktop.Constants;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public TicketDetailsPanelViewModel TicketDetailsPanel { get; private set; } = null!;

    private void InitializeTicketDetailsPanel()
    {
        TicketDetailsPanel = new TicketDetailsPanelViewModel(
            _ticketService,
            _auditLogService,
            new TicketDetailsPanelCallbacks
            {
                ShowToastKey = ShowToastKey,
                ShowToastRaw = ShowToast,
                SetIsOffline = value => IsOffline = value,
                GetIsOffline = () => IsOffline,
                GetApiErrorMessage = GetApiErrorMessage,
                FindCachedTicket = ticketId => TicketsPanel.FindCachedTicket(ticketId),
                NotifyDetailsSideEffects = () => OnPropertyChanged(nameof(CanOpenDetailsPage)),
                NotifyDetailsLoadingChanged = () => TicketDetailsPanel.NotifyCapabilityProperties(),
                GetCurrentUser = () => CurrentUser,
                GetCanManageTickets = () => CanManageTickets,
                GetIsRegularUser = () => IsRegularUser,
                LogAuditAsync = LogAuditAsync,
                RefreshTicketsAsync = async () => await LoadTicketsAsync(),
                NavigateToTickets = () => CurrentSection = AppSections.Tickets,
                ClearSelectedTicket = () => TicketsPanel.SelectedTicket = null,
                ExecuteApiAsyncCore = ExecuteApiAsync
            });

        TicketDetailsPanel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TicketDetailsPanelViewModel.TicketDetails))
                OnPropertyChanged(nameof(CanOpenDetailsPage));
        };
    }
}
