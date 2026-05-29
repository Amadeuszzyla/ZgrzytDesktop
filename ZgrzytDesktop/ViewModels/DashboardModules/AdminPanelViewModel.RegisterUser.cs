using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class AdminPanelViewModel
{
    private string _newUserName = string.Empty;
    private string _newUserLogin = string.Empty;
    private string _newUserEmail = string.Empty;
    private string _newUserPassword = string.Empty;
    private string _newUserPasswordConfirmation = string.Empty;
    private string _registerUserStatusMessage = string.Empty;
    private bool _isRegisteringUser;
    private RegisterUserRoleOption? _selectedNewUserRole;

    public ObservableCollection<RegisterUserRoleOption> NewUserRoles { get; } = new();

    public string NewUserName
    {
        get => _newUserName;
        set => SetProperty(ref _newUserName, value);
    }

    public string NewUserLogin
    {
        get => _newUserLogin;
        set => SetProperty(ref _newUserLogin, value);
    }

    public string NewUserEmail
    {
        get => _newUserEmail;
        set => SetProperty(ref _newUserEmail, value);
    }

    public string NewUserPassword
    {
        get => _newUserPassword;
        set => SetProperty(ref _newUserPassword, value);
    }

    public string NewUserPasswordConfirmation
    {
        get => _newUserPasswordConfirmation;
        set => SetProperty(ref _newUserPasswordConfirmation, value);
    }

    public RegisterUserRoleOption? SelectedNewUserRole
    {
        get => _selectedNewUserRole;
        set => SetProperty(ref _selectedNewUserRole, value);
    }

    public string RegisterUserStatusMessage
    {
        get => _registerUserStatusMessage;
        private set => SetProperty(ref _registerUserStatusMessage, value);
    }

    public bool IsRegisteringUser
    {
        get => _isRegisteringUser;
        private set
        {
            if (SetProperty(ref _isRegisteringUser, value))
            {
                OnPropertyChanged(nameof(CanRegisterUser));
                RegisterUserCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanRegisterUser =>
        _callbacks.GetIsStaffRole() &&
        _callbacks.GetCanUseOnlineActions() &&
        !IsRegisteringUser;

    public IAsyncRelayCommand RegisterUserCommand { get; private set; } = null!;

    private void InitializeRegisterUser()
    {
        foreach (var option in RegisterUserRoleOption.All)
            NewUserRoles.Add(option);

        SelectedNewUserRole = NewUserRoles.FirstOrDefault();
        RegisterUserCommand = new AsyncRelayCommand(RegisterUserAsync, () => CanRegisterUser);
    }

    private void NotifyRegisterUserLocalization()
    {
        var selectedRole = SelectedNewUserRole?.Role;

        NewUserRoles.Clear();
        foreach (var option in RegisterUserRoleOption.All)
            NewUserRoles.Add(option);

        SelectedNewUserRole =
            NewUserRoles.FirstOrDefault(option => option.Role == selectedRole)
            ?? NewUserRoles.FirstOrDefault();

        OnPropertyChanged(nameof(NewUserRoles));
        OnPropertyChanged(nameof(SelectedNewUserRole));
    }

    public void NotifyCanRegisterUserChanged()
    {
        OnPropertyChanged(nameof(CanRegisterUser));
        RegisterUserCommand.NotifyCanExecuteChanged();
    }

    private async Task RegisterUserAsync()
    {
        if (_callbacks.GetIsOffline())
        {
            RegisterUserStatusMessage = AppStrings.Get("RequestAccount_Offline");
            return;
        }

        var validationError = RegisterUserValidator.Validate(
            NewUserName,
            NewUserLogin,
            NewUserEmail,
            NewUserPassword,
            NewUserPasswordConfirmation,
            SelectedNewUserRole);

        if (validationError is not null)
        {
            RegisterUserStatusMessage = validationError;
            return;
        }

        var request = RegisterUserValidator.BuildRequest(
            NewUserName,
            NewUserLogin,
            NewUserEmail,
            NewUserPassword,
            NewUserPasswordConfirmation,
            SelectedNewUserRole!);

        try
        {
            IsRegisteringUser = true;
            RegisterUserStatusMessage = AppStrings.Get("RequestAccount_Sending");

            await _callbacks.ExecuteApiAsync(
                async () =>
                {
                    await _userAdminService.RegisterUserAsync(request);

                    NewUserName = string.Empty;
                    NewUserLogin = string.Empty;
                    NewUserEmail = string.Empty;
                    NewUserPassword = string.Empty;
                    NewUserPasswordConfirmation = string.Empty;

                    RegisterUserStatusMessage = AppStrings.Get("RegisterUser_Success");
                    _callbacks.ShowToastKey("RegisterUser_Success", ToastTypes.Success);

                    await _callbacks.LogAuditAsync(
                        "RegisterUser",
                        null,
                        "Audit_Desc_RegisterUser",
                        [request.Login, request.Role]);
                },
                setStatusMessage: message => RegisterUserStatusMessage = message,
                unexpectedStatusMessageKey: "RegisterUser_Failed",
                unexpectedToastMessageKey: "RegisterUser_Failed",
                setOfflineOnServiceUnavailable: false);
        }
        finally
        {
            IsRegisteringUser = false;
        }
    }
}
