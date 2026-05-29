using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Security;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string CurrentApiBaseUrl { get; private set; } = ApiDefaults.ProductionApiBaseUrl;

    public Func<Task<bool>>? TryRefreshSessionAsync { get; set; }

    public Func<Task>? OnSessionExpiredAsync { get; set; }

    public ApiService(HttpMessageHandler handler, string apiBaseUrl = ApiDefaults.ProductionApiBaseUrl)
    {
        _httpClient = new HttpClient(handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        SetBaseAddress(apiBaseUrl);
    }

    public ApiService(ITokenStorage tokenStorage, ISettingsService settingsService)
    {
        var settings = settingsService.LoadSync();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        SetBaseAddress(settings.ApiBaseUrl);
        TryLoadStoredToken(tokenStorage);
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

    public void ClearToken()
    {
        SetToken(null);
    }

    public Task<T?> GetAsync<T>(string endpoint) =>
        SendAsync<T>(HttpMethod.Get, endpoint, null);

    public Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data) =>
        SendAsync<TResponse>(HttpMethod.Post, endpoint, data);

    public Task<TResponse?> PostAsync<TResponse>(string endpoint) =>
        SendAsync<TResponse>(HttpMethod.Post, endpoint, null);

    public Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data) =>
        SendAsync<TResponse>(HttpMethod.Put, endpoint, data);

    public async Task<bool> DeleteAsync(string endpoint)
    {
        await SendAsync<object>(HttpMethod.Delete, endpoint, null);
        return true;
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string endpoint, object? body)
    {
        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var request = new HttpRequestMessage(method, NormalizeEndpoint(endpoint));

                if (body is not null)
                    request.Content = JsonContent.Create(body);

                AddAuthorizationHeader(request);

                using var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized &&
                    attempt == 0 &&
                    !IsAuthEndpoint(endpoint))
                {
                    if (await TryRefreshSessionOnceAsync())
                        continue;

                    await HandleSessionExpiredAsync();
                    await ThrowApiExceptionAsync(response);
                }

                if (!response.IsSuccessStatusCode)
                    await ThrowApiExceptionAsync(response);

                if (method == HttpMethod.Delete)
                    return default;

                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }

            return default;
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"{AppStrings.Get("Api_ServiceUnavailable")} ({ex.Message})"
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                $"{AppStrings.Get("Api_ServiceUnavailable")} ({ex.Message})"
            );
        }
    }

    private async Task<bool> TryRefreshSessionOnceAsync()
    {
        if (TryRefreshSessionAsync is null)
            return false;

        try
        {
            return await TryRefreshSessionAsync();
        }
        catch
        {
            return false;
        }
    }

    private async Task HandleSessionExpiredAsync()
    {
        if (OnSessionExpiredAsync is null)
            return;

        try
        {
            await OnSessionExpiredAsync();
        }
        catch
        {
            // Wylogowanie nie może zablokować propagacji błędu API.
        }
    }

    private static bool IsAuthEndpoint(string endpoint)
    {
        var normalized = endpoint.TrimStart('/').Split('?')[0];

        return normalized.Equals("login", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("refresh", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("logout", StringComparison.OrdinalIgnoreCase);
    }

    private void TryLoadStoredToken(ITokenStorage tokenStorage)
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

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    private static async Task ThrowApiExceptionAsync(HttpResponseMessage response)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        var mediaType = response.Content.Headers.ContentType?.MediaType;

        if (ApiErrorSanitizer.IsHtmlContentType(mediaType) ||
            ApiErrorSanitizer.IsHtmlResponse(errorContent))
        {
            errorContent = string.Empty;
        }

        var message = ApiErrorSanitizer.SanitizeApiErrorMessage(errorContent, response.StatusCode);

        throw new ApiException(response.StatusCode, message, errorContent);
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
        if (ApiDefaults.ShouldMigrateToProduction(apiBaseUrl))
            return ApiDefaults.ProductionApiBaseUrl;

        var normalized = apiBaseUrl.Trim();

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "https://" + normalized;
        }

        if (!normalized.EndsWith('/'))
            normalized += "/";

        if (!normalized.EndsWith("api/", StringComparison.OrdinalIgnoreCase))
            normalized += "api/";

        normalized = normalized.Replace("api/api/", "api/", StringComparison.OrdinalIgnoreCase);

        return ApiUrlSecurityHelper.EnsureSecureApiBaseUrl(normalized);
    }
}