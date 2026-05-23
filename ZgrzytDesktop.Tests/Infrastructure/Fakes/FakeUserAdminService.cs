using System.Collections.Generic;
using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeUserAdminService : IUserAdminService
{
    public int GetUsersCallCount { get; private set; }

    public int BanCallCount { get; private set; }

    public int ActivateCallCount { get; private set; }

    public int UnbanCallCount { get; private set; }

    public UserAdminListFilter LastFilter { get; private set; }

    public int? LastBanUserId { get; private set; }

    public int? LastActivateUserId { get; private set; }

    public int? LastUnbanUserId { get; private set; }

    public string? LastUnbanPassword { get; private set; }

    public List<User> NextUsers { get; set; } = [];

    public string? NextInformationalMessage { get; set; }

    public UserAdminListInfoKind NextInfoKind { get; set; }

    public bool NextUsedLocalFilterFallback { get; set; }

    public ApiException? GetUsersApiException { get; set; }

    public ApiException? BanApiException { get; set; }

    public ApiException? ActivateApiException { get; set; }

    public ApiException? UnbanApiException { get; set; }

    public Task<UserAdminListResult> GetUsersAsync(UserAdminListFilter filter = UserAdminListFilter.All)
    {
        GetUsersCallCount++;
        LastFilter = filter;

        if (GetUsersApiException is not null)
            throw GetUsersApiException;

        return Task.FromResult(new UserAdminListResult
        {
            Users = NextUsers,
            InformationalMessage = NextInformationalMessage,
            UsedLocalFilterFallback = NextUsedLocalFilterFallback,
            InfoKind = NextInfoKind
        });
    }

    public Task<UserAdminListResult> GetActiveUsersAsync() => GetUsersAsync(UserAdminListFilter.Active);

    public Task<UserAdminListResult> GetInactiveUsersAsync() => GetUsersAsync(UserAdminListFilter.Inactive);

    public Task<UserAdminListResult> GetBannedUsersAsync() => GetUsersAsync(UserAdminListFilter.Banned);

    public Task BanUserAsync(int userId)
    {
        BanCallCount++;
        LastBanUserId = userId;

        if (BanApiException is not null)
            throw BanApiException;

        return Task.CompletedTask;
    }

    public Task ActivateUserAsync(int userId)
    {
        ActivateCallCount++;
        LastActivateUserId = userId;

        if (ActivateApiException is not null)
            throw ActivateApiException;

        return Task.CompletedTask;
    }

    public Task UnbanUserAsync(int userId, string password)
    {
        UnbanCallCount++;
        LastUnbanUserId = userId;
        LastUnbanPassword = password;

        if (UnbanApiException is not null)
            throw UnbanApiException;

        return Task.CompletedTask;
    }
}
