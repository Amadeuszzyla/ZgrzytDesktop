using System.Threading.Tasks;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private Task<bool> ConfirmRiskyActionAsync(string messageKey, string? titleKey = null) =>
        ConfirmationServiceHolder.Instance.ConfirmAsync(messageKey, titleKey);
}
