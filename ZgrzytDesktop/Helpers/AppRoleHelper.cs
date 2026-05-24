using System;
using ZgrzytDesktop.Constants;

namespace ZgrzytDesktop.Helpers;

public static class AppRoleHelper
{
    public static string? NormalizeRole(string? role) =>
        string.IsNullOrWhiteSpace(role) ? role : role.Trim();

    public static bool IsAdmin(string? role)
    {
        role = NormalizeRole(role);
        return string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "administrator", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsIt(string? role) =>
        string.Equals(NormalizeRole(role), AppRoles.It, StringComparison.OrdinalIgnoreCase);

    public static bool IsAssignableStaffRole(string? role) =>
        IsAdmin(role) || IsIt(role);

    public static bool IsDesktopStaff(string? role) =>
        IsAssignableStaffRole(role);
}
