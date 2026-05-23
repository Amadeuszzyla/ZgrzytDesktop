using System;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketDetailsPanelViewModel
{
    public bool CanUseOnlineDetailsActions => !_callbacks.GetIsOffline() && !IsLoadingDetails;

    public bool CanSendMessage => !_callbacks.GetIsOffline() && !IsLoadingDetails;

    public bool CanEditTicket => _callbacks.GetCanManageTickets() && !_callbacks.GetIsOffline() && !IsLoadingDetails;

    public bool CanAssignTicket => _callbacks.GetCanManageTickets() && !_callbacks.GetIsOffline() && !IsLoadingDetails;

    public bool CanCloseOwnTicket =>
        _callbacks.GetIsRegularUser() &&
        TicketDetails is not null &&
        TicketDetails.UserId == _callbacks.GetCurrentUser().Id &&
        !string.Equals(TicketDetails.Status, TicketStatuses.Zamkniete, StringComparison.OrdinalIgnoreCase);

    public bool CanCloseTicket =>
        !_callbacks.GetIsOffline() &&
        !IsLoadingDetails &&
        TicketDetails is not null &&
        !string.Equals(TicketDetails.Status, TicketStatuses.Zamkniete, StringComparison.OrdinalIgnoreCase) &&
        (_callbacks.GetCanManageTickets() || CanCloseOwnTicket);

    public bool CanDeleteTicket =>
        _callbacks.GetCanManageTickets() &&
        !_callbacks.GetIsOffline() &&
        !IsLoadingDetails &&
        TicketDetails is not null;

    public void NotifyCapabilityProperties()
    {
        OnPropertyChanged(nameof(CanUseOnlineDetailsActions));
        OnPropertyChanged(nameof(CanSendMessage));
        OnPropertyChanged(nameof(CanEditTicket));
        OnPropertyChanged(nameof(CanAssignTicket));
        OnPropertyChanged(nameof(CanCloseOwnTicket));
        OnPropertyChanged(nameof(CanCloseTicket));
        OnPropertyChanged(nameof(CanDeleteTicket));
    }
}
