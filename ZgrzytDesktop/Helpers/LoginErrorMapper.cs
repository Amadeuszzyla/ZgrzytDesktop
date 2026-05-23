using System;
using System.Net;
using System.Text.Json;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class LoginErrorMapper
{
    public static string GetErrorMessage(Exception exception)
    {
        if (exception is ApiException apiException)
            return MapApiException(apiException);

        if (exception is JsonException)
            return AppStrings.Get("Login_InvalidApiResponse");

        return AppStrings.Get("Login_UnexpectedError");
    }

    private static string MapApiException(ApiException exception)
    {
        if (IsConnectionFailure(exception))
            return AppStrings.Get("Login_ConnectionError");

        return exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => AppStrings.Get("Login_InvalidCredentials"),
            HttpStatusCode.Forbidden => AppStrings.Get("Login_NoDesktopAccess"),
            HttpStatusCode.UnprocessableEntity => AppStrings.Get("Login_CheckCredentials"),
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout => AppStrings.Get("Login_ApiServerError"),
            _ when IsServerErrorStatus(exception.StatusCode) => AppStrings.Get("Login_ApiServerError"),
            _ => AppStrings.Get("Login_UnexpectedError")
        };
    }

    private static bool IsConnectionFailure(ApiException exception)
    {
        if (exception.StatusCode != HttpStatusCode.ServiceUnavailable)
            return false;

        if (exception.ResponseContent is not null)
            return false;

        var offlinePrefix = AppStrings.Get("Api_ServiceUnavailable");
        return exception.Message.StartsWith(offlinePrefix, StringComparison.Ordinal);
    }

    private static bool IsServerErrorStatus(HttpStatusCode statusCode) =>
        (int)statusCode >= 500 && (int)statusCode <= 599;
}
