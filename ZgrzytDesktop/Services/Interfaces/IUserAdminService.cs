using System.Collections.Generic;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Services.Interfaces;

public interface IUserAdminService
{
    Task<UserAdminListResult> GetUsersAsync(UserAdminListFilter filter = UserAdminListFilter.All);

    Task<UserAdminListResult> GetActiveUsersAsync();

    Task<UserAdminListResult> GetInactiveUsersAsync();

    Task<UserAdminListResult> GetBannedUsersAsync();

    Task BanUserAsync(int userId);

    Task ActivateUserAsync(int userId);

    Task UnbanUserAsync(int userId, string password);

    Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request);
}
