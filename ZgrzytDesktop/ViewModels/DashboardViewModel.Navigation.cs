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
        AdminPanel.PrepareAdminPage(IsAdminRole);
    }

    private async Task RequestAccountAsync()
    {
        if (IsOffline)
        {
            RequestAccountStatusMessage = AppStrings.Get("RequestAccount_Offline");
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestName))
        {
            RequestAccountStatusMessage = AppStrings.Get("RequestAccount_ValidationName");
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestLogin))
        {
            RequestAccountStatusMessage = AppStrings.Get("Login_ProvideLogin");
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestEmail))
        {
            RequestAccountStatusMessage = AppStrings.Get("RequestAccount_ValidationEmail");
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestPassword))
        {
            RequestAccountStatusMessage = AppStrings.Get("RequestAccount_ValidationPassword");
            return;
        }

        if (string.IsNullOrWhiteSpace(RequestPasswordConfirmation))
        {
            RequestAccountStatusMessage = AppStrings.Get("RequestAccount_ValidationPasswordConfirm");
            return;
        }

        if (!string.Equals(RequestPassword, RequestPasswordConfirmation, StringComparison.Ordinal))
        {
            RequestAccountStatusMessage = AppStrings.Get("RequestAccount_ValidationPasswordMismatch");
            return;
        }

        try
        {
            IsRequestingAccount = true;
            RequestAccountStatusMessage = AppStrings.Get("RequestAccount_Sending");

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
                        RequestAccountStatusMessage = AppStrings.Get("RequestAccount_Failed");
                        return;
                    }

                    IsOffline = false;

                    RequestName = string.Empty;
                    RequestLogin = string.Empty;
                    RequestEmail = string.Empty;
                    RequestPassword = string.Empty;
                    RequestPasswordConfirmation = string.Empty;

                    RequestAccountStatusMessage = AppStrings.Get("RequestAccount_Sent");
                    ShowToast(AppStrings.Get("Toast_RequestAccountSent"), ToastTypes.Success);

                    await LogAuditAsync(
                        "RequestAccount",
                        null,
                        "RequestAccount_AuditDesc",
                        [request.Login]);
                },
                setStatusMessage: message => RequestAccountStatusMessage = message,
                unexpectedStatusMessage: AppStrings.Get("RequestAccount_UnexpectedError"),
                unexpectedToastMessage: AppStrings.Get("RequestAccount_UnexpectedError"),
                onServiceUnavailableAsync: async _ =>
                {
                    IsOffline = true;
                    RequestAccountStatusMessage = AppStrings.Get("RequestAccount_OfflineError");
                    ShowToast(AppStrings.Get("Toast_RequestAccountOffline"), ToastTypes.Warning);
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

        await LogAuditAsync("Logout", null, "Audit_Desc_LogoutDesktop", null);

        ShowToast(AppStrings.Get("Toast_LoggedOut"), ToastTypes.Info);

        await _onLogoutRequested();
    }
}
