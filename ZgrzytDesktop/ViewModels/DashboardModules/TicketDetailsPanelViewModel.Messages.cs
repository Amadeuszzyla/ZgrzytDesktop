using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketDetailsPanelViewModel
{
    public async Task LoadMessagesAsync(int ticketId)
    {
        Messages.Clear();

        var messages = await _ticketService.GetTicketMessagesAsync(ticketId);

        foreach (var message in messages)
            Messages.Add(message);

        NotifyMessagesUiState();
    }

    public async Task RefreshTicketAuditLogAsync(int ticketId)
    {
        var entries = await _auditLogService.LoadForTicketAsync(ticketId);

        TicketAuditLogEntries.Clear();

        foreach (var entry in entries)
            TicketAuditLogEntries.Add(entry);

        OnPropertyChanged(nameof(HasNoTicketAuditLogEntries));
    }

    private async Task SendMessageAsync()
    {
        if (_callbacks.GetIsOffline())
        {
            DetailsStatusMessage = AppStrings.Get("Details_OfflineSend");
            _callbacks.ShowToastKey("Toast_DetailsMessageOffline", ToastTypes.Warning);
            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = AppStrings.Get("Details_SelectFirst");
            return;
        }

        var messageError = TicketMessageValidator.ValidateBody(NewMessageText);
        if (messageError is not null)
        {
            DetailsStatusMessage = messageError;
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = AppStrings.Get("Details_Sending");

            var ticketId = TicketDetails.Id;
            var messageBody = NewMessageText.Trim();

            await _callbacks.ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.SendMessageAsync(ticketId, messageBody);

                    _callbacks.SetIsOffline(false);

                    NewMessageText = string.Empty;

                    await LoadTicketDetailsAsync(ticketId);

                    DetailsStatusMessage = AppStrings.Get("Details_MessageSent");
                    _callbacks.ShowToastKey("Toast_MessageSent", ToastTypes.Success);

                    await _callbacks.LogAuditAsync(
                        "SendMessage",
                        ticketId,
                        "Details_MessageAuditDesc",
                        null);
                },
                new DashboardApiExecutionOptions
                {
                    SetStatusMessage = message => DetailsStatusMessage = message,
                    UnexpectedStatusMessageKey = "Details_MessageSendUnexpectedError",
                    UnexpectedToastMessageKey = "Details_MessageSendFailed",
                    OnServiceUnavailableAsync = async _ =>
                    {
                        _callbacks.SetIsOffline(true);
                        DetailsStatusMessage = AppStrings.Get("Details_OfflineSendFailed");
                        _callbacks.ShowToastKey("Toast_MessageSendOffline", ToastTypes.Error);
                        await Task.CompletedTask;
                    }
                });
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private void NotifyMessagesUiState() => OnPropertyChanged(nameof(HasNoMessages));
}
