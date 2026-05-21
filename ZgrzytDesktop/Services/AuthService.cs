using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

public class AuthService : IAuthService
{
    private readonly ApiService _apiService;
    private readonly ITokenStorage _tokenStorage;

    public AuthService(ApiService apiService, ITokenStorage tokenStorage)
    {
        _apiService = apiService;
        _tokenStorage = tokenStorage;
    }

    public async Task<User?> LoginAsync(string login, string password)
    {
        var request = new LoginRequest
        {
            Login = login,
            Password = password
        };

        var response = await _apiService.PostAsync<LoginRequest, LoginResponse>("login", request);

        if (response is null || string.IsNullOrWhiteSpace(response.AccessToken))
            return null;

        await _tokenStorage.SaveTokenAsync(response.AccessToken);
        _apiService.SetToken(response.AccessToken);

        var user = await _apiService.GetAsync<User>("user");

        return user;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        return await _apiService.GetAsync<User>("user");
    }

    public async Task<bool> RequestAccountAsync(RequestAccountRequest request)
    {
        await _apiService.PostAsync<RequestAccountRequest, object?>("request-account", request);
        return true;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        var response = await _apiService.PostAsync<object, LoginResponse>("refresh", new { });

        if (response is null || string.IsNullOrWhiteSpace(response.AccessToken))
            return false;

        await _tokenStorage.SaveTokenAsync(response.AccessToken);
        _apiService.SetToken(response.AccessToken);

        return true;
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _apiService.PostAsync<object, LogoutResponse>("logout", new { });
        }
        finally
        {
            await _tokenStorage.ClearTokenAsync();
            _apiService.ClearToken();
        }
    }
}

public class LoginRequest
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public class LogoutResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}