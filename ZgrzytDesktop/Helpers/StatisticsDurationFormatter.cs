using System;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class StatisticsDurationFormatter
{
    public static string Format(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return AppStrings.GetFormat(
                "Stats_Duration_DaysHours",
                (int)duration.TotalDays,
                duration.Hours);
        }

        if (duration.TotalHours >= 1)
        {
            return AppStrings.GetFormat(
                "Stats_Duration_HoursMinutes",
                (int)duration.TotalHours,
                duration.Minutes);
        }

        var minutes = Math.Max(1, (int)Math.Round(duration.TotalMinutes));
        return AppStrings.GetFormat("Stats_Duration_Minutes", minutes);
    }
}
