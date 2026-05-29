using System;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _currentSection = AppSections.Tickets;

    public const int TicketAutoRefreshIntervalSeconds = 45;

    public string CurrentSection
    {
        get => _currentSection;
        set
        {
            if (SetProperty(ref _currentSection, value))
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
                OnPropertyChanged(nameof(IsAdminUsersPanelVisible));
                OnPropertyChanged(nameof(IsAdminNewAccountPanelVisible));
            }
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
}
