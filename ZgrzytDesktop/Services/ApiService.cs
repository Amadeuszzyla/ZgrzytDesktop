using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Storage;

namespace ZgrzytDesktop.Services;

public class ApiService
{
    private HttpClient _httpClient;

    private readonly TokenStorage _tokenStorage;
    private readonly SettingsService _settingsService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string CurrentApiBaseUrl => _httpClient.BaseAddress?.ToString() ?? string.Empty;

    public ApiService(TokenStorage tokenStorage, SettingsService settingsService)
    {
        _tokenStorage = tokenStorage;
        _settingsService = settingsService;

        var settings = _settingsService.LoadSync();
        var normalizedUrl = _settingsService.NormalizeApiBaseUrl(settings.ApiBaseUrl);

        _httpClient = CreateHttpClient(normalizedUrl);
    }

    public void SetBaseAddress(string apiBaseUrl)
    {
        var normalizedUrl = _settingsService.NormalizeApiBaseUrl(apiBaseUrl);

        var oldClient = _httpClient;
        _httpClient = CreateHttpClient(normalizedUrl);

        oldClient.Dispose();
    }

    private static HttpClient CreateHttpClient(string apiBaseUrl)
    {
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri))
        {
            uri = new Uri("http://127.0.0.1:9000/api/");
        }

        return new HttpClient
        {
            BaseAddress = uri,
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        await AddAuthorizationHeaderAsync();

        try
        {
            var response = await _httpClient.GetAsync("user");

            if (response.IsSuccessStatusCode)
            {
                return (true, "Połączenie z API działa poprawnie.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                return (true, "API odpowiada. Endpoint wymaga autoryzacji, więc połączenie jest poprawne.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, "Serwer odpowiada, ale nie znaleziono endpointu /api/user. Sprawdź adres API.");
            }

            return (
                false,
                $"Serwer odpowiada, ale zwrócił kod {(int)response.StatusCode} {response.ReasonPhrase}."
            );
        }
        catch (HttpRequestException ex)
        {
            return (
                false,
                $"Brak połączenia z API. Sprawdź adres i uruchomienie backendu. Szczegóły: {ex.Message}"
            );
        }
        catch (TaskCanceledException)
        {
            return (false, "Przekroczono czas oczekiwania na odpowiedź API.");
        }
        catch (Exception ex)
        {
            return (false, $"Nieoczekiwany błąd testu połączenia: {ex.Message}");
        }
    }

    private async Task AddAuthorizationHeaderAsync()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var token = await _tokenStorage.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        await AddAuthorizationHeaderAsync();

        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            await EnsureSuccessAsync(response);

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                "Brak połączenia z API. Sprawdź, czy backend Laravel jest uruchomiony.",
                ex.Message
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                "Przekroczono czas oczekiwania na odpowiedź API.",
                ex.Message
            );
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        await AddAuthorizationHeaderAsync();

        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            await EnsureSuccessAsync(response);

            var responseJson = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                "Brak połączenia z API. Sprawdź, czy backend Laravel jest uruchomiony.",
                ex.Message
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                "Przekroczono czas oczekiwania na odpowiedź API.",
                ex.Message
            );
        }
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        await AddAuthorizationHeaderAsync();

        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync(endpoint, content);
            await EnsureSuccessAsync(response);

            var responseJson = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                "Brak połączenia z API. Sprawdź, czy backend Laravel jest uruchomiony.",
                ex.Message
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                "Przekroczono czas oczekiwania na odpowiedź API.",
                ex.Message
            );
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        await AddAuthorizationHeaderAsync();

        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            await EnsureSuccessAsync(response);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                "Brak połączenia z API. Sprawdź, czy backend Laravel jest uruchomiony.",
                ex.Message
            );
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException(
                HttpStatusCode.ServiceUnavailable,
                "Przekroczono czas oczekiwania na odpowiedź API.",
                ex.Message
            );
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorContent = await response.Content.ReadAsStringAsync();

        var message = response.StatusCode switch
        {
            HttpStatusCode.BadRequest =>
                "Nieprawidłowe żądanie wysłane do API.",

            HttpStatusCode.Unauthorized =>
                "Nieprawidłowy login, hasło albo wygasła sesja.",

            HttpStatusCode.Forbidden =>
                "Brak uprawnień do wykonania tej operacji.",

            HttpStatusCode.NotFound =>
                "Nie znaleziono wymaganego zasobu w API.",

            HttpStatusCode.UnprocessableEntity =>
                "Dane formularza są niepoprawne. Sprawdź wymagane pola.",

            HttpStatusCode.InternalServerError =>
                "Wystąpił błąd po stronie serwera.",

            _ =>
                $"API zwróciło błąd: {(int)response.StatusCode} {response.ReasonPhrase}"
        };

        throw new ApiException(response.StatusCode, message, errorContent);
    }
}