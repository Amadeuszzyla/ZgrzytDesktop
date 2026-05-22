using System.Collections.Generic;
using System.Threading.Tasks;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services.Interfaces;

public interface ILocalTicketCacheService
{
    Task SaveTicketsAsync(IEnumerable<Ticket> tickets);

    Task<List<Ticket>> LoadTicketsAsync();

    Task ClearAsync();
}
