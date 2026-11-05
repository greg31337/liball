using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class CacheKeyBuilderTests
{
    [Test]
    public void EquivalentUrisProduceTheSameKey()
    {
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://API.USPTO.GOV:443/a/../search/%7e?z=2&q=a%20b");
        using HttpRequestMessage second = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/search/~?q=a%20b&z=2");

        string firstKey = CacheKeyBuilder.Build(ApiService.Odp, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.Odp, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void QueryPairAndDuplicateValueOrderIsCanonical()
    {
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/search?field=b&field=a&size=10");
        using HttpRequestMessage second = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/search?size=10&field=a&field=b");

        string firstKey = CacheKeyBuilder.Build(ApiService.Odp, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.Odp, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void ServiceAndMethodRemainPartOfTheKey()
    {
        using HttpRequestMessage get = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/search");
        using HttpRequestMessage post = new HttpRequestMessage(HttpMethod.Post, "https://api.uspto.gov/search");

        string odpGet = CacheKeyBuilder.Build(ApiService.Odp, get, null, CancellationToken.None);
        string officeActionGet = CacheKeyBuilder.Build(ApiService.OfficeAction, get, null, CancellationToken.None);
        string odpPost = CacheKeyBuilder.Build(ApiService.Odp, post, null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(odpGet, Is.Not.EqualTo(officeActionGet));
            Assert.That(odpGet, Is.Not.EqualTo(odpPost));
        });
    }

    [Test]
    public void SelectedRepresentationHeadersVaryTheKey()
    {
        using HttpRequestMessage json = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        json.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(UsptoMediaTypes.Json));
        using HttpRequestMessage xml = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        xml.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(UsptoMediaTypes.Xml));
        using HttpRequestMessage ranged = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        ranged.Headers.Range = new RangeHeaderValue(100, 199);

        string jsonKey = CacheKeyBuilder.Build(ApiService.Odp, json, null, CancellationToken.None);
        string xmlKey = CacheKeyBuilder.Build(ApiService.Odp, xml, null, CancellationToken.None);
        string rangeKey = CacheKeyBuilder.Build(ApiService.Odp, ranged, null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(jsonKey, Is.Not.EqualTo(xmlKey));
            Assert.That(jsonKey, Is.Not.EqualTo(rangeKey));
        });
    }

    [Test]
    public void RepresentationHeaderTokenCasingIsCanonical()
    {
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        first.Headers.TryAddWithoutValidation("Accept", "Application/JSON");
        using HttpRequestMessage second = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        second.Headers.TryAddWithoutValidation("Accept", "application/json");

        string firstKey = CacheKeyBuilder.Build(ApiService.Odp, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.Odp, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void NonSelectedAndCredentialHeadersDoNotVaryTheKey()
    {
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        first.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "first-secret");
        first.Headers.TryAddWithoutValidation(UsptoHeaders.OdpApiKey, "first-api-key");
        first.Headers.TryAddWithoutValidation("Cookie", "session=first-cookie");
        first.Headers.TryAddWithoutValidation(UsptoHeaders.UserAgent, "first-client");
        first.Headers.TryAddWithoutValidation("X-Correlation-ID", "first-correlation");
        using HttpRequestMessage second = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");
        second.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "second-secret");
        second.Headers.TryAddWithoutValidation(UsptoHeaders.OdpApiKey, "second-api-key");
        second.Headers.TryAddWithoutValidation("Cookie", "session=second-cookie");
        second.Headers.TryAddWithoutValidation(UsptoHeaders.UserAgent, "second-client");
        second.Headers.TryAddWithoutValidation("X-Correlation-ID", "second-correlation");

        string firstKey = CacheKeyBuilder.Build(ApiService.Odp, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.Odp, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void CredentialQueryParametersAreOmitted()
    {
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource?q=patent&api_key=first&sig=first-signature");
        using HttpRequestMessage second = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource?sig=second-signature&q=patent&api_key=second");

        string firstKey = CacheKeyBuilder.Build(ApiService.Odp, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.Odp, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void UserInformationAndFragmentsAreExcludedFromRequestIdentity()
    {
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://first:secret@api.uspto.gov/resource#first");
        using HttpRequestMessage second = new HttpRequestMessage(HttpMethod.Get, "https://second:secret@api.uspto.gov/resource#second");

        string firstKey = CacheKeyBuilder.Build(ApiService.Odp, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.Odp, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void JsonWhitespaceAndPropertyOrderAreCanonical()
    {
        using HttpRequestMessage first = CreateJsonRequest("{\"z\":2,\"nested\":{\"b\":false,\"a\":true},\"a\":[1,2]}");
        using HttpRequestMessage second = CreateJsonRequest(" { \"a\" : [ 1 , 2 ], \"nested\" : { \"a\" : true, \"b\" : false }, \"z\" : 2 } ");

        string firstKey = CacheKeyBuilder.Build(ApiService.PatentsView, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.PatentsView, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void CredentialJsonFieldsAreRecursivelyOmitted()
    {
        using HttpRequestMessage first = CreateJsonRequest("{\"query\":{\"b\":2,\"api_key\":\"first\",\"a\":1},\"token\":\"first-token\"}");
        using HttpRequestMessage second = CreateJsonRequest("{\"token\":\"second-token\",\"query\":{\"a\":1,\"api_key\":\"second\",\"b\":2}}");

        string firstKey = CacheKeyBuilder.Build(ApiService.PatentsView, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.PatentsView, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void NonCredentialJsonValuesStillVaryTheKey()
    {
        using HttpRequestMessage first = CreateJsonRequest("{\"query\":\"first\"}");
        using HttpRequestMessage second = CreateJsonRequest("{\"query\":\"second\"}");

        string firstKey = CacheKeyBuilder.Build(ApiService.PatentsView, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.PatentsView, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.Not.EqualTo(secondKey));
    }

    [Test]
    public void EquivalentJsonNumberSpellingsAreCanonical()
    {
        using HttpRequestMessage first = CreateJsonRequest("{\"first\":1.0,\"second\":1E+00,\"zero\":-0.0}");
        using HttpRequestMessage second = CreateJsonRequest("{\"zero\":0,\"second\":1,\"first\":1}");

        string firstKey = CacheKeyBuilder.Build(ApiService.PatentsView, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.PatentsView, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void JsonArrayOrderRemainsSignificant()
    {
        using HttpRequestMessage first = CreateJsonRequest("{\"fields\":[\"a\",\"b\"]}");
        using HttpRequestMessage second = CreateJsonRequest("{\"fields\":[\"b\",\"a\"]}");

        string firstKey = CacheKeyBuilder.Build(ApiService.PatentsView, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.PatentsView, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.Not.EqualTo(secondKey));
    }

    [Test]
    public void FormFieldOrderingEncodingAndCredentialsAreCanonical()
    {
        using HttpRequestMessage first = CreateFormRequest("z=last&q=hello+world&api_key=first");
        using HttpRequestMessage second = CreateFormRequest("token=second&q=hello%20world&z=last");

        string firstKey = CacheKeyBuilder.Build(ApiService.OfficeAction, first, null, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.OfficeAction, second, null, CancellationToken.None);

        Assert.That(firstKey, Is.EqualTo(secondKey));
    }

    [Test]
    public void KnownSecretValuesAreNormalizedOutsideNamedCredentialFields()
    {
        using HttpRequestMessage first = CreateJsonRequest("{\"query\":\"first-secret\"}", "https://api.uspto.gov/files/first-secret?label=first-secret");
        using HttpRequestMessage second = CreateJsonRequest("{\"query\":\"second-secret\"}", "https://api.uspto.gov/files/second-secret?label=second-secret");

        string firstKey = CacheKeyBuilder.Build(ApiService.Odp, first, new string[] { "first-secret" }, CancellationToken.None);
        string secondKey = CacheKeyBuilder.Build(ApiService.Odp, second, new string[] { "second-secret" }, CancellationToken.None);
        string material = CacheKeyBuilder.BuildCanonicalMaterial(ApiService.Odp, first, new string[] { "first-secret" }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(firstKey, Is.EqualTo(secondKey));
            Assert.That(material, Does.Not.Contain("first-secret"));
        });
    }

    [Test]
    public void BinaryBodiesUseTheirContentDigest()
    {
        using HttpRequestMessage first = CreateBinaryRequest(new byte[] { 1, 2, 3 });
        using HttpRequestMessage same = CreateBinaryRequest(new byte[] { 1, 2, 3 });
        using HttpRequestMessage different = CreateBinaryRequest(new byte[] { 1, 2, 4 });

        string firstKey = CacheKeyBuilder.Build(ApiService.Odp, first, null, CancellationToken.None);
        string sameKey = CacheKeyBuilder.Build(ApiService.Odp, same, null, CancellationToken.None);
        string differentKey = CacheKeyBuilder.Build(ApiService.Odp, different, null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(firstKey, Is.EqualTo(sameKey));
            Assert.That(firstKey, Is.Not.EqualTo(differentKey));
        });
    }

    [Test]
    public void ContentTypeVariesOtherwiseIdenticalBodyKeys()
    {
        using HttpRequestMessage text = new HttpRequestMessage(HttpMethod.Post, "https://api.uspto.gov/resource");
        text.Content = new StringContent("same", Encoding.UTF8, UsptoMediaTypes.Text);
        using HttpRequestMessage html = new HttpRequestMessage(HttpMethod.Post, "https://api.uspto.gov/resource");
        html.Content = new StringContent("same", Encoding.UTF8, UsptoMediaTypes.Html);

        string textKey = CacheKeyBuilder.Build(ApiService.Odp, text, null, CancellationToken.None);
        string htmlKey = CacheKeyBuilder.Build(ApiService.Odp, html, null, CancellationToken.None);

        Assert.That(textKey, Is.Not.EqualTo(htmlKey));
    }

    [Test]
    public void KeyUsesStableSha256Format()
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.uspto.gov/resource");

        string key = CacheKeyBuilder.Build(ApiService.Odp, request, null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(key, Does.Match("^sha256:[0-9a-f]{64}$"));
            Assert.That(key, Is.EqualTo("sha256:84d7319b02c65b2bf2827bf0ff7b0adf17e8c51a97c231cb7de88887cedcc434"));
        });
    }

    [Test]
    public void CallerCancellationStopsKeyConstruction()
    {
        using HttpRequestMessage request = CreateJsonRequest("{\"query\":\"value\"}");
        using CancellationTokenSource caller = new CancellationTokenSource();
        caller.Cancel();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() => CacheKeyBuilder.Build(ApiService.Odp, request, null, caller.Token));

        Assert.That(exception.CancellationToken, Is.EqualTo(caller.Token));
    }

    [Test]
    public void RequestUriMustBeAbsolute()
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri("relative", UriKind.Relative));

        ValidationException exception = Assert.Throws<ValidationException>(() => CacheKeyBuilder.Build(ApiService.Odp, request, null, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("ABSOLUTE_URI_REQUIRED"));
    }

    private static HttpRequestMessage CreateJsonRequest(string json)
    {
        return CreateJsonRequest(json, "https://api.uspto.gov/search");
    }

    private static HttpRequestMessage CreateJsonRequest(string json, string uri)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Content = new StringContent(json, Encoding.UTF8, UsptoMediaTypes.Json);
        return request;
    }

    private static HttpRequestMessage CreateFormRequest(string form)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.uspto.gov/search");
        request.Content = new StringContent(form, Encoding.UTF8, UsptoMediaTypes.FormUrlEncoded);
        return request;
    }

    private static HttpRequestMessage CreateBinaryRequest(byte[] body)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.uspto.gov/upload");
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return request;
    }
}
