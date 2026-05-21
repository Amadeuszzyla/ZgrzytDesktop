using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Exceptions;
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

    public string LoginButtonText => IsLoading ? "Logowanie..." : "Zaloguj";

    public IAsyncRelayCommand LoginCommand { get; }

    public LoginViewModel(
        IAuthService authService,
        ILocalAuditLogService auditLogService,
        Action<User> onLoginSuccess)
    {
        _authService = authService;
        _auditLogService = auditLogService;
        _onLoginSuccess = onLoginSuccess;

        LoginCommand = new AsyncRelayCommand(LoginAsync);
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
                ErrorMessage = "Nie udało się zalogować. Sprawdź login i hasło.";
                return;
            }

            ErrorMessage = string.Empty;

            await _auditLogService.AddAsync(new AuditLogEntry
            {
                Timestamp = DateTime.Now,
                UserLogin = user.Login,
                Action = "Login",
                Description = "Zalogowano przez formularz logowania."
            });

            _onLoginSuccess(user);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized =>
                    AppStrings.Get("Login_InvalidCredentials"),

                System.Net.HttpStatusCode.ServiceUnavailable =>
                    AppStrings.Get("Login_Offline"),

                _ => ex.Message
            };
        }
        catch
        {
            ErrorMessage = AppStrings.Get("Login_UnexpectedError");
        }
        finally
        {
            IsLoading = false;
        }
    }
}