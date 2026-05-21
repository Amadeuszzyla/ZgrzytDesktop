using System.Threading.Tasks;

namespace ZgrzytDesktop.Services.Interfaces;

public interface ITokenStorage
{
    Task SaveTokenAsync(string token);

    string? LoadTokenSync();

    Task<string?> GetTokenAsync();

    Task ClearTokenAsync();
}
