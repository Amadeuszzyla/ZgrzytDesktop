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
                OnPropertyChanged(nameof(CanUseOnlineDetailsActions));
                OnPropertyChanged(nameof(CanSendMessage));
                OnPropertyChanged(nameof(CanEditTicket));
                OnPropertyChanged(nameof(CanAssignTicket));
                OnPropertyChanged(nameof(CanCloseOwnTicket));
                OnPropertyChanged(nameof(CanCloseTicket));
                OnPropertyChanged(nameof(CanDeleteTicket));
            }
        }
    }

    public bool IsOnline => !IsOffline;

    public string ConnectionStatusText => IsOffline ? "Tryb offline" : "Online";
}
