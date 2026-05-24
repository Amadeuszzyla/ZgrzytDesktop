using System.Collections.Generic;
using System.Text.Json;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

public static class ApiUserListParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<User> ParseUsers(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<User>>(root.GetRawText(), JsonOptions) ?? [];

        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<User>>(data.GetRawText(), JsonOptions) ?? [];

        return [];
    }
}
