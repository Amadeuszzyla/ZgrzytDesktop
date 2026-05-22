using System;
using System.Net;
using System.Threading.Tasks;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.ViewModels;

public partial class DashboardViewModel
{
    private async Task LogAuditAsync(string action, int? ticketId, string description)
    {
        if (ApiErrorSanitizer.IsHtmlResponse(description))
        {
            description = ApiErrorSanitizer.SanitizeForDisplay(
                description,
                System.Net.HttpStatusCode.InternalServerError);
        }

        await _auditLogService.AddAsync(new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            UserLogin = CurrentUser.Login,
            Action = action,
            TicketId = ticketId,
            Description = description
        });
    }

    private async Task RefreshTicketAuditLogAsync(int ticketId)
    {
        var entries = await _auditLogService.LoadForTicketAsync(ticketId);

        TicketAuditLogEntries.Clear();

        foreach (var entry in entries)
            TicketAuditLogEntries.Add(entry);

        OnPropertyChanged(nameof(HasNoTicketAuditLogEntries));
    }

}
