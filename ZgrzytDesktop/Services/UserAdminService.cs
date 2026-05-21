using System.Collections.Generic;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

public class UserAdminService : IUserAdminService
{
    private readonly ApiService _apiService;

    public UserAdminService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public static string ResolveUsersListEndpoint(UserAdminListFilter filter) =>
        filter switch
        {
            UserAdminListFilter.Active => "active-users",
            UserAdminListFilter.Inactive => "inactive-users",
            UserAdminListFilter.Banned => "banned-users",
            _ => "users"
        };

    public async Task<List<User>?> GetUsersAsync(UserAdminListFilter filter = UserAdminListFilter.All)
    {
        return await _apiService.GetAsync<List<User>>(ResolveUsersListEndpoint(filter));
    }

    public Task<List<User>?> GetActiveUsersAsync() =>
        GetUsersAsync(UserAdminListFilter.Active);

    public Task<List<User>?> GetInactiveUsersAsync() =>
        GetUsersAsync(UserAdminListFilter.Inactive);

    public Task<List<User>?> GetBannedUsersAsync() =>
        GetUsersAsync(UserAdminListFilter.Banned);

    public async Task BanUserAsync(int userId)
    {
        await _apiService.PostAsync<object, object?>($"users/{userId}/ban", new { });
    }

    public async Task ActivateUserAsync(int userId)
    {
        await _apiService.PostAsync<object, object?>($"users/{userId}/activate", new { });
    }

    public async Task UnbanUserAsync(int userId, string password)
    {
        var request = new UnbanUserRequest
        {
            Password = password
        };

        await _apiService.PostAsync<UnbanUserRequest, object?>($"users/{userId}/unban", request);
    }
}
