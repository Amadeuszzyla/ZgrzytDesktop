using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private async Task SearchTicketsAsync()
    {
        SetCurrentPageSilently(1);
        await LoadTicketsAsync();
    }

    private async Task GoToFirstPageAsync()
    {
        if (CurrentPage == 1)
            return;

        CurrentPage = 1;
        await Task.CompletedTask;
    }

    private async Task GoToPreviousPageAsync()
    {
        if (CurrentPage <= 1)
            return;

        CurrentPage--;
        await Task.CompletedTask;
    }

    private async Task GoToNextPageAsync()
    {
        if (CurrentPage >= LastPage)
            return;

        CurrentPage++;
        await Task.CompletedTask;
    }

    private async Task GoToLastPageAsync()
    {
        if (CurrentPage == LastPage)
            return;

        CurrentPage = LastPage;
        await Task.CompletedTask;
    }

    private async Task LoadTicketsAsync(bool silentRefresh = false)
    {
        try
        {
            if (!silentRefresh)
            {
                IsLoading = true;
                StatusMessage = "Pobieranie zgłoszeń...";
            }

            var response = await _ticketService.GetTicketsAsync(
                page: CurrentPage,
                perPage: PageSize,
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                status: GetSelectedFilterValue(SelectedFilterStatus),
                priority: GetSelectedFilterValue(SelectedFilterPriority),
                sortBy: SelectedTicketSortField?.SortBy ?? TicketSortHelper.DefaultField.SortBy,
                sortDirection: SelectedTicketSortDirection?.Direction ?? TicketSortHelper.DefaultDirection.Direction,
                queueView: GetSelectedTicketQueueView()
            );

            IsOffline = false;

            _allTickets.Clear();

            if (response?.Data is not null)
            {
                _allTickets.AddRange(response.Data);

                TotalTickets = response.Total;
                LastPage = Math.Max(1, (int)Math.Ceiling((double)TotalTickets / PageSize));

                if (CurrentPage > LastPage)
                {
                    SetCurrentPageSilently(LastPage);
                    await LoadTicketsAsync(silentRefresh);
                    return;
                }

                await _ticketCacheService.SaveTicketsAsync(_allTickets);

                ApplyVisibleTickets();
                UpdateTicketStatistics();

                StatusMessage = $"Pobrano zgłoszeń: {Tickets.Count} z {TotalTickets}";

                if (!silentRefresh)
                    PollingStatusMessage = AutoRefreshStatusText;
            }
            else
            {
                Tickets.Clear();
                TotalTickets = 0;
                LastPage = 1;
                UpdatePageNumbers();
                UpdateTicketStatistics();
                StatusMessage = "Brak zgłoszeń.";
            }
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;

            if (!silentRefresh)
                ShowToast("Brak połączenia z API. Pokazuję dane offline.", "warning");

            await LoadTicketsFromCacheAsync();
        }
        catch (ApiException ex)
        {
            StatusMessage = GetApiErrorMessage(ex);

            if (!silentRefresh)
                ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            StatusMessage = "Wystąpił nieoczekiwany błąd podczas pobierania zgłoszeń.";

            if (!silentRefresh)
                ShowToast("Wystąpił błąd podczas pobierania zgłoszeń.", "error");
        }
        finally
        {
            if (!silentRefresh)
                IsLoading = false;

            RefreshPaginationProperties();
        }
    }

    private async Task LoadTicketsFromCacheAsync()
    {
        var cachedTickets = await _ticketCacheService.LoadTicketsAsync();

        _allTickets.Clear();
        _allTickets.AddRange(cachedTickets);

        TotalTickets = cachedTickets.Count;
        LastPage = Math.Max(1, (int)Math.Ceiling((double)TotalTickets / PageSize));

        if (CurrentPage > LastPage)
            SetCurrentPageSilently(LastPage);

        var pageItems = _allTickets
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Tickets.Clear();

        foreach (var ticket in pageItems)
            Tickets.Add(ticket);

        UpdatePageNumbers();

        StatusMessage = cachedTickets.Count > 0
            ? $"Brak połączenia z API. Pokazuję dane offline: {Tickets.Count} zgłoszeń."
            : "Brak połączenia z API i brak zapisanych danych offline.";

        UpdateTicketStatistics();
    }

    private async Task RefreshTicketsNowAsync()
    {
        await LoadTicketsAsync();
        PollingStatusMessage = $"{AutoRefreshStatusText} Ostatnie odświeżenie: {DateTime.Now:HH:mm:ss}.";
    }

    private async Task AutoRefreshTicketsAsync()
    {
        if (CurrentSection != "Tickets" || IsOffline || IsLoading)
            return;

        try
        {
            await LoadTicketsAsync(silentRefresh: true);
            _autoRefreshErrorToastShown = false;
            PollingStatusMessage = AutoRefreshStatusText;
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            PollingStatusMessage = "Brak połączenia z API. Automatyczne odświeżanie wstrzymane.";

            if (!_autoRefreshErrorToastShown)
            {
                _autoRefreshErrorToastShown = true;
                ShowToast("Utracono połączenie z API.", "warning");
            }
        }
        catch
        {
            PollingStatusMessage = "Nie udało się automatycznie odświeżyć listy.";

            if (!_autoRefreshErrorToastShown)
            {
                _autoRefreshErrorToastShown = true;
                ShowToast("Nie udało się odświeżyć listy zgłoszeń.", "error");
            }
        }
    }

    private void ApplyVisibleTickets()
    {
        Tickets.Clear();

        foreach (var ticket in _allTickets)
            Tickets.Add(ticket);

        UpdatePageNumbers();
        RefreshPaginationProperties();
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedFilterStatus = "Wszystkie";
        SelectedFilterPriority = "Wszystkie";
        SetSelectedTicketQueueViewSilently("Wszystkie");
        SetCurrentPageSilently(1);

        _ = LoadTicketsAsync();
    }

    private async Task CreateTicketAsync()
    {
        if (IsOffline)
        {
            CreateTicketStatusMessage = "Nie można utworzyć zgłoszenia w trybie offline.";

            ShowToast("Nie można utworzyć zgłoszenia w trybie offline.", "warning");

            return;
        }

        if (string.IsNullOrWhiteSpace(NewTicketTitle))
        {
            CreateTicketStatusMessage = "Podaj tytuł zgłoszenia.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTicketDescription))
        {
            CreateTicketStatusMessage = "Podaj opis zgłoszenia.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedNewTicketCategory))
        {
            CreateTicketStatusMessage = "Wybierz kategorię zgłoszenia.";
            return;
        }

        try
        {
            IsLoading = true;
            CreateTicketStatusMessage = "Tworzenie zgłoszenia...";

            var request = new CreateTicketRequest
            {
                Title = TicketCategoryHelper.FormatTitle(SelectedNewTicketCategory, NewTicketTitle),
                Description = TicketCategoryHelper.FormatDescription(
                    SelectedNewTicketCategory,
                    NewTicketDescription),
                Priority = NewTicketPriority
            };

            var createdTicket = await _ticketService.CreateTicketAsync(request);

            IsOffline = false;

            NewTicketTitle = string.Empty;
            NewTicketDescription = string.Empty;
            NewTicketPriority = "niski";
            SelectedNewTicketCategory = "Hardware";

            SetCurrentPageSilently(1);
            await LoadTicketsAsync();

            if (createdTicket is not null)
                SelectedTicket = Tickets.FirstOrDefault(ticket => ticket.Id == createdTicket.Id);

            CreateTicketStatusMessage = "Zgłoszenie zostało utworzone.";

            ShowToast("Nowe zgłoszenie zostało utworzone.", "success");

            if (createdTicket is not null)
            {
                await LogAuditAsync(
                    "CreateTicket",
                    createdTicket.Id,
                    $"Utworzono zgłoszenie: {createdTicket.Title}");
            }
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            CreateTicketStatusMessage = "Brak połączenia z API. Nie można utworzyć zgłoszenia offline.";

            ShowToast("Brak połączenia z API. Nie można utworzyć zgłoszenia.", "error");
        }
        catch (ApiException ex)
        {
            CreateTicketStatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            CreateTicketStatusMessage = "Wystąpił nieoczekiwany błąd podczas tworzenia zgłoszenia.";

            ShowToast("Wystąpił błąd podczas tworzenia zgłoszenia.", "error");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
