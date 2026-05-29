using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Request-account form for non-staff users (prośba o konto).
/// </summary>
public sealed class RequestAccountPanelViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IDashboardContext _context;
    private readonly Func<bool> _getCanSubmit;

    private string _name = string.Empty;
    private string _login = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _passwordConfirmation = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isSubmitting;

    public RequestAccountPanelViewModel(
        IAuthService authService,
        IDashboardContext context,
        Func<bool> getCanSubmit)
    {
        _authService = authService;
        _context = context;
        _getCanSubmit = getCanSubmit;

        SubmitCommand = new AsyncRelayCommand(SubmitAsync, () => CanSubmit);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string PasswordConfirmation
    {
        get => _passwordConfirmation;
        set => SetProperty(ref _passwordConfirmation, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsSubmitting
    {
        get => _isSubmitting;
        private set
        {
            if (SetProperty(ref _isSubmitting, value))
            {
                OnPropertyChanged(nameof(CanSubmit));
                SubmitCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanSubmit => _getCanSubmit() && !IsSubmitting;

    public IAsyncRelayCommand SubmitCommand { get; }

    public void NotifyCanSubmitChanged()
    {
        OnPropertyChanged(nameof(CanSubmit));
        SubmitCommand.NotifyCanExecuteChanged();
    }

    private async Task SubmitAsync()
    {
        if (_context.IsOffline)
        {
            StatusMessage = AppStrings.Get("RequestAccount_Offline");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = AppStrings.Get("RequestAccount_ValidationName");
            return;
        }

        if (string.IsNullOrWhiteSpace(Login))
        {
            StatusMessage = AppStrings.Get("Login_ProvideLogin");
            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            StatusMessage = AppStrings.Get("RequestAccount_ValidationEmail");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = AppStrings.Get("RequestAccount_ValidationPassword");
            return;
        }

        if (string.IsNullOrWhiteSpace(PasswordConfirmation))
        {
            StatusMessage = AppStrings.Get("RequestAccount_ValidationPasswordConfirm");
            return;
        }

        if (!string.Equals(Password, PasswordConfirmation, StringComparison.Ordinal))
        {
            StatusMessage = AppStrings.Get("RequestAccount_ValidationPasswordMismatch");
            return;
        }

        try
        {
            IsSubmitting = true;
            StatusMessage = AppStrings.Get("RequestAccount_Sending");

            await _context.ExecuteApiAsync(
                async () =>
                {
                    var request = new RequestAccountRequest
                    {
                        Name = Name.Trim(),
                        Login = Login.Trim(),
                        Email = Email.Trim(),
                        Password = Password,
                        PasswordConfirmation = PasswordConfirmation
                    };

                    var success = await _authService.RequestAccountAsync(request);

                    if (!success)
                    {
                        StatusMessage = AppStrings.Get("RequestAccount_Failed");
                        return;
                    }

                    _context.IsOffline = false;

                    Name = string.Empty;
                    Login = string.Empty;
                    Email = string.Empty;
                    Password = string.Empty;
                    PasswordConfirmation = string.Empty;

                    StatusMessage = AppStrings.Get("RequestAccount_Sent");
                    _context.ShowToastKey("Toast_RequestAccountSent", ToastTypes.Success);

                    await _context.LogAuditAsync(
                        "RequestAccount",
                        null,
                        "RequestAccount_AuditDesc",
                        [request.Login]);
                },
                setStatusMessage: message => StatusMessage = message,
                unexpectedStatusMessageKey: "RequestAccount_UnexpectedError",
                unexpectedToastMessageKey: "RequestAccount_UnexpectedError",
                onServiceUnavailableAsync: async _ =>
                {
                    _context.IsOffline = true;
                    StatusMessage = AppStrings.Get("RequestAccount_OfflineError");
                    _context.ShowToastKey("Toast_RequestAccountOffline", ToastTypes.Warning);
                    await Task.CompletedTask;
                });
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
