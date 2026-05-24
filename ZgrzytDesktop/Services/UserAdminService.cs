using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

public class UserAdminService : IUserAdminService
{
    private readonly IApiService _apiService;

    public UserAdminService(IApiService apiService)
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

    public async Task<UserAdminListResult> GetUsersAsync(UserAdminListFilter filter = UserAdminListFilter.All)
    {
        if (filter == UserAdminListFilter.All)
        {
            var users = await FetchUsersAsync("users");
            return new UserAdminListResult { Users = users };
        }

        var endpoint = ResolveUsersListEndpoint(filter);

        try
        {
            var users = await FetchUsersAsync(endpoint);
            return new UserAdminListResult { Users = users };
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return await FetchWithLocalFilterFallbackAsync(filter);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new ApiException(
                HttpStatusCode.Forbidden,
                AppStrings.Get("Admin_ListForbidden"),
                ex.ResponseContent);
        }
    }

    public Task<UserAdminListResult> GetActiveUsersAsync() =>
        GetUsersAsync(UserAdminListFilter.Active);

    public Task<UserAdminListResult> GetInactiveUsersAsync() =>
        GetUsersAsync(UserAdminListFilter.Inactive);

    public Task<UserAdminListResult> GetBannedUsersAsync() =>
        GetUsersAsync(UserAdminListFilter.Banned);

    public async Task BanUserAsync(int userId)
    {
        try
        {
            await _apiService.PostAsync<object, object?>($"users/{userId}/ban", new { });
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ApiException(
                ex.StatusCode,
                AppStrings.Get("Admin_ActionNotSupported"),
                ex.ResponseContent);
        }
    }

    public async Task ActivateUserAsync(int userId)
    {
        try
        {
            await _apiService.PostAsync<object, object?>($"users/{userId}/activate", new { });
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ApiException(
                ex.StatusCode,
                AppStrings.Get("Admin_ActionNotSupported"),
                ex.ResponseContent);
        }
    }

    public async Task UnbanUserAsync(int userId, string password)
    {
        var request = new UnbanUserRequest
        {
            Password = password
        };

        try
        {
            await _apiService.PostAsync<UnbanUserRequest, object?>($"users/{userId}/unban", request);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ApiException(
                ex.StatusCode,
                AppStrings.Get("Admin_ActionNotSupported"),
                ex.ResponseContent);
        }
    }

    private async Task<UserAdminListResult> FetchWithLocalFilterFallbackAsync(UserAdminListFilter filter)
    {
        var allUsers = await FetchUsersAsync("users");

        if (filter == UserAdminListFilter.Banned && !UsersExposeBanStatus(allUsers))
        {
            return new UserAdminListResult
            {
                Users = [],
                UsedLocalFilterFallback = true,
                InfoKind = UserAdminListInfoKind.BannedListNotSupported
            };
        }

        var filtered = FilterUsersLocally(allUsers, filter);

        return new UserAdminListResult
        {
            Users = filtered,
            UsedLocalFilterFallback = true,
            InfoKind = UserAdminListInfoKind.LocalFilterFallback
        };
    }

    internal static List<User> FilterUsersLocally(IReadOnlyList<User> users, UserAdminListFilter filter) =>
        filter switch
        {
            UserAdminListFilter.Active => users.Where(user => user.Active && !user.Ban).ToList(),
            UserAdminListFilter.Inactive => users.Where(user => !user.Active && !user.Ban).ToList(),
            UserAdminListFilter.Banned => users.Where(user => user.Ban || user.BannedAt.HasValue).ToList(),
            _ => users.ToList()
        };

    internal static bool UsersExposeBanStatus(IReadOnlyList<User> users) =>
        users.Any(user => user.Ban || user.BannedAt.HasValue);

    public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request)
    {
        var response = await _apiService.PostAsync<RegisterUserRequest, RegisterUserResponse>("register", request);

        return response ?? new RegisterUserResponse();
    }

    private async Task<List<User>> FetchUsersAsync(string endpoint)
    {
        var json = await _apiService.GetAsync<JsonElement?>(endpoint);
        if (json is not JsonElement root ||
            root.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return [];
        }

        return ApiUserListParser.ParseUsers(root);
    }
}
