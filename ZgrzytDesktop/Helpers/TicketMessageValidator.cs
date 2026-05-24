using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class TicketMessageValidator
{
    public static string? ValidateBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return AppStrings.Get("Details_EmptyMessage");

        if (body.Trim().Length > ValidationLimits.MessageBodyMaxLength)
            return AppStrings.Get("Validation_MessageBodyTooLong");

        return null;
    }
}
