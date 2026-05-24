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
            _userAdminService,
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
                GetIsAdminRole = () => IsAdminRole,
                GetIsRegularUser = () => IsRegularUser,
                LogAuditAsync = LogAuditAsync,
                RefreshTicketsAsync = async () => await LoadTicketsAsync(),
                NavigateToTickets = () => CurrentSection = AppSections.Tickets,
                ClearSelectedTicket = () => TicketsPanel.SelectedTicket = null,
                ExecuteApiAsyncCore = ExecuteApiAsync,
                ConfirmAsync = ConfirmRiskyActionAsync
            });

        TicketDetailsPanel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(TicketDetailsPanelViewModel.TicketDetails):
                    OnPropertyChanged(nameof(CanOpenDetailsPage));
                    break;
                case nameof(TicketDetailsPanelViewModel.CanAssignSelectedUser):
                case nameof(TicketDetailsPanelViewModel.CanSelectAssignee):
                case nameof(TicketDetailsPanelViewModel.CanShowAdminAssignmentControls):
                case nameof(TicketDetailsPanelViewModel.ShowAssignableUsersEmptyMessage):
                case nameof(TicketDetailsPanelViewModel.SelectedAssignableUser):
                case nameof(TicketDetailsPanelViewModel.SelectedAssignedUser):
                    OnPropertyChanged($"TicketDetailsPanel.{e.PropertyName}");
                    break;
            }
        };
    }
}
