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

    public IAsyncRelayCommand LoadTicketsCommand { get; private set; } = null!;

    public IAsyncRelayCommand SearchTicketsCommand { get; private set; } = null!;

    public IRelayCommand ClearFiltersCommand { get; private set; } = null!;

    public IAsyncRelayCommand CreateTicketCommand { get; private set; } = null!;

    public IAsyncRelayCommand SendMessageCommand { get; private set; } = null!;

    public IAsyncRelayCommand UpdateTicketCommand { get; private set; } = null!;

    public IAsyncRelayCommand AssignToMeCommand { get; private set; } = null!;

    public IAsyncRelayCommand CloseTicketCommand { get; private set; } = null!;

    public IAsyncRelayCommand DeleteTicketCommand { get; private set; } = null!;

    public IAsyncRelayCommand LoadAdminUsersCommand { get; private set; } = null!;

    public IAsyncRelayCommand BanAdminUserCommand { get; private set; } = null!;

    public IAsyncRelayCommand ActivateAdminUserCommand { get; private set; } = null!;

    public IAsyncRelayCommand UnbanAdminUserCommand { get; private set; } = null!;

    public IRelayCommand ShowAdminPageCommand { get; private set; } = null!;

    public IAsyncRelayCommand FirstPageCommand { get; private set; } = null!;

    public IAsyncRelayCommand PreviousPageCommand { get; private set; } = null!;

    public IAsyncRelayCommand NextPageCommand { get; private set; } = null!;

    public IAsyncRelayCommand LastPageCommand { get; private set; } = null!;

    public IAsyncRelayCommand RefreshTicketsNowCommand { get; private set; } = null!;

    public IAsyncRelayCommand LogoutCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        ShowTicketsPageCommand = new RelayCommand(ShowTicketsPage);
        ShowSettingsPageCommand = new RelayCommand(ShowSettingsPage);
        ShowRequestAccountPageCommand = new RelayCommand(ShowRequestAccountPage);
        ShowStatisticsPageCommand = new RelayCommand(ShowStatisticsPage);
        ShowAdminPageCommand = new RelayCommand(ShowAdminPage);
        ShowAdminUsersTabCommand = new RelayCommand(() => AdminTab = AdminTabs.Users);
        ShowAdminNewAccountTabCommand = new RelayCommand(() => AdminTab = AdminTabs.NewAccount);
        RequestAccountCommand = new AsyncRelayCommand(RequestAccountAsync);

        LoadTicketsCommand = new AsyncRelayCommand(() => LoadTicketsAsync());
        SearchTicketsCommand = new AsyncRelayCommand(SearchTicketsAsync);
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        CreateTicketCommand = new AsyncRelayCommand(CreateTicketAsync);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
        UpdateTicketCommand = new AsyncRelayCommand(UpdateTicketAsync);
        AssignToMeCommand = new AsyncRelayCommand(AssignToMeAsync);
        CloseTicketCommand = new AsyncRelayCommand(CloseTicketAsync);
        DeleteTicketCommand = new AsyncRelayCommand(DeleteTicketAsync);
        LoadAdminUsersCommand = new AsyncRelayCommand(LoadAdminUsersAsync);
        BanAdminUserCommand = new AsyncRelayCommand(BanAdminUserAsync, () => CanBanAdminUser);
        ActivateAdminUserCommand = new AsyncRelayCommand(ActivateAdminUserAsync, () => CanActivateAdminUser);
        UnbanAdminUserCommand = new AsyncRelayCommand(UnbanAdminUserAsync, () => CanUnbanAdminUser);

        FirstPageCommand = new AsyncRelayCommand(GoToFirstPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(GoToPreviousPageAsync);
        NextPageCommand = new AsyncRelayCommand(GoToNextPageAsync);
        LastPageCommand = new AsyncRelayCommand(GoToLastPageAsync);
        RefreshTicketsNowCommand = new AsyncRelayCommand(() => RefreshTicketsNowAsync());

        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
    }
}
