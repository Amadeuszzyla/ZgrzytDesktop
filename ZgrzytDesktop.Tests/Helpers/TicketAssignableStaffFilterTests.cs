using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Tests.Helpers;

public class TicketAssignableStaffFilterTests
{
    [Fact]
    public void FilterAssignableStaff_IncludesStaffFromActiveUsersList_WhenActiveFieldMissing()
    {
        var users = new[]
        {
            new User { Id = 1, Name = "IT", Role = AppRoles.It, Active = false },
            new User { Id = 2, Name = "Admin", Role = "administrator", Active = false }
        };

        var assignable = TicketAssignableStaffFilter.FilterAssignableStaff(users, fromActiveUsersList: true);

        Assert.Equal(2, assignable.Count);
    }

    [Fact]
    public void FilterAssignableStaff_IncludesAdministratorRole()
    {
        var users = new[]
        {
            new User { Id = 1, Name = "Admin", Role = "administrator", Active = true, Ban = false }
        };

        var assignable = TicketAssignableStaffFilter.FilterAssignableStaff(users);

        Assert.Single(assignable);
        Assert.Equal(1, assignable[0].Id);
    }

    [Fact]
    public void AssignSelectedUser_ShouldNotSendRegularUserId()
    {
        var users = new[]
        {
            new User { Id = 1, Name = "Staff IT", Role = AppRoles.It, Active = true, Ban = false },
            new User { Id = 2, Name = "Staff Admin", Role = AppRoles.Admin, Active = true, Ban = false },
            new User { Id = 3, Name = "Regular", Role = AppRoles.User, Active = true, Ban = false },
            new User { Id = 4, Name = "Inactive IT", Role = AppRoles.It, Active = false, Ban = false },
            new User { Id = 5, Name = "Banned IT", Role = AppRoles.It, Active = true, Ban = true }
        };

        var assignable = TicketAssignableStaffFilter.FilterAssignableStaff(users);

        Assert.Equal(2, assignable.Count);
        Assert.All(assignable, user =>
            Assert.True(
                string.Equals(user.Role, AppRoles.It, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(assignable, user => user.Id == 3);
        Assert.DoesNotContain(assignable, user => user.Id == 4);
        Assert.DoesNotContain(assignable, user => user.Id == 5);
    }
}
