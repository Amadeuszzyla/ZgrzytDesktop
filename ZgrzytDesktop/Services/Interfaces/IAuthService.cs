using System.Threading.Tasks;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services.Interfaces;

public interface IAuthService
{
    Task<User?> LoginAsync(string login, string password);

    Task<User?> GetCurrentUserAsync();

    Task<bool> RequestAccountAsync(RequestAccountRequest request);

    Task<bool> RefreshTokenAsync();

    Task LogoutAsync();
}
