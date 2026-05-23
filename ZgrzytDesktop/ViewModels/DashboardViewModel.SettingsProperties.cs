namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private bool _isOffline;

    public bool IsOffline
    {
        get => _isOffline;
        set
        {
            if (SetProperty(ref _isOffline, value))
            {
                OnPropertyChanged(nameof(IsOnline));
                OnPropertyChanged(nameof(ConnectionStatusText));
                OnPropertyChanged(nameof(CanUseOnlineActions));
                OnPropertyChanged(nameof(CanRequestAccount));
                TicketDetailsPanel?.NotifyCapabilityProperties();
            }
        }
    }

    public bool IsOnline => !IsOffline;

    public string ConnectionStatusText => IsOffline ? "Tryb offline" : "Online";
}
