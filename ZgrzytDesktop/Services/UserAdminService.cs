using System.Collections.Generic;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services;

public class UserAdminService
{
    private readonly ApiService _apiService;

    public UserAdminService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<User>?> GetUsersAsync()
    {
        return await _apiService.GetAsync<List<User>>("users");
    }

    public async Task BanUserAsync(int userId)
    {
        await _apiService.PostAsync<object, object?>($"users/{userId}/ban", new { });
    }

    public async Task ActivateUserAsync(int userId)
    {
        await _apiService.PostAsync<object, object?>($"users/{userId}/activate", new { });
    }
}
