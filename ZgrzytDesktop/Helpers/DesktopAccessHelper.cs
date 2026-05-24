using System;
using ZgrzytDesktop.Constants;

namespace ZgrzytDesktop.Helpers;

public static class DesktopAccessHelper
{
    public static bool IsDesktopAccessAllowed(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        return AppRoleHelper.IsDesktopStaff(role);
    }
}
