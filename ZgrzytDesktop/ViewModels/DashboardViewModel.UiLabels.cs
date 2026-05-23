using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
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

    public string LblAdminRegisterTitle => AppStrings.Get("Admin_RegisterTitle");

    public string LblAdminRegisterSubtitle => AppStrings.Get("Admin_RegisterSubtitle");

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

    private void NotifyUiLabels()
    {
        OnPropertyChanged(nameof(LblAppBrandName));
        OnPropertyChanged(nameof(LblAppBrandSuffix));
        OnPropertyChanged(nameof(LblGridId));
        OnPropertyChanged(nameof(LblGridCategory));
        OnPropertyChanged(nameof(LblGridTitle));
        OnPropertyChanged(nameof(LblGridStatus));
        OnPropertyChanged(nameof(LblGridPriority));
        OnPropertyChanged(nameof(LblGridCreatedAt));
        OnPropertyChanged(nameof(LblGridLogin));
        OnPropertyChanged(nameof(LblGridEmail));
        OnPropertyChanged(nameof(LblGridRole));
        OnPropertyChanged(nameof(LblGridActive));
        OnPropertyChanged(nameof(LblGridBan));
        OnPropertyChanged(nameof(LblTicketsPageSizeLabel));
        OnPropertyChanged(nameof(LblTicketsPageFirst));
        OnPropertyChanged(nameof(LblTicketsPagePrevious));
        OnPropertyChanged(nameof(LblTicketsPageLabel));
        OnPropertyChanged(nameof(LblTicketsPageNext));
        OnPropertyChanged(nameof(LblTicketsPageLast));
        OnPropertyChanged(nameof(LblTicketsNewSubtitle));
        OnPropertyChanged(nameof(LblTicketsFieldCategory));
        OnPropertyChanged(nameof(LblTicketsFieldPriority));
        OnPropertyChanged(nameof(LblTicketsFieldTitle));
        OnPropertyChanged(nameof(LblTicketsFieldDescription));
        OnPropertyChanged(nameof(LblTicketsNewTitlePlaceholder));
        OnPropertyChanged(nameof(LblTicketsNewDescriptionPlaceholder));
        OnPropertyChanged(nameof(LblTicketsCreateButton));
        OnPropertyChanged(nameof(LblTicketsEmptyList));
        OnPropertyChanged(nameof(LblDetailsBackToList));
        OnPropertyChanged(nameof(LblDetailsInfoTitle));
        OnPropertyChanged(nameof(LblDetailsProcessing));
        OnPropertyChanged(nameof(LblDetailsReporter));
        OnPropertyChanged(nameof(LblDetailsAssignedIt));
        OnPropertyChanged(nameof(LblDetailsDescription));
        OnPropertyChanged(nameof(LblDetailsCloseOwnHint));
        OnPropertyChanged(nameof(LblDetailsStaffHint));
        OnPropertyChanged(nameof(LblDetailsOfflineHint));
        OnPropertyChanged(nameof(LblDetailsManageTitle));
        OnPropertyChanged(nameof(LblDetailsSaveChanges));
        OnPropertyChanged(nameof(LblDetailsAssignToMe));
        OnPropertyChanged(nameof(LblDetailsCloseTicket));
        OnPropertyChanged(nameof(LblDetailsDeleteTicket));
        OnPropertyChanged(nameof(LblDetailsLocalAuditTitle));
        OnPropertyChanged(nameof(LblDetailsLocalAuditSubtitle));
        OnPropertyChanged(nameof(LblDetailsLocalAuditEmpty));
        OnPropertyChanged(nameof(LblDetailsMessagesTitle));
        OnPropertyChanged(nameof(LblDetailsMessagesSubtitle));
        OnPropertyChanged(nameof(LblDetailsMessagePlaceholder));
        OnPropertyChanged(nameof(LblDetailsSend));
        OnPropertyChanged(nameof(LblDetailsNoMessages));
        OnPropertyChanged(nameof(LblDetailsAssignedHint));
        OnPropertyChanged(nameof(LblDetailsNotAssignedHint));
        OnPropertyChanged(nameof(LblRequestAccountTitle));
        OnPropertyChanged(nameof(LblRequestAccountSubtitle));
        OnPropertyChanged(nameof(LblRequestAccountFullName));
        OnPropertyChanged(nameof(LblRequestAccountLogin));
        OnPropertyChanged(nameof(LblRequestAccountEmail));
        OnPropertyChanged(nameof(LblRequestAccountPassword));
        OnPropertyChanged(nameof(LblRequestAccountPasswordConfirm));
        OnPropertyChanged(nameof(LblRequestAccountPlaceholderName));
        OnPropertyChanged(nameof(LblRequestAccountPlaceholderLogin));
        OnPropertyChanged(nameof(LblRequestAccountPlaceholderEmail));
        OnPropertyChanged(nameof(LblRequestAccountPlaceholderPassword));
        OnPropertyChanged(nameof(LblRequestAccountPlaceholderPasswordConfirm));
        OnPropertyChanged(nameof(LblRequestAccountSubmit));
        OnPropertyChanged(nameof(LblAdminRegisterTitle));
        OnPropertyChanged(nameof(LblAdminRegisterSubtitle));
        OnPropertyChanged(nameof(LblAuditTitle));
        OnPropertyChanged(nameof(LblAuditSubtitle));
        OnPropertyChanged(nameof(LblAuditRefresh));
        OnPropertyChanged(nameof(LblAuditClear));
        OnPropertyChanged(nameof(LblAuditEmpty));
        OnPropertyChanged(nameof(LblAuditColumnTimestamp));
        OnPropertyChanged(nameof(LblAuditColumnUser));
        OnPropertyChanged(nameof(LblAuditColumnAction));
        OnPropertyChanged(nameof(LblAuditColumnTicketId));
        OnPropertyChanged(nameof(LblAuditColumnDescription));
    }
}
