using uspto;

namespace uspto.Tests;

public class RequestBuilderTests
{
    [Test]
    public void BuildUriEscapesOpaqueSegmentsAndSortsQueryOrdinally()
    {
        Uri baseUri = new Uri("https://example.test/root/base");
        string[] segments = new string[] { "patent", "a/b", "x y", "café", ".", "..", "100%" };
        Dictionary<string, string?> query = new Dictionary<string, string?>
        {
            ["z"] = "last",
            ["unicode"] = "naïve",
            ["flag"] = null,
            ["empty"] = string.Empty,
            ["a"] = "1&2"
        };

        Uri result = RequestBuilder.BuildUri(baseUri, segments, query);

        Assert.That(
            result.AbsoluteUri,
            Is.EqualTo("https://example.test/root/base/patent/a%2Fb/x%20y/caf%C3%A9/%2E/%2E%2E/100%25?a=1%262&empty=&flag&unicode=na%C3%AFve&z=last"));
    }

    [Test]
    public void QueryOrderingDoesNotDependOnInsertionOrder()
    {
        Uri baseUri = new Uri("https://example.test/api/");
        Dictionary<string, string?> first = new Dictionary<string, string?>
        {
            ["offset"] = "100",
            ["limit"] = "25",
            ["q"] = "title:battery"
        };
        Dictionary<string, string?> second = new Dictionary<string, string?>
        {
            ["q"] = "title:battery",
            ["limit"] = "25",
            ["offset"] = "100"
        };

        Uri firstUri = RequestBuilder.BuildUri(baseUri, new string[] { "v1", "search" }, first);
        Uri secondUri = RequestBuilder.BuildUri(baseUri, new string[] { "v1", "search" }, second);

        Assert.Multiple(() =>
        {
            Assert.That(firstUri, Is.EqualTo(secondUri));
            Assert.That(firstUri.Query, Is.EqualTo("?limit=25&offset=100&q=title%3Abattery"));
        });
    }

    [Test]
    public void CreateGetRequestBuildsMethodAndUriWithoutContent()
    {
        using HttpRequestMessage request = RequestBuilder.CreateGetRequest(
            new Uri("https://api.uspto.gov/api/v1/"),
            new string[] { "patent", "applications", "16/123,456" },
            new Dictionary<string, string?> { ["include"] = "metadata documents" });

        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri?.AbsoluteUri, Is.EqualTo("https://api.uspto.gov/api/v1/patent/applications/16%2F123%2C456?include=metadata%20documents"));
            Assert.That(request.Content, Is.Null);
        });
    }

    [Test]
    public async Task CreateJsonPostRequestPreservesBodyAndSetsMediaType()
    {
        const string json = "{\"q\":\"title:café\",\"size\":25}";
        using HttpRequestMessage request = RequestBuilder.CreateJsonPostRequest(
            new Uri("https://api.uspto.gov/"),
            new string[] { "api", "v1", "patent", "applications", "search" },
            new Dictionary<string, string?>(),
            json);

        string body = await request.Content!.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.RequestUri?.AbsoluteUri, Is.EqualTo("https://api.uspto.gov/api/v1/patent/applications/search"));
            Assert.That(request.Content.Headers.ContentType?.MediaType, Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(request.Content.Headers.ContentType?.CharSet, Is.EqualTo("utf-8"));
            Assert.That(body, Is.EqualTo(json));
        });
    }

    [Test]
    public async Task CreateFormPostRequestSortsAndEncodesFields()
    {
        Dictionary<string, string> fields = new Dictionary<string, string>
        {
            ["start"] = "0",
            ["rows"] = "100",
            ["criteria"] = "applicationNumber:16/123,456 + A&B",
            ["note"] = "café"
        };
        using HttpRequestMessage request = RequestBuilder.CreateFormPostRequest(
            new Uri("https://api.uspto.gov/api/v1/"),
            new string[] { "patent", "oa", "oa_actions", "v1", "records" },
            new Dictionary<string, string?>(),
            fields);

        string body = await request.Content!.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Content.Headers.ContentType?.MediaType, Is.EqualTo(UsptoMediaTypes.FormUrlEncoded));
            Assert.That(body, Is.EqualTo("criteria=applicationNumber%3A16%2F123%2C456+%2B+A%26B&note=caf%C3%A9&rows=100&start=0"));
        });
    }

    [Test]
    public void InvalidUriAndInputShapesHaveStructuredErrors()
    {
        ValidationException relative = Assert.Throws<ValidationException>(() => RequestBuilder.BuildUri(
            new Uri("api/v1", UriKind.Relative),
            new string[0],
            new Dictionary<string, string?>()));
        ValidationException baseQuery = Assert.Throws<ValidationException>(() => RequestBuilder.BuildUri(
            new Uri("https://example.test/api?existing=true"),
            new string[0],
            new Dictionary<string, string?>()));
        ValidationException emptySegment = Assert.Throws<ValidationException>(() => RequestBuilder.BuildUri(
            new Uri("https://example.test/"),
            new string[] { string.Empty },
            new Dictionary<string, string?>()));
        ValidationException emptyQueryName = Assert.Throws<ValidationException>(() => RequestBuilder.BuildUri(
            new Uri("https://example.test/"),
            new string[0],
            new Dictionary<string, string?> { [string.Empty] = "value" }));
        ValidationException emptyFormName = Assert.Throws<ValidationException>(() => RequestBuilder.CreateFormPostRequest(
            new Uri("https://example.test/"),
            new string[] { "search" },
            new Dictionary<string, string?>(),
            new Dictionary<string, string> { [string.Empty] = "value" }));

        Assert.Multiple(() =>
        {
            Assert.That(relative.ErrorCode, Is.EqualTo("ABSOLUTE_URI_REQUIRED"));
            Assert.That(relative.FieldName, Is.EqualTo("baseUri"));
            Assert.That(baseQuery.ErrorCode, Is.EqualTo("BASE_URI_COMPONENT"));
            Assert.That(emptySegment.ErrorCode, Is.EqualTo("EMPTY_PATH_SEGMENT"));
            Assert.That(emptyQueryName.ErrorCode, Is.EqualTo("EMPTY_QUERY_NAME"));
            Assert.That(emptyFormName.ErrorCode, Is.EqualTo("EMPTY_FORM_NAME"));
        });
    }
}
