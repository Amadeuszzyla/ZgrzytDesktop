using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private void UpdateTicketStatistics()
    {
        ApplyTicketStatistics(_allTickets, TotalTickets, fromCurrentPageOnly: true);
    }

    private void ApplyTicketStatistics(IReadOnlyList<Ticket> tickets, int totalInSystem, bool fromCurrentPageOnly)
    {
        StatsTotalTickets = tickets.Count;
        StatsNewTickets = tickets.Count(ticket =>
            string.Equals(ticket.Status, TicketStatuses.Nowe, StringComparison.OrdinalIgnoreCase));
        StatsInProgressTickets = tickets.Count(ticket =>
            string.Equals(ticket.Status, TicketStatuses.WTrakcie, StringComparison.OrdinalIgnoreCase));
        StatsClosedTickets = tickets.Count(ticket =>
            string.Equals(ticket.Status, TicketStatuses.Zamkniete, StringComparison.OrdinalIgnoreCase));

        StatsLowPriorityTickets = tickets.Count(ticket =>
            string.Equals(ticket.Priority, TicketPriorities.Low, StringComparison.OrdinalIgnoreCase));
        StatsMediumPriorityTickets = tickets.Count(ticket =>
            string.Equals(ticket.Priority, TicketPriorities.Medium, StringComparison.OrdinalIgnoreCase));
        StatsHighPriorityTickets = tickets.Count(ticket =>
            string.Equals(ticket.Priority, TicketPriorities.High, StringComparison.OrdinalIgnoreCase));

        StatsAssignedTickets = tickets.Count(ticket => ticket.AssignedItId.HasValue);
        StatsUnassignedTickets = Math.Max(0, tickets.Count - StatsAssignedTickets);

        StatsStatusChartMaximum = Math.Max(
            1,
            Math.Max(StatsNewTickets, Math.Max(StatsInProgressTickets, StatsClosedTickets)));
        StatsPriorityChartMaximum = Math.Max(
            1,
            Math.Max(StatsLowPriorityTickets, Math.Max(StatsMediumPriorityTickets, StatsHighPriorityTickets)));
        StatsAssignmentChartMaximum = Math.Max(
            1,
            Math.Max(StatsAssignedTickets, StatsUnassignedTickets));

        StatsScopeMessage = StatsTotalTickets == 0
            ? "Brak zgłoszeń do analizy."
            : fromCurrentPageOnly && totalInSystem > StatsTotalTickets
                ? $"Statystyki dla {StatsTotalTickets} zgłoszeń na bieżącej stronie listy (łącznie w systemie: {totalInSystem})."
                : $"Statystyki dla {StatsTotalTickets} zgłoszeń (łącznie w systemie: {totalInSystem}).";
    }
    private async Task LoadAllPagesStatisticsAsync()
    {
        if (IsOffline)
        {
            StatsScopeMessage = "Statystyki wielostronicowe wymagają połączenia z API.";
            return;
        }

        try
        {
            IsLoadingAllStatistics = true;

            var aggregated = new List<Ticket>();
            var page = 1;
            var lastPage = 1;
            var totalInSystem = 0;

            do
            {
                var response = await _ticketService.GetTicketsAsync(
                    page: page,
                    perPage: 50,
                    queueView: TicketQueueView.All);

                if (response?.Data is null || response.Data.Count == 0)
                    break;

                aggregated.AddRange(response.Data);
                lastPage = Math.Max(1, response.LastPage);
                totalInSystem = response.Total;
                page++;
            } while (page <= lastPage);

            ApplyTicketStatistics(aggregated, totalInSystem, fromCurrentPageOnly: false);
            ShowToast($"Zaktualizowano statystyki ({aggregated.Count} zgłoszeń).", ToastTypes.Success);
        }
        catch (ApiException ex)
        {
            StatsScopeMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), ToastTypes.Error);
        }
        catch
        {
            StatsScopeMessage = "Nie udało się pobrać statystyk ze wszystkich stron.";
            ShowToast("Nie udało się pobrać statystyk.", ToastTypes.Error);
        }
        finally
        {
            IsLoadingAllStatistics = false;
        }
    }
}
