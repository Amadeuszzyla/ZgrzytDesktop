using System;
using System.Collections.Generic;
using System.Linq;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Helpers;

public static class TicketAssignableStaffFilter
{
    public static bool IsAssignableStaff(User user, bool fromActiveUsersList = false) =>
        (fromActiveUsersList || user.Active) &&
        !user.Ban &&
        AppRoleHelper.IsAssignableStaffRole(user.Role);

    public static List<User> FilterAssignableStaff(
        IEnumerable<User> users,
        bool fromActiveUsersList = false) =>
        users
            .Where(user => IsAssignableStaff(user, fromActiveUsersList))
            .OrderBy(user => user.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.Login, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
