using System.Linq;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketsPanelViewModel
{
    private string _newTicketTitle = string.Empty;
    private string _newTicketDescription = string.Empty;
    private string _newTicketPriority = PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Low);
    private string _selectedNewTicketCategory = TicketCategoryHelper.Hardware;
    private string _createTicketStatusMessage = string.Empty;

    public string NewTicketTitle
    {
        get => _newTicketTitle;
        set => SetProperty(ref _newTicketTitle, value);
    }

    public string NewTicketDescription
    {
        get => _newTicketDescription;
        set => SetProperty(ref _newTicketDescription, value);
    }

    public string NewTicketPriority
    {
        get => _newTicketPriority;
        set => SetProperty(ref _newTicketPriority, value);
    }

    public string SelectedNewTicketCategory
    {
        get => _selectedNewTicketCategory;
        set => SetProperty(ref _selectedNewTicketCategory, value);
    }

    public string CreateTicketStatusMessage
    {
        get => _createTicketStatusMessage;
        set => SetProperty(ref _createTicketStatusMessage, value);
    }

    public async Task CreateTicketAsync()
    {
        if (_callbacks.GetIsOffline())
        {
            CreateTicketStatusMessage = AppStrings.Get("Tickets_CreateOffline");
            _callbacks.ShowToastKey("Toast_TicketCreateOfflineBlocked", ToastTypes.Warning);
            return;
        }

        var titleError = TicketFormValidator.ValidateTitle(NewTicketTitle);
        if (titleError is not null)
        {
            CreateTicketStatusMessage = titleError;
            return;
        }

        var descriptionError = TicketFormValidator.ValidateDescription(NewTicketDescription);
        if (descriptionError is not null)
        {
            CreateTicketStatusMessage = descriptionError;
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedNewTicketCategory))
        {
            CreateTicketStatusMessage = AppStrings.Get("Tickets_ValidationCategory");
            return;
        }

        try
        {
            IsLoading = true;
            CreateTicketStatusMessage = AppStrings.Get("Tickets_Creating");

            await _callbacks.ExecuteApiAsync(
                async () =>
                {
                    var request = new CreateTicketRequest
                    {
                        Title = TicketCategoryHelper.FormatTitle(SelectedNewTicketCategory, NewTicketTitle),
                        Description = TicketCategoryHelper.FormatDescription(
                            SelectedNewTicketCategory,
                            NewTicketDescription),
                        Priority = PriorityDisplayHelper.ToApiPriority(NewTicketPriority)
                    };

                    var createdTicket = await _ticketService.CreateTicketAsync(request);

                    _callbacks.SetIsOffline(false);

                    NewTicketTitle = string.Empty;
                    NewTicketDescription = string.Empty;
                    NewTicketPriority = PriorityDisplayHelper.ToDisplayPriority(TicketPriorities.Low);
                    SelectedNewTicketCategory = TicketCategoryHelper.Hardware;

                    SetCurrentPageSilently(1);
                    await LoadTicketsAsync();

                    if (createdTicket is not null)
                        SelectedTicket = Tickets.FirstOrDefault(ticket => ticket.Id == createdTicket.Id);

                    CreateTicketStatusMessage = AppStrings.Get("Tickets_Created");
                    _callbacks.ShowToastKey("Toast_TicketCreated", ToastTypes.Success);

                    if (createdTicket is not null)
                    {
                        await _callbacks.LogAuditAsync(
                            "CreateTicket",
                            createdTicket.Id,
                            "Audit_Desc_TicketCreated",
                            [createdTicket.Title]);
                    }
                },
                setStatusMessage: message => CreateTicketStatusMessage = message,
                unexpectedStatusMessageKey: "Tickets_CreateUnexpectedError",
                unexpectedToastMessageKey: "Tickets_CreateError",
                onServiceUnavailableAsync: async _ =>
                {
                    _callbacks.SetIsOffline(true);
                    CreateTicketStatusMessage = AppStrings.Get("Tickets_CreateOfflineError");
                    _callbacks.ShowToastKey("Toast_TicketCreateOffline", ToastTypes.Error);
                    await Task.CompletedTask;
                });
        }
        finally
        {
            IsLoading = false;
        }
    }
}
