using System;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Dashboard section navigation: current section, visibility flags, and nav commands.
/// </summary>
public sealed class DashboardNavigationViewModel : ViewModelBase
{
    private readonly Action? _onSettingsNavigated;
    private readonly Action<bool>? _onAdminNavigated;
    private string _currentSection = AppSections.Tickets;

    public DashboardNavigationViewModel(
        User currentUser,
        Action? onSettingsNavigated = null,
        Action<bool>? onAdminNavigated = null)
    {
        CurrentUser = currentUser;
        _onSettingsNavigated = onSettingsNavigated;
        _onAdminNavigated = onAdminNavigated;

        ShowTicketsPageCommand = new RelayCommand(ShowTicketsPage);
        ShowSettingsPageCommand = new RelayCommand(ShowSettingsPage);
        ShowRequestAccountPageCommand = new RelayCommand(ShowRequestAccountPage);
        ShowStatisticsPageCommand = new RelayCommand(ShowStatisticsPage);
        ShowAdminPageCommand = new RelayCommand(ShowAdminPage);
    }

    public User CurrentUser { get; }

    public string CurrentSection
    {
        get => _currentSection;
        set
        {
            if (SetProperty(ref _currentSection, value))
                NotifySectionChanged();
        }
    }

    public bool IsTicketsNavActive => CurrentSection == AppSections.Tickets;

    public bool IsRequestAccountNavActive => CurrentSection == AppSections.RequestAccount;

    public bool IsStatisticsNavActive => CurrentSection == AppSections.Statistics;

    public bool IsSettingsNavActive => CurrentSection == AppSections.Settings;

    public bool IsAdminNavActive => CurrentSection == AppSections.Admin;

    public bool IsTicketsPageVisible => CurrentSection == AppSections.Tickets;

    public bool IsDetailsPageVisible => CurrentSection == AppSections.Details;

    public bool IsSettingsPageVisible => CurrentSection == AppSections.Settings;

    public bool IsRequestAccountPageVisible => CurrentSection == AppSections.RequestAccount;

    public bool IsStatisticsPageVisible => CurrentSection == AppSections.Statistics;

    public bool IsAdminPageVisible => CurrentSection == AppSections.Admin;

    public bool IsAdminRole => AppRoleHelper.IsAdmin(CurrentUser.Role);

    public bool IsStaffRole => AppRoleHelper.IsDesktopStaff(CurrentUser.Role);

    public bool ShowAdministrationNav => IsStaffRole;

    public bool ShowRequestAccountNav => !IsStaffRole;

    public string CurrentSectionTitle => CurrentSection switch
    {
        AppSections.Tickets => AppStrings.Get("Section_Tickets"),
        AppSections.Details => AppStrings.Get("Section_Details"),
        AppSections.Settings => AppStrings.Get("Section_Settings"),
        AppSections.RequestAccount => AppStrings.Get("Section_RequestAccount"),
        AppSections.Statistics => AppStrings.Get("Section_Statistics"),
        AppSections.Admin => AppStrings.Get("Section_Admin"),
        _ => AppStrings.Get("App_Title")
    };

    public IRelayCommand ShowTicketsPageCommand { get; }

    public IRelayCommand ShowSettingsPageCommand { get; }

    public IRelayCommand ShowRequestAccountPageCommand { get; }

    public IRelayCommand ShowStatisticsPageCommand { get; }

    public IRelayCommand ShowAdminPageCommand { get; }

    public void NotifyLocalization() => OnPropertyChanged(nameof(CurrentSectionTitle));

    private void ShowTicketsPage() => CurrentSection = AppSections.Tickets;

    private void ShowSettingsPage()
    {
        CurrentSection = AppSections.Settings;
        _onSettingsNavigated?.Invoke();
    }

    private void ShowRequestAccountPage() => CurrentSection = AppSections.RequestAccount;

    private void ShowStatisticsPage() => CurrentSection = AppSections.Statistics;

    private void ShowAdminPage()
    {
        CurrentSection = AppSections.Admin;
        _onAdminNavigated?.Invoke(IsAdminRole);
    }

    private void NotifySectionChanged()
    {
        OnPropertyChanged(nameof(IsTicketsPageVisible));
        OnPropertyChanged(nameof(IsDetailsPageVisible));
        OnPropertyChanged(nameof(IsSettingsPageVisible));
        OnPropertyChanged(nameof(IsRequestAccountPageVisible));
        OnPropertyChanged(nameof(IsStatisticsPageVisible));
        OnPropertyChanged(nameof(IsAdminPageVisible));
        OnPropertyChanged(nameof(CurrentSectionTitle));
        OnPropertyChanged(nameof(IsTicketsNavActive));
        OnPropertyChanged(nameof(IsRequestAccountNavActive));
        OnPropertyChanged(nameof(ShowRequestAccountNav));
        OnPropertyChanged(nameof(ShowAdministrationNav));
        OnPropertyChanged(nameof(IsStatisticsNavActive));
        OnPropertyChanged(nameof(IsSettingsNavActive));
        OnPropertyChanged(nameof(IsAdminNavActive));
    }
}
