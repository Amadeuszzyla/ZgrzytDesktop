using System.Threading.Tasks;
using Avalonia.Controls;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services.Interfaces;
using ZgrzytDesktop.Views;

namespace ZgrzytDesktop.Services;

public sealed class UserConfirmationService : IUserConfirmationService
{
    private Window? _owner;

    public void SetDialogOwner(Window owner) => _owner = owner;

    public Task<bool> ConfirmAsync(string messageResourceKey, string? titleResourceKey = null)
    {
        var message = AppStrings.Get(messageResourceKey);
        var title = AppStrings.Get(titleResourceKey ?? "Confirm_Title");

        if (_owner is null)
            return Task.FromResult(true);

        return ConfirmDialog.ShowAsync(_owner, title, message);
    }
}
