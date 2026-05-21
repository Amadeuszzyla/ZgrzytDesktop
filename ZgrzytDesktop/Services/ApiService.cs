using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Storage;

namespace ZgrzytDesktop.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string CurrentApiBaseUrl { get; private set; } = "http://127.0.0.1:9000/api/";

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        SetBaseAddress(CurrentApiBaseUrl);
    }

    public ApiService(string apiBaseUrl)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        SetBaseAddress(apiBaseUrl);
    }

    public ApiService(AppSettings settings)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        SetBaseAddress(settings.ApiBaseUrl);
    }

    public ApiService(TokenStorage tokenStorage, SettingsService settingsService)
    {
        var settings = settingsService.LoadSync();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        SetBaseAddress(settings.ApiBaseUrl);
        TryLoadStoredToken(tokenStorage);
    }

    // Kompatybilność ze starym kodem:
    // new ApiService(apiBaseUrl, coś)
    public ApiService(string apiBaseUrl, object? tokenOrStorage)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        SetBaseAddress(apiBaseUrl);
        TryApplyTokenFromSecondArgument(tokenOrStorage);
    }

    // Kompatybilność ze starym kodem:
    // new ApiService(settings, coś)
    public ApiService(AppSettings settings, object? tokenOrStorage)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        SetBaseAddress(settings.ApiBaseUrl);
        TryApplyTokenFromSecondArgument(tokenOrStorage);
    }

    public void SetBaseAddress(string apiBaseUrl)
    {
        var normalized = NormalizeApiBaseUrl(apiBaseUrl);

        CurrentApiBaseUrl = normalized;
        _httpClient.BaseAddress = new Uri(normalized);
    }

    public void SetToken(string? accessToken)
    {
        _accessToken = accessToken;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public void SetBearerToken(string? accessToken)
    {
        SetToken(accessToken);
    }

    public void SetAuthorizationToken(string? accessToken)
    {
        SetToken(accessToken);
    }

    public void ClearToken()
    {
        SetToken(null);
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, NormalizeEndpoint(endpoint));
            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                await ThrowApiExceptionAsync(response);

            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Brak połączenia z API: {ex.Message}"
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Przekroczono czas oczekiwania na odpowiedź API: {ex.Message}"
            );
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, NormalizeEndpoint(endpoint))
            {
                Content = JsonContent.Create(data)
            };

            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                await ThrowApiExceptionAsync(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Brak połączenia z API: {ex.Message}"
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Przekroczono czas oczekiwania na odpowiedź API: {ex.Message}"
            );
        }
    }

    public async Task<TResponse?> PostAsync<TResponse>(string endpoint)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, NormalizeEndpoint(endpoint));
            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                await ThrowApiExceptionAsync(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Brak połączenia z API: {ex.Message}"
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Przekroczono czas oczekiwania na odpowiedź API: {ex.Message}"
            );
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Put, NormalizeEndpoint(endpoint))
            {
                Content = JsonContent.Create(data)
            };

            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                await ThrowApiExceptionAsync(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Brak połączenia z API: {ex.Message}"
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Przekroczono czas oczekiwania na odpowiedź API: {ex.Message}"
            );
        }
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, NormalizeEndpoint(endpoint))
            {
                Content = JsonContent.Create(data)
            };

            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                await ThrowApiExceptionAsync(response);

            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Brak połączenia z API: {ex.Message}"
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Przekroczono czas oczekiwania na odpowiedź API: {ex.Message}"
            );
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, NormalizeEndpoint(endpoint));
            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                await ThrowApiExceptionAsync(response);

            return true;
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Brak połączenia z API: {ex.Message}"
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"Przekroczono czas oczekiwania na odpowiedź API: {ex.Message}"
            );
        }
    }

    public async Task<ApiConnectionTestResult> TestConnectionAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, NormalizeEndpoint("user"));
            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return new ApiConnectionTestResult
                {
                    Success = true,
                    Message = "Połączenie z API działa poprawnie."
                };
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new ApiConnectionTestResult
                {
                    Success = true,
                    Message = "API odpowiada, ale token użytkownika jest nieprawidłowy albo wygasł."
                };
            }

            return new ApiConnectionTestResult
            {
                Success = false,
                Message = $"API odpowiedziało błędem: {(int)response.StatusCode} {response.StatusCode}"
            };
        }
        catch
        {
            return new ApiConnectionTestResult
            {
                Success = false,
                Message = "Nie udało się połączyć z API."
            };
        }
    }

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    private void TryLoadStoredToken(TokenStorage tokenStorage)
    {
        try
        {
            var token = tokenStorage.LoadTokenSync();

            if (!string.IsNullOrWhiteSpace(token))
                SetToken(token);
        }
        catch
        {
            // Brak tokena przy starcie nie może zatrzymać aplikacji.
        }
    }

    private void TryApplyTokenFromSecondArgument(object? tokenOrStorage)
    {
        if (tokenOrStorage is null)
            return;

        if (tokenOrStorage is string token)
        {
            SetToken(token);
            return;
        }

        try
        {
            var type = tokenOrStorage.GetType();

            var loadTokenSyncMethod = type.GetMethod(
                "LoadTokenSync",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (loadTokenSyncMethod is not null)
            {
                var result = loadTokenSyncMethod.Invoke(tokenOrStorage, null);

                if (result is string loadedToken && !string.IsNullOrWhiteSpace(loadedToken))
                {
                    SetToken(loadedToken);
                }
            }
        }
        catch
        {
            // Kompatybilność wsteczna — brak tokena nie może zatrzymać aplikacji.
        }
    }

    private static async Task ThrowApiExceptionAsync(HttpResponseMessage response)
    {
        var errorContent = await response.Content.ReadAsStringAsync();

        throw new ApiException(response.StatusCode, errorContent);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        var normalized = endpoint.TrimStart('/');

        if (normalized.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[4..];

        return normalized;
    }

    private static string NormalizeApiBaseUrl(string apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return "http://127.0.0.1:9000/api/";

        var normalized = apiBaseUrl.Trim();

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "http://" + normalized;
        }

        if (!normalized.EndsWith('/'))
            normalized += "/";

        if (!normalized.EndsWith("api/", StringComparison.OrdinalIgnoreCase))
            normalized += "api/";

        return normalized;
    }
}

public class ApiConnectionTestResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}