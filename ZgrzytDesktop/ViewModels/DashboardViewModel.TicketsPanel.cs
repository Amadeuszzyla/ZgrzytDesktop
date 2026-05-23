using System.Threading.Tasks;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public TicketsPanelViewModel TicketsPanel { get; private set; } = null!;

    private void InitializeTicketsPanel()
    {
        TicketsPanel = new TicketsPanelViewModel(
            _ticketService,
            _ticketCacheService,
            new TicketsPanelCallbacks
            {
                ShowToast = ShowToast,
                SetIsOffline = value => IsOffline = value,
                GetIsOffline = () => IsOffline,
                NotifyStatistics = (tickets, total) =>
                    StatisticsPanel.ApplyFromTickets(tickets, total, fromCurrentPageOnly: true),
                NotifyTicketsLoadingChanged = () => StatisticsPanel.NotifyTicketsLoadingChanged(),
                NotifyOnlineActionsChanged = NotifyOnlineActionsChanged,
                GetApiErrorMessage = GetApiErrorMessage,
                TicketSelected = ticketId => _ = LoadTicketDetailsAndOpenAsync(ticketId),
                RefreshPaginationSideEffects = () => TicketDetailsPanel.NotifyCapabilityProperties(),
                LogAuditAsync = LogAuditAsync,
                ExecuteApiAsyncCore = ExecuteApiAsync
            });

        TicketsPanel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TicketsPanelViewModel.IsLoading))
                NotifyOnlineActionsChanged();

            if (e.PropertyName == nameof(TicketsPanelViewModel.SelectedTicket))
                OnPropertyChanged(nameof(CanOpenDetailsPage));
        };
    }

    private void NotifyOnlineActionsChanged()
    {
        OnPropertyChanged(nameof(CanUseOnlineActions));
        OnPropertyChanged(nameof(CanRequestAccount));
    }

    private Task LoadTicketsAsync(bool silentRefresh = false) =>
        TicketsPanel.LoadTicketsAsync(silentRefresh);
}
