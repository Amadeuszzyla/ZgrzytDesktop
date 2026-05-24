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
    public async Task LoadAsync(int ticketId) => await LoadTicketDetailsAsync(ticketId);

    public async Task LoadTicketDetailsAsync(int ticketId)
    {
        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = AppStrings.Get("Details_Loading");

            if (_callbacks.GetIsOffline())
            {
                ApplyCachedTicket(ticketId);
                return;
            }

            var ticket = await _ticketService.GetTicketAsync(ticketId);

            _callbacks.SetIsOffline(false);

            if (!IsValidTicketForDisplay(ticket))
            {
                DetailsStatusMessage = AppStrings.Get("Details_InvalidServerData");
                _callbacks.ShowToastKey("Api_HtmlResponse", ToastTypes.Error);
                return;
            }

            TicketDetails = ticket;

            await LoadMessagesAsync(ticketId);

            ApplyDetailsFormFromTicket(ticket);

            await LoadAssignableUsersIfNeededAsync();

            DetailsStatusMessage = ticket is null
                ? AppStrings.Get("Details_LoadFailed")
                : AppStrings.GetFormat("Details_Selected", ticket.Id);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _callbacks.SetIsOffline(true);
            DetailsStatusMessage = AppStrings.Get("Details_OfflineDetails");
            _callbacks.ShowToastKey("Toast_DetailsOffline", ToastTypes.Warning);
            ApplyCachedTicket(ticketId);
        }
        catch (ApiException ex)
        {
            DetailsStatusMessage = _callbacks.GetApiErrorMessage(ex);
            _callbacks.ShowToastRaw(_callbacks.GetApiErrorMessage(ex), ToastTypes.Error);
        }
        catch
        {
            DetailsStatusMessage = AppStrings.Get("Details_LoadUnexpectedError");
            _callbacks.ShowToastKey("Toast_DetailsLoadError", ToastTypes.Error);
        }
        finally
        {
            IsLoadingDetails = false;

            if (TicketDetails?.Id is int detailsTicketId)
                await RefreshTicketAuditLogAsync(detailsTicketId);
        }
    }

    private void ApplyCachedTicket(int ticketId)
    {
        var cachedTicket = _callbacks.FindCachedTicket(ticketId);

        TicketDetails = cachedTicket;

        Messages.Clear();

        if (cachedTicket?.Messages is not null)
        {
            foreach (var message in cachedTicket.Messages)
                Messages.Add(message);
        }

        NotifyMessagesUiState();

        ApplyDetailsFormFromTicket(cachedTicket);

        DetailsStatusMessage = cachedTicket is null
            ? AppStrings.Get("Details_NotFoundOffline")
            : AppStrings.GetFormat("Details_OfflineCacheSelected", cachedTicket.Id);
    }

    private void ApplyDetailsFormFromTicket(Ticket? ticket)
    {
        SelectedStatus = StatusDisplayHelper.ToDisplayStatus(ticket?.Status);
        SelectedPriority = PriorityDisplayHelper.ToDisplayPriority(ticket?.Priority);
        NotifyCapabilityProperties();
    }

    private static bool IsValidTicketForDisplay(Ticket? ticket)
    {
        if (ticket is null)
            return false;

        return !ApiErrorSanitizer.IsHtmlResponse(ticket.Title) &&
               !ApiErrorSanitizer.IsHtmlResponse(ticket.Description);
    }
}
