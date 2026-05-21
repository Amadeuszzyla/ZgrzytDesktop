using System;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace ZgrzytDesktop.Services;

public static class ApiErrorSanitizer
{
    public static bool IsHtmlResponse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var trimmed = content.TrimStart();

        if (trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return trimmed.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("<head", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("<body", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("<title>Laravel</title>", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsHtmlContentType(string? mediaType)
    {
        return !string.IsNullOrWhiteSpace(mediaType) &&
               mediaType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    public static string SanitizeApiErrorMessage(string? content, HttpStatusCode statusCode)
    {
        if (IsHtmlResponse(content))
        {
            return "Serwer zwrócił stronę błędu zamiast danych API. Sprawdź endpoint lub uprawnienia.";
        }

        if (statusCode == HttpStatusCode.UnprocessableEntity &&
            TryExtractValidationMessage(content, out var validationMessage))
        {
            return validationMessage;
        }

        return statusCode switch
        {
            HttpStatusCode.Unauthorized =>
                "Sesja wygasła albo użytkownik nie jest zalogowany.",
            HttpStatusCode.Forbidden =>
                "Brak uprawnień do wykonania tej operacji.",
            HttpStatusCode.NotFound =>
                "Nie znaleziono zasobu lub endpoint nie istnieje.",
            HttpStatusCode.Conflict =>
                "Operacja jest sprzeczna z aktualnym stanem danych.",
            HttpStatusCode.ServiceUnavailable =>
                "Brak połączenia z API. Sprawdź, czy backend działa.",
            HttpStatusCode.InternalServerError =>
                "Błąd serwera. Spróbuj ponownie później.",
            _ => TruncatePlainText(content, 240)
        };
    }

    public static string SanitizeForDisplay(string? content, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return SanitizeApiErrorMessage(content, statusCode);
    }

    private static bool TryExtractValidationMessage(string? content, out string message)
    {
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(content))
            return false;

        try
        {
            using var document = JsonDocument.Parse(content);

            if (!document.RootElement.TryGetProperty("errors", out var errors) ||
                errors.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var parts = errors.EnumerateObject()
                .SelectMany(property =>
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        return property.Value.EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(text => !string.IsNullOrWhiteSpace(text))
                            .Select(text => $"{property.Name}: {text}");
                    }

                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var text = property.Value.GetString();

                        if (!string.IsNullOrWhiteSpace(text))
                            return new[] { $"{property.Name}: {text}" };
                    }

                    return Enumerable.Empty<string>();
                })
                .Take(4)
                .ToList();

            if (parts.Count == 0)
                return false;

            message = string.Join(" ", parts);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string TruncatePlainText(string? content, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Wystąpił nieoczekiwany błąd podczas komunikacji z API.";

        var trimmed = content.Trim();

        if (trimmed.Length <= maxLength)
            return trimmed;

        return trimmed[..maxLength] + "…";
    }
}
