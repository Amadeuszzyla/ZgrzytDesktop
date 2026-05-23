using System.Net;
using ZgrzytDesktop.Constants;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Regression;

public class TicketQueueFetchLimitTests
{
    public TicketQueueFetchLimitTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public async Task GetTicketsAsync_ActiveWithStatusFilter_MarksTruncationWhenMaxPagesReached()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            for (var page = 1; page <= TicketQueueFetchPolicy.MaxPages + 2; page++)
            {
                handler.EnqueueJson(HttpStatusCode.OK, FullQueuePageJson(page, TicketQueueFetchPolicy.MaxPages + 2));
            }

            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                status: TicketStatuses.Nowe,
                queueView: TicketQueueView.Active);

            Assert.NotNull(response);
            Assert.True(response!.IsQueueFetchTruncated);
            Assert.Equal(TicketQueueFetchPolicy.MaxPages, handler.Requests.Count);
            Assert.Equal(TicketQueueFetchPolicy.MaxPages * TicketQueueFetchPolicy.PageSize, response.Total);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketsAsync_UnassignedWithStatusFilter_AppliesLocalFiltering()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, UnassignedQueuePageJson());
            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                status: TicketStatuses.WTrakcie,
                queueView: TicketQueueView.Unassigned);

            Assert.NotNull(response);
            Assert.Single(response!.Data);
            Assert.Equal(20, response.Data[0].Id);
            Assert.False(response.IsQueueFetchTruncated);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketsAsync_ActiveQueue_ApiError_IsSanitized()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();

        try
        {
            handler.EnqueueHtml(HttpStatusCode.InternalServerError, "<html><body>Laravel</body></html>");
            var service = TestApiFactory.CreateTickets(api);

            var ex = await Assert.ThrowsAsync<ApiException>(() =>
                service.GetTicketsAsync(
                    status: TicketStatuses.Nowe,
                    queueView: TicketQueueView.Active));

            Assert.DoesNotContain("<html", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Laravel", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public void QueueFetchTruncatedMessage_IsLocalized_PlAndEn()
    {
        AppStrings.ApplyCulture("pl");
        var pl = AppStrings.GetFormat("Tickets_QueueFetchTruncated", 5000, 50);
        Assert.Contains("niepełna", pl, StringComparison.Ordinal);

        AppStrings.ApplyCulture("en");
        var en = AppStrings.GetFormat("Tickets_QueueFetchTruncated", 5000, 50);
        Assert.Contains("incomplete", en, StringComparison.OrdinalIgnoreCase);
    }

    private static string FullQueuePageJson(int currentPage, int lastPage)
    {
        var items = string.Join(
            ",",
            Enumerable.Range(1, TicketQueueFetchPolicy.PageSize)
                .Select(index =>
                    $$"""{"id":{{currentPage * 1000 + index}},"title":"T","status":"nowe","priority":"niski","user_id":1}"""));

        return $$"""{"current_page":{{currentPage}},"data":[{{items}}],"last_page":{{lastPage}},"per_page":100,"total":99999}""";
    }

    private static string UnassignedQueuePageJson() =>
        """
        {
          "current_page": 1,
          "data": [
            { "id": 10, "title": "Yankee", "status": "nowe", "priority": "niski", "user_id": 1 },
            { "id": 20, "title": "Zulu", "status": "w trakcie", "priority": "średni", "user_id": 1 }
          ],
          "last_page": 1,
          "per_page": 100,
          "total": 2
        }
        """;
}
