using System;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private void ShowTicketsPage()
    {
        CurrentSection = AppSections.Tickets;
    }

    private void ShowSettingsPage()
    {
        CurrentSection = AppSections.Settings;
        _ = AuditPanel.RefreshAsync();
    }

    private void ConfigureTicketQueueViewsForRole()
    {
        TicketQueueViews.Clear();
        TicketQueueViews.Add(FilterLabels.All);

        if (CanManageTickets)
        {
            TicketQueueViews.Add(FilterLabels.Active);
            TicketQueueViews.Add(FilterLabels.Unassigned);
        }

        if (!TicketQueueViews.Contains(SelectedTicketQueueView))
            SelectedTicketQueueView = FilterLabels.All;
    }

    private void ShowRequestAccountPage()
    {
        CurrentSection = AppSections.RequestAccount;
    }

    private void ShowStatisticsPage()
    {
        CurrentSection = AppSections.Statistics;
    }

    private void ShowAdminPage()
    {
        CurrentSection = AppSections.Admin;
        AdminTab = IsAdminRole ? AdminTabs.Users : AdminTabs.NewAccount;

        if (IsAdminRole)
            _ = LoadAdminUsersAsync();
    }

    private async Task RequestAccountAsync()
    {
        if (IsOffline)
        {
            RequestAccountStatusMessage = "Nie można wysłać prośby w trybie offline.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestName))
        {
            RequestAccountStatusMessage = "Podaj imię i nazwisko.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestLogin))
        {
            RequestAccountStatusMessage = "Podaj login.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestEmail))
        {
            RequestAccountStatusMessage = "Podaj adres e-mail.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestPassword))
        {
            RequestAccountStatusMessage = "Podaj hasło.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestPasswordConfirmation))
        {
            RequestAccountStatusMessage = "Potwierdź hasło.";
            return;
        }

        if (!string.Equals(RequestPassword, RequestPasswordConfirmation, StringComparison.Ordinal))
        {
            RequestAccountStatusMessage = "Hasła nie są identyczne.";
            return;
        }

        try
        {
            IsRequestingAccount = true;
            RequestAccountStatusMessage = "Wysyłanie prośby...";

            await ExecuteApiAsync(
                async () =>
                {
                    var request = new RequestAccountRequest
                    {
                        Name = RequestName.Trim(),
                        Login = RequestLogin.Trim(),
                        Email = RequestEmail.Trim(),
                        Password = RequestPassword,
                        PasswordConfirmation = RequestPasswordConfirmation
                    };

                    var success = await _authService.RequestAccountAsync(request);

                    if (!success)
                    {
                        RequestAccountStatusMessage = "Nie udało się wysłać prośby o utworzenie konta.";
                        return;
                    }

                    IsOffline = false;

                    RequestName = string.Empty;
                    RequestLogin = string.Empty;
                    RequestEmail = string.Empty;
                    RequestPassword = string.Empty;
                    RequestPasswordConfirmation = string.Empty;

                    RequestAccountStatusMessage = "Prośba o utworzenie konta została wysłana.";
                    ShowToast("Prośba o utworzenie konta została wysłana.", ToastTypes.Success);

                    await LogAuditAsync(
                        "RequestAccount",
                        null,
                        $"Wysłano prośbę o utworzenie konta: {request.Login}.");
                },
                setStatusMessage: message => RequestAccountStatusMessage = message,
                unexpectedStatusMessage: "Wystąpił nieoczekiwany błąd podczas wysyłania prośby.",
                unexpectedToastMessage: "Wystąpił nieoczekiwany błąd podczas wysyłania prośby.",
                onServiceUnavailableAsync: async _ =>
                {
                    IsOffline = true;
                    RequestAccountStatusMessage = "Brak połączenia z API. Nie można wysłać prośby offline.";
                    ShowToast("Brak połączenia z API. Nie można wysłać prośby offline.", ToastTypes.Warning);
                    await Task.CompletedTask;
                });
        }
        finally
        {
            IsRequestingAccount = false;
        }
    }
    private async Task LogoutAsync()
    {
        if (_ticketPollingTimer is not null)
            _ticketPollingTimer.IsEnabled = false;

        await LogAuditAsync("Logout", null, "Wylogowano użytkownika z aplikacji desktopowej.");

        ShowToast("Wylogowano z aplikacji.", ToastTypes.Info);

        await _onLogoutRequested();
    }
    private void SetCurrentPageSilently(int page)
    {
        _isChangingPageInternally = true;
        CurrentPage = page;
        _isChangingPageInternally = false;

        SetSelectedPageNumberSilently(CurrentPage);
    }

    private void SetSelectedPageNumberSilently(int? page)
    {
        if (_selectedPageNumber == page)
            return;

        _selectedPageNumber = page;
        OnPropertyChanged(nameof(SelectedPageNumber));
    }

    private void SetSelectedPageSizeSilently(int? pageSize)
    {
        if (_selectedPageSize == pageSize)
            return;

        _selectedPageSize = pageSize;
        OnPropertyChanged(nameof(SelectedPageSize));
    }

    private void SetSelectedTicketQueueViewSilently(string value)
    {
        if (_selectedTicketQueueView == value)
            return;

        _selectedTicketQueueView = value;
        OnPropertyChanged(nameof(SelectedTicketQueueView));
    }

    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();

        var pageCount = Math.Max(1, LastPage);

        for (var i = 1; i <= pageCount; i++)
            PageNumbers.Add(i);

        if (PageNumbers.Contains(CurrentPage))
            SetSelectedPageNumberSilently(CurrentPage);
        else
            SetSelectedPageNumberSilently(null);

        RefreshPaginationProperties();
    }

    private void RefreshPaginationProperties()
    {
        OnPropertyChanged(nameof(PageInfoText));
        OnPropertyChanged(nameof(IsOnLastPage));
        OnPropertyChanged(nameof(CanGoPreviousPage));
        OnPropertyChanged(nameof(CanGoNextPage));
        OnPropertyChanged(nameof(CanRefreshTicketsNow));
        OnPropertyChanged(nameof(PagePositionText));
        OnPropertyChanged(nameof(CanCloseTicket));
    }

    private TicketQueueView GetSelectedTicketQueueView()
    {
        return SelectedTicketQueueView switch
        {
            FilterLabels.Active => TicketQueueView.Active,
            FilterLabels.Unassigned => TicketQueueView.Unassigned,
            _ => TicketQueueView.All
        };
    }

    private static string? GetSelectedFilterValue(string value)
    {
        return string.Equals(value, FilterLabels.All, StringComparison.OrdinalIgnoreCase)
            ? null
            : value;
    }

    private void NotifyMessagesUiState()
    {
        OnPropertyChanged(nameof(HasNoMessages));
    }
}
