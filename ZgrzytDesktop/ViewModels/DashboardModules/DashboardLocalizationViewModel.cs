using System;
using System.Reflection;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

/// <summary>
/// Localized dashboard shell labels (nav, grids, tickets, details, admin, audit, request account).
/// </summary>
public sealed class DashboardLocalizationViewModel : ViewModelBase
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

    public string LblTicketsFilterCategory => AppStrings.Get("Tickets_FilterCategory");

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

    public string LblAppBrandName => AppStrings.Get("App_BrandName");

    public string LblAppBrandSuffix => AppStrings.Get("App_BrandSuffix");

    public string LblGridId => AppStrings.Get("Grid_Id");

    public string LblGridCategory => AppStrings.Get("Grid_Category");

    public string LblGridTitle => AppStrings.Get("Grid_Title");

    public string LblGridStatus => AppStrings.Get("Grid_Status");

    public string LblGridPriority => AppStrings.Get("Grid_Priority");

    public string LblGridCreatedAt => AppStrings.Get("Grid_CreatedAt");

    public string LblGridLogin => AppStrings.Get("Grid_Login");

    public string LblGridEmail => AppStrings.Get("Grid_Email");

    public string LblGridRole => AppStrings.Get("Grid_Role");

    public string LblGridActive => AppStrings.Get("Grid_Active");

    public string LblGridBan => AppStrings.Get("Grid_Ban");

    public string LblTicketsPageSizeLabel => AppStrings.Get("Tickets_PageSizeLabel");

    public string LblTicketsPageFirst => AppStrings.Get("Tickets_PageFirst");

    public string LblTicketsPagePrevious => AppStrings.Get("Tickets_PagePrevious");

    public string LblTicketsPageLabel => AppStrings.Get("Tickets_PageLabel");

    public string LblTicketsPageNext => AppStrings.Get("Tickets_PageNext");

    public string LblTicketsPageLast => AppStrings.Get("Tickets_PageLast");

    public string LblTicketsNewSubtitle => AppStrings.Get("Tickets_NewSubtitle");

    public string LblTicketsFieldCategory => AppStrings.Get("Tickets_FieldCategory");

    public string LblTicketsFieldPriority => AppStrings.Get("Tickets_FieldPriority");

    public string LblTicketsFieldTitle => AppStrings.Get("Tickets_FieldTitle");

    public string LblTicketsFieldDescription => AppStrings.Get("Tickets_FieldDescription");

    public string LblTicketsNewTitlePlaceholder => AppStrings.Get("Tickets_NewTitlePlaceholder");

    public string LblTicketsNewDescriptionPlaceholder => AppStrings.Get("Tickets_NewDescriptionPlaceholder");

    public string LblTicketsCreateButton => AppStrings.Get("Tickets_CreateButton");

    public string LblTicketsEmptyList => AppStrings.Get("Tickets_EmptyList");

    public string LblDetailsBackToList => AppStrings.Get("Details_BackToList");

    public string LblDetailsInfoTitle => AppStrings.Get("Details_InfoTitle");

    public string LblDetailsProcessing => AppStrings.Get("Details_Processing");

    public string LblDetailsReporter => AppStrings.Get("Details_Reporter");

    public string LblDetailsAssignedIt => AppStrings.Get("Details_AssignedIt");

    public string LblDetailsDescription => AppStrings.Get("Details_Description");

    public string LblDetailsCloseOwnHint => AppStrings.Get("Details_CloseOwnHint");

    public string LblDetailsStaffHint => AppStrings.Get("Details_StaffHint");

    public string LblDetailsOfflineHint => AppStrings.Get("Details_OfflineHint");

    public string LblDetailsManageTitle => AppStrings.Get("Details_ManageTitle");

    public string LblDetailsSaveChanges => AppStrings.Get("Details_SaveChanges");

    public string LblDetailsAssignToMe => AppStrings.Get("Details_AssignToMe");

    public string LblTicketAssignTo => AppStrings.Get("Ticket_AssignTo");

    public string LblTicketNoAssignableUsers => AppStrings.Get("Ticket_NoAssignableUsers");

    public string LblTicketAssignToMe => AppStrings.Get("Ticket_AssignToMe");

    public string LblTicketSaveAssignment => AppStrings.Get("Ticket_SaveAssignment");

    public string LblDetailsCloseTicket => AppStrings.Get("Details_CloseTicket");

    public string LblDetailsDeleteTicket => AppStrings.Get("Details_DeleteTicket");

    public string LblDetailsLocalAuditTitle => AppStrings.Get("Details_LocalAuditTitle");

    public string LblDetailsLocalAuditSubtitle => AppStrings.Get("Details_LocalAuditSubtitle");

    public string LblDetailsLocalAuditEmpty => AppStrings.Get("Details_LocalAuditEmpty");

    public string LblDetailsMessagesTitle => AppStrings.Get("Details_MessagesTitle");

    public string LblDetailsMessagesSubtitle => AppStrings.Get("Details_MessagesSubtitle");

    public string LblDetailsMessagePlaceholder => AppStrings.Get("Details_MessagePlaceholder");

    public string LblDetailsSend => AppStrings.Get("Details_Send");

    public string LblDetailsNoMessages => AppStrings.Get("Details_NoMessages");

    public string LblDetailsAssignedHint => AppStrings.Get("Details_AssignedHint");

    public string LblDetailsNotAssignedHint => AppStrings.Get("Details_NotAssignedHint");

    public string LblRequestAccountTitle => AppStrings.Get("RequestAccount_Title");

    public string LblRequestAccountSubtitle => AppStrings.Get("RequestAccount_Subtitle");

    public string LblRequestAccountFullName => AppStrings.Get("RequestAccount_FullName");

    public string LblRequestAccountLogin => AppStrings.Get("RequestAccount_Login");

    public string LblRequestAccountEmail => AppStrings.Get("RequestAccount_Email");

    public string LblRequestAccountPassword => AppStrings.Get("RequestAccount_Password");

    public string LblRequestAccountPasswordConfirm => AppStrings.Get("RequestAccount_PasswordConfirm");

    public string LblRequestAccountPlaceholderName => AppStrings.Get("RequestAccount_PlaceholderName");

    public string LblRequestAccountPlaceholderLogin => AppStrings.Get("RequestAccount_PlaceholderLogin");

    public string LblRequestAccountPlaceholderEmail => AppStrings.Get("RequestAccount_PlaceholderEmail");

    public string LblRequestAccountPlaceholderPassword => AppStrings.Get("RequestAccount_PlaceholderPassword");

    public string LblRequestAccountPlaceholderPasswordConfirm => AppStrings.Get("RequestAccount_PlaceholderPasswordConfirm");

    public string LblRequestAccountSubmit => AppStrings.Get("RequestAccount_Submit");

    public string LblAdminRegisterTitle => AppStrings.Get("RegisterUser_Title");

    public string LblAdminRegisterSubtitle => AppStrings.Get("RegisterUser_Subtitle");

    public string LblRegisterUserTitle => AppStrings.Get("RegisterUser_Title");

    public string LblRegisterUserSubtitle => AppStrings.Get("RegisterUser_Subtitle");

    public string LblRegisterUserFullName => AppStrings.Get("RequestAccount_FullName");

    public string LblRegisterUserLogin => AppStrings.Get("RequestAccount_Login");

    public string LblRegisterUserEmail => AppStrings.Get("RequestAccount_Email");

    public string LblRegisterUserPassword => AppStrings.Get("RequestAccount_Password");

    public string LblRegisterUserPasswordConfirm => AppStrings.Get("RequestAccount_PasswordConfirm");

    public string LblRegisterUserRole => AppStrings.Get("RegisterUser_Role");

    public string LblRegisterUserSubmit => AppStrings.Get("RegisterUser_Submit");

    public string LblAuditTitle => AppStrings.Get("Audit_Title");

    public string LblAuditSubtitle => AppStrings.Get("Audit_Subtitle");

    public string LblAuditRefresh => AppStrings.Get("Audit_Refresh");

    public string LblAuditClear => AppStrings.Get("Audit_Clear");

    public string LblAuditEmpty => AppStrings.Get("Audit_Empty");

    public string LblAuditColumnTimestamp => AppStrings.Get("Audit_ColumnTimestamp");

    public string LblAuditColumnUser => AppStrings.Get("Audit_ColumnUser");

    public string LblAuditColumnAction => AppStrings.Get("Audit_ColumnAction");

    public string LblAuditColumnTicketId => AppStrings.Get("Audit_ColumnTicketId");

    public string LblAuditColumnDescription => AppStrings.Get("Audit_ColumnDescription");

    public void NotifyLabels()
    {
        foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType != typeof(string) ||
                !property.Name.StartsWith("Lbl", StringComparison.Ordinal))
                continue;

            OnPropertyChanged(property.Name);
        }
    }
}
