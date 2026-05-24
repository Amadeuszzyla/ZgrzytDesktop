using System;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private string _currentSection = AppSections.Tickets;

    private string _requestName = string.Empty;
    private string _requestLogin = string.Empty;
    private string _requestEmail = string.Empty;
    private string _requestPassword = string.Empty;
    private string _requestPasswordConfirmation = string.Empty;
    private string _requestAccountStatusMessage = string.Empty;
    private bool _isRequestingAccount;

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

    public string RequestName
    {
        get => _requestName;
        set => SetProperty(ref _requestName, value);
    }

    public string RequestLogin
    {
        get => _requestLogin;
        set => SetProperty(ref _requestLogin, value);
    }

    public string RequestEmail
    {
        get => _requestEmail;
        set => SetProperty(ref _requestEmail, value);
    }

    public string RequestPassword
    {
        get => _requestPassword;
        set => SetProperty(ref _requestPassword, value);
    }

    public string RequestPasswordConfirmation
    {
        get => _requestPasswordConfirmation;
        set => SetProperty(ref _requestPasswordConfirmation, value);
    }

    public string RequestAccountStatusMessage
    {
        get => _requestAccountStatusMessage;
        set => SetProperty(ref _requestAccountStatusMessage, value);
    }

    public bool IsRequestingAccount
    {
        get => _isRequestingAccount;
        private set
        {
            if (SetProperty(ref _isRequestingAccount, value))
                OnPropertyChanged(nameof(CanRequestAccount));
        }
    }

    public bool CanRequestAccount => CanUseOnlineActions && !IsRequestingAccount;
}
