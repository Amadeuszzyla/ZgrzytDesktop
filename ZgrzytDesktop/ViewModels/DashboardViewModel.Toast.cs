using System;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private readonly DashboardToastViewModel _toast = new();

    public string ToastMessage => _toast.Message;

    public bool IsToastVisible => _toast.IsVisible;

    public string ToastBackground => _toast.Background;

    public string ToastForeground => _toast.Foreground;

    /// <summary>Localized toast from <see cref="AppStrings"/>; refreshes when UI culture changes.</summary>
    public void ShowToastKey(string key, string type = ToastTypes.Info, params object[] args) =>
        _toast.ShowKey(key, type, args);

    /// <summary>Raw message (e.g. sanitized API error); not updated on culture change.</summary>
    public void ShowToast(string message, string type = ToastTypes.Info) =>
        _toast.ShowRaw(message, type);

    internal void RefreshActiveLocalizedToast() => _toast.RefreshActiveLocalized();

    private void EnsureToastHideTimer() => _toast.EnsureHideTimer();
}
