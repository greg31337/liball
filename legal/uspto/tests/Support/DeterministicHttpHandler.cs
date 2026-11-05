using System.Net;
using System.Text;

namespace uspto.Tests;

internal class HttpRequestRecord
{
    internal HttpMethod Method = HttpMethod.Get;
    internal Uri? Uri;
    internal byte[] Body = Array.Empty<byte>();
    internal Dictionary<string, string[]> Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

    internal string BodyText()
    {
        return Encoding.UTF8.GetString(Body);
    }
}

internal class DeterministicHttpHandler : HttpMessageHandler
{
    internal Queue<Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>> Steps = new Queue<Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>>();
    internal List<HttpRequestRecord> Requests = new List<HttpRequestRecord>();

    internal void AddJsonResponse(HttpStatusCode statusCode, string body)
    {
        AddTextResponse(statusCode, body, "application/json");
    }

    internal void AddTextResponse(HttpStatusCode statusCode, string body, string mediaType)
    {
        Steps.Enqueue((request, cancellationToken) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(statusCode);
            response.Content = new StringContent(body, Encoding.UTF8, mediaType);
            return response;
        });
    }

    internal void AddBinaryResponse(HttpStatusCode statusCode, byte[] body, string mediaType)
    {
        Steps.Enqueue((request, cancellationToken) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(statusCode);
            response.Content = new ByteArrayContent(body);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
            return response;
        });
    }

    internal void AddResponse(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> step)
    {
        Steps.Enqueue(step);
    }

    internal void AddFailure(Exception failure)
    {
        Steps.Enqueue((request, cancellationToken) => throw failure);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HttpRequestRecord record = new HttpRequestRecord();
        record.Method = request.Method;
        record.Uri = request.RequestUri;
        if (request.Content != null)
        {
            record.Body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        CaptureHeaders(request.Headers, record.Headers);
        if (request.Content != null)
        {
            CaptureHeaders(request.Content.Headers, record.Headers);
        }

        Requests.Add(record);
        if (Steps.Count == 0)
        {
            throw new InvalidOperationException("No deterministic HTTP response remains for the request.");
        }

        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> step = Steps.Dequeue();
        return step(request, cancellationToken);
    }

    private static void CaptureHeaders(System.Net.Http.Headers.HttpHeaders source, Dictionary<string, string[]> destination)
    {
        foreach (KeyValuePair<string, IEnumerable<string>> header in source)
        {
            destination[header.Key] = header.Value.ToArray();
        }
    }
}
