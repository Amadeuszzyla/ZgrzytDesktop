using System.Net;
using System.Text.Json;
using Xunit;
using ZgrzytDesktop.Exceptions;
using ZgrzytDesktop.Services;
using ZgrzytDesktop.Tests.Infrastructure;

namespace ZgrzytDesktop.Tests.Services;

public class TicketServiceTests
{
    [Fact]
    public async Task GetTicketsAsync_ShouldRequestTicketsWithSortParameters()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                {
                  "current_page": 1,
                  "data": [{ "id": 1, "title": "A", "status": "nowe", "priority": "niski", "user_id": 1 }],
                  "last_page": 1,
                  "per_page": 15,
                  "total": 1
                }
                """);
            var service = TestApiFactory.CreateTickets(api);

            var response = await service.GetTicketsAsync(
                page: 2,
                perPage: 10,
                search: "monitor",
                status: "nowe",
                priority: "wysoki",
                sortBy: "title",
                sortDirection: "asc");

            Assert.NotNull(response);
            Assert.Single(response!.Data);
            Assert.Equal(1, response.Total);

            var uri = handler.Requests[0].Uri!.ToString();
            Assert.Contains("/api/tickets?", uri, StringComparison.Ordinal);
            Assert.Contains("page=2", uri, StringComparison.Ordinal);
            Assert.Contains("per_page=10", uri, StringComparison.Ordinal);
            Assert.Contains("search=monitor", uri, StringComparison.Ordinal);
            Assert.Contains("sort_by=title", uri, StringComparison.Ordinal);
            Assert.Contains("sort_direction=asc", uri, StringComparison.Ordinal);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task UpdateTicketAsync_ShouldPutStatusAndAssignedItId()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                { "id": 5, "title": "T", "status": "w trakcie", "priority": "średni", "user_id": 1, "assigned_it_id": 2 }
                """);
            var service = TestApiFactory.CreateTickets(api);

            var updated = await service.UpdateTicketAsync(5, new UpdateTicketRequest
            {
                Status = "w trakcie",
                AssignedItId = 2
            });

            Assert.NotNull(updated);
            Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
            Assert.EndsWith("/api/tickets/5", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);

            var body = TestApiFactory.LastRequestBody(handler)!;
            using var doc = JsonDocument.Parse(body);
            Assert.Equal("w trakcie", doc.RootElement.GetProperty("status").GetString());
            Assert.Equal(2, doc.RootElement.GetProperty("assigned_it_id").GetInt32());
            Assert.False(body.Contains("category", StringComparison.OrdinalIgnoreCase));
            Assert.False(body.Contains("closed_at", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task CreateTicketAsync_ShouldPostWithoutCategoryField()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.Created, """
                { "id": 99, "title": "[Hardware] X", "description": "Opis", "status": "nowe", "priority": "niski", "user_id": 1 }
                """);
            var service = TestApiFactory.CreateTickets(api);

            var created = await service.CreateTicketAsync(new CreateTicketRequest
            {
                Title = "[Hardware] X",
                Description = "Opis",
                Priority = "niski"
            });

            Assert.NotNull(created);
            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
            Assert.EndsWith("/api/tickets", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);

            var body = TestApiFactory.LastRequestBody(handler)!;
            Assert.False(body.Contains("category", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetTicketMessagesAsync_ShouldGetMessagesEndpoint()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                [{ "id": 1, "body": "Cześć", "ticket_id": 3, "sender_id": 1 }]
                """);
            var service = TestApiFactory.CreateTickets(api);

            var messages = await service.GetTicketMessagesAsync(3);

            Assert.Single(messages);
            Assert.Equal("Cześć", messages[0].Content);
            Assert.EndsWith("/api/tickets/3/messages", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task SendMessageAsync_ShouldPostBodyField()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.Created, """
                { "id": 2, "body": "Odpowiedź", "ticket_id": 8, "sender_id": 1 }
                """);
            var service = TestApiFactory.CreateTickets(api);

            var message = await service.SendMessageAsync(8, "Odpowiedź");

            Assert.NotNull(message);
            Assert.Equal("Odpowiedź", message!.Content);
            Assert.EndsWith("/api/tickets/8/messages", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);

            var body = TestApiFactory.LastRequestBody(handler)!;
            using var doc = JsonDocument.Parse(body);
            Assert.Equal("Odpowiedź", doc.RootElement.GetProperty("body").GetString());
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task GetActiveTicketsAsync_ShouldUseActiveTicketsEndpoint()
    {
        var (api, handler, tempDir) = TestApiFactory.CreateApi();
        try
        {
            handler.EnqueueJson(HttpStatusCode.OK, """
                { "current_page": 1, "data": [], "last_page": 1, "per_page": 15, "total": 0 }
                """);
            var service = TestApiFactory.CreateTickets(api);

            await service.GetActiveTicketsAsync();

            Assert.Contains("/api/active-tickets", handler.Requests[0].Uri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestApiFactory.Cleanup(tempDir);
        }
    }
}
