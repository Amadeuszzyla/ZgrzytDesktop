using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly LocalAuditLogService _auditLogService;
    private readonly Action<User> _onLoginSuccess;

    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _isPasswordVisible;

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

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set
        {
            if (SetProperty(ref _isPasswordVisible, value))
            {
                OnPropertyChanged(nameof(IsPasswordHidden));
            }
        }
    }

    public bool IsPasswordHidden => !IsPasswordVisible;

    public IAsyncRelayCommand LoginCommand { get; }

    public IRelayCommand TogglePasswordVisibilityCommand { get; }

    public LoginViewModel(
        AuthService authService,
        LocalAuditLogService auditLogService,
        Action<User> onLoginSuccess)
    {
        _authService = authService;
        _auditLogService = auditLogService;
        _onLoginSuccess = onLoginSuccess;

        LoginCommand = new AsyncRelayCommand(LoginAsync);
        TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
    }

    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Login))
        {
            ErrorMessage = "Podaj login.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Podaj hasło.";
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
                    "Nieprawidłowy login albo hasło.",

                System.Net.HttpStatusCode.ServiceUnavailable =>
                    "Brak połączenia z API. Sprawdź, czy backend jest uruchomiony.",

                _ => ex.Message
            };
        }
        catch
        {
            ErrorMessage = "Wystąpił nieoczekiwany błąd podczas logowania.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}