using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeAuthService : IAuthService
{
    public User? LoginResult { get; set; }

    public Exception? LoginException { get; set; }

    public User? CurrentUserResult { get; set; }

    public Exception? CurrentUserException { get; set; }

    public bool RefreshResult { get; set; } = true;

    public Exception? RefreshException { get; set; }

    public bool RequestAccountResult { get; set; } = true;

    public int LogoutCallCount { get; private set; }

    public (string Login, string Password)? LastLoginCredentials { get; private set; }

    public Task<User?> LoginAsync(string login, string password)
    {
        LastLoginCredentials = (login, password);

        if (LoginException is not null)
            throw LoginException;

        return Task.FromResult(LoginResult);
    }

    public Task<User?> GetCurrentUserAsync()
    {
        if (CurrentUserException is not null)
            throw CurrentUserException;

        return Task.FromResult(CurrentUserResult);
    }

    public Task<bool> RequestAccountAsync(RequestAccountRequest request) =>
        Task.FromResult(RequestAccountResult);

    public Task<bool> RefreshTokenAsync()
    {
        if (RefreshException is not null)
            throw RefreshException;

        return Task.FromResult(RefreshResult);
    }

    public Task LogoutAsync()
    {
        LogoutCallCount++;
        return Task.CompletedTask;
    }
}
