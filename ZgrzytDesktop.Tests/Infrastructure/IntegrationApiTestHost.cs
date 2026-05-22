using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.Storage;

namespace ZgrzytDesktop.Tests.Infrastructure;

public sealed class IntegrationApiTestHost : IAsyncLifetime, IDisposable
{
    private readonly IntegrationTestSettings? _settings;
    private string _tempDirectory = string.Empty;
    private bool _disposed;

    public IntegrationApiTestHost()
    {
        IsConfigured = IntegrationTestEnvironment.TryGetSettings(out _settings);
    }

    public static async Task<IntegrationApiTestHost> CreateConnectedAsync()
    {
        var host = new IntegrationApiTestHost();
        await host.InitializeAsync();

        if (!host.IsConfigured || host.User is null)
            throw new InvalidOperationException("Live API integration host is not configured.");

        return host;
    }

    public bool IsConfigured { get; }

    public bool IsStaffRole =>
        User is not null &&
        (string.Equals(User.Role, "admin", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(User.Role, "it", StringComparison.OrdinalIgnoreCase));

    public ApiService? Api { get; private set; }

    public AuthService? Auth { get; private set; }

    public TicketService? Tickets { get; private set; }

    public UserAdminService? UserAdmin { get; private set; }

    public User? User { get; private set; }

    internal async Task<string?> GetStoredAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_tempDirectory))
            return null;

        return await new TokenStorage(_tempDirectory).GetTokenAsync();
    }

    public async Task InitializeAsync()
    {
        if (!IsConfigured || _settings is null)
            return;

        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "ZgrzytDesktop.Integration",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };

        Api = new ApiService(handler, _settings.ApiBaseUrl)
        {
            TryRefreshSessionAsync = () => Task.FromResult(false)
        };

        Auth = new AuthService(Api, new TokenStorage(_tempDirectory));
        Tickets = new TicketService(Api);
        UserAdmin = new UserAdminService(Api);

        User = await RetryAsync(() => Auth.LoginAsync(_settings.Login, _settings.Password));

        if (User is null)
            throw new InvalidOperationException("Live API login returned no user profile.");
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignoruj problemy sprzątania katalogu tymczasowego po teście integracyjnym.
            }
        }
    }

    private static async Task<T?> RetryAsync<T>(Func<Task<T?>> action, int attempts = 3)
        where T : class
    {
        Exception? last = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < attempts && IsTransient(ex))
            {
                last = ex;
                await Task.Delay(TimeSpan.FromSeconds(5 * attempt));
            }
        }

        if (last is not null)
            throw last;

        return await action();
    }

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or ApiException
        {
            StatusCode: HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
        };
}
