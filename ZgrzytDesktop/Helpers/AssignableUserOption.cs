using System;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public sealed class AssignableUserOption
{
    public const int UnassignedSentinelId = 0;

    public AssignableUserOption(int? userId, string label)
    {
        UserId = userId;
        Label = label;
    }

    public int? UserId { get; }

    public string Label { get; }

    public bool IsUnassigned => UserId is null or UnassignedSentinelId;

    public static AssignableUserOption CreateUnassigned() =>
        new(null, AppStrings.Get("Ticket_Unassigned"));

    public static AssignableUserOption FromUser(User user) =>
        new(user.Id, FormatUserLabel(user));

    public static string FormatUserLabel(User user)
    {
        var name = string.IsNullOrWhiteSpace(user.Name) ? user.Login : user.Name;
        var roleLabel = FormatRoleLabel(user.Role);
        return string.IsNullOrWhiteSpace(roleLabel) ? name : $"{name} ({roleLabel})";
    }

    public static string FormatRoleLabel(string role)
    {
        if (AppRoleHelper.IsAdmin(role))
            return AppStrings.Get("RegisterUser_RoleAdmin");

        if (AppRoleHelper.IsIt(role))
            return AppStrings.Get("RegisterUser_RoleIt");

        return string.Empty;
    }

    public override string ToString() => Label;

    public override bool Equals(object? obj) =>
        obj is AssignableUserOption other &&
        UserId == other.UserId &&
        IsUnassigned == other.IsUnassigned;

    public override int GetHashCode() => HashCode.Combine(UserId, IsUnassigned);
}
