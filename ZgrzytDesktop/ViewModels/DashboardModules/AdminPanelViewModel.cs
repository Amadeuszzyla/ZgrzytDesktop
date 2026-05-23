using System.Collections.ObjectModel;

using System.Linq;

using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using ZgrzytDesktop.Constants;

using ZgrzytDesktop.Helpers;

using ZgrzytDesktop.Models;

using ZgrzytDesktop.Resources;

using ZgrzytDesktop.Services;

using ZgrzytDesktop.Services.Interfaces;



namespace ZgrzytDesktop.ViewModels.DashboardModules;



public sealed partial class AdminPanelViewModel : ViewModelBase

{

    private readonly IUserAdminService _userAdminService;

    private readonly AdminPanelCallbacks _callbacks;



    private string _adminTab = AdminTabs.Users;

    private string _adminStatusMessage = string.Empty;

    private string _adminUnbanPassword = string.Empty;

    private AdminListFilterOption? _selectedAdminUserListFilterOption;

    private User? _selectedAdminUser;



    public AdminPanelViewModel(IUserAdminService userAdminService, AdminPanelCallbacks callbacks)

    {

        _userAdminService = userAdminService;

        _callbacks = callbacks;



        foreach (var option in AdminListFilterOption.All)

            AdminUserListFilterOptions.Add(option);



        ShowAdminUsersTabCommand = new RelayCommand(() => AdminTab = AdminTabs.Users);

        ShowAdminNewAccountTabCommand = new RelayCommand(() => AdminTab = AdminTabs.NewAccount);

        LoadAdminUsersCommand = new AsyncRelayCommand(LoadUsersAsync);

        BanAdminUserCommand = new AsyncRelayCommand(BanUserAsync, () => CanBanAdminUser);

        ActivateAdminUserCommand = new AsyncRelayCommand(ActivateUserAsync, () => CanActivateAdminUser);

        UnbanAdminUserCommand = new AsyncRelayCommand(UnbanUserAsync, () => CanUnbanAdminUser);

        InitializeRegisterUser();
    }



    public ObservableCollection<User> AdminUsers { get; } = new();



    public ObservableCollection<AdminListFilterOption> AdminUserListFilterOptions { get; } = new();



    public string AdminTab

    {

        get => _adminTab;

        set

        {

            if (SetProperty(ref _adminTab, value))

            {

                OnPropertyChanged(nameof(IsAdminUsersTabActive));

                OnPropertyChanged(nameof(IsAdminNewAccountTabActive));

                OnPropertyChanged(nameof(IsAdminUsersPanelVisible));

                OnPropertyChanged(nameof(IsAdminNewAccountPanelVisible));

            }

        }

    }



    public bool IsAdminUsersTabActive => AdminTab == AdminTabs.Users;



    public bool IsAdminNewAccountTabActive => AdminTab == AdminTabs.NewAccount;



    public bool IsAdminUsersPanelVisible => IsAdminUsersTabActive && _callbacks.GetIsAdminRole();



    public bool IsAdminNewAccountPanelVisible => IsAdminNewAccountTabActive && _callbacks.GetIsStaffRole();



    public bool ShowAdminUnbanPassword =>

        _callbacks.GetIsAdminRole() &&

        (GetSelectedAdminUserListFilter() == UserAdminListFilter.Banned ||

         SelectedAdminUser?.Ban == true);



    public AdminListFilterOption? SelectedAdminUserListFilterOption

    {

        get => _selectedAdminUserListFilterOption;

        set

        {

            if (SetProperty(ref _selectedAdminUserListFilterOption, value))

            {

                ClearUnbanPasswordIfHidden();

                OnPropertyChanged(nameof(ShowAdminUnbanPassword));

                _ = LoadUsersAsync();

            }

        }

    }



    public string AdminUnbanPassword

    {

        get => _adminUnbanPassword;

        set => SetProperty(ref _adminUnbanPassword, value);

    }



    public string AdminStatusMessage

    {

        get => _adminStatusMessage;

        private set => SetProperty(ref _adminStatusMessage, value);

    }



    public User? SelectedAdminUser

    {

        get => _selectedAdminUser;

        set

        {

            if (SetProperty(ref _selectedAdminUser, value))

            {

                OnPropertyChanged(nameof(CanBanAdminUser));

                OnPropertyChanged(nameof(CanActivateAdminUser));

                OnPropertyChanged(nameof(CanUnbanAdminUser));

                OnPropertyChanged(nameof(ShowAdminUnbanPassword));

                BanAdminUserCommand.NotifyCanExecuteChanged();

                ActivateAdminUserCommand.NotifyCanExecuteChanged();

                UnbanAdminUserCommand.NotifyCanExecuteChanged();

                ClearUnbanPasswordIfHidden();

            }

        }

    }



    public bool CanBanAdminUser =>

        _callbacks.GetIsAdminRole() && SelectedAdminUser is not null && !SelectedAdminUser.Ban;



    public bool CanActivateAdminUser =>

        _callbacks.GetIsAdminRole() &&

        SelectedAdminUser is not null &&

        !SelectedAdminUser.Active &&

        !SelectedAdminUser.Ban;



    public bool CanUnbanAdminUser =>

        _callbacks.GetIsAdminRole() && SelectedAdminUser is not null && SelectedAdminUser.Ban;



    public IRelayCommand ShowAdminUsersTabCommand { get; }



    public IRelayCommand ShowAdminNewAccountTabCommand { get; }



    public IAsyncRelayCommand LoadAdminUsersCommand { get; }



    public IAsyncRelayCommand BanAdminUserCommand { get; }



    public IAsyncRelayCommand ActivateAdminUserCommand { get; }



    public IAsyncRelayCommand UnbanAdminUserCommand { get; }



    public void ApplyDefaultFilter() =>

        SelectedAdminUserListFilterOption = AdminUserListFilterOptions[0];



    public void NotifyLocalization()

    {

        var selectedFilter = SelectedAdminUserListFilterOption?.Filter ?? UserAdminListFilter.All;



        AdminUserListFilterOptions.Clear();

        foreach (var option in AdminListFilterOption.All)

            AdminUserListFilterOptions.Add(option);



        SelectedAdminUserListFilterOption =

            AdminUserListFilterOptions.FirstOrDefault(option => option.Filter == selectedFilter)

            ?? AdminUserListFilterOptions.FirstOrDefault();



        OnPropertyChanged(nameof(AdminUserListFilterOptions));

        OnPropertyChanged(nameof(SelectedAdminUserListFilterOption));



        if (AdminUsers.Count > 0)

            AdminStatusMessage = BuildAdminStatusMessage(AdminUsers.Count);

        NotifyRegisterUserLocalization();
    }



    public void PrepareAdminPage(bool isAdminRole)

    {

        AdminTab = isAdminRole ? AdminTabs.Users : AdminTabs.NewAccount;



        if (isAdminRole)

            _ = LoadUsersAsync();

    }



    public async Task LoadUsersAsync()

    {

        if (!_callbacks.GetIsAdminRole())

        {

            AdminStatusMessage = AppStrings.Get("Api_Forbidden");

            return;

        }



        await _callbacks.ExecuteApiAsync(

            async () =>

            {

                AdminStatusMessage = AppStrings.Get("Admin_LoadingUsers");



                var filter = GetSelectedAdminUserListFilter();

                var result = await _userAdminService.GetUsersAsync(filter);



                AdminUsers.Clear();



                if (result.Users.Count == 0)

                {

                    AdminStatusMessage = ResolveEmptyListStatusMessage(result);

                    return;

                }



                foreach (var user in result.Users.OrderBy(user => user.Login))

                    AdminUsers.Add(user);



                AdminStatusMessage = BuildAdminStatusMessage(AdminUsers.Count);

            },

            setStatusMessage: message => AdminStatusMessage = message,

            unexpectedStatusMessage: AppStrings.Get("Toast_AdminUsersLoadFailed"),

            unexpectedToastMessage: AppStrings.Get("Toast_AdminUsersLoadFailed"),

            setOfflineOnServiceUnavailable: false);

    }



    private async Task BanUserAsync()

    {

        if (SelectedAdminUser is null)

            return;



        var login = SelectedAdminUser.Login;



        await _callbacks.ExecuteApiAsync(

            async () =>

            {

                await _userAdminService.BanUserAsync(SelectedAdminUser!.Id);

                await LoadUsersAsync();

                _callbacks.ShowToast(AppStrings.Get("Toast_UserBanned"), ToastTypes.Success);

                await _callbacks.LogAuditAsync("BanUser", null, "Audit_Desc_UserBanned", [login]);

            },

            setStatusMessage: message => AdminStatusMessage = message,

            unexpectedStatusMessage: AppStrings.Get("Toast_AdminBanFailed"),

            unexpectedToastMessage: AppStrings.Get("Toast_AdminBanFailed"),

            setOfflineOnServiceUnavailable: false);

    }



    private async Task ActivateUserAsync()

    {

        if (SelectedAdminUser is null)

            return;



        var login = SelectedAdminUser.Login;



        await _callbacks.ExecuteApiAsync(

            async () =>

            {

                await _userAdminService.ActivateUserAsync(SelectedAdminUser!.Id);

                await LoadUsersAsync();

                _callbacks.ShowToast(AppStrings.Get("Toast_UserActivated"), ToastTypes.Success);

                await _callbacks.LogAuditAsync("ActivateUser", null, "Audit_Desc_UserActivated", [login]);

            },

            setStatusMessage: message => AdminStatusMessage = message,

            unexpectedStatusMessage: AppStrings.Get("Toast_AdminActivateFailed"),

            unexpectedToastMessage: AppStrings.Get("Toast_AdminActivateFailed"),

            setOfflineOnServiceUnavailable: false);

    }



    private async Task UnbanUserAsync()

    {

        if (SelectedAdminUser is null)

            return;



        if (string.IsNullOrWhiteSpace(AdminUnbanPassword))

        {

            _callbacks.ShowToast(AppStrings.Get("Toast_AdminUnbanPasswordRequired"), ToastTypes.Warning);

            return;

        }



        var login = SelectedAdminUser.Login;



        await _callbacks.ExecuteApiAsync(

            async () =>

            {

                await _userAdminService.UnbanUserAsync(SelectedAdminUser!.Id, AdminUnbanPassword.Trim());

                AdminUnbanPassword = string.Empty;

                await LoadUsersAsync();

                _callbacks.ShowToast(AppStrings.Get("Toast_UserUnbanned"), ToastTypes.Success);

                await _callbacks.LogAuditAsync("UnbanUser", null, "Audit_Desc_UserUnbanned", [login]);

            },

            setStatusMessage: message => AdminStatusMessage = message,

            unexpectedStatusMessage: AppStrings.Get("Toast_AdminUnbanFailed"),

            unexpectedToastMessage: AppStrings.Get("Toast_AdminUnbanFailed"),

            setOfflineOnServiceUnavailable: false);

    }



    private string BuildAdminStatusMessage(int userCount)

    {

        var filterLabel = SelectedAdminUserListFilterOption?.Label ?? AppStrings.Get("Admin_Filter_All");

        return AppStrings.GetFormat("Admin_StatusCount", userCount, filterLabel);

    }



    private static string ResolveEmptyListStatusMessage(UserAdminListResult result) =>

        result.InfoKind switch

        {

            UserAdminListInfoKind.BannedListNotSupported => AppStrings.Get("Admin_BannedListNotSupported"),

            _ => AppStrings.Get("Admin_NoUsers")

        };



    private void ClearUnbanPasswordIfHidden()

    {

        if (!ShowAdminUnbanPassword && !string.IsNullOrEmpty(AdminUnbanPassword))

            AdminUnbanPassword = string.Empty;

    }



    private UserAdminListFilter GetSelectedAdminUserListFilter() =>

        SelectedAdminUserListFilterOption?.Filter ?? UserAdminListFilter.All;

}

