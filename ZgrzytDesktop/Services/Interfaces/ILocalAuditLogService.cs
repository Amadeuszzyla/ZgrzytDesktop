using System.Collections.Generic;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services.Interfaces;

public interface ILocalAuditLogService
{
    Task AddAsync(AuditLogEntry entry);

    Task<List<AuditLogEntry>> LoadAsync();

    Task<List<AuditLogEntry>> LoadForTicketAsync(int ticketId);

    Task ClearAsync();
}
