using System;
using ZgrzytDesktop.Constants;

namespace ZgrzytDesktop.Helpers;

public static class DesktopAccessHelper
{
    public static bool IsDesktopAccessAllowed(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        return string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, AppRoles.It, StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, "administrator", StringComparison.OrdinalIgnoreCase);
    }
}
