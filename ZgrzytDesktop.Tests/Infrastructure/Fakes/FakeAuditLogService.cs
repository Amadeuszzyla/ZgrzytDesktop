using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services.Interfaces;

namespace ZgrzytDesktop.Tests.Infrastructure.Fakes;

public sealed class FakeAuditLogService : ILocalAuditLogService
{
    public List<AuditLogEntry> Entries { get; } = new();

    public Task AddAsync(AuditLogEntry entry)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<List<AuditLogEntry>> LoadAsync() => Task.FromResult(new List<AuditLogEntry>(Entries));

    public Task<List<AuditLogEntry>> LoadForTicketAsync(int ticketId) =>
        Task.FromResult(Entries.Where(entry => entry.TicketId == ticketId).ToList());

    public Task ClearAsync()
    {
        Entries.Clear();
        return Task.CompletedTask;
    }
}
