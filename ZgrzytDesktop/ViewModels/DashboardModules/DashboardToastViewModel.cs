using System;
using System.Net;
using Avalonia.Threading;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Toast overlay state and display logic for the dashboard shell.
/// </summary>
public sealed class DashboardToastViewModel : ViewModelBase
{
    private string _message = string.Empty;
    private bool _isVisible;
    private string _background = "#7C3AED";
    private string _foreground = "#FFFFFF";
    private string? _activeKey;
    private object[]? _activeArgs;
    private DispatcherTimer? _hideTimer;

    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public string Background
    {
        get => _background;
        private set => SetProperty(ref _background, value);
    }

    public string Foreground
    {
        get => _foreground;
        private set => SetProperty(ref _foreground, value);
    }

    public void ShowKey(string key, string type = ToastTypes.Info, params object[] args)
    {
        _activeKey = key;
        _activeArgs = args.Length > 0 ? args : null;
        Display(ResolveMessage(key, _activeArgs), type);
    }

    public void ShowRaw(string message, string type = ToastTypes.Info)
    {
        if (ApiErrorSanitizer.IsHtmlResponse(message))
        {
            message = ApiErrorSanitizer.SanitizeForDisplay(
                message,
                HttpStatusCode.InternalServerError);
        }

        _activeKey = null;
        _activeArgs = null;
        Display(message, type);
    }

    public void RefreshActiveLocalized()
    {
        if (!IsVisible || string.IsNullOrEmpty(_activeKey))
            return;

        Message = ResolveMessage(_activeKey, _activeArgs);
    }

    internal void EnsureHideTimer()
    {
        if (_hideTimer is not null)
            return;

        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            IsVisible = false;
            _activeKey = null;
            _activeArgs = null;
        };
    }

    private static string ResolveMessage(string key, object[]? args) =>
        args is { Length: > 0 }
            ? AppStrings.GetFormat(key, args)
            : AppStrings.Get(key);

    private void Display(string message, string type)
    {
        void Show()
        {
            EnsureHideTimer();
            _hideTimer!.Stop();

            Message = message;
            ApplyStyle(type);
            IsVisible = true;
            _hideTimer.Start();
        }

        if (Dispatcher.UIThread.CheckAccess() || Avalonia.Application.Current is null)
            Show();
        else
            Dispatcher.UIThread.Post(Show);
    }

    private void ApplyStyle(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case ToastTypes.Success:
                Background = "#059669";
                Foreground = "#FFFFFF";
                break;
            case ToastTypes.Warning:
                Background = "#D97706";
                Foreground = "#FFFFFF";
                break;
            case ToastTypes.Error:
                Background = "#DC2626";
                Foreground = "#FFFFFF";
                break;
            default:
                Background = "#7C3AED";
                Foreground = "#FFFFFF";
                break;
        }
    }
}
