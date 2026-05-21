using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeUserAdminService : IUserAdminService
{
    public int GetUsersCallCount { get; private set; }

    public Task<List<User>?> GetUsersAsync(UserAdminListFilter filter = UserAdminListFilter.All)
    {
        GetUsersCallCount++;
        return Task.FromResult<List<User>?>(new List<User>());
    }

    public Task<List<User>?> GetActiveUsersAsync() => GetUsersAsync(UserAdminListFilter.Active);

    public Task<List<User>?> GetInactiveUsersAsync() => GetUsersAsync(UserAdminListFilter.Inactive);

    public Task<List<User>?> GetBannedUsersAsync() => GetUsersAsync(UserAdminListFilter.Banned);

    public Task BanUserAsync(int userId) => Task.CompletedTask;

    public Task ActivateUserAsync(int userId) => Task.CompletedTask;

    public Task UnbanUserAsync(int userId, string password) => Task.CompletedTask;
}
