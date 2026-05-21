using System.Collections.Generic;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Helpers;

public sealed class AdminListFilterOption
{
    public UserAdminListFilter Filter { get; init; }

    public AdminListFilterOption(UserAdminListFilter filter)
    {
        Filter = filter;
    }

    public string Label => AppStrings.Get(Filter switch
    {
        UserAdminListFilter.Active => "Admin_Filter_Active",
        UserAdminListFilter.Inactive => "Admin_Filter_Inactive",
        UserAdminListFilter.Banned => "Admin_Filter_Banned",
        _ => "Admin_Filter_All"
    });

    public static IReadOnlyList<AdminListFilterOption> All { get; } =
    [
        new(UserAdminListFilter.All),
        new(UserAdminListFilter.Active),
        new(UserAdminListFilter.Inactive),
        new(UserAdminListFilter.Banned)
    ];
}
