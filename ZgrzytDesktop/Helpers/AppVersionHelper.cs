using System;
using System.Reflection;

namespace ZgrzytDesktop.Helpers;

public static class AppVersionHelper
{
    private static readonly Lazy<string> DisplayVersionLazy = new(() =>
        FormatDisplayVersion(ReadInformationalVersion(), ReadAssemblyVersion()));

    public static string DisplayVersion => DisplayVersionLazy.Value;

    public static string FormatDisplayVersion(string? informationalVersion, string? assemblyVersion = null)
    {
        var raw = !string.IsNullOrWhiteSpace(informationalVersion)
            ? informationalVersion.Trim()
            : !string.IsNullOrWhiteSpace(assemblyVersion)
                ? assemblyVersion.Trim()
                : "0.0.0";

        var plusIndex = raw.IndexOf('+', StringComparison.Ordinal);
        if (plusIndex >= 0)
            raw = raw[..plusIndex];

        return raw.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? raw
            : "v" + raw;
    }

    private static string? ReadInformationalVersion() =>
        typeof(AppVersionHelper).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

    private static string? ReadAssemblyVersion() =>
        typeof(AppVersionHelper).Assembly.GetName().Version?.ToString(3);
}
