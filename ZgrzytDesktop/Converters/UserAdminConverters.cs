using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Converters;

public sealed class InvertBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class UserCanActivateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is User user && !user.Active && !user.Ban;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class UserCanUnbanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is User user && user.Ban;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class AdminUserMetaConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString() ?? string.Empty;

        return (parameter as string)?.ToLowerInvariant() switch
        {
            "login" => AppStrings.GetFormat("Admin_UserLoginLine", text),
            "email" => AppStrings.GetFormat("Admin_UserEmailLine", text),
            "role" => AppStrings.GetFormat("Admin_UserRoleLine", text),
            _ => text
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
