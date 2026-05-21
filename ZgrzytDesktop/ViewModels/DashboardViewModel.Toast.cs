using System;
using Avalonia.Threading;
using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public void ShowToast(string message, string type = "info")
    {
        if (ApiErrorSanitizer.IsHtmlResponse(message))
        {
            message = ApiErrorSanitizer.SanitizeForDisplay(
                message,
                System.Net.HttpStatusCode.InternalServerError);
        }

        void DisplayToast()
        {
            _toastHideTimer.Stop();

            ToastMessage = message;
            ApplyToastStyle(type);
            IsToastVisible = true;
            _toastHideTimer.Start();
        }

        if (Dispatcher.UIThread.CheckAccess())
            DisplayToast();
        else
            Dispatcher.UIThread.Post(DisplayToast);
    }

    private void ApplyToastStyle(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case "success":
                ToastBackground = "#059669";
                ToastForeground = "#FFFFFF";
                break;
            case "warning":
                ToastBackground = "#D97706";
                ToastForeground = "#FFFFFF";
                break;
            case "error":
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
