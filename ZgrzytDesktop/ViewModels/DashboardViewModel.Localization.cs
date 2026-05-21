using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    public string LblNavTickets => AppStrings.Get("Nav_Tickets");
    public string LblNavRequestAccount => AppStrings.Get("Nav_RequestAccount");
    public string LblNavStatistics => AppStrings.Get("Nav_Statistics");
    public string LblNavSettings => AppStrings.Get("Nav_Settings");
    public string LblNavAdmin => AppStrings.Get("Nav_Admin");
    public string LblNavLogout => AppStrings.Get("Nav_Logout");
    public string LblHeaderSubtitle => AppStrings.Get("Header_Subtitle");
    public string LblTicketsFiltersTitle => AppStrings.Get("Tickets_FiltersTitle");
    public string LblTicketsFiltersSubtitle => AppStrings.Get("Tickets_FiltersSubtitle");
    public string LblTicketsSearchPlaceholder => AppStrings.Get("Tickets_SearchPlaceholder");
    public string LblTicketsSearch => AppStrings.Get("Tickets_Search");
    public string LblTicketsClear => AppStrings.Get("Tickets_Clear");
    public string LblTicketsRefreshNow => AppStrings.Get("Tickets_RefreshNow");
    public string LblTicketsListTitle => AppStrings.Get("Tickets_ListTitle");
    public string LblTicketsNewTitle => AppStrings.Get("Tickets_NewTitle");
    public string LblTicketsSortField => AppStrings.Get("Tickets_SortField");
    public string LblTicketsSortDirection => AppStrings.Get("Tickets_SortDirection");
    public string LblAdminUsersTitle => AppStrings.Get("Admin_UsersTitle");
    public string LblAdminUsersSubtitle => AppStrings.Get("Admin_UsersSubtitle");
    public string LblAdminRefreshList => AppStrings.Get("Admin_RefreshList");
    public string LblAdminActivate => AppStrings.Get("Admin_Activate");
    public string LblAdminBan => AppStrings.Get("Admin_Ban");
    public string LblAdminUnban => AppStrings.Get("Admin_Unban");
    public string LblAdminUnbanPassword => AppStrings.Get("Admin_UnbanPassword");
    public string LblAdminTabUsers => AppStrings.Get("Admin_TabUsers");
    public string LblAdminTabNewAccount => AppStrings.Get("Admin_TabNewAccount");
    public string LblSettingsTitle => AppStrings.Get("Settings_Title");
    public string LblSettingsSubtitle => AppStrings.Get("Settings_Subtitle");
    public string LblSettingsTheme => AppStrings.Get("Settings_Theme");
    public string LblSettingsLanguage => AppStrings.Get("Settings_Language");
    public string LblSettingsSave => AppStrings.Get("Settings_Save");
    public string LblSettingsRefreshSession => AppStrings.Get("Settings_RefreshSession");
    public string LblStatsTitle => AppStrings.Get("Stats_Title");
    public string LblStatsLoadAll => AppStrings.Get("Stats_LoadAll");
    public string LblStatsKpiAll => AppStrings.Get("Stats_KpiAll");
    public string LblStatsKpiNew => AppStrings.Get("Stats_KpiNew");
    public string LblStatsKpiInProgress => AppStrings.Get("Stats_KpiInProgress");
    public string LblStatsKpiClosed => AppStrings.Get("Stats_KpiClosed");
    public string LblStatsKpiHighPriority => AppStrings.Get("Stats_KpiHighPriority");

    private void NotifyLocalizationProperties()
    {
        OnPropertyChanged(nameof(LblNavTickets));
        OnPropertyChanged(nameof(LblNavRequestAccount));
        OnPropertyChanged(nameof(LblNavStatistics));
        OnPropertyChanged(nameof(LblNavSettings));
        OnPropertyChanged(nameof(LblNavAdmin));
        OnPropertyChanged(nameof(LblNavLogout));
        OnPropertyChanged(nameof(LblHeaderSubtitle));
        OnPropertyChanged(nameof(CurrentSectionTitle));
        OnPropertyChanged(nameof(LblTicketsFiltersTitle));
        OnPropertyChanged(nameof(LblTicketsFiltersSubtitle));
        OnPropertyChanged(nameof(LblTicketsSearchPlaceholder));
        OnPropertyChanged(nameof(LblTicketsSearch));
        OnPropertyChanged(nameof(LblTicketsClear));
        OnPropertyChanged(nameof(LblTicketsRefreshNow));
        OnPropertyChanged(nameof(LblTicketsListTitle));
        OnPropertyChanged(nameof(LblTicketsNewTitle));
        OnPropertyChanged(nameof(LblTicketsSortField));
        OnPropertyChanged(nameof(LblTicketsSortDirection));
        OnPropertyChanged(nameof(LblAdminUsersTitle));
        OnPropertyChanged(nameof(LblAdminUsersSubtitle));
        OnPropertyChanged(nameof(LblAdminRefreshList));
        OnPropertyChanged(nameof(LblAdminActivate));
        OnPropertyChanged(nameof(LblAdminBan));
        OnPropertyChanged(nameof(LblAdminUnban));
        OnPropertyChanged(nameof(LblAdminUnbanPassword));
        OnPropertyChanged(nameof(LblAdminTabUsers));
        OnPropertyChanged(nameof(LblAdminTabNewAccount));
        OnPropertyChanged(nameof(LblSettingsTitle));
        OnPropertyChanged(nameof(LblSettingsSubtitle));
        OnPropertyChanged(nameof(LblSettingsTheme));
        OnPropertyChanged(nameof(LblSettingsLanguage));
        OnPropertyChanged(nameof(LblSettingsSave));
        OnPropertyChanged(nameof(LblSettingsRefreshSession));
        OnPropertyChanged(nameof(LblStatsTitle));
        OnPropertyChanged(nameof(LblStatsLoadAll));
        OnPropertyChanged(nameof(LblStatsKpiAll));
        OnPropertyChanged(nameof(LblStatsKpiNew));
        OnPropertyChanged(nameof(LblStatsKpiInProgress));
        OnPropertyChanged(nameof(LblStatsKpiClosed));
        OnPropertyChanged(nameof(LblStatsKpiHighPriority));
        OnPropertyChanged(nameof(TicketSortFields));
        OnPropertyChanged(nameof(TicketSortDirections));
        OnPropertyChanged(nameof(AdminUserListFilterOptions));
        OnPropertyChanged(nameof(SelectedTicketSortField));
        OnPropertyChanged(nameof(SelectedTicketSortDirection));
        OnPropertyChanged(nameof(SelectedAdminUserListFilterOption));
    }
}
