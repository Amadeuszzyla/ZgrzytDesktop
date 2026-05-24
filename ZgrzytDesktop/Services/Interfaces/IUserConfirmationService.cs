using System.Threading.Tasks;

namespace ZgrzytDesktop.Services.Interfaces;

public interface IUserConfirmationService
{
    Task<bool> ConfirmAsync(string messageResourceKey, string? titleResourceKey = null);
}
