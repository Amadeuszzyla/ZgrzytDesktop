using System.Text.Json;
using Xunit;
using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Services;

namespace ZgrzytDesktop.Tests.Models;

public class ModelJsonSerializationTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Message_ShouldDeserializeBodyToContent()
    {
        const string json = """{"id":1,"body":"Witaj","ticket_id":5}""";

        var message = JsonSerializer.Deserialize<ZgrzytDesktop.Models.Message>(json, _options);

        Assert.NotNull(message);
        Assert.Equal("Witaj", message!.Content);
    }

    [Fact]
    public void Message_ShouldDeserializeMessageAliasWhenBodyMissing()
    {
        const string json = """{"id":2,"message":"Odpowiedź IT","ticket_id":8}""";

        var message = JsonSerializer.Deserialize<ZgrzytDesktop.Models.Message>(json, _options);

        Assert.NotNull(message);
        Assert.Equal("Odpowiedź IT", message!.Content);
    }

    [Fact]
    public void Message_ShouldSerializeContentAsBody()
    {
        var message = new ZgrzytDesktop.Models.Message { Content = "Treść wiadomości" };

        var json = JsonSerializer.Serialize(message, _options);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Treść wiadomości", doc.RootElement.GetProperty("body").GetString());
        Assert.False(doc.RootElement.TryGetProperty("content", out _));
    }

    [Fact]
    public void CreateMessageRequest_ShouldSerializeBody()
    {
        var request = new CreateMessageRequest { Body = "Odpowiedź IT" };

        var json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("body", json, StringComparison.Ordinal);
        Assert.Equal("Odpowiedź IT", JsonSerializer.Deserialize<CreateMessageRequest>(json, _options)?.Body);
    }

    [Fact]
    public void Ticket_ShouldDeserializeAssignedTo()
    {
        const string json = """
            {
              "id": 10,
              "title": "Test",
              "status": "nowe",
              "priority": "niski",
              "user_id": 1,
              "assignedTo": { "id": 2, "login": "it.user" }
            }
            """;

        var ticket = JsonSerializer.Deserialize<Ticket>(json, _options);

        Assert.NotNull(ticket);
        Assert.Equal(2, ticket!.AssignedTo?.Id);
        Assert.Equal("it.user", ticket.AssignedTo?.Login);
    }

    [Fact]
    public void UpdateTicketRequest_ShouldOmitAssignedItIdNull_OnStatusUpdate()
    {
        var request = new UpdateTicketRequest
        {
            Status = "w trakcie",
            Priority = "wysoki"
        };

        var json = JsonSerializer.Serialize(request, _options);

        Assert.DoesNotContain("assigned_it_id", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateTicketRequest_ShouldSerializeAssignedItIdNull_OnlyIfSupportedByContract()
    {
        var json = JsonSerializer.Serialize(new UpdateTicketRequest { AssignedItId = null }, _options);

        if (TicketAssignmentContract.SupportsClearAssignment)
            Assert.Contains("\"assigned_it_id\":null", json, StringComparison.Ordinal);
        else
            Assert.DoesNotContain("assigned_it_id", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateTicketRequest_ShouldSerializeAssignedItId()
    {
        var request = new UpdateTicketRequest
        {
            Status = "w trakcie",
            Priority = "wysoki",
            AssignedItId = 3
        };

        var json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"assigned_it_id\":3", json, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"w trakcie\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateTicketRequest_ShouldNotSerializeCategoryOrClosedAt()
    {
        var request = new UpdateTicketRequest { Status = "nowe", AssignedItId = 1 };

        var json = JsonSerializer.Serialize(request, _options);

        Assert.DoesNotContain("category", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("closed_at", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateTicketRequest_ShouldNotSerializeCategory()
    {
        var request = new CreateTicketRequest
        {
            Title = "[Hardware] Problem",
            Description = "Kategoria: Hardware\n\nOpis",
            Priority = "niski"
        };

        var json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"title\"", json, StringComparison.Ordinal);
        Assert.Contains("\"description\"", json, StringComparison.Ordinal);
        Assert.Contains("\"priority\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("category", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ticket_ShouldNotSerializeCategoryOrClosedAtOnWrite()
    {
        var ticket = new Ticket
        {
            Title = "Test",
            Category = "Hardware",
            ClosedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(ticket, _options);

        Assert.DoesNotContain("category", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("closed_at", json, StringComparison.OrdinalIgnoreCase);
    }
}
