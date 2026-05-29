using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Services;

public class UserConfirmationServiceTests
{
    public UserConfirmationServiceTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task ConfirmAsync_WithoutOwner_ReturnsFalse()
    {
        var service = new UserConfirmationService();

        var confirmed = await service.ConfirmAsync("Confirm_DeleteTicket");

        Assert.False(confirmed);
    }
}
