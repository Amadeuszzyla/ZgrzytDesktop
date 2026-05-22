using System.Threading.Tasks;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services.Interfaces;

public interface ILocalUserCacheService
{
    Task SaveUserAsync(User user);

    Task<User?> LoadUserAsync();

    Task ClearAsync();
}
