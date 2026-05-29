using CommunityToolkit.Mvvm.ComponentModel;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public string AppVersionDisplay => AppVersionHelper.DisplayVersion;
}