using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeUserConfirmationService : IUserConfirmationService
{
    public bool NextResult { get; set; } = true;

    public int ConfirmCallCount { get; private set; }

    public string? LastMessageKey { get; private set; }

    public Task<bool> ConfirmAsync(string messageResourceKey, string? titleResourceKey = null)
    {
        ConfirmCallCount++;
        LastMessageKey = messageResourceKey;
        return Task.FromResult(NextResult);
    }
}
