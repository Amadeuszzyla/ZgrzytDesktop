using System.Net;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketDetailsPanelViewModel
{
    private async Task UpdateTicketAsync()
    {
        if (!_callbacks.GetCanManageTickets())
        {
            DetailsStatusMessage = AppStrings.Get("Details_NoEditPermission");
            _callbacks.ShowToastKey("Toast_DetailsEditForbidden", ToastTypes.Warning);
            return;
        }

        if (_callbacks.GetIsOffline())
        {
            DetailsStatusMessage = AppStrings.Get("Details_OfflineEdit");
            _callbacks.ShowToastKey("Toast_DetailsEditOffline", ToastTypes.Warning);
            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = AppStrings.Get("Details_SelectFirst");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedStatus))
        {
            DetailsStatusMessage = AppStrings.Get("Details_SelectStatus");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPriority))
        {
            DetailsStatusMessage = AppStrings.Get("Details_SelectPriority");
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = AppStrings.Get("Details_Saving");

            var ticketId = TicketDetails.Id;

            var request = new UpdateTicketRequest
            {
                Status = StatusDisplayHelper.ToApiStatus(SelectedStatus),
                Priority = PriorityDisplayHelper.ToApiPriority(SelectedPriority)
            };

            await _callbacks.ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.UpdateTicketAsync(ticketId, request);

                    _callbacks.SetIsOffline(false);

                    await LoadTicketDetailsAsync(ticketId);
                    await _callbacks.RefreshTicketsAsync();

                    DetailsStatusMessage = AppStrings.Get("Details_Saved");
                    _callbacks.ShowToastKey("Toast_TicketSaved", ToastTypes.Success);

                    await _callbacks.LogAuditAsync(
                        "UpdateTicket",
                        ticketId,
                        "Details_UpdateAuditDesc",
                        [SelectedStatus, SelectedPriority]);
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessageKey: "Details_SaveUnexpectedError",
                unexpectedToastMessageKey: "Details_SaveFailed",
                onServiceUnavailableAsync: async _ =>
                {
                    _callbacks.SetIsOffline(true);
                    DetailsStatusMessage = AppStrings.Get("Details_OfflineSaveFailed");
                    _callbacks.ShowToastKey("Toast_TicketSaveOffline", ToastTypes.Error);
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
            DetailsStatusMessage = AppStrings.Get("Details_NoClosePermission");
            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = AppStrings.Get("Details_SelectFirst");
            return;
        }

        if (_callbacks.GetIsOffline())
        {
            DetailsStatusMessage = AppStrings.Get("Details_OfflineClose");
            return;
        }

        var ticketId = TicketDetails.Id;
        var previousDetails = TicketDetails;
        var previousStatus = SelectedStatus;
        var previousPriority = SelectedPriority;

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = AppStrings.Get("Details_Closing");

            var request = new UpdateTicketRequest
            {
                Status = TicketStatuses.Zamkniete
            };

            var closed = await _callbacks.ExecuteApiAsync(
                async () =>
                {
                    var updatedTicket = await _ticketService.UpdateTicketAsync(ticketId, request);

                    if (updatedTicket is not null && !IsValidTicketForDisplay(updatedTicket))
                    {
                        throw new ApiException(
                            HttpStatusCode.InternalServerError,
                            AppStrings.Get("Api_HtmlResponse"));
                    }

                    _callbacks.SetIsOffline(false);

                    await LoadTicketDetailsAsync(ticketId);
                    await _callbacks.RefreshTicketsAsync();

                    DetailsStatusMessage = AppStrings.Get("Details_Closed");
                    _callbacks.ShowToastKey("Toast_TicketClosed", ToastTypes.Success);

                    await _callbacks.LogAuditAsync(
                        "CloseTicket",
                        ticketId,
                        "Details_CloseAuditDesc",
                        null);
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessageKey: "Details_CloseUnexpectedError",
                unexpectedToastMessageKey: "Details_CloseFailed",
                showApiErrorToast: true);

            if (!closed)
            {
                TicketDetails = previousDetails;
                SelectedStatus = previousStatus;
                SelectedPriority = previousPriority;

                await _callbacks.LogAuditAsync(
                    "CloseTicket",
                    ticketId,
                    "Details_CloseForbidden",
                    null);
            }
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
            DetailsStatusMessage = AppStrings.Get("Details_NoDeletePermission");
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = AppStrings.Get("Details_Deleting");

            var ticketId = TicketDetails.Id;

            await _callbacks.ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.DeleteTicketAsync(ticketId);

                    TicketDetails = null;
                    _callbacks.ClearSelectedTicket();
                    _callbacks.NavigateToTickets();

                    await _callbacks.RefreshTicketsAsync();

                    DetailsStatusMessage = AppStrings.Get("Details_Deleted");
                    _callbacks.ShowToastKey("Toast_TicketDeleted", ToastTypes.Success);

                    await _callbacks.LogAuditAsync(
                        "DeleteTicket",
                        ticketId,
                        "Details_DeleteAuditDesc",
                        null);
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessageKey: "Details_DeleteFailed",
                unexpectedToastMessageKey: "Details_DeleteFailed");
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private async Task AssignToMeAsync()
    {
        if (!_callbacks.GetCanManageTickets())
        {
            DetailsStatusMessage = AppStrings.Get("Details_NoAssignPermission");
            _callbacks.ShowToastKey("Toast_DetailsAssignForbidden", ToastTypes.Warning);
            return;
        }

        if (_callbacks.GetIsOffline())
        {
            DetailsStatusMessage = AppStrings.Get("Details_OfflineAssign");
            _callbacks.ShowToastKey("Toast_DetailsAssignOffline", ToastTypes.Warning);
            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = AppStrings.Get("Details_SelectFirst");
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = AppStrings.Get("Details_Assigning");

            var ticketId = TicketDetails.Id;
            var user = _callbacks.GetCurrentUser();

            var request = new UpdateTicketRequest
            {
                AssignedItId = user.Id
            };

            await _callbacks.ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.UpdateTicketAsync(ticketId, request);

                    _callbacks.SetIsOffline(false);

                    await LoadTicketDetailsAsync(ticketId);
                    await _callbacks.RefreshTicketsAsync();

                    DetailsStatusMessage = AppStrings.Get("Details_Assigned");
                    _callbacks.ShowToastKey("Toast_TicketAssigned", ToastTypes.Success);

                    await _callbacks.LogAuditAsync(
                        "AssignToMe",
                        ticketId,
                        "Details_AssignAuditDesc",
                        [user.Login]);
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessageKey: "Details_AssignUnexpectedError",
                unexpectedToastMessageKey: "Details_AssignFailed",
                onServiceUnavailableAsync: async _ =>
                {
                    _callbacks.SetIsOffline(true);
                    DetailsStatusMessage = AppStrings.Get("Details_OfflineAssignFailed");
                    _callbacks.ShowToastKey("Toast_TicketAssignOffline", ToastTypes.Error);
                    await Task.CompletedTask;
                });
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }
}
