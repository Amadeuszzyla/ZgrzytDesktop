using System.ComponentModel;
using ZgrzytDesktop.ViewModels.DashboardModules;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private DashboardLocalizationViewModel _localization = null!;

    private void InitializeLocalization()
    {
        _localization = new DashboardLocalizationViewModel();
        _localization.PropertyChanged += OnLocalizationPropertyChanged;
    }

    private void OnLocalizationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName))
            OnPropertyChanged(e.PropertyName);
    }

    public string LblNavTickets => _localization.LblNavTickets;

    public string LblNavRequestAccount => _localization.LblNavRequestAccount;

    public string LblNavStatistics => _localization.LblNavStatistics;

    public string LblNavSettings => _localization.LblNavSettings;

    public string LblNavAdmin => _localization.LblNavAdmin;

    public string LblNavLogout => _localization.LblNavLogout;

    public string LblHeaderSubtitle => _localization.LblHeaderSubtitle;

    public string LblTicketsFiltersTitle => _localization.LblTicketsFiltersTitle;

    public string LblTicketsFiltersSubtitle => _localization.LblTicketsFiltersSubtitle;

    public string LblTicketsSearchPlaceholder => _localization.LblTicketsSearchPlaceholder;

    public string LblTicketsFilterCategory => _localization.LblTicketsFilterCategory;

    public string LblTicketsSearch => _localization.LblTicketsSearch;

    public string LblTicketsClear => _localization.LblTicketsClear;

    public string LblTicketsRefreshNow => _localization.LblTicketsRefreshNow;

    public string LblTicketsListTitle => _localization.LblTicketsListTitle;

    public string LblTicketsNewTitle => _localization.LblTicketsNewTitle;

    public string LblTicketsSortField => _localization.LblTicketsSortField;

    public string LblTicketsSortDirection => _localization.LblTicketsSortDirection;

    public string LblAdminUsersTitle => _localization.LblAdminUsersTitle;

    public string LblAdminUsersSubtitle => _localization.LblAdminUsersSubtitle;

    public string LblAdminRefreshList => _localization.LblAdminRefreshList;

    public string LblAdminActivate => _localization.LblAdminActivate;

    public string LblAdminBan => _localization.LblAdminBan;

    public string LblAdminUnban => _localization.LblAdminUnban;

    public string LblAdminUnbanPassword => _localization.LblAdminUnbanPassword;

    public string LblAdminTabUsers => _localization.LblAdminTabUsers;

    public string LblAdminTabNewAccount => _localization.LblAdminTabNewAccount;

    public string LblAppBrandName => _localization.LblAppBrandName;

    public string LblAppBrandSuffix => _localization.LblAppBrandSuffix;

    public string LblGridId => _localization.LblGridId;

    public string LblGridCategory => _localization.LblGridCategory;

    public string LblGridTitle => _localization.LblGridTitle;

    public string LblGridStatus => _localization.LblGridStatus;

    public string LblGridPriority => _localization.LblGridPriority;

    public string LblGridCreatedAt => _localization.LblGridCreatedAt;

    public string LblGridLogin => _localization.LblGridLogin;

    public string LblGridEmail => _localization.LblGridEmail;

    public string LblGridRole => _localization.LblGridRole;

    public string LblGridActive => _localization.LblGridActive;

    public string LblGridBan => _localization.LblGridBan;

    public string LblTicketsPageSizeLabel => _localization.LblTicketsPageSizeLabel;

    public string LblTicketsPageFirst => _localization.LblTicketsPageFirst;

    public string LblTicketsPagePrevious => _localization.LblTicketsPagePrevious;

    public string LblTicketsPageLabel => _localization.LblTicketsPageLabel;

    public string LblTicketsPageNext => _localization.LblTicketsPageNext;

    public string LblTicketsPageLast => _localization.LblTicketsPageLast;

    public string LblTicketsNewSubtitle => _localization.LblTicketsNewSubtitle;

    public string LblTicketsFieldCategory => _localization.LblTicketsFieldCategory;

    public string LblTicketsFieldPriority => _localization.LblTicketsFieldPriority;

    public string LblTicketsFieldTitle => _localization.LblTicketsFieldTitle;

    public string LblTicketsFieldDescription => _localization.LblTicketsFieldDescription;

    public string LblTicketsNewTitlePlaceholder => _localization.LblTicketsNewTitlePlaceholder;

    public string LblTicketsNewDescriptionPlaceholder => _localization.LblTicketsNewDescriptionPlaceholder;

    public string LblTicketsCreateButton => _localization.LblTicketsCreateButton;

    public string LblTicketsEmptyList => _localization.LblTicketsEmptyList;

    public string LblDetailsBackToList => _localization.LblDetailsBackToList;

    public string LblDetailsInfoTitle => _localization.LblDetailsInfoTitle;

    public string LblDetailsProcessing => _localization.LblDetailsProcessing;

    public string LblDetailsReporter => _localization.LblDetailsReporter;

    public string LblDetailsAssignedIt => _localization.LblDetailsAssignedIt;

    public string LblDetailsDescription => _localization.LblDetailsDescription;

    public string LblDetailsCloseOwnHint => _localization.LblDetailsCloseOwnHint;

    public string LblDetailsStaffHint => _localization.LblDetailsStaffHint;

    public string LblDetailsOfflineHint => _localization.LblDetailsOfflineHint;

    public string LblDetailsManageTitle => _localization.LblDetailsManageTitle;

    public string LblDetailsSaveChanges => _localization.LblDetailsSaveChanges;

    public string LblDetailsAssignToMe => _localization.LblDetailsAssignToMe;

    public string LblTicketAssignTo => _localization.LblTicketAssignTo;

    public string LblTicketNoAssignableUsers => _localization.LblTicketNoAssignableUsers;

    public string LblTicketAssignToMe => _localization.LblTicketAssignToMe;

    public string LblTicketSaveAssignment => _localization.LblTicketSaveAssignment;

    public string LblDetailsCloseTicket => _localization.LblDetailsCloseTicket;

    public string LblDetailsDeleteTicket => _localization.LblDetailsDeleteTicket;

    public string LblDetailsLocalAuditTitle => _localization.LblDetailsLocalAuditTitle;

    public string LblDetailsLocalAuditSubtitle => _localization.LblDetailsLocalAuditSubtitle;

    public string LblDetailsLocalAuditEmpty => _localization.LblDetailsLocalAuditEmpty;

    public string LblDetailsMessagesTitle => _localization.LblDetailsMessagesTitle;

    public string LblDetailsMessagesSubtitle => _localization.LblDetailsMessagesSubtitle;

    public string LblDetailsMessagePlaceholder => _localization.LblDetailsMessagePlaceholder;

    public string LblDetailsSend => _localization.LblDetailsSend;

    public string LblDetailsNoMessages => _localization.LblDetailsNoMessages;

    public string LblDetailsAssignedHint => _localization.LblDetailsAssignedHint;

    public string LblDetailsNotAssignedHint => _localization.LblDetailsNotAssignedHint;

    public string LblRequestAccountTitle => _localization.LblRequestAccountTitle;

    public string LblRequestAccountSubtitle => _localization.LblRequestAccountSubtitle;

    public string LblRequestAccountFullName => _localization.LblRequestAccountFullName;

    public string LblRequestAccountLogin => _localization.LblRequestAccountLogin;

    public string LblRequestAccountEmail => _localization.LblRequestAccountEmail;

    public string LblRequestAccountPassword => _localization.LblRequestAccountPassword;

    public string LblRequestAccountPasswordConfirm => _localization.LblRequestAccountPasswordConfirm;

    public string LblRequestAccountPlaceholderName => _localization.LblRequestAccountPlaceholderName;

    public string LblRequestAccountPlaceholderLogin => _localization.LblRequestAccountPlaceholderLogin;

    public string LblRequestAccountPlaceholderEmail => _localization.LblRequestAccountPlaceholderEmail;

    public string LblRequestAccountPlaceholderPassword => _localization.LblRequestAccountPlaceholderPassword;

    public string LblRequestAccountPlaceholderPasswordConfirm => _localization.LblRequestAccountPlaceholderPasswordConfirm;

    public string LblRequestAccountSubmit => _localization.LblRequestAccountSubmit;

    public string LblAdminRegisterTitle => _localization.LblAdminRegisterTitle;

    public string LblAdminRegisterSubtitle => _localization.LblAdminRegisterSubtitle;

    public string LblRegisterUserTitle => _localization.LblRegisterUserTitle;

    public string LblRegisterUserSubtitle => _localization.LblRegisterUserSubtitle;

    public string LblRegisterUserFullName => _localization.LblRegisterUserFullName;

    public string LblRegisterUserLogin => _localization.LblRegisterUserLogin;

    public string LblRegisterUserEmail => _localization.LblRegisterUserEmail;

    public string LblRegisterUserPassword => _localization.LblRegisterUserPassword;

    public string LblRegisterUserPasswordConfirm => _localization.LblRegisterUserPasswordConfirm;

    public string LblRegisterUserRole => _localization.LblRegisterUserRole;

    public string LblRegisterUserSubmit => _localization.LblRegisterUserSubmit;

    public string LblAuditTitle => _localization.LblAuditTitle;

    public string LblAuditSubtitle => _localization.LblAuditSubtitle;

    public string LblAuditRefresh => _localization.LblAuditRefresh;

    public string LblAuditClear => _localization.LblAuditClear;

    public string LblAuditEmpty => _localization.LblAuditEmpty;

    public string LblAuditColumnTimestamp => _localization.LblAuditColumnTimestamp;

    public string LblAuditColumnUser => _localization.LblAuditColumnUser;

    public string LblAuditColumnAction => _localization.LblAuditColumnAction;

    public string LblAuditColumnTicketId => _localization.LblAuditColumnTicketId;

    public string LblAuditColumnDescription => _localization.LblAuditColumnDescription;

    private void NotifyLocalizationProperties()
    {
        _localization.NotifyLabels();
        _navigation.NotifyLocalization();
        SettingsPanel.NotifyLocalization();
        AuditPanel.NotifyLocalization();
        StatisticsPanel.NotifyLocalization();
        TicketsPanel.NotifyLocalization();
        TicketDetailsPanel.NotifyLocalization();
        AdminPanel.NotifyLocalization();
        RefreshAvailableStatusAndPriorityOptions();
        RefreshActiveLocalizedToast();
    }
}
