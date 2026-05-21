using System.Collections.ObjectModel;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _settingsStatusMessage = "Ustawienia gotowe.";
    private string _selectedThemeMode = "System";
    private string _selectedUiCulture = "pl";
    private bool _isOffline;

    public ObservableCollection<AuditLogEntry> SettingsAuditLogEntries { get; } = new();

    public bool HasNoSettingsAuditLogEntries => SettingsAuditLogEntries.Count == 0;

    public ObservableCollection<string> ThemeModes { get; } = new();

    public ObservableCollection<string> UiCultures { get; } = new();

    public string SettingsStatusMessage
    {
        get => _settingsStatusMessage;
        set => SetProperty(ref _settingsStatusMessage, value);
    }

    public string SelectedThemeMode
    {
        get => _selectedThemeMode;
        set => SetProperty(ref _selectedThemeMode, value);
    }

    public string SelectedUiCulture
    {
        get => _selectedUiCulture;
        set => SetProperty(ref _selectedUiCulture, value);
    }

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

    private void InitializeSettingsCollections()
    {
        foreach (var mode in new[] { "System", "Light", "Dark" })
            ThemeModes.Add(mode);

        foreach (var culture in new[] { "pl", "en" })
            UiCultures.Add(culture);
    }
}
