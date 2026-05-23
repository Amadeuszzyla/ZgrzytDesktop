using System;
using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Helpers;

namespace ZgrzytDesktop.Converters;

public sealed class TicketStatusBadgeClassConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(status))
            return "ticket-badge ticket-badge-status-default";

        var apiStatus = StatusDisplayHelper.ToApiStatus(status);

        if (string.Equals(apiStatus, TicketStatuses.Nowe, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-status-new";

        if (string.Equals(apiStatus, TicketStatuses.WTrakcie, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-status-progress";

        if (string.Equals(apiStatus, TicketStatuses.Zamkniete, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-status-closed";

        return "ticket-badge ticket-badge-status-default";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class TicketPriorityBadgeClassConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var priority = value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(priority))
            return "ticket-badge ticket-badge-priority-default";

        var apiPriority = PriorityDisplayHelper.ToApiPriority(priority);

        if (string.Equals(apiPriority, TicketPriorities.High, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-priority-high";

        if (string.Equals(apiPriority, TicketPriorities.Medium, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-priority-medium";

        if (string.Equals(apiPriority, TicketPriorities.Low, StringComparison.OrdinalIgnoreCase))
            return "ticket-badge ticket-badge-priority-low";

        return "ticket-badge ticket-badge-priority-default";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class CollectionHasItemsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items)
            return false;

        foreach (var _ in items)
            return true;

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class CollectionIsEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items)
            return true;

        foreach (var _ in items)
            return false;

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class ObjectIsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNotNull = value is not null;
        var invert = parameter is string s &&
                     s.Equals("invert", StringComparison.OrdinalIgnoreCase);
        return invert ? !isNotNull : isNotNull;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class CategoryDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string category ? TicketCategoryHelper.ToDisplayCategory(category) : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
