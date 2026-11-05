using System.Net;
using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentApplicationPaginationOperationTests
{
    [Test]
    public void GetAggregationReadsUntilAnEmptyPageWithoutMutatingTheCallerRequest()
    {
        string firstPage = FixtureLoader.LoadText("patent-applications/page-one.json");
        ScriptedPatentPaginationHandler handler = new ScriptedPatentPaginationHandler(new string[]
        {
            firstPage,
            FixtureLoader.LoadText("patent-applications/page-two.json"),
            FixtureLoader.LoadText("patent-applications/page-empty.json")
        });
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, options);
        SearchRequest request = CreateRequest();
        request.FieldSelection.Fields = new string[] { "applicationMetaData.inventionTitle" };
        request.Context.RetainRawResponse = true;

        PatentApplicationResponse result = operation.SearchAllGet(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Summaries.Select(summary => summary.ApplicationNumber), Is.EqualTo(new string[] { "14104993", "15123456", "16234567" }));
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.HasTotal, Is.False);
            Assert.That(result.Page.Offset, Is.Zero);
            Assert.That(result.Page.Limit, Is.EqualTo(2));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Facets[0].Field, Is.EqualTo("applicationMetaData.applicationStatusCode"));
            Assert.That(result.RequestIdentifier, Is.EqualTo("page-one"));
            Assert.That(result.RawResponse, Is.EqualTo(firstPage));
            Assert.That(handler.Methods, Is.EqualTo(new HttpMethod[] { HttpMethod.Get, HttpMethod.Get, HttpMethod.Get }));
            Assert.That(handler.Offsets, Is.EqualTo(new long[] { 0L, 2L, 3L }));
            Assert.That(handler.Limits, Is.EqualTo(new int[] { 2, 2, 2 }));
            Assert.That(handler.SelectedFields, Has.All.EqualTo("applicationMetaData.inventionTitle,applicationNumberText"));
            Assert.That(request.OffsetPage.Offset, Is.Zero);
            Assert.That(request.FieldSelection.Fields, Is.EqualTo(new string[] { "applicationMetaData.inventionTitle" }));
        });
    }

    [Test]
    public void DefaultPostAggregationHonorsTheConfiguredResultLimit()
    {
        ScriptedPatentPaginationHandler handler = new ScriptedPatentPaginationHandler(new string[]
        {
            Page(new string[] { "14/104,993", "15/123,456" }, "page-one", string.Empty),
            Page(new string[] { "16/234,567", "17/345,678" }, "page-two", string.Empty)
        });
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.Limits.MaximumResults = 3;
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, options);
        SearchRequest request = CreateRequest();
        request.MaximumResults = 10;

        PatentApplicationResponse result = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Summaries.Select(summary => summary.ApplicationNumber), Is.EqualTo(new string[] { "14104993", "15123456", "16234567" }));
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(handler.Methods, Is.EqualTo(new HttpMethod[] { HttpMethod.Post, HttpMethod.Post }));
            Assert.That(handler.Offsets, Is.EqualTo(new long[] { 0L, 2L }));
            Assert.That(handler.Limits, Is.EqualTo(new int[] { 2, 2 }));
        });
    }

    [Test]
    public void DuplicateApplicationPagesAreRejectedByTheSharedEngine()
    {
        string repeated = Page(new string[] { "14/104,993", "15/123,456" }, "page", string.Empty);
        ScriptedPatentPaginationHandler handler = new ScriptedPatentPaginationHandler(new string[] { repeated, repeated });
        using HttpClient client = new HttpClient(handler);
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, CreateOptions(client));

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.SearchAllPost(CreateRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DUPLICATE_PAGE"));
            Assert.That(handler.Offsets, Is.EqualTo(new long[] { 0L, 2L }));
        });
    }

    [Test]
    public void AggregateSearchRequiresTheApplicationNumberAsTheFinalStableSort()
    {
        ScriptedPatentPaginationHandler handler = new ScriptedPatentPaginationHandler(new string[0]);
        using HttpClient client = new HttpClient(handler);
        PatentApplicationSearchOperation operation = new PatentApplicationSearchOperation(client, CreateOptions(client));
        SearchRequest request = CreateRequest();
        request.Sorts = new SearchSort[]
        {
            new SearchSort { Field = "applicationMetaData.filingDate", Direction = SortDirection.Ascending }
        };

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.SearchAllGet(request));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DETERMINISTIC_SORT_REQUIRED"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    private static SearchRequest CreateRequest()
    {
        return new SearchRequest
        {
            OffsetPage = new OffsetPage { Offset = 0L, Limit = 2 },
            MaximumPages = 10,
            MaximumResults = 10,
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "applicationMetaData.filingDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "applicationNumberText", Direction = SortDirection.Ascending }
            }
        };
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        return new UsptoClientOptions
        {
            OdpBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000
        };
    }

    private static string Page(string[] applicationNumbers, string requestIdentifier, string leadingFields)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        builder.Append(leadingFields);
        builder.Append("\"count\":");
        builder.Append(applicationNumbers.Length);
        builder.Append(",\"patentFileWrapperDataBag\":[");
        for (int index = 0; index < applicationNumbers.Length; index += 1)
        {
            if (index != 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"applicationNumberText\":\"");
            builder.Append(applicationNumbers[index]);
            builder.Append("\",\"applicationMetaData\":{\"inventionTitle\":\"Title ");
            builder.Append(index);
            builder.Append("\"}}");
        }

        builder.Append("],\"requestIdentifier\":\"");
        builder.Append(requestIdentifier);
        builder.Append("\"}");
        return builder.ToString();
    }
}

internal class ScriptedPatentPaginationHandler : HttpMessageHandler
{
    internal Queue<string> Responses = new Queue<string>();
    internal List<HttpMethod> Methods = new List<HttpMethod>();
    internal List<long> Offsets = new List<long>();
    internal List<int> Limits = new List<int>();
    internal List<string?> SelectedFields = new List<string?>();
    internal int CallCount = 0;

    internal ScriptedPatentPaginationHandler(string[] responses)
    {
        foreach (string response in responses)
        {
            Responses.Enqueue(response);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount += 1;
        Methods.Add(request.Method);
        if (request.Method == HttpMethod.Get)
        {
            Dictionary<string, string?> query = ReadQuery(request.RequestUri);
            Offsets.Add(long.Parse(query["offset"]!));
            Limits.Add(int.Parse(query["limit"]!));
            SelectedFields.Add(query.GetValueOrDefault("fields"));
        }
        else
        {
            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement pagination = document.RootElement.GetProperty("pagination");
            Offsets.Add(pagination.GetProperty("offset").GetInt64());
            Limits.Add(pagination.GetProperty("limit").GetInt32());
            if (document.RootElement.TryGetProperty("fields", out JsonElement fields))
            {
                SelectedFields.Add(string.Join(",", fields.EnumerateArray().Select(field => field.GetString())));
            }
            else
            {
                SelectedFields.Add(null);
            }
        }

        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Responses.Dequeue(), Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return response;
    }

    private static Dictionary<string, string?> ReadQuery(Uri? uri)
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>(StringComparer.Ordinal);
        string query = uri?.Query.TrimStart('?') ?? string.Empty;
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(parts[0]);
            string? value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : null;
            values[key] = value;
        }

        return values;
    }
}
