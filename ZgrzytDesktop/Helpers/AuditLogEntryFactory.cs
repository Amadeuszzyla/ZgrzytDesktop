using System;
using System.Text.Json;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

public static class AuditLogEntryFactory
{
    public static AuditLogEntry Create(
        string actionKey,
        string userLogin,
        int? ticketId,
        string? detailsKey,
        params object?[] formatParameters)
    {
        return new AuditLogEntry
        {
            Timestamp = DateTime.Now,
            UserLogin = userLogin,
            Action = actionKey,
            TicketId = ticketId,
            DetailsKey = detailsKey,
            ParametersJson = formatParameters.Length > 0
                ? JsonSerializer.Serialize(formatParameters)
                : null
        };
    }
}
