using System.ComponentModel;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private void InitializeNavigation()
    {
        _navigation = new DashboardNavigationViewModel(
            CurrentUser,
            onSettingsNavigated: () => SafeFireAndForget.Run(AuditPanel.RefreshAsync()),
            onAdminNavigated: isAdminRole => AdminPanel.PrepareAdminPage(isAdminRole));

        _navigation.PropertyChanged += OnNavigationPropertyChanged;
    }

    private void OnNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
            return;

        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName == nameof(DashboardNavigationViewModel.CurrentSection))
        {
            OnPropertyChanged(nameof(IsAdminUsersPanelVisible));
            OnPropertyChanged(nameof(IsAdminNewAccountPanelVisible));
        }
    }

    private async Task LogoutAsync()
    {
        _ticketPolling?.Stop();

        await LogAuditAsync("Logout", null, "Audit_Desc_LogoutDesktop", null);

        ShowToastKey("Toast_LoggedOut", ToastTypes.Info);

        await _onLogoutRequested();
    }
}
