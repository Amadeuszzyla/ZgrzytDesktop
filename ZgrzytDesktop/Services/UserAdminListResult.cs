using System.Collections.Generic;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Services;

public sealed class UserAdminListResult
{
    public IReadOnlyList<User> Users { get; init; } = [];

    public bool UsedLocalFilterFallback { get; init; }

    public UserAdminListInfoKind InfoKind { get; init; }

    public string? InformationalMessage { get; init; }
}
