using System;
using System.Collections.ObjectModel;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _detailsStatusMessage = "Wybierz zgłoszenie z listy.";
    private string _newMessageText = string.Empty;
    private bool _isLoadingDetails;

    private string? _selectedStatus;
    private string? _selectedPriority;

    private Ticket? _ticketDetails;

    public ObservableCollection<Message> Messages { get; } = new();

    public bool HasNoMessages => Messages.Count == 0;

    public ObservableCollection<AuditLogEntry> TicketAuditLogEntries { get; } = new();

    public bool HasNoTicketAuditLogEntries => TicketAuditLogEntries.Count == 0;

    public Ticket? TicketDetails
    {
        get => _ticketDetails;
        set
        {
            if (SetProperty(ref _ticketDetails, value))
            {
                OnPropertyChanged(nameof(CanCloseOwnTicket));
                OnPropertyChanged(nameof(CanCloseTicket));
                OnPropertyChanged(nameof(CanDeleteTicket));
                OnPropertyChanged(nameof(CanOpenDetailsPage));
            }
        }
    }

    public string DetailsStatusMessage
    {
        get => _detailsStatusMessage;
        set => SetProperty(ref _detailsStatusMessage, value);
    }

    public string NewMessageText
    {
        get => _newMessageText;
        set => SetProperty(ref _newMessageText, value);
    }

    public string? SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public string? SelectedPriority
    {
        get => _selectedPriority;
        set => SetProperty(ref _selectedPriority, value);
    }

    public bool IsLoadingDetails
    {
        get => _isLoadingDetails;
        set
        {
            if (SetProperty(ref _isLoadingDetails, value))
            {
                OnPropertyChanged(nameof(IsNotLoadingDetails));
                OnPropertyChanged(nameof(CanUseOnlineDetailsActions));
                OnPropertyChanged(nameof(CanSendMessage));
                OnPropertyChanged(nameof(CanEditTicket));
                OnPropertyChanged(nameof(CanAssignTicket));
                OnPropertyChanged(nameof(CanCloseOwnTicket));
                OnPropertyChanged(nameof(CanCloseTicket));
                OnPropertyChanged(nameof(CanDeleteTicket));
            }
        }
    }

    public bool IsNotLoadingDetails => !IsLoadingDetails;

    public bool CanUseOnlineDetailsActions => !IsOffline && !IsLoadingDetails;

    public bool CanSendMessage => !IsOffline && !IsLoadingDetails;

    public bool CanEditTicket => CanManageTickets && !IsOffline && !IsLoadingDetails;

    public bool CanAssignTicket => CanManageTickets && !IsOffline && !IsLoadingDetails;

    public bool CanCloseOwnTicket =>
        IsRegularUser &&
        TicketDetails is not null &&
        TicketDetails.UserId == CurrentUser.Id &&
        !string.Equals(TicketDetails.Status, TicketStatuses.Zamkniete, StringComparison.OrdinalIgnoreCase);

    public bool CanCloseTicket =>
        !IsOffline &&
        !IsLoadingDetails &&
        TicketDetails is not null &&
        !string.Equals(TicketDetails.Status, TicketStatuses.Zamkniete, StringComparison.OrdinalIgnoreCase) &&
        (CanManageTickets || CanCloseOwnTicket);

    public bool CanDeleteTicket =>
        CanManageTickets &&
        !IsOffline &&
        !IsLoadingDetails &&
        TicketDetails is not null;
}
