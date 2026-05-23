using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ILocalAuditLogService _auditLogService;
    private readonly Action<User> _onLoginSuccess;

    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsNotLoading));
                OnPropertyChanged(nameof(LoginButtonText));
            }
        }
    }

    public bool IsNotLoading => !IsLoading;

    public string LoginButtonText =>
        IsLoading ? AppStrings.Get("Login_ButtonLoading") : AppStrings.Get("Login_Button");

    public string LblAppTitle => AppStrings.Get("Login_AppTitle");

    public string LblAppSubtitle => AppStrings.Get("Login_AppSubtitle");

    public string LblLoginTitle => AppStrings.Get("Login_Title");

    public string LblLoginLabel => AppStrings.Get("Login_LabelLogin");

    public string LblPasswordLabel => AppStrings.Get("Login_LabelPassword");

    public string LblLoginPlaceholder => AppStrings.Get("Login_PlaceholderLogin");

    public string LblPasswordPlaceholder => AppStrings.Get("Login_PlaceholderPassword");

    public IAsyncRelayCommand LoginCommand { get; }

    public LoginViewModel(
        IAuthService authService,
        ILocalAuditLogService auditLogService,
        Action<User> onLoginSuccess,
        string? initialErrorMessage = null)
    {
        _authService = authService;
        _auditLogService = auditLogService;
        _onLoginSuccess = onLoginSuccess;

        LoginCommand = new AsyncRelayCommand(LoginAsync);

        if (!string.IsNullOrWhiteSpace(initialErrorMessage))
            ErrorMessage = initialErrorMessage;
    }

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Login))
        {
            ErrorMessage = AppStrings.Get("Login_ProvideLogin");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = AppStrings.Get("Login_ProvidePassword");
            return;
        }

        try
        {
            IsLoading = true;

            var user = await _authService.LoginAsync(Login.Trim(), Password);

            if (user is null)
            {
                ErrorMessage = AppStrings.Get("Login_InvalidCredentials");
                return;
            }

            ErrorMessage = string.Empty;
            _onLoginSuccess(user);
        }
        catch (Exception ex)
        {
            ErrorMessage = LoginErrorMapper.GetErrorMessage(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
}