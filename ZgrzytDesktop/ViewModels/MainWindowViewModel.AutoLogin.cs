using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels;

public partial class MainWindowViewModel
{
    private readonly TimeSpan _autoLoginColdStartHintDelay;

    private CancellationTokenSource? _autoLoginCts;
    private bool _isAutoLoginInProgress;
    private string _autoLoginStatusMessage = string.Empty;

    public bool IsAutoLoginInProgress
    {
        get => _isAutoLoginInProgress;
        private set
        {
            if (SetProperty(ref _isAutoLoginInProgress, value))
            {
                OnPropertyChanged(nameof(CanCancelAutoLogin));
                OnPropertyChanged(nameof(IsManualLoginAllowed));
            }
        }
    }

    public string AutoLoginStatusMessage
    {
        get => _autoLoginStatusMessage;
        private set => SetProperty(ref _autoLoginStatusMessage, value);
    }

    public bool CanCancelAutoLogin => IsAutoLoginInProgress;

    public bool IsManualLoginAllowed =>
        !IsAutoLoginInProgress &&
        _currentViewModel is LoginViewModel login &&
        !login.IsLoading;

    public IRelayCommand CancelAutoLoginCommand { get; }

    private void BeginAutoLogin()
    {
        _autoLoginCts?.Cancel();
        _autoLoginCts?.Dispose();
        _autoLoginCts = new CancellationTokenSource();

        IsAutoLoginInProgress = true;
        AutoLoginStatusMessage = AppStrings.Get("Login_AutoLogin_CheckingSession");

        SafeFireAndForget.Run(RunAutoLoginColdStartHintAsync(_autoLoginCts.Token));
    }

    private async Task RunAutoLoginColdStartHintAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_autoLoginColdStartHintDelay, cancellationToken);

            if (!cancellationToken.IsCancellationRequested && IsAutoLoginInProgress)
                AutoLoginStatusMessage = AppStrings.Get("Login_AutoLogin_ColdStartHint");
        }
        catch (OperationCanceledException)
        {
            // Auto-login finished or was cancelled.
        }
    }

    private void EndAutoLogin()
    {
        _autoLoginCts?.Cancel();
        _autoLoginCts?.Dispose();
        _autoLoginCts = null;

        IsAutoLoginInProgress = false;
        AutoLoginStatusMessage = string.Empty;
    }

    private void CancelAutoLogin()
    {
        if (!IsAutoLoginInProgress)
            return;

        EndAutoLogin();
    }

    private void AttachLoginViewModelHandlers(ViewModelBase viewModel)
    {
        if (viewModel is LoginViewModel loginViewModel)
            loginViewModel.PropertyChanged += OnLoginViewModelPropertyChanged;
    }

    private void DetachLoginViewModelHandlers(ViewModelBase viewModel)
    {
        if (viewModel is LoginViewModel loginViewModel)
            loginViewModel.PropertyChanged -= OnLoginViewModelPropertyChanged;
    }

    private void OnLoginViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LoginViewModel.IsLoading))
            OnPropertyChanged(nameof(IsManualLoginAllowed));
    }

    private void SetCurrentViewModel(ViewModelBase viewModel)
    {
        DetachLoginViewModelHandlers(_currentViewModel);

        if (SetProperty(ref _currentViewModel, viewModel, nameof(CurrentViewModel)))
        {
            AttachLoginViewModelHandlers(viewModel);
            OnPropertyChanged(nameof(IsManualLoginAllowed));
        }
    }

    private async Task<User?> WaitForAutoLoginUserAsync(Task<User?> getUserTask, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return null;

        var cancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        var completed = await Task.WhenAny(getUserTask, cancellationTask);

        if (completed != getUserTask || cancellationToken.IsCancellationRequested)
            return null;

        return await getUserTask;
    }
}
