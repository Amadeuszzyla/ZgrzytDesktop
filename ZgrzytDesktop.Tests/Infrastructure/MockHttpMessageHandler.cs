using System.Net;
using System.Text;

namespace ZgrzytDesktop.Tests.Infrastructure;

public sealed record RecordedHttpRequest(HttpMethod Method, Uri? Uri, string? Body);

public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public IList<RecordedHttpRequest> Requests { get; } = new List<RecordedHttpRequest>();

    public void EnqueueJson(HttpStatusCode statusCode, string json)
    {
        Enqueue(statusCode, json, "application/json");
    }

    public void Enqueue(HttpStatusCode statusCode, string content, string mediaType)
    {
        _responses.Enqueue(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType)
        });
    }

    public void EnqueueHtml(HttpStatusCode statusCode, string html)
    {
        Enqueue(statusCode, html, "text/html");
    }

    public void EnqueueException(Exception exception)
    {
        _responses.Enqueue(_ => throw exception);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new RecordedHttpRequest(request.Method, request.RequestUri, body));

        if (_responses.Count == 0)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("No mock response configured", Encoding.UTF8, "text/plain")
            };
        }

        var factory = _responses.Dequeue();
        return factory(request);
    }
}
