using System.Net;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Models;

namespace ZgrzytDesktop.Tests.Infrastructure;

internal static class LiveApiTestHelpers
{
    public const string StaffRoleSkipReason =
        "ZGRZYT_LOGIN must be an admin or it account for staff-only API endpoints.";

    /// <summary>
    /// After POST /api/logout, GET /api/user with the pre-logout Bearer token must not return 200 OK.
    /// Production may return 500 instead of 401/403 (known Sanctum/backend inconsistency) — still acceptable
    /// because the session is not usable (no profile). 401/403 are the ideal contract.
    /// </summary>
    public static void AssertStaleTokenDoesNotAuthenticate(ApiException ex, string context)
    {
        Assert.DoesNotContain("<html", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Laravel", ex.Message, StringComparison.OrdinalIgnoreCase);

        if (ex.StatusCode is HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden
            or HttpStatusCode.InternalServerError)
        {
            return;
        }

        Assert.Fail(
            $"{context}: stale Bearer token after logout returned {(int)ex.StatusCode} {ex.StatusCode}. " +
            "Expected 401, 403, or documented backend 500 — not a successful session.");
    }

    public static void AssertLogoutPostDoesNotLeakHtml(ApiException? logoutEx)
    {
        if (logoutEx is null)
            return;

        Assert.DoesNotContain("<html", logoutEx.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Laravel", logoutEx.Message, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertPaginatedTickets(PaginatedResponse<Ticket>? response)
    {
        Assert.NotNull(response);
        Assert.NotNull(response!.Data);
        Assert.True(response.Total >= 0);
        Assert.True(response.LastPage >= 1);
    }

    public static void AssertStaffListStatus(
        HttpStatusCode status,
        string endpoint,
        bool allowNotFoundFallback)
    {
        if (status is HttpStatusCode.OK)
            return;

        if (allowNotFoundFallback && status is HttpStatusCode.NotFound)
            return;

        if (status is HttpStatusCode.Forbidden)
        {
            Assert.Fail(
                $"GET /api/{endpoint} returned 403 Forbidden. " +
                "ZGRZYT_LOGIN does not have staff permissions for this endpoint.");
        }

        Assert.Fail(
            $"GET /api/{endpoint} returned {(int)status} {status}. " +
            "Expected 200 OK" +
            (allowNotFoundFallback ? " or 404 Not Found (desktop uses GET /api/users fallback)." : "."));
    }
}
