using System.Collections.Generic;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public sealed class RegisterUserRoleOption
{
    public string Role { get; init; }

    public RegisterUserRoleOption(string role)
    {
        Role = role;
    }

    public string Label => AppStrings.Get(Role switch
    {
        AppRoles.It => "RegisterUser_RoleIt",
        AppRoles.Admin => "RegisterUser_RoleAdmin",
        _ => "RegisterUser_RoleUser"
    });

    public static IReadOnlyList<RegisterUserRoleOption> All { get; } =
    [
        new(AppRoles.User),
        new(AppRoles.It),
        new(AppRoles.Admin)
    ];

    public static bool IsAllowedRole(string? role) =>
        role is AppRoles.User or AppRoles.It or AppRoles.Admin;
}
