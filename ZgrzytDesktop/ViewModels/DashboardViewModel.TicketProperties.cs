using System;
using System.Collections.ObjectModel;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _pollingStatusMessage = string.Empty;

    public ObservableCollection<string> AvailableStatuses { get; } = new();

    public ObservableCollection<string> AvailablePriorities { get; } = new();

    public bool CanOpenDetailsPage =>
        TicketsPanel.SelectedTicket is not null || TicketDetailsPanel.TicketDetails is not null;

    public string PollingStatusMessage
    {
        get => _pollingStatusMessage;
        set => SetProperty(ref _pollingStatusMessage, value);
    }

    public bool CanManageTickets =>
        string.Equals(CurrentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(CurrentUser.Role, AppRoles.It, StringComparison.OrdinalIgnoreCase);

    public bool IsRegularUser =>
        string.Equals(CurrentUser.Role, AppRoles.User, StringComparison.OrdinalIgnoreCase);

    public bool CanUseOnlineActions => !IsOffline && !TicketsPanel.IsLoading;

    private void InitializeTicketCollections()
    {
        RefreshAvailableStatusAndPriorityOptions();
    }

    internal void RefreshAvailableStatusAndPriorityOptions()
    {
        var selectedStatusApi = string.IsNullOrWhiteSpace(TicketDetailsPanel.SelectedStatus)
            ? null
            : StatusDisplayHelper.ToApiStatus(TicketDetailsPanel.SelectedStatus);
        var selectedPriorityApi = string.IsNullOrWhiteSpace(TicketDetailsPanel.SelectedPriority)
            ? null
            : PriorityDisplayHelper.ToApiPriority(TicketDetailsPanel.SelectedPriority);
        var newTicketPriorityApi = PriorityDisplayHelper.ToApiPriority(TicketsPanel.NewTicketPriority);

        AvailableStatuses.Clear();
        foreach (var apiStatus in new[]
                 {
                     TicketStatuses.Nowe,
                     TicketStatuses.WTrakcie,
                     TicketStatuses.Zamkniete
                 })
            AvailableStatuses.Add(StatusDisplayHelper.ToDisplayStatus(apiStatus));

        AvailablePriorities.Clear();
        foreach (var apiPriority in new[]
                 {
                     TicketPriorities.Low,
                     TicketPriorities.Medium,
                     TicketPriorities.High
                 })
            AvailablePriorities.Add(PriorityDisplayHelper.ToDisplayPriority(apiPriority));

        if (selectedStatusApi is not null)
            TicketDetailsPanel.SelectedStatus = StatusDisplayHelper.ToDisplayStatus(selectedStatusApi);

        if (selectedPriorityApi is not null)
            TicketDetailsPanel.SelectedPriority = PriorityDisplayHelper.ToDisplayPriority(selectedPriorityApi);

        TicketsPanel.NewTicketPriority = PriorityDisplayHelper.ToDisplayPriority(newTicketPriorityApi);
    }
}
