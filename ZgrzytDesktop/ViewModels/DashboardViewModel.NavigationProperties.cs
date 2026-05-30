using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private DashboardNavigationViewModel _navigation = null!;

    public const int TicketAutoRefreshIntervalSeconds = 45;

    public string CurrentSection
    {
        get => _navigation.CurrentSection;
        set => _navigation.CurrentSection = value;
    }

    public bool IsTicketsNavActive => _navigation.IsTicketsNavActive;

    public bool IsRequestAccountNavActive => _navigation.IsRequestAccountNavActive;

    public bool IsStatisticsNavActive => _navigation.IsStatisticsNavActive;

    public bool IsSettingsNavActive => _navigation.IsSettingsNavActive;

    public bool IsAdminNavActive => _navigation.IsAdminNavActive;

    public bool IsTicketsPageVisible => _navigation.IsTicketsPageVisible;

    public bool IsDetailsPageVisible => _navigation.IsDetailsPageVisible;

    public bool IsSettingsPageVisible => _navigation.IsSettingsPageVisible;

    public bool IsRequestAccountPageVisible => _navigation.IsRequestAccountPageVisible;

    public bool IsStatisticsPageVisible => _navigation.IsStatisticsPageVisible;

    public bool IsAdminPageVisible => _navigation.IsAdminPageVisible;

    public bool IsAdminRole => _navigation.IsAdminRole;

    public bool IsStaffRole => _navigation.IsStaffRole;

    public bool ShowAdministrationNav => _navigation.ShowAdministrationNav;

    public bool ShowRequestAccountNav => _navigation.ShowRequestAccountNav;

    public string CurrentSectionTitle => _navigation.CurrentSectionTitle;
}
