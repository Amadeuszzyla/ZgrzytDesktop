using System;
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
    private async Task LoadTicketDetailsAndOpenAsync(int ticketId)
    {
        await LoadTicketDetailsAsync(ticketId);
        CurrentSection = AppSections.Details;
    }

    private async Task LoadTicketDetailsAsync(int ticketId)
    {
        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Pobieranie szczegółów zgłoszenia...";

            if (IsOffline)
            {
                var cachedTicket = _allTickets.FirstOrDefault(ticket => ticket.Id == ticketId);

                TicketDetails = cachedTicket;

                Messages.Clear();

                if (cachedTicket?.Messages is not null)
                {
                    foreach (var message in cachedTicket.Messages)
                        Messages.Add(message);
                }

                NotifyMessagesUiState();

                SelectedStatus = StatusDisplayHelper.ToDisplayStatus(cachedTicket?.Status);
                SelectedPriority = cachedTicket?.Priority;

                DetailsStatusMessage = cachedTicket is null
                    ? "Nie znaleziono zgłoszenia w danych offline."
                    : $"Wybrano zgłoszenie #{cachedTicket.Id} z cache offline.";

                return;
            }

            var ticket = await _ticketService.GetTicketAsync(ticketId);

            IsOffline = false;

            if (!IsValidTicketForDisplay(ticket))
            {
                DetailsStatusMessage =
                    "Serwer zwrócił nieprawidłowe dane zgłoszenia. Poprzednie dane pozostają na ekranie.";

                ShowToast(
                    "Serwer zwrócił stronę błędu zamiast danych API. Sprawdź endpoint lub uprawnienia.",
                    ToastTypes.Error);

                return;
            }

            TicketDetails = ticket;

            Messages.Clear();

            var messages = await _ticketService.GetTicketMessagesAsync(ticketId);

            foreach (var message in messages)
                Messages.Add(message);

            NotifyMessagesUiState();

            SelectedStatus = StatusDisplayHelper.ToDisplayStatus(ticket?.Status);
            SelectedPriority = ticket?.Priority;

            DetailsStatusMessage = ticket is null
                ? "Nie udało się pobrać szczegółów zgłoszenia."
                : $"Wybrano zgłoszenie #{ticket.Id}.";
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            IsOffline = true;
            DetailsStatusMessage = "Brak połączenia z API. Pokazuję dane dostępne offline.";

            ShowToast("Brak połączenia z API. Szczegóły zgłoszenia są dostępne offline.", ToastTypes.Warning);
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = GetApiErrorMessage(ex);

            ShowToast(GetApiErrorMessage(ex), ToastTypes.Error);
        }
        catch
        {
            DetailsStatusMessage = "Wystąpił nieoczekiwany błąd podczas pobierania szczegółów zgłoszenia.";

            ShowToast("Wystąpił błąd podczas pobierania szczegółów zgłoszenia.", ToastTypes.Error);
        }
        finally
        {
            IsLoadingDetails = false;

            if (TicketDetails?.Id is int detailsTicketId)
                await RefreshTicketAuditLogAsync(detailsTicketId);
        }
    }

    private async Task SendMessageAsync()
    {
        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można wysłać wiadomości w trybie offline.";

            ShowToast("Nie można wysłać wiadomości w trybie offline.", ToastTypes.Warning);

            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewMessageText))
        {
            DetailsStatusMessage = "Treść wiadomości nie może być pusta.";
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Wysyłanie wiadomości...";

            var ticketId = TicketDetails.Id;
            var messageBody = NewMessageText.Trim();

            await ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.SendMessageAsync(ticketId, messageBody);

                    IsOffline = false;

                    NewMessageText = string.Empty;

                    await LoadTicketDetailsAsync(ticketId);

                    DetailsStatusMessage = "Wiadomość została wysłana.";

                    ShowToast("Wiadomość została wysłana.", ToastTypes.Success);

                    await LogAuditAsync(
                        "SendMessage",
                        ticketId,
                        "Wysłano wiadomość w zgłoszeniu.");
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessage: "Wystąpił nieoczekiwany błąd podczas wysyłania wiadomości.",
                unexpectedToastMessage: "Wystąpił błąd podczas wysyłania wiadomości.",
                onServiceUnavailableAsync: async _ =>
                {
                    IsOffline = true;
                    DetailsStatusMessage = "Brak połączenia z API. Nie można wysłać wiadomości offline.";
                    ShowToast("Brak połączenia z API. Wiadomość nie została wysłana.", ToastTypes.Error);
                    await Task.CompletedTask;
                });
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task UpdateTicketAsync()
    {
        if (!CanManageTickets)
        {
            DetailsStatusMessage = "Brak uprawnień do edycji zgłoszenia.";

            ShowToast("Brak uprawnień do edycji zgłoszenia.", ToastTypes.Warning);

            return;
        }

        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można edytować zgłoszenia w trybie offline.";

            ShowToast("Nie można edytować zgłoszenia w trybie offline.", ToastTypes.Warning);

            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedStatus))
        {
            DetailsStatusMessage = "Wybierz status zgłoszenia.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPriority))
        {
            DetailsStatusMessage = "Wybierz priorytet zgłoszenia.";
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Zapisywanie zmian...";

            var ticketId = TicketDetails.Id;

            var request = new UpdateTicketRequest
            {
                Status = StatusDisplayHelper.ToApiStatus(SelectedStatus),
                Priority = SelectedPriority
            };

            await ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.UpdateTicketAsync(ticketId, request);

                    IsOffline = false;

                    await LoadTicketDetailsAsync(ticketId);
                    await LoadTicketsAsync();

                    DetailsStatusMessage = "Zmiany zostały zapisane.";

                    ShowToast("Zmiany w zgłoszeniu zostały zapisane.", ToastTypes.Success);

                    await LogAuditAsync(
                        "UpdateTicket",
                        ticketId,
                        $"Zmieniono status na „{SelectedStatus}”, priorytet na „{SelectedPriority}”.");
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessage: "Wystąpił nieoczekiwany błąd podczas zapisywania zmian.",
                unexpectedToastMessage: "Wystąpił błąd podczas zapisywania zmian.",
                onServiceUnavailableAsync: async _ =>
                {
                    IsOffline = true;
                    DetailsStatusMessage = "Brak połączenia z API. Nie można zapisać zmian offline.";
                    ShowToast("Brak połączenia z API. Zmiany nie zostały zapisane.", ToastTypes.Error);
                    await Task.CompletedTask;
                });
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task CloseTicketAsync()
    {
        if (!CanCloseTicket)
        {
            DetailsStatusMessage = "Brak uprawnień do zamknięcia zgłoszenia.";
            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie.";
            return;
        }

        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można zamknąć zgłoszenia w trybie offline.";
            return;
        }

        var ticketId = TicketDetails.Id;
        var previousDetails = TicketDetails;
        var previousStatus = SelectedStatus;
        var previousPriority = SelectedPriority;

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Zamykanie zgłoszenia...";

            var request = new UpdateTicketRequest
            {
                Status = TicketStatuses.Zamkniete
            };

            var updatedTicket = await _ticketService.UpdateTicketAsync(ticketId, request);

            if (updatedTicket is not null && !IsValidTicketForDisplay(updatedTicket))
            {
                throw new ApiException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Serwer zwrócił stronę błędu zamiast danych API. Sprawdź endpoint lub uprawnienia.");
            }

            IsOffline = false;

            await LoadTicketDetailsAsync(ticketId);
            await LoadTicketsAsync();

            DetailsStatusMessage = "Zgłoszenie zostało zamknięte.";

            ShowToast("Zgłoszenie zostało zamknięte.", ToastTypes.Success);

            await LogAuditAsync(
                "CloseTicket",
                ticketId,
                "Zamknięto zgłoszenie.");
        }
        catch (ApiException ex)
        {
            TicketDetails = previousDetails;
            SelectedStatus = previousStatus;
            SelectedPriority = previousPriority;

            var errorMessage = GetApiErrorMessage(ex);

            DetailsStatusMessage = errorMessage;

            ShowToast(errorMessage, ToastTypes.Error);

            await LogAuditAsync(
                "CloseTicket",
                ticketId,
                "Nie udało się zamknąć zgłoszenia: brak uprawnień lub błąd serwera.");
        }
        catch
        {
            TicketDetails = previousDetails;
            SelectedStatus = previousStatus;
            SelectedPriority = previousPriority;

            DetailsStatusMessage = "Wystąpił błąd podczas zamykania zgłoszenia.";

            ShowToast("Nie udało się zamknąć zgłoszenia.", ToastTypes.Error);

            await LogAuditAsync(
                "CloseTicket",
                ticketId,
                "Nie udało się zamknąć zgłoszenia: brak uprawnień lub błąd serwera.");
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task DeleteTicketAsync()
    {
        if (!CanDeleteTicket || TicketDetails is null)
        {
            DetailsStatusMessage = "Brak uprawnień do usunięcia zgłoszenia.";
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Usuwanie zgłoszenia...";

            var ticketId = TicketDetails.Id;

            await ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.DeleteTicketAsync(ticketId);

                    TicketDetails = null;
                    SelectedTicket = null;
                    CurrentSection = AppSections.Tickets;

                    await LoadTicketsAsync();

                    DetailsStatusMessage = "Zgłoszenie zostało usunięte.";
                    ShowToast("Zgłoszenie zostało usunięte.", ToastTypes.Success);

                    await LogAuditAsync("DeleteTicket", ticketId, "Usunięto zgłoszenie.");
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessage: "Nie udało się usunąć zgłoszenia.",
                unexpectedToastMessage: "Nie udało się usunąć zgłoszenia.");
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }
    private async Task AssignToMeAsync()
    {
        if (!CanManageTickets)
        {
            DetailsStatusMessage = "Brak uprawnień do przypisania zgłoszenia.";

            ShowToast("Brak uprawnień do przypisania zgłoszenia.", ToastTypes.Warning);

            return;
        }

        if (IsOffline)
        {
            DetailsStatusMessage = "Nie można przypisać zgłoszenia w trybie offline.";

            ShowToast("Nie można przypisać zgłoszenia w trybie offline.", ToastTypes.Warning);

            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = "Najpierw wybierz zgłoszenie.";
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = "Przypisywanie zgłoszenia...";

            var ticketId = TicketDetails.Id;

            var request = new UpdateTicketRequest
            {
                AssignedItId = CurrentUser.Id
            };

            await ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.UpdateTicketAsync(ticketId, request);

                    IsOffline = false;

                    await LoadTicketDetailsAsync(ticketId);
                    await LoadTicketsAsync();

                    DetailsStatusMessage = "Zgłoszenie zostało przypisane do Ciebie.";

                    ShowToast("Zgłoszenie zostało przypisane do Ciebie.", ToastTypes.Success);

                    await LogAuditAsync(
                        "AssignToMe",
                        ticketId,
                        $"Przypisano zgłoszenie do użytkownika {CurrentUser.Login}.");
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessage: "Wystąpił nieoczekiwany błąd podczas przypisywania zgłoszenia.",
                unexpectedToastMessage: "Wystąpił błąd podczas przypisywania zgłoszenia.",
                onServiceUnavailableAsync: async _ =>
                {
                    IsOffline = true;
                    DetailsStatusMessage = "Brak połączenia z API. Nie można przypisać zgłoszenia offline.";
                    ShowToast("Brak połączenia z API. Zgłoszenie nie zostało przypisane.", ToastTypes.Error);
                    await Task.CompletedTask;
                });
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }
}
