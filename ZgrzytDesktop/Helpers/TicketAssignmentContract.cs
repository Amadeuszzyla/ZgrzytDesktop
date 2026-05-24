using System;
using System.Text.Json;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Helpers;

public static class TicketAssignmentContract
{
    /// <summary>
    /// Clearing assignment requires sending assigned_it_id: null in JSON.
    /// With <see cref="JsonIgnoreCondition.WhenWritingNull"/> on <see cref="UpdateTicketRequest"/>,
    /// null is omitted and backend clear is not attempted from desktop.
    /// </summary>
    public static bool SupportsClearAssignment { get; } = DetectClearAssignmentSupport();

    private static bool DetectClearAssignmentSupport()
    {
        var json = JsonSerializer.Serialize(new UpdateTicketRequest { AssignedItId = null });

        return json.Contains("\"assigned_it_id\":null", StringComparison.Ordinal);
    }
}
