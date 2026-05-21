using System;
using System.Collections.ObjectModel;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _adminTab = AdminTabs.Users;
    private string _adminStatusMessage = string.Empty;
    private string _adminUnbanPassword = string.Empty;
    private AdminListFilterOption? _selectedAdminUserListFilterOption;

    private User? _selectedAdminUser;

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

    public bool IsAdminUsersPanelVisible => IsAdminUsersTabActive && IsAdminRole;

    public bool IsAdminNewAccountPanelVisible => IsAdminNewAccountTabActive && IsStaffRole;

    public AdminListFilterOption? SelectedAdminUserListFilterOption
    {
        get => _selectedAdminUserListFilterOption;
        set
        {
            if (SetProperty(ref _selectedAdminUserListFilterOption, value))
                _ = LoadAdminUsersAsync();
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
            }
        }
    }

    public bool CanBanAdminUser =>
        IsAdminRole && SelectedAdminUser is not null && !SelectedAdminUser.Ban;

    public bool CanActivateAdminUser =>
        IsAdminRole && SelectedAdminUser is not null && !SelectedAdminUser.Active && !SelectedAdminUser.Ban;

    public bool CanUnbanAdminUser =>
        IsAdminRole && SelectedAdminUser is not null && SelectedAdminUser.Ban;

    private void InitializeAdminCollections()
    {
        foreach (var option in AdminListFilterOption.All)
            AdminUserListFilterOptions.Add(option);
    }
}
