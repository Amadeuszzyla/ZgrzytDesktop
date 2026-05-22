using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeUserAdminService : IUserAdminService
{
    public int GetUsersCallCount { get; private set; }

    public Task<UserAdminListResult> GetUsersAsync(UserAdminListFilter filter = UserAdminListFilter.All)
    {
        GetUsersCallCount++;
        return Task.FromResult(new UserAdminListResult { Users = [] });
    }

    public Task<UserAdminListResult> GetActiveUsersAsync() => GetUsersAsync(UserAdminListFilter.Active);

    public Task<UserAdminListResult> GetInactiveUsersAsync() => GetUsersAsync(UserAdminListFilter.Inactive);

    public Task<UserAdminListResult> GetBannedUsersAsync() => GetUsersAsync(UserAdminListFilter.Banned);

    public Task BanUserAsync(int userId) => Task.CompletedTask;

    public Task ActivateUserAsync(int userId) => Task.CompletedTask;

    public Task UnbanUserAsync(int userId, string password) => Task.CompletedTask;
}
