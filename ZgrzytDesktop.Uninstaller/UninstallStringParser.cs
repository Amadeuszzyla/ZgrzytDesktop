namespace ZgrzytDesktop.Uninstaller;

public static class UninstallStringParser
{
    public static bool TryParseExecutablePath(string? uninstallString, out string? executablePath)
    {
        executablePath = null;
        if (string.IsNullOrWhiteSpace(uninstallString))
            return false;

        var trimmed = uninstallString.Trim();

        if (trimmed.StartsWith('"'))
        {
            var endQuote = trimmed.IndexOf('"', 1);
            if (endQuote <= 1)
                return false;

            executablePath = trimmed[1..endQuote];
            return !string.IsNullOrWhiteSpace(executablePath);
        }

        var spaceIndex = trimmed.IndexOf(' ');
        executablePath = spaceIndex >= 0 ? trimmed[..spaceIndex] : trimmed;
        return !string.IsNullOrWhiteSpace(executablePath);
    }
}
