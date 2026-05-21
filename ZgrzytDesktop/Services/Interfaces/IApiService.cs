using System.Threading.Tasks;

namespace ZgrzytDesktop.Services.Interfaces;

public interface IApiService
{
    Task<T?> GetAsync<T>(string endpoint);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);

    Task<TResponse?> PostAsync<TResponse>(string endpoint);

    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);

    Task<bool> DeleteAsync(string endpoint);

    void SetToken(string? accessToken);

    void ClearToken();
}
