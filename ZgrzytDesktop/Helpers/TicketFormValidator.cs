using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class TicketFormValidator
{
    public static string? ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return AppStrings.Get("Tickets_ValidationTitle");

        if (title.Trim().Length > ValidationLimits.TicketTitleMaxLength)
            return AppStrings.Get("Validation_TicketTitleTooLong");

        return null;
    }

    public static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return AppStrings.Get("Tickets_ValidationDescription");

        if (description.Trim().Length > ValidationLimits.TicketDescriptionMaxLength)
            return AppStrings.Get("Validation_TicketDescriptionTooLong");

        return null;
    }
}
