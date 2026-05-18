using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Avalonia.Threading;

namespace ZgrzytDesktop.Services;

public class SystemNotificationService
{
    private static NotifyIcon? _notifyIcon;

    public void ShowInfo(string title, string message)
    {
        Show(title, message, ToolTipIcon.Info);
    }

    public void ShowSuccess(string title, string message)
    {
        Show(title, message, ToolTipIcon.Info);
    }

    public void ShowWarning(string title, string message)
    {
        Show(title, message, ToolTipIcon.Warning);
    }

    public void ShowError(string title, string message)
    {
        Show(title, message, ToolTipIcon.Error);
    }

    private static void Show(string title, string message, ToolTipIcon icon)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _notifyIcon ??= CreateNotifyIcon();

                    _notifyIcon.BalloonTipTitle = title;
                    _notifyIcon.BalloonTipText = message;
                    _notifyIcon.BalloonTipIcon = icon;
                    _notifyIcon.Visible = true;

                    _notifyIcon.ShowBalloonTip(4000);
                }
                catch
                {
                    // Powiadomienia systemowe nie mogą wywalić aplikacji.
                }
            });
        }
        catch
        {
            // Celowo puste.
        }
    }

    private static NotifyIcon CreateNotifyIcon()
    {
        return new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "ZGRZYT Desktop"
        };
    }
}