using System;
using Avalonia.Threading;
using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _toastMessage = string.Empty;
    private bool _isToastVisible;
    private string _toastBackground = "#2563EB";
    private string _toastForeground = "#FFFFFF";

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

    public void ShowToast(string message, string type = ToastTypes.Info)
    {
        if (ApiErrorSanitizer.IsHtmlResponse(message))
        {
            message = ApiErrorSanitizer.SanitizeForDisplay(
                message,
                System.Net.HttpStatusCode.InternalServerError);
        }

        void DisplayToast()
        {
            EnsureToastHideTimer();
            _toastHideTimer.Stop();

            ToastMessage = message;
            ApplyToastStyle(type);
            IsToastVisible = true;
            _toastHideTimer.Start();
        }

        if (Dispatcher.UIThread.CheckAccess() || Avalonia.Application.Current is null)
            DisplayToast();
        else
            Dispatcher.UIThread.Post(DisplayToast);
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
                ToastBackground = "#2563EB";
                ToastForeground = "#FFFFFF";
                break;
        }
    }
}
