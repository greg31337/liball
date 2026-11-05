using System.Net.Http.Headers;

namespace jmap.Tests;

internal sealed class RecordedHttpRequest
{
    public string Method = "";
    public string Uri = "";
    public string Accept = "";
    public string? Authorization;
    public string? ContentType;
    public byte[] Body = Array.Empty<byte>();
    public Dictionary<string, string[]> Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    public Queue<HttpResponseMessage> Responses = new Queue<HttpResponseMessage>();
    public Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> Steps =
        new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();
    public List<RecordedHttpRequest> Requests = new List<RecordedHttpRequest>();
    public Exception? Failure;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RecordedHttpRequest recorded = new RecordedHttpRequest();
        recorded.Method = request.Method.Method;
        recorded.Uri = request.RequestUri == null ? "" : request.RequestUri.AbsoluteUri;
        recorded.Accept = string.Join(", ", request.Headers.Accept.Select(value => value.MediaType));
        recorded.Authorization = request.Headers.Authorization?.ToString();
        recorded.ContentType = request.Content?.Headers.ContentType?.ToString();
        RecordHeaders(request.Headers, recorded.Headers);
        if (request.Content != null)
        {
            RecordHeaders(request.Content.Headers, recorded.Headers);
            recorded.Body = request.Content.ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult();
        }

        Requests.Add(recorded);
        if (Failure != null)
        {
            return Task.FromException<HttpResponseMessage>(Failure);
        }

        if (Steps.Count > 0)
        {
            return Steps.Dequeue()(request, cancellationToken);
        }

        if (Responses.Count == 0)
        {
            return Task.FromException<HttpResponseMessage>(
                new InvalidOperationException("The scripted HTTP handler has no response remaining."));
        }

        return Task.FromResult(Responses.Dequeue());
    }

    private static void RecordHeaders(HttpHeaders source, Dictionary<string, string[]> target)
    {
        foreach (KeyValuePair<string, IEnumerable<string>> header in source)
        {
            target[header.Key] = header.Value.ToArray();
        }
    }
}
