using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketDetailsPanelViewModel : ViewModelBase
{
    private readonly ITicketService _ticketService;
    private readonly ILocalAuditLogService _auditLogService;
    private readonly TicketDetailsPanelCallbacks _callbacks;

    private string _detailsStatusMessage = AppStrings.Get("Details_SelectFromList");
    private string _newMessageText = string.Empty;
    private bool _isLoadingDetails;
    private Ticket? _ticketDetails;

    private string? _selectedStatus;
    private string? _selectedPriority;

    public TicketDetailsPanelViewModel(
        ITicketService ticketService,
        ILocalAuditLogService auditLogService,
        TicketDetailsPanelCallbacks callbacks)
    {
        _ticketService = ticketService;
        _auditLogService = auditLogService;
        _callbacks = callbacks;

        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
        UpdateTicketCommand = new AsyncRelayCommand(UpdateTicketAsync);
        AssignToMeCommand = new AsyncRelayCommand(AssignToMeAsync);
        CloseTicketCommand = new AsyncRelayCommand(CloseTicketAsync);
        DeleteTicketCommand = new AsyncRelayCommand(DeleteTicketAsync);
    }

    public ObservableCollection<Message> Messages { get; } = new();

    public bool HasNoMessages => Messages.Count == 0;

    public ObservableCollection<AuditLogEntry> TicketAuditLogEntries { get; } = new();

    public bool HasNoTicketAuditLogEntries => TicketAuditLogEntries.Count == 0;

    public Ticket? TicketDetails
    {
        get => _ticketDetails;
        set
        {
            if (SetProperty(ref _ticketDetails, value))
            {
                NotifyCapabilityProperties();
                _callbacks.NotifyDetailsSideEffects();
            }
        }
    }

    public string DetailsStatusMessage
    {
        get => _detailsStatusMessage;
        set => SetProperty(ref _detailsStatusMessage, value);
    }

    public string NewMessageText
    {
        get => _newMessageText;
        set => SetProperty(ref _newMessageText, value);
    }

    public string? SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public string? SelectedPriority
    {
        get => _selectedPriority;
        set => SetProperty(ref _selectedPriority, value);
    }

    public bool IsLoadingDetails
    {
        get => _isLoadingDetails;
        set
        {
            if (SetProperty(ref _isLoadingDetails, value))
            {
                OnPropertyChanged(nameof(IsNotLoadingDetails));
                NotifyCapabilityProperties();
                _callbacks.NotifyDetailsLoadingChanged();
            }
        }
    }

    public bool IsNotLoadingDetails => !IsLoadingDetails;

    public IAsyncRelayCommand SendMessageCommand { get; }

    public IAsyncRelayCommand UpdateTicketCommand { get; }

    public IAsyncRelayCommand AssignToMeCommand { get; }

    public IAsyncRelayCommand CloseTicketCommand { get; }

    public IAsyncRelayCommand DeleteTicketCommand { get; }

    public void NotifyLocalization()
    {
        if (TicketDetails is null)
            DetailsStatusMessage = AppStrings.Get("Details_SelectFromList");
        else
        {
            SelectedStatus = StatusDisplayHelper.ToDisplayStatus(TicketDetails.Status);
            SelectedPriority = PriorityDisplayHelper.ToDisplayPriority(TicketDetails.Priority);
            RefreshTicketDetailsDisplayBindings();
        }

        RefreshTicketAuditDisplayBindings();
    }

    private void RefreshTicketDetailsDisplayBindings()
    {
        if (TicketDetails is null)
            return;

        var snapshot = TicketDetails;
        TicketDetails = null;
        TicketDetails = snapshot;
        OnPropertyChanged(nameof(TicketDetails));
    }

    private void RefreshTicketAuditDisplayBindings()
    {
        if (TicketAuditLogEntries.Count == 0)
            return;

        var snapshot = TicketAuditLogEntries.ToList();
        TicketAuditLogEntries.Clear();

        foreach (var entry in snapshot)
            TicketAuditLogEntries.Add(entry);
    }
}
