using System.Threading.Tasks;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private async Task LogAuditAsync(
        string actionKey,
        int? ticketId,
        string? detailsKey,
        object?[]? formatParameters = null)
    {
        await _auditLogService.AddAsync(
            AuditLogEntryFactory.Create(
                actionKey,
                CurrentUser.Login,
                ticketId,
                detailsKey,
                formatParameters ?? []));
    }
}
