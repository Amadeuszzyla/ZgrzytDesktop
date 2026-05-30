using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
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
    private User? _selectedAdminUser;
    private bool _isLoadingAdminUsers;

    public AdminPanelViewModel(IUserAdminService userAdminService, AdminPanelCallbacks callbacks)
    {
        _userAdminService = userAdminService;
        _callbacks = callbacks;

        AdminUsers.CollectionChanged += OnAdminUsersCollectionChanged;

        InitializeAdminUserFilters();

        ShowAdminUsersTabCommand = new RelayCommand(() => AdminTab = AdminTabs.Users);
        ShowAdminNewAccountTabCommand = new RelayCommand(() => AdminTab = AdminTabs.NewAccount);
        LoadAdminUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
        BanAdminUserCommand = new AsyncRelayCommand(BanUserAsync, () => CanBanAdminUser);
        ActivateAdminUserCommand = new AsyncRelayCommand(ActivateUserAsync, () => CanActivateAdminUser);
        UnbanAdminUserCommand = new AsyncRelayCommand(UnbanUserAsync, () => CanUnbanAdminUser);

        InitializeRegisterUser();
    }

    public ObservableCollection<User> AdminUsers { get; } = new();

    public bool IsLoadingAdminUsers
    {
        get => _isLoadingAdminUsers;
        private set
        {
            if (SetProperty(ref _isLoadingAdminUsers, value))
                RefreshAdminUsersVisibility();
        }
    }

    public bool HasNoAdminUsers => !IsLoadingAdminUsers && AdminUsers.Count == 0;

    public bool HasAdminUsers => !IsLoadingAdminUsers && AdminUsers.Count > 0;

    public string LblAdminNoUsersFound => AppStrings.Get("Admin_NoUsersFound");

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

    public bool IsAdminUsersManagementVisible => _callbacks.GetIsAdminRole();

    public bool IsAdminNewAccountPanelVisible => IsAdminNewAccountTabActive && _callbacks.GetIsStaffRole();

    public bool ShowAdminUnbanPassword =>
        _callbacks.GetIsAdminRole() && SelectedAdminUser?.Ban == true;

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

    public void NotifyLocalization()
    {
        NotifyAdminUserFilterLocalization();

        if (AdminUsers.Count > 0)
            AdminStatusMessage = BuildAdminStatusMessage(AdminUsers.Count, GetSelectedFilterLabel());

        OnPropertyChanged(nameof(LblAdminNoUsersFound));
        RefreshAdminUsersVisibility();
        NotifyRegisterUserLocalization();
    }

    public void PrepareAdminPage(bool isAdminRole)
    {
        if (isAdminRole)
        {
            AdminTab = AdminTabs.Users;
            ApplyDefaultFilter();
            SafeFireAndForget.Run(LoadUsersAsync());
            return;
        }

        if (_callbacks.GetIsStaffRole())
        {
            AdminTab = AdminTabs.NewAccount;
            AdminStatusMessage = string.Empty;
            AdminUsers.Clear();
            SelectedAdminUser = null;
            return;
        }

        AdminTab = AdminTabs.NewAccount;
    }

    public async Task LoadUsersAsync()
    {
        if (!_callbacks.GetIsAdminRole())
            return;

        try
        {
            IsLoadingAdminUsers = true;

            await ExecuteAdminUsersLoadAsync(async () =>
            {
                AdminStatusMessage = AppStrings.Get("Admin_LoadingUsers");

                var result = await _userAdminService.GetUsersAsync(GetSelectedListFilter());

                AdminUsers.Clear();
                SelectedAdminUser = null;

                if (result.InfoKind == UserAdminListInfoKind.BannedListNotSupported)
                {
                    AdminStatusMessage = AppStrings.Get("Admin_BannedListNotSupported");
                    return;
                }

                if (result.Users.Count == 0)
                {
                    AdminStatusMessage = BuildAdminStatusMessage(0, GetSelectedFilterLabel());
                    return;
                }

                foreach (var user in result.Users.OrderBy(user => user.Login))
                    AdminUsers.Add(user);

                AdminStatusMessage = BuildAdminStatusMessage(AdminUsers.Count, GetSelectedFilterLabel());
            });
        }
        finally
        {
            IsLoadingAdminUsers = false;
        }
    }

    private async Task BanUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        if (!await _callbacks.ConfirmAsync("Confirm_BanUser", "Confirm_Title"))
            return;

        var login = SelectedAdminUser.Login;

        await _callbacks.ExecuteApiAsync(
            async () =>
            {
                await _userAdminService.BanUserAsync(SelectedAdminUser!.Id);
                await LoadUsersAsync();
                _callbacks.ShowToastKey("Toast_UserBanned", ToastTypes.Success);
                await _callbacks.LogAuditAsync("BanUser", null, "Audit_Desc_UserBanned", [login]);
            },
            new DashboardApiExecutionOptions
            {
                SetStatusMessage = message => AdminStatusMessage = message,
                UnexpectedStatusMessageKey = "Toast_AdminBanFailed",
                UnexpectedToastMessageKey = "Toast_AdminBanFailed",
                SetOfflineOnServiceUnavailable = false
            });
    }

    private async Task ActivateUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        if (!await _callbacks.ConfirmAsync("Confirm_ActivateUser", "Confirm_Title"))
            return;

        var login = SelectedAdminUser.Login;

        await _callbacks.ExecuteApiAsync(
            async () =>
            {
                await _userAdminService.ActivateUserAsync(SelectedAdminUser!.Id);
                await LoadUsersAsync();
                _callbacks.ShowToastKey("Toast_UserActivated", ToastTypes.Success);
                await _callbacks.LogAuditAsync("ActivateUser", null, "Audit_Desc_UserActivated", [login]);
            },
            new DashboardApiExecutionOptions
            {
                SetStatusMessage = message => AdminStatusMessage = message,
                UnexpectedStatusMessageKey = "Toast_AdminActivateFailed",
                UnexpectedToastMessageKey = "Toast_AdminActivateFailed",
                SetOfflineOnServiceUnavailable = false
            });
    }

    private async Task UnbanUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        if (string.IsNullOrWhiteSpace(AdminUnbanPassword))
        {
            _callbacks.ShowToastKey("Toast_AdminUnbanPasswordRequired", ToastTypes.Warning);
            return;
        }

        if (!await _callbacks.ConfirmAsync("Confirm_UnbanUser", "Confirm_Title"))
            return;

        var login = SelectedAdminUser.Login;

        await _callbacks.ExecuteApiAsync(
            async () =>
            {
                await _userAdminService.UnbanUserAsync(SelectedAdminUser!.Id, AdminUnbanPassword.Trim());
                AdminUnbanPassword = string.Empty;
                await LoadUsersAsync();
                _callbacks.ShowToastKey("Toast_UserUnbanned", ToastTypes.Success);
                await _callbacks.LogAuditAsync("UnbanUser", null, "Audit_Desc_UserUnbanned", [login]);
            },
            new DashboardApiExecutionOptions
            {
                SetStatusMessage = message => AdminStatusMessage = message,
                UnexpectedStatusMessageKey = "Toast_AdminUnbanFailed",
                UnexpectedToastMessageKey = "Toast_AdminUnbanFailed",
                SetOfflineOnServiceUnavailable = false
            });
    }

    private static string BuildAdminStatusMessage(int userCount, string filterLabel) =>
        AppStrings.GetFormat("Admin_StatusCount", userCount, filterLabel);

    private void OnAdminUsersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        RefreshAdminUsersVisibility();

    private void RefreshAdminUsersVisibility()
    {
        OnPropertyChanged(nameof(HasNoAdminUsers));
        OnPropertyChanged(nameof(HasAdminUsers));
    }

    private void ClearUnbanPasswordIfHidden()
    {
        if (!ShowAdminUnbanPassword && !string.IsNullOrEmpty(AdminUnbanPassword))
            AdminUnbanPassword = string.Empty;
    }

    private async Task<bool> ExecuteAdminUsersLoadAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            AdminStatusMessage = _callbacks.GetApiErrorMessage(ex);
            return false;
        }
        catch (ApiException ex)
        {
            AdminStatusMessage = ResolveAdminUsersLoadErrorMessage(ex);
            return false;
        }
        catch
        {
            AdminStatusMessage = AppStrings.Get("Toast_AdminUsersLoadFailed");
            return false;
        }
    }

    private static string ResolveAdminUsersLoadErrorMessage(ApiException ex)
    {
        if (ex.StatusCode == HttpStatusCode.Forbidden)
            return AppStrings.Get("Admin_ListForbidden");

        if ((int)ex.StatusCode >= 500)
            return AppStrings.Get("Admin_UsersLoadServerError");

        return ApiErrorSanitizer.SanitizeApiErrorMessage(
            ex.ResponseContent ?? ex.Message,
            ex.StatusCode);
    }
}
