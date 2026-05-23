using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public AdminPanelViewModel AdminPanel { get; private set; } = null!;

    private void InitializeAdminPanel()
    {
        AdminPanel = new AdminPanelViewModel(
            _userAdminService,
            new AdminPanelCallbacks
            {
                ShowToastKey = ShowToastKey,
                ShowToastRaw = ShowToast,
                GetIsOffline = () => IsOffline,
                GetIsAdminRole = () => IsAdminRole,
                GetIsStaffRole = () => IsStaffRole,
                GetCanUseOnlineActions = () => CanUseOnlineActions,
                GetApiErrorMessage = GetApiErrorMessage,
                LogAuditAsync = LogAuditAsync,
                ExecuteApiAsyncCore = ExecuteApiAsync
            });

        AdminPanel.PropertyChanged += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.PropertyName))
                return;

            OnPropertyChanged(e.PropertyName);
        };
    }

    public ObservableCollection<User> AdminUsers => AdminPanel.AdminUsers;

    public ObservableCollection<AdminListFilterOption> AdminUserListFilterOptions =>
        AdminPanel.AdminUserListFilterOptions;

    public string AdminTab
    {
        get => AdminPanel.AdminTab;
        set => AdminPanel.AdminTab = value;
    }

    public bool IsAdminUsersTabActive => AdminPanel.IsAdminUsersTabActive;

    public bool IsAdminNewAccountTabActive => AdminPanel.IsAdminNewAccountTabActive;

    public bool IsAdminUsersPanelVisible => AdminPanel.IsAdminUsersPanelVisible;

    public bool IsAdminNewAccountPanelVisible => AdminPanel.IsAdminNewAccountPanelVisible;

    public AdminListFilterOption? SelectedAdminUserListFilterOption
    {
        get => AdminPanel.SelectedAdminUserListFilterOption;
        set => AdminPanel.SelectedAdminUserListFilterOption = value;
    }

    public string AdminUnbanPassword
    {
        get => AdminPanel.AdminUnbanPassword;
        set => AdminPanel.AdminUnbanPassword = value;
    }

    public string AdminStatusMessage => AdminPanel.AdminStatusMessage;

    public bool IsLoadingAdminUsers => AdminPanel.IsLoadingAdminUsers;

    public bool HasNoAdminUsers => AdminPanel.HasNoAdminUsers;

    public bool HasAdminUsers => AdminPanel.HasAdminUsers;

    public string LblAdminNoUsersFound => AdminPanel.LblAdminNoUsersFound;

    public User? SelectedAdminUser
    {
        get => AdminPanel.SelectedAdminUser;
        set => AdminPanel.SelectedAdminUser = value;
    }

    public bool CanBanAdminUser => AdminPanel.CanBanAdminUser;

    public bool CanActivateAdminUser => AdminPanel.CanActivateAdminUser;

    public bool CanUnbanAdminUser => AdminPanel.CanUnbanAdminUser;

    public bool ShowAdminUnbanPassword => AdminPanel.ShowAdminUnbanPassword;

    public ObservableCollection<RegisterUserRoleOption> NewUserRoles => AdminPanel.NewUserRoles;

    public string NewUserName
    {
        get => AdminPanel.NewUserName;
        set => AdminPanel.NewUserName = value;
    }

    public string NewUserLogin
    {
        get => AdminPanel.NewUserLogin;
        set => AdminPanel.NewUserLogin = value;
    }

    public string NewUserEmail
    {
        get => AdminPanel.NewUserEmail;
        set => AdminPanel.NewUserEmail = value;
    }

    public string NewUserPassword
    {
        get => AdminPanel.NewUserPassword;
        set => AdminPanel.NewUserPassword = value;
    }

    public string NewUserPasswordConfirmation
    {
        get => AdminPanel.NewUserPasswordConfirmation;
        set => AdminPanel.NewUserPasswordConfirmation = value;
    }

    public RegisterUserRoleOption? SelectedNewUserRole
    {
        get => AdminPanel.SelectedNewUserRole;
        set => AdminPanel.SelectedNewUserRole = value;
    }

    public string RegisterUserStatusMessage => AdminPanel.RegisterUserStatusMessage;

    public bool CanRegisterUser => AdminPanel.CanRegisterUser;
}
