using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels.DashboardModules;

public sealed partial class TicketDetailsPanelViewModel
{
    private AssignableUserOption? _selectedAssignedUser;
    private bool _assignableUsersLoaded;
    private bool _assignableUsersLoadFailed;

    public ObservableCollection<AssignableUserOption> AssignableUsers { get; } = new();

    public AssignableUserOption? SelectedAssignedUser
    {
        get => _selectedAssignedUser;
        set
        {
            if (SetProperty(ref _selectedAssignedUser, value))
            {
                OnPropertyChanged(nameof(SelectedAssignableUser));
                OnPropertyChanged(nameof(SelectedAssignedUserId));
                NotifyCapabilityProperties();
            }
        }
    }

    public AssignableUserOption? SelectedAssignableUser
    {
        get => SelectedAssignedUser;
        set => SelectedAssignedUser = value;
    }

    public int? SelectedAssignedUserId =>
        SelectedAssignedUser?.IsUnassigned == true ? null : SelectedAssignedUser?.UserId;

    public bool CanSelectAssignee =>
        _callbacks.GetIsAdminRole() &&
        !_callbacks.GetIsOffline() &&
        !IsLoadingDetails &&
        TicketDetails is not null;

    public bool HasNoAssignableStaff =>
        _assignableUsersLoaded &&
        !_assignableUsersLoadFailed &&
        !AssignableUsers.Any(option => !option.IsUnassigned);

    public bool ShowAssignableUsersEmptyMessage =>
        CanSelectAssignee && HasNoAssignableStaff;

    public bool CanShowAdminAssignmentControls => CanSelectAssignee;

    public bool CanClearAssignment => TicketAssignmentContract.SupportsClearAssignment;

    public bool CanAssignSelectedUser =>
        CanSelectAssignee &&
        _assignableUsersLoaded &&
        !_assignableUsersLoadFailed &&
        SelectedAssignedUser is not null &&
        !SelectedAssignedUser.IsUnassigned &&
        SelectedAssignedUser.UserId is > 0 &&
        IsSelectedUserAssignableStaff();

    public IAsyncRelayCommand AssignSelectedUserCommand { get; private set; } = null!;

    private void InitializeAssignmentCommands()
    {
        AssignSelectedUserCommand = new AsyncRelayCommand(AssignSelectedUserAsync, () => CanAssignSelectedUser);
    }

    private async Task LoadAssignableUsersIfNeededAsync()
    {
        if (!_callbacks.GetIsAdminRole() || _callbacks.GetIsOffline())
        {
            ClearAssignableUsers();
            return;
        }

        try
        {
            var staff = await FetchAssignableStaffAsync();
            ApplyAssignableStaffList(staff);
        }
        catch (ApiException ex)
        {
            ClearAssignableUsers();
            _assignableUsersLoadFailed = true;
            DetailsStatusMessage = _callbacks.GetApiErrorMessage(ex);
        }
        catch
        {
            ClearAssignableUsers();
            _assignableUsersLoadFailed = true;
            DetailsStatusMessage = AppStrings.Get("Ticket_AssignableUsersLoadFailed");
        }
        finally
        {
            OnPropertyChanged(nameof(HasNoAssignableStaff));
            OnPropertyChanged(nameof(ShowAssignableUsersEmptyMessage));
            NotifyCapabilityProperties();
            OnPropertyChanged(nameof(CanShowAdminAssignmentControls));
        }
    }

    private async Task<List<User>> FetchAssignableStaffAsync()
    {
        var allResult = await _userAdminService.GetUsersAsync(UserAdminListFilter.All);

        var staffFromAll = TicketAssignableStaffFilter.FilterAssignableStaff(
            allResult.Users,
            fromActiveUsersList: false);

        if (staffFromAll.Count > 0)
            return staffFromAll;

        staffFromAll = TicketAssignableStaffFilter.FilterAssignableStaff(
            allResult.Users,
            fromActiveUsersList: true);

        if (staffFromAll.Count > 0)
            return staffFromAll;

        try
        {
            var activeResult = await _userAdminService.GetActiveUsersAsync();
            var fromActiveList = !activeResult.UsedLocalFilterFallback;
            var staffFromActive = TicketAssignableStaffFilter.FilterAssignableStaff(
                activeResult.Users,
                fromActiveList);

            if (staffFromActive.Count > 0)
                return staffFromActive;
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            // Already tried the full users list above.
        }

        return [];
    }

    private void ApplyAssignableStaffList(List<User> staff)
    {
        _cachedAssignableStaffUsers = staff;

        AssignableUsers.Clear();

        if (CanClearAssignment)
            AssignableUsers.Add(AssignableUserOption.CreateUnassigned());

        foreach (var user in staff)
            AssignableUsers.Add(AssignableUserOption.FromUser(user));

        _assignableUsersLoaded = true;
        _assignableUsersLoadFailed = false;

        if (HasNoAssignableStaff)
            DetailsStatusMessage = AppStrings.Get("Ticket_NoAssignableUsers");

        SyncSelectedAssignedUserFromTicket();
    }

    private void ClearAssignableUsers()
    {
        AssignableUsers.Clear();
        SelectedAssignedUser = null;
        _assignableUsersLoaded = false;
        _assignableUsersLoadFailed = false;
        OnPropertyChanged(nameof(HasNoAssignableStaff));
        OnPropertyChanged(nameof(ShowAssignableUsersEmptyMessage));
    }

    private void SyncSelectedAssignedUserFromTicket()
    {
        if (!_callbacks.GetIsAdminRole() || TicketDetails is null || AssignableUsers.Count == 0)
            return;

        var assigneeId = TicketDetails.AssignedItId ?? TicketDetails.AssignedTo?.Id;

        SelectedAssignedUser = assigneeId is null or 0
            ? AssignableUsers.FirstOrDefault(option => option.IsUnassigned) ??
              AssignableUsers.FirstOrDefault()
            : AssignableUsers.FirstOrDefault(option => option.UserId == assigneeId) ??
              AssignableUsers.FirstOrDefault();
    }

    private bool IsSelectedUserAssignableStaff()
    {
        if (SelectedAssignedUser?.UserId is not int userId)
            return false;

        return _cachedAssignableStaffUsers.Any(staff => staff.Id == userId);
    }

    private async Task AssignSelectedUserAsync()
    {
        if (TicketDetails is null || SelectedAssignedUser is null)
            return;

        if (SelectedAssignedUser.IsUnassigned)
            return;

        if (!IsSelectedUserAssignableStaff())
        {
            DetailsStatusMessage = AppStrings.Get("Ticket_AssignmentFailed");
            _callbacks.ShowToastKey("Toast_TicketAssignmentFailed", ToastTypes.Error);
            return;
        }

        var targetUserId = SelectedAssignedUser.UserId;
        var currentAssigneeId = TicketDetails.AssignedItId ?? TicketDetails.AssignedTo?.Id;

        if (currentAssigneeId != targetUserId &&
            !await _callbacks.ConfirmAsync("Confirm_AssignmentChange", "Confirm_Title"))
        {
            SyncSelectedAssignedUserFromTicket();
            return;
        }

        await AssignTicketAsync(
            targetUserId,
            auditAction: "AssignTicket",
            auditDescriptionKey: "Details_AssignAuditDesc",
            auditArgs: [SelectedAssignedUser.Label],
            successStatusKey: "Ticket_AssignmentSaved",
            successToastKey: "Toast_TicketAssignmentSaved",
            unexpectedStatusKey: "Ticket_AssignmentFailed",
            unexpectedToastKey: "Toast_TicketAssignmentFailed");
    }

    private async Task AssignTicketAsync(
        int? assignedItId,
        string auditAction,
        string auditDescriptionKey,
        object?[]? auditArgs,
        string successStatusKey,
        string successToastKey,
        string unexpectedStatusKey,
        string unexpectedToastKey)
    {
        if (!_callbacks.GetCanManageTickets())
        {
            DetailsStatusMessage = AppStrings.Get("Details_NoAssignPermission");
            _callbacks.ShowToastKey("Toast_DetailsAssignForbidden", ToastTypes.Warning);
            return;
        }

        if (_callbacks.GetIsOffline())
        {
            DetailsStatusMessage = AppStrings.Get("Details_OfflineAssign");
            _callbacks.ShowToastKey("Toast_DetailsAssignOffline", ToastTypes.Warning);
            return;
        }

        if (TicketDetails is null)
        {
            DetailsStatusMessage = AppStrings.Get("Details_SelectFirst");
            return;
        }

        try
        {
            IsLoadingDetails = true;
            DetailsStatusMessage = AppStrings.Get("Details_Assigning");

            var ticketId = TicketDetails.Id;
            var request = new UpdateTicketRequest
            {
                AssignedItId = assignedItId
            };

            await _callbacks.ExecuteApiAsync(
                async () =>
                {
                    await _ticketService.UpdateTicketAsync(ticketId, request);

                    _callbacks.SetIsOffline(false);

                    await LoadTicketDetailsAsync(ticketId);
                    await _callbacks.RefreshTicketsAsync();

                    DetailsStatusMessage = AppStrings.Get(successStatusKey);
                    _callbacks.ShowToastKey(successToastKey, ToastTypes.Success);

                    await _callbacks.LogAuditAsync(
                        auditAction,
                        ticketId,
                        auditDescriptionKey,
                        auditArgs);

                    SyncSelectedAssignedUserFromTicket();
                },
                setStatusMessage: message => DetailsStatusMessage = message,
                unexpectedStatusMessageKey: unexpectedStatusKey,
                unexpectedToastMessageKey: unexpectedToastKey,
                onServiceUnavailableAsync: async _ =>
                {
                    _callbacks.SetIsOffline(true);
                    DetailsStatusMessage = AppStrings.Get("Details_OfflineAssignFailed");
                    _callbacks.ShowToastKey("Toast_TicketAssignOffline", ToastTypes.Error);
                    await Task.CompletedTask;
                });
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private void RefreshAssignmentLocalization()
    {
        if (!_callbacks.GetIsAdminRole() || AssignableUsers.Count == 0)
            return;

        var selectedUserId = SelectedAssignedUserId;
        var wasUnassigned = SelectedAssignedUser?.IsUnassigned == true;

        AssignableUsers.Clear();

        if (CanClearAssignment)
            AssignableUsers.Add(AssignableUserOption.CreateUnassigned());

        foreach (var user in _cachedAssignableStaffUsers)
            AssignableUsers.Add(AssignableUserOption.FromUser(user));

        SelectedAssignedUser = wasUnassigned
            ? AssignableUsers.FirstOrDefault(option => option.IsUnassigned)
            : AssignableUsers.FirstOrDefault(option => option.UserId == selectedUserId) ??
              AssignableUsers.FirstOrDefault();
    }

    private List<User> _cachedAssignableStaffUsers = new();
}
