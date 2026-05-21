using System.Collections.Generic;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Services.Interfaces;

public interface IUserAdminService
{
    Task<List<User>?> GetUsersAsync(UserAdminListFilter filter = UserAdminListFilter.All);

    Task<List<User>?> GetActiveUsersAsync();

    Task<List<User>?> GetInactiveUsersAsync();

    Task<List<User>?> GetBannedUsersAsync();

    Task BanUserAsync(int userId);

    Task ActivateUserAsync(int userId);

    Task UnbanUserAsync(int userId, string password);
}
