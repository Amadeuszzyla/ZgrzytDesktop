using System.Linq;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private async Task LoadAdminUsersAsync()
    {
        if (!IsAdminRole)
        {
            AdminStatusMessage = AppStrings.Get("Api_Forbidden");
            return;
        }

        await ExecuteApiAsync(
            async () =>
            {
                AdminStatusMessage = AppStrings.Get("Admin_LoadingUsers");

                var filter = GetSelectedAdminUserListFilter();
                var users = await _userAdminService.GetUsersAsync(filter);

                AdminUsers.Clear();

                if (users is null || users.Count == 0)
                {
                    AdminStatusMessage = AppStrings.Get("Admin_NoUsers");
                    return;
                }

                foreach (var user in users.OrderBy(user => user.Login))
                    AdminUsers.Add(user);

                var filterLabel = SelectedAdminUserListFilterOption?.Label ?? AppStrings.Get("Admin_Filter_All");
                AdminStatusMessage = $"{AdminUsers.Count} — {filterLabel}";
            },
            setStatusMessage: message => AdminStatusMessage = message,
            unexpectedStatusMessage: AppStrings.Get("Toast_AdminUsersLoadFailed"),
            unexpectedToastMessage: AppStrings.Get("Toast_AdminUsersLoadFailed"),
            setOfflineOnServiceUnavailable: false);
    }

    private async Task BanAdminUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        var login = SelectedAdminUser.Login;

        await ExecuteApiAsync(
            async () =>
            {
                await _userAdminService.BanUserAsync(SelectedAdminUser!.Id);
                await LoadAdminUsersAsync();
                ShowToast(AppStrings.Get("Toast_UserBanned"), ToastTypes.Success);
                await LogAuditAsync("BanUser", null, $"Zbanowano użytkownika: {login}.");
            },
            setStatusMessage: message => AdminStatusMessage = message,
            unexpectedStatusMessage: AppStrings.Get("Toast_AdminBanFailed"),
            unexpectedToastMessage: AppStrings.Get("Toast_AdminBanFailed"),
            setOfflineOnServiceUnavailable: false);
    }

    private async Task ActivateAdminUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        var login = SelectedAdminUser.Login;

        await ExecuteApiAsync(
            async () =>
            {
                await _userAdminService.ActivateUserAsync(SelectedAdminUser!.Id);
                await LoadAdminUsersAsync();
                ShowToast(AppStrings.Get("Toast_UserActivated"), ToastTypes.Success);
                await LogAuditAsync("ActivateUser", null, $"Aktywowano użytkownika: {login}.");
            },
            setStatusMessage: message => AdminStatusMessage = message,
            unexpectedStatusMessage: AppStrings.Get("Toast_AdminActivateFailed"),
            unexpectedToastMessage: AppStrings.Get("Toast_AdminActivateFailed"),
            setOfflineOnServiceUnavailable: false);
    }

    private async Task UnbanAdminUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        if (string.IsNullOrWhiteSpace(AdminUnbanPassword))
        {
            ShowToast(AppStrings.Get("Toast_AdminUnbanPasswordRequired"), ToastTypes.Warning);
            return;
        }

        var login = SelectedAdminUser.Login;

        await ExecuteApiAsync(
            async () =>
            {
                await _userAdminService.UnbanUserAsync(SelectedAdminUser!.Id, AdminUnbanPassword.Trim());
                AdminUnbanPassword = string.Empty;
                await LoadAdminUsersAsync();
                ShowToast(AppStrings.Get("Toast_UserUnbanned"), ToastTypes.Success);
                await LogAuditAsync("UnbanUser", null, $"Odbanowano użytkownika: {login}.");
            },
            setStatusMessage: message => AdminStatusMessage = message,
            unexpectedStatusMessage: AppStrings.Get("Toast_AdminUnbanFailed"),
            unexpectedToastMessage: AppStrings.Get("Toast_AdminUnbanFailed"),
            setOfflineOnServiceUnavailable: false);
    }

    private UserAdminListFilter GetSelectedAdminUserListFilter() =>
        SelectedAdminUserListFilterOption?.Filter ?? UserAdminListFilter.All;
}
