using System.Text;

namespace http.Tests;

public class CacheKeySemanticsTests
{
    [Test]
    public void PrimaryKeyNormalizesEquivalentTargetUris()
    {
        HttpRequest origin = Request(
            HttpMethods.Get,
            "/a/./x/../%7e?q=%41%2f",
            "HTTP",
            "%65XAMPLE.test:080");
        HttpRequest absolute = Request(
            HttpMethods.Get,
            "http://example.test/a/~?q=A%2F",
            null,
            null);

        HttpCacheDecision originKey = Build(origin, new HttpResponse());
        HttpCacheDecision absoluteKey = Build(absolute, new HttpResponse());

        Assert.Multiple(() =>
        {
            Assert.That(originKey.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(originKey.PrimaryKey,
                Is.EqualTo("GET http://example.test/a/~?q=A%2F"));
            Assert.That(originKey.PrimaryKey, Is.EqualTo(absoluteKey.PrimaryKey));
            Assert.That(originKey.SecondaryKey, Is.EqualTo("V1;0;"));
        });
    }

    [Test]
    public void PrimaryKeyPreservesSemanticPathAndQueryDifferences()
    {
        HttpCacheDecision encodedSlash = Build(
            Request(HttpMethods.Get, "/a%2fb", "https", "example.test"),
            new HttpResponse());
        HttpCacheDecision pathSlash = Build(
            Request(HttpMethods.Get, "/a/b", "https", "example.test"),
            new HttpResponse());
        HttpCacheDecision absentQuery = Build(
            Request(HttpMethods.Get, "/a", "https", "example.test"),
            new HttpResponse());
        HttpCacheDecision emptyQuery = Build(
            Request(HttpMethods.Get, "/a?", "https", "example.test"),
            new HttpResponse());

        Assert.Multiple(() =>
        {
            Assert.That(encodedSlash.PrimaryKey,
                Is.EqualTo("GET https://example.test/a%2Fb"));
            Assert.That(encodedSlash.PrimaryKey, Is.Not.EqualTo(pathSlash.PrimaryKey));
            Assert.That(absentQuery.PrimaryKey, Is.Not.EqualTo(emptyQuery.PrimaryKey));
        });
    }

    [Test]
    public void PrimaryKeysRetainMethodAndCompatibilityIsAsymmetric()
    {
        HttpCacheDecision get = Build(
            Request(HttpMethods.Get, "/items", "https", "example.test"),
            new HttpResponse());
        HttpCacheDecision head = Build(
            Request(HttpMethods.Head, "/items", "https", "example.test"),
            new HttpResponse());
        HttpCacheDecision post = Build(
            Request(HttpMethods.Post, "/items", "https", "example.test"),
            new HttpResponse());
        HttpCacheDecision put = Build(
            Request(HttpMethods.Put, "/items", "https", "example.test"),
            new HttpResponse());

        Assert.Multiple(() =>
        {
            Assert.That(get.PrimaryKey,
                Is.EqualTo("GET https://example.test/items"));
            Assert.That(head.PrimaryKey,
                Is.EqualTo("HEAD https://example.test/items"));
            Assert.That(post.PrimaryKey,
                Is.EqualTo("POST https://example.test/items"));
            Assert.That(post.PrimaryKey, Is.Not.EqualTo(get.PrimaryKey));
            Assert.That(put.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(put.PrimaryKey, Is.Null);
            Assert.That(CacheKeySemantics.AreMethodsCompatible(
                HttpMethods.Get,
                HttpMethods.Head), Is.True);
            Assert.That(CacheKeySemantics.AreMethodsCompatible(
                HttpMethods.Head,
                HttpMethods.Get), Is.False);
            Assert.That(CacheKeySemantics.AreMethodsCompatible(
                HttpMethods.Post,
                HttpMethods.Get), Is.True);
            Assert.That(CacheKeySemantics.AreMethodsCompatible(
                HttpMethods.Post,
                HttpMethods.Head), Is.True);
            Assert.That(CacheKeySemantics.AreMethodsCompatible(
                HttpMethods.Post,
                HttpMethods.Post), Is.False);
            Assert.That(CacheKeySemantics.AreMethodsCompatible(
                HttpMethods.Get,
                HttpMethods.Post), Is.False);
            Assert.That(CacheKeySemantics.AreMethodsCompatible(
                HttpMethods.Put,
                HttpMethods.Put), Is.False);
        });
    }

    [Test]
    public void VarySecondaryKeysMatchNormalizedSelectingFields()
    {
        HttpResponse response = new HttpResponse();
        AddField(
            response.Headers,
            HttpHeaderNames.Vary,
            "X-Mode, Accept-Encoding");
        HttpRequest storedRequest = Request(
            HttpMethods.Get,
            "/items",
            "https",
            "example.test");
        AddField(storedRequest.Headers, HttpHeaderNames.AcceptEncoding, "gzip");
        AddField(storedRequest.Headers, HttpHeaderNames.AcceptEncoding, "br");
        AddField(storedRequest.Headers, "X-Mode", " fast ");
        HttpRequest presentedRequest = Request(
            HttpMethods.Head,
            "/items",
            "HTTPS",
            "EXAMPLE.TEST:443");
        AddField(presentedRequest.Headers, "accept-encoding", "gzip, br");
        AddField(presentedRequest.Headers, "x-mode", "fast");

        HttpCacheDecision stored = Build(storedRequest, response);
        HttpCacheDecision presented = Build(presentedRequest, response);

        Assert.Multiple(() =>
        {
            Assert.That(stored.SecondaryKey, Is.EqualTo(presented.SecondaryKey));
            Assert.That(CacheKeySemantics.MatchesVariant(stored, presented), Is.True);
            Assert.That(stored.Response, Is.SameAs(response));
        });
    }

    [Test]
    public void VariantMatchingSelectsOnlyTheMatchingStoredVariant()
    {
        HttpResponse response = new HttpResponse();
        AddField(response.Headers, HttpHeaderNames.Vary, HttpHeaderNames.AcceptEncoding);
        HttpRequest gzipRequest = Request(
            HttpMethods.Get,
            "/asset",
            "https",
            "example.test");
        AddField(gzipRequest.Headers, HttpHeaderNames.AcceptEncoding, "gzip");
        HttpRequest brRequest = Request(
            HttpMethods.Get,
            "/asset",
            "https",
            "example.test");
        AddField(brRequest.Headers, HttpHeaderNames.AcceptEncoding, "br");

        HttpCacheDecision gzip = Build(gzipRequest, response);
        HttpCacheDecision br = Build(brRequest, response);
        HttpCacheDecision presented = Build(brRequest, response);

        Assert.Multiple(() =>
        {
            Assert.That(CacheKeySemantics.MatchesVariant(gzip, presented), Is.False);
            Assert.That(CacheKeySemantics.MatchesVariant(br, presented), Is.True);
        });
    }

    [Test]
    public void StoredHeadResponseCannotMatchPresentedGet()
    {
        HttpResponse response = new HttpResponse();
        HttpCacheDecision storedHead = Build(
            Request(HttpMethods.Head, "/items", "https", "example.test"),
            response);
        HttpCacheDecision presentedGet = Build(
            Request(HttpMethods.Get, "/items", "https", "example.test"),
            response);

        Assert.That(
            CacheKeySemantics.MatchesVariant(storedHead, presentedGet),
            Is.False);
    }

    [Test]
    public void StoredPostResponseMatchesLaterRetrievalButNotPresentedPost()
    {
        HttpResponse response = new HttpResponse();
        HttpCacheDecision storedPost = Build(
            Request(HttpMethods.Post, "/items", "https", "example.test"),
            response);
        HttpCacheDecision presentedGet = Build(
            Request(HttpMethods.Get, "/items", "https", "example.test"),
            response);
        HttpCacheDecision presentedHead = Build(
            Request(HttpMethods.Head, "/items", "https", "example.test"),
            response);
        HttpCacheDecision presentedPost = Build(
            Request(HttpMethods.Post, "/items", "https", "example.test"),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(
                CacheKeySemantics.MatchesVariant(storedPost, presentedGet),
                Is.True);
            Assert.That(
                CacheKeySemantics.MatchesVariant(storedPost, presentedHead),
                Is.True);
            Assert.That(
                CacheKeySemantics.MatchesVariant(storedPost, presentedPost),
                Is.False);
        });
    }

    [Test]
    public void VaryWildcardRefusesStorageAndNeverMatches()
    {
        HttpResponse response = new HttpResponse();
        AddField(response.Headers, HttpHeaderNames.Vary, "*");
        HttpCacheDecision first = Build(
            Request(HttpMethods.Get, "/items", "https", "example.test"),
            response);
        HttpCacheDecision second = Build(
            Request(HttpMethods.Get, "/items", "https", "example.test"),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(first.PrimaryKey,
                Is.EqualTo("GET https://example.test/items"));
            Assert.That(first.SecondaryKey, Is.EqualTo("V1;*"));
            Assert.That(CacheKeySemantics.MatchesVariant(first, second), Is.False);
        });
    }

    [Test]
    public void MalformedVaryIsRejected()
    {
        HttpResponse response = new HttpResponse();
        AddField(response.Headers, HttpHeaderNames.Vary, "Accept Encoding");

        Assert.Throws<HttpFieldException>(() => Build(
            Request(HttpMethods.Get, "/items", "https", "example.test"),
            response));
    }

    static HttpCacheDecision Build(HttpRequest request, HttpResponse response)
    {
        HttpCacheRequestContext requestContext = new HttpCacheRequestContext();
        requestContext.Request = request;
        HttpCacheResponseContext responseContext = new HttpCacheResponseContext();
        responseContext.Response = response;
        return CacheKeySemantics.Build(requestContext, responseContext);
    }

    static HttpRequest Request(
        string method,
        string target,
        string? scheme,
        string? authority)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http2;
        request.Method = method;
        request.Target = target;
        request.Scheme = scheme;
        request.Authority = authority;
        return request;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }
}
