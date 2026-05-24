using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Services;

public static class ConfirmationServiceHolder
{
    public static IUserConfirmationService Instance { get; set; } = new UserConfirmationService();
}
