using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public IRelayCommand ShowTicketsPageCommand { get; private set; } = null!;

    public IRelayCommand ShowSettingsPageCommand { get; private set; } = null!;

    public IRelayCommand ShowAdminUsersTabCommand { get; private set; } = null!;

    public IRelayCommand ShowAdminNewAccountTabCommand { get; private set; } = null!;

    public IRelayCommand ShowRequestAccountPageCommand { get; private set; } = null!;

    public IRelayCommand ShowStatisticsPageCommand { get; private set; } = null!;

    public IAsyncRelayCommand RequestAccountCommand { get; private set; } = null!;

    public IAsyncRelayCommand LoadAdminUsersCommand { get; private set; } = null!;

    public IAsyncRelayCommand BanAdminUserCommand { get; private set; } = null!;

    public IAsyncRelayCommand ActivateAdminUserCommand { get; private set; } = null!;

    public IAsyncRelayCommand UnbanAdminUserCommand { get; private set; } = null!;

    public IRelayCommand ShowAdminPageCommand { get; private set; } = null!;

    public IAsyncRelayCommand LogoutCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        ShowTicketsPageCommand = new RelayCommand(ShowTicketsPage);
        ShowSettingsPageCommand = new RelayCommand(ShowSettingsPage);
        ShowRequestAccountPageCommand = new RelayCommand(ShowRequestAccountPage);
        ShowStatisticsPageCommand = new RelayCommand(ShowStatisticsPage);
        ShowAdminPageCommand = new RelayCommand(ShowAdminPage);
        ShowAdminUsersTabCommand = AdminPanel.ShowAdminUsersTabCommand;
        ShowAdminNewAccountTabCommand = AdminPanel.ShowAdminNewAccountTabCommand;
        RequestAccountCommand = new AsyncRelayCommand(RequestAccountAsync);

        LoadAdminUsersCommand = AdminPanel.LoadAdminUsersCommand;
        BanAdminUserCommand = AdminPanel.BanAdminUserCommand;
        ActivateAdminUserCommand = AdminPanel.ActivateAdminUserCommand;
        UnbanAdminUserCommand = AdminPanel.UnbanAdminUserCommand;

        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
    }
}
