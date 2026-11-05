using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class OfficeActionTextOperationTests
{
    [Test]
    public void OnePageSearchPostsDsapiFormAndParsesResponse()
    {
        string page = Page(5, 4, new string[] { "oa-5" }, ",\"generation\":8");
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler(page);
        handler.RequestIdentifier = "request-1";
        using HttpClient client = new HttpClient(handler);
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Query = "bodyText:battery",
            OffsetPage = new OffsetPage { Offset = 4, Limit = 2 },
            Context = new RequestContext { RetainRawResponse = true }
        };

        OfficeActionTextResponse response = operation.Search(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods, Is.EqualTo(new[] { HttpMethod.Post }));
            Assert.That(handler.RequestUris[0].AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/oa/oa_actions/v1/records"));
            Assert.That(handler.Bodies[0], Is.EqualTo("criteria=bodyText%3Abattery&rows=2&start=4"));
            Assert.That(handler.Starts, Is.EqualTo(new long[] { 4 }));
            Assert.That(handler.Rows, Is.EqualTo(new int[] { 2 }));
            Assert.That(handler.ApiKeys, Is.EqualTo(new[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new[] { UsptoMediaTypes.Json }));
            Assert.That(handler.ContentTypes, Is.EqualTo(new[] { UsptoMediaTypes.FormUrlEncoded }));
            Assert.That(response.Request, Is.SameAs(request));
            Assert.That(response.Records.Select(record => record.Identifier), Is.EqualTo(new[] { "oa-5" }));
            Assert.That(response.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(5));
            Assert.That(response.Start, Is.EqualTo(4));
            Assert.That(response.RequestIdentifier, Is.EqualTo("request-1"));
            Assert.That(response.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(response.Provenance.DatasetIdentifier, Is.EqualTo("oa_actions"));
            Assert.That(response.RawResponse, Is.EqualTo(page));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(response.UnknownFields["generation"], Is.EqualTo("8"));
        });
    }

    [Test]
    public void FormEncodingPreservesUnicodeAndEscapesReservedSeparators()
    {
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler(Page(0, 0, new string[0], string.Empty));
        using HttpClient client = new HttpClient(handler);
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Query = "bodyText:\"café & battery+pack\"",
            OffsetPage = new OffsetPage { Offset = 0, Limit = 5 }
        };

        operation.Search(request);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Bodies[0], Is.EqualTo("criteria=bodyText%3A%22caf%C3%A9+%26+battery%2Bpack%22&rows=5&start=0"));
            Assert.That(handler.Criteria[0], Is.EqualTo("bodyText:\"café & battery+pack\""));
            Assert.That(handler.Rows[0], Is.EqualTo(5));
            Assert.That(handler.Starts[0], Is.Zero);
        });
    }

    [Test]
    public void AllPageSearchAdvancesSolrOffsetsAndPreservesCallerRequest()
    {
        string firstPage = Page(5, 0, new string[] { "oa-1", "oa-2" }, ",\"generation\":8");
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler(
            firstPage,
            Page(5, 2, new string[] { "oa-3", "oa-4" }, string.Empty),
            Page(5, 4, new string[] { "oa-5" }, string.Empty));
        handler.RequestIdentifier = "aggregate-request";
        using HttpClient client = new HttpClient(handler);
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            Query = "applicationTypeCategory:REGULAR",
            OffsetPage = new OffsetPage { Offset = 0, Limit = 2 },
            MaximumPages = 5,
            MaximumResults = 10,
            Context = new RequestContext { RetainRawResponse = true }
        };

        OfficeActionTextResponse response = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.Request, Is.SameAs(request));
            Assert.That(response.Records.Select(record => record.Identifier), Is.EqualTo(new[] { "oa-1", "oa-2", "oa-3", "oa-4", "oa-5" }));
            Assert.That(response.Count, Is.EqualTo(5));
            Assert.That(response.Total, Is.EqualTo(5));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Start, Is.Zero);
            Assert.That(response.Page.Offset, Is.Zero);
            Assert.That(response.Page.Limit, Is.EqualTo(2));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(response.RequestIdentifier, Is.EqualTo("aggregate-request"));
            Assert.That(response.RawResponse, Is.EqualTo(firstPage));
            Assert.That(response.UnknownFields["generation"], Is.EqualTo("8"));
            Assert.That(handler.Starts, Is.EqualTo(new long[] { 0, 2, 4 }));
            Assert.That(handler.Rows, Is.EqualTo(new int[] { 2, 2, 2 }));
            Assert.That(handler.Criteria, Has.All.EqualTo("applicationTypeCategory:REGULAR"));
            Assert.That(request.OffsetPage.Offset, Is.Zero);
            Assert.That(request.Sorts, Is.Empty);
        });
    }

    [Test]
    public void AllPageSearchStopsAtConfiguredResultLimit()
    {
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler(
            Page(5, 0, new string[] { "oa-1", "oa-2" }, string.Empty),
            Page(5, 2, new string[] { "oa-3", "oa-4" }, string.Empty));
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.Limits.MaximumResults = 3;
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, options);
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 10,
            MaximumResults = 10
        };

        OfficeActionTextResponse response = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.Records.Select(record => record.Identifier), Is.EqualTo(new[] { "oa-1", "oa-2", "oa-3" }));
            Assert.That(response.Count, Is.EqualTo(3));
            Assert.That(response.Total, Is.EqualTo(5));
            Assert.That(response.IsComplete, Is.False);
            Assert.That(handler.Starts, Is.EqualTo(new long[] { 0, 2 }));
        });
    }

    [Test]
    public void AllPageSearchStopsAtCallerPageLimit()
    {
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler(
            Page(5, 0, new string[] { "oa-1", "oa-2" }, string.Empty));
        using HttpClient client = new HttpClient(handler);
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 1,
            MaximumResults = 10
        };

        OfficeActionTextResponse response = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.Count, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(5));
            Assert.That(response.IsComplete, Is.False);
            Assert.That(handler.Starts, Is.EqualTo(new long[] { 0 }));
        });
    }

    [Test]
    public void RepeatedPageIsRejectedBySharedPaginationProtection()
    {
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler(
            Page(4, 0, new string[] { "oa-1", "oa-2" }, string.Empty),
            Page(4, 2, new string[] { "oa-1", "oa-2" }, string.Empty));
        using HttpClient client = new HttpClient(handler);
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, CreateOptions(client));
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Limit = 2 },
            MaximumPages = 5,
            MaximumResults = 10
        };

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.SearchAll(request));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DUPLICATE_PAGE"));
            Assert.That(handler.Starts, Is.EqualTo(new long[] { 0, 2 }));
        });
    }

    [Test]
    public void AggregateRequiresStableRecordIdentifiers()
    {
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler("{\"response\":{\"numFound\":1,\"start\":0,\"docs\":[{\"bodyText\":[\"text\"]}]}}");
        using HttpClient client = new HttpClient(handler);
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, CreateOptions(client));

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.SearchAll(new SearchRequest()));

        Assert.That(exception.ErrorCode, Is.EqualTo("MISSING_RECORD_IDENTIFIER"));
    }

    [Test]
    public void RemoteFailureUsesSharedOfficeActionErrorMapping()
    {
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler("{\"error\":\"busy\"}");
        handler.StatusCode = HttpStatusCode.TooManyRequests;
        handler.RetryAfter = "3";
        using HttpClient client = new HttpClient(handler);
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, CreateOptions(client));

        RateLimitException exception = Assert.Throws<RateLimitException>(() => operation.Search(new SearchRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.OfficeAction));
            Assert.That(exception.StatusCode, Is.EqualTo(429));
            Assert.That(exception.RetryAfterMilliseconds, Is.EqualTo(3000));
        });
    }

    [Test]
    public void CancellationStopsBeforeRequestCreation()
    {
        RecordingOfficeActionTextHandler handler = new RecordingOfficeActionTextHandler(Page(0, 0, new string[0], string.Empty));
        using HttpClient client = new HttpClient(handler);
        OfficeActionTextOperation operation = new OfficeActionTextOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        SearchRequest request = new SearchRequest { Context = new RequestContext { CancellationToken = cancellation.Token } };

        Assert.Throws<OperationCanceledException>(() => operation.Search(request));

        Assert.That(handler.Methods, Is.Empty);
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        return new UsptoClientOptions
        {
            OfficeActionBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000
        };
    }

    private static string Page(long total, long start, string[] identifiers, string extraResponseFields)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"response\":{\"numFound\":");
        builder.Append(total);
        builder.Append(",\"start\":");
        builder.Append(start);
        builder.Append(",\"docs\":[");
        for (int index = 0; index < identifiers.Length; index += 1)
        {
            if (index != 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"id\":\"");
            builder.Append(identifiers[index]);
            builder.Append("\",\"patentApplicationNumber\":[\"16");
            builder.Append((100000 + index).ToString());
            builder.Append("\"],\"bodyText\":[\"Office Action ");
            builder.Append(identifiers[index]);
            builder.Append("\"]}");
        }

        builder.Append(']');
        builder.Append(extraResponseFields);
        builder.Append("}}");
        return builder.ToString();
    }
}

internal class RecordingOfficeActionTextHandler : HttpMessageHandler
{
    internal Queue<string> Responses = new Queue<string>();
    internal HttpStatusCode StatusCode = HttpStatusCode.OK;
    internal List<HttpMethod> Methods = new List<HttpMethod>();
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string> Bodies = new List<string>();
    internal List<long> Starts = new List<long>();
    internal List<int> Rows = new List<int>();
    internal List<string?> Criteria = new List<string?>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();
    internal List<string?> ContentTypes = new List<string?>();
    internal string? RequestIdentifier;
    internal string? RetryAfter;

    internal RecordingOfficeActionTextHandler(params string[] responses)
    {
        foreach (string response in responses)
        {
            Responses.Enqueue(response);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Methods.Add(request.Method);
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        ContentTypes.Add(request.Content?.Headers.ContentType?.MediaType);
        string body = await request.Content!.ReadAsStringAsync(cancellationToken);
        Bodies.Add(body);
        Dictionary<string, string> form = ReadForm(body);
        Starts.Add(long.Parse(form["start"]));
        Rows.Add(int.Parse(form["rows"]));
        Criteria.Add(form["criteria"]);
        string responseBody = Responses.Count == 0 ? "{\"response\":{\"numFound\":0,\"start\":0,\"docs\":[]}}" : Responses.Dequeue();
        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        if (RequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, RequestIdentifier);
        }

        if (RetryAfter != null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", RetryAfter);
        }

        return response;
    }

    private static Dictionary<string, string> ReadForm(string body)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            string value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
