using System.Linq;
using System.Threading.Tasks;
using ZgrzytDesktop.Exceptions;
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

        try
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
        }
        catch (ApiException ex)
        {
            AdminStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            AdminStatusMessage = AppStrings.Get("Toast_AdminUsersLoadFailed");
            ShowToast(AppStrings.Get("Toast_AdminUsersLoadFailed"), "error");
        }
    }

    private async Task BanAdminUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        try
        {
            var login = SelectedAdminUser.Login;

            await _userAdminService.BanUserAsync(SelectedAdminUser.Id);
            await LoadAdminUsersAsync();
            ShowToast(AppStrings.Get("Toast_UserBanned"), "success");

            await LogAuditAsync("BanUser", null, $"Zbanowano użytkownika: {login}.");
        }
        catch (ApiException ex)
        {
            AdminStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            AdminStatusMessage = AppStrings.Get("Toast_AdminBanFailed");
            ShowToast(AppStrings.Get("Toast_AdminBanFailed"), "error");
        }
    }

    private async Task ActivateAdminUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        try
        {
            var login = SelectedAdminUser.Login;

            await _userAdminService.ActivateUserAsync(SelectedAdminUser.Id);
            await LoadAdminUsersAsync();
            ShowToast(AppStrings.Get("Toast_UserActivated"), "success");

            await LogAuditAsync("ActivateUser", null, $"Aktywowano użytkownika: {login}.");
        }
        catch (ApiException ex)
        {
            AdminStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            AdminStatusMessage = AppStrings.Get("Toast_AdminActivateFailed");
            ShowToast(AppStrings.Get("Toast_AdminActivateFailed"), "error");
        }
    }

    private async Task UnbanAdminUserAsync()
    {
        if (SelectedAdminUser is null)
            return;

        if (string.IsNullOrWhiteSpace(AdminUnbanPassword))
        {
            ShowToast(AppStrings.Get("Toast_AdminUnbanPasswordRequired"), "warning");
            return;
        }

        try
        {
            var login = SelectedAdminUser.Login;

            await _userAdminService.UnbanUserAsync(SelectedAdminUser.Id, AdminUnbanPassword.Trim());
            AdminUnbanPassword = string.Empty;
            await LoadAdminUsersAsync();
            ShowToast(AppStrings.Get("Toast_UserUnbanned"), "success");

            await LogAuditAsync("UnbanUser", null, $"Odbanowano użytkownika: {login}.");
        }
        catch (ApiException ex)
        {
            AdminStatusMessage = GetApiErrorMessage(ex);
            ShowToast(GetApiErrorMessage(ex), "error");
        }
        catch
        {
            AdminStatusMessage = AppStrings.Get("Toast_AdminUnbanFailed");
            ShowToast(AppStrings.Get("Toast_AdminUnbanFailed"), "error");
        }
    }

    private UserAdminListFilter GetSelectedAdminUserListFilter() =>
        SelectedAdminUserListFilterOption?.Filter ?? UserAdminListFilter.All;
}
