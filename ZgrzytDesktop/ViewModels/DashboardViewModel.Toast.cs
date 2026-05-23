using System;
using Avalonia.Threading;
using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _toastMessage = string.Empty;
    private bool _isToastVisible;
    private string _toastBackground = "#7C3AED";
    private string _toastForeground = "#FFFFFF";
    private string? _activeToastKey;
    private object[]? _activeToastArgs;

    public string ToastMessage
    {
        get => _toastMessage;
        private set => SetProperty(ref _toastMessage, value);
    }

    public bool IsToastVisible
    {
        get => _isToastVisible;
        private set => SetProperty(ref _isToastVisible, value);
    }

    public string ToastBackground
    {
        get => _toastBackground;
        private set => SetProperty(ref _toastBackground, value);
    }

    public string ToastForeground
    {
        get => _toastForeground;
        private set => SetProperty(ref _toastForeground, value);
    }

    /// <summary>Localized toast from <see cref="AppStrings"/>; refreshes when UI culture changes.</summary>
    public void ShowToastKey(string key, string type = ToastTypes.Info, params object[] args)
    {
        _activeToastKey = key;
        _activeToastArgs = args.Length > 0 ? args : null;
        DisplayToast(ResolveToastMessage(key, _activeToastArgs), type);
    }

    /// <summary>Raw message (e.g. sanitized API error); not updated on culture change.</summary>
    public void ShowToast(string message, string type = ToastTypes.Info)
    {
        if (ApiErrorSanitizer.IsHtmlResponse(message))
        {
            message = ApiErrorSanitizer.SanitizeForDisplay(
                message,
                HttpStatusCode.InternalServerError);
        }

        _activeToastKey = null;
        _activeToastArgs = null;
        DisplayToast(message, type);
    }

    internal void RefreshActiveLocalizedToast()
    {
        if (!IsToastVisible || string.IsNullOrEmpty(_activeToastKey))
            return;

        ToastMessage = ResolveToastMessage(_activeToastKey, _activeToastArgs);
    }

    private static string ResolveToastMessage(string key, object[]? args) =>
        args is { Length: > 0 }
            ? AppStrings.GetFormat(key, args)
            : AppStrings.Get(key);

    private void DisplayToast(string message, string type)
    {
        void Show()
        {
            EnsureToastHideTimer();
            _toastHideTimer!.Stop();

            ToastMessage = message;
            ApplyToastStyle(type);
            IsToastVisible = true;
            _toastHideTimer.Start();
        }

        if (Dispatcher.UIThread.CheckAccess() || Avalonia.Application.Current is null)
            Show();
        else
            Dispatcher.UIThread.Post(Show);
    }

    private void EnsureToastHideTimer()
    {
        if (_toastHideTimer is not null)
            return;

        _toastHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        _toastHideTimer.Tick += (_, _) =>
        {
            _toastHideTimer.Stop();
            IsToastVisible = false;
            _activeToastKey = null;
            _activeToastArgs = null;
        };
    }

    private void ApplyToastStyle(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case ToastTypes.Success:
                ToastBackground = "#059669";
                ToastForeground = "#FFFFFF";
                break;
            case ToastTypes.Warning:
                ToastBackground = "#D97706";
                ToastForeground = "#FFFFFF";
                break;
            case ToastTypes.Error:
                ToastBackground = "#DC2626";
                ToastForeground = "#FFFFFF";
                break;
            default:
                ToastBackground = "#7C3AED";
                ToastForeground = "#FFFFFF";
                break;
        }
    }
}
