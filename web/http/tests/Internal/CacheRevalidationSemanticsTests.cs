using System.Text;

namespace http.Tests;

public class CacheRevalidationSemanticsTests
{
    const string LastModified = "Sun, 06 Nov 1994 08:49:37 GMT";

    [Test]
    public void OrdinaryRequestPreservesSemanticsAndAddsBothValidators()
    {
        HttpRequest request = Request(HttpMethods.Get);
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Target = "/resource?x=1";
        request.TargetForm = HttpRequestTargetForm.Origin;
        request.Scheme = "https";
        request.Authority = "example.test";
        request.Path = "/resource";
        request.HasQuery = true;
        request.Query = "x=1";
        request.Content = [1, 2, 3];
        request.IsComplete = true;
        AddField(request.Headers, HttpHeaderNames.Authorization, "Bearer token");
        AddField(request.Headers, HttpHeaderNames.CacheControl, "max-age=0");
        AddField(request.Trailers, "Example-Trailer", "value");
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Headers, HttpHeaderNames.ETag, " W/\"v1\" ");
        AddField(response.Response.Headers, HttpHeaderNames.LastModified, LastModified);

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(request));
            Assert.That(result.ProtocolVersion, Is.EqualTo(request.ProtocolVersion));
            Assert.That(result.Method, Is.EqualTo(request.Method));
            Assert.That(result.Target, Is.EqualTo(request.Target));
            Assert.That(result.TargetForm, Is.EqualTo(request.TargetForm));
            Assert.That(result.Scheme, Is.EqualTo(request.Scheme));
            Assert.That(result.Authority, Is.EqualTo(request.Authority));
            Assert.That(result.Path, Is.EqualTo(request.Path));
            Assert.That(result.HasQuery, Is.True);
            Assert.That(result.Query, Is.EqualTo(request.Query));
            Assert.That(result.Content, Is.EqualTo(request.Content));
            Assert.That(result.Content, Is.Not.SameAs(request.Content));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(Value(result.Headers, HttpHeaderNames.Authorization),
                Is.EqualTo("Bearer token"));
            Assert.That(Value(result.Headers, HttpHeaderNames.CacheControl),
                Is.EqualTo("max-age=0"));
            Assert.That(Value(result.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("W/\"v1\""));
            Assert.That(Value(result.Headers, HttpHeaderNames.IfModifiedSince),
                Is.EqualTo(LastModified));
            Assert.That(Value(result.Trailers, "Example-Trailer"), Is.EqualTo("value"));
            Assert.That(Fields(request.Headers, HttpHeaderNames.IfNoneMatch), Is.Empty);
            Assert.That(Fields(request.Headers, HttpHeaderNames.IfModifiedSince), Is.Empty);
        });
    }

    [Test]
    public void StrongEntityTagIsSelectedForRangeRevalidation()
    {
        HttpRequest request = Request(HttpMethods.Get);
        AddField(request.Headers, HttpHeaderNames.Range, "bytes=0-9");
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Headers, HttpHeaderNames.ETag, "\"strong\"");
        AddField(response.Response.Headers, HttpHeaderNames.LastModified, LastModified);

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(result.Headers, HttpHeaderNames.IfRange),
                Is.EqualTo("\"strong\""));
            Assert.That(Fields(result.Headers, HttpHeaderNames.IfNoneMatch), Is.Empty);
            Assert.That(Fields(result.Headers, HttpHeaderNames.IfModifiedSince), Is.Empty);
        });
    }

    [Test]
    public void WeakRangeValidatorFallsBackToIfNoneMatch()
    {
        HttpRequest request = Request(HttpMethods.Get);
        AddField(request.Headers, HttpHeaderNames.Range, "bytes=0-9");
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Headers, HttpHeaderNames.ETag, "W/\"weak\"");
        AddField(response.Response.Headers, HttpHeaderNames.LastModified, LastModified);

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(result.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("W/\"weak\""));
            Assert.That(Fields(result.Headers, HttpHeaderNames.IfRange), Is.Empty);
            Assert.That(Fields(result.Headers, HttpHeaderNames.IfModifiedSince), Is.Empty);
        });
    }

    [Test]
    public void LastModifiedIsUsedForRangeWhenNoEntityTagExists()
    {
        HttpRequest request = Request(HttpMethods.Get);
        AddField(request.Headers, HttpHeaderNames.Range, "bytes=0-9");
        HttpCacheResponseContext response = ResponseContext();
        response.HasLastModified = true;
        response.LastModified = DateTimeOffset.Parse("1994-11-06T08:49:37+00:00");

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(result.Headers, HttpHeaderNames.IfRange),
                Is.EqualTo(LastModified));
            Assert.That(Fields(result.Headers, HttpHeaderNames.IfNoneMatch), Is.Empty);
        });
    }

    [Test]
    public void ExistingIfNoneMatchIsUnionedWithoutDuplicateWeakMatches()
    {
        HttpRequest request = Request(HttpMethods.Get);
        AddField(request.Headers, HttpHeaderNames.IfNoneMatch, "\"client\"");
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Headers, HttpHeaderNames.ETag, "W/\"stored\"");

        HttpRequest union = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);
        AddField(request.Headers, HttpHeaderNames.IfNoneMatch, "W/\"stored\"");
        HttpRequest duplicate = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(union.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("\"client\", W/\"stored\""));
            Assert.That(Value(duplicate.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("\"client\", W/\"stored\""));
            Assert.That(Fields(duplicate.Headers, HttpHeaderNames.IfNoneMatch),
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void StrongAndWeakFormsRemainDistinctWhenValidatorsAreUnioned()
    {
        HttpRequest request = Request(HttpMethods.Get);
        AddField(request.Headers, HttpHeaderNames.IfNoneMatch, "W/\"same\"");
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Headers, HttpHeaderNames.ETag, "\"same\"");

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);

        Assert.That(Value(result.Headers, HttpHeaderNames.IfNoneMatch),
            Is.EqualTo("W/\"same\", \"same\""));
    }

    [Test]
    public void StoredTrailerValidatorsParticipateInRevalidation()
    {
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Trailers, HttpHeaderNames.ETag, "\"trailer\"");
        AddField(
            response.Response.Trailers,
            HttpHeaderNames.LastModified,
            LastModified);

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(Request(HttpMethods.Get)),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(result.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("\"trailer\""));
            Assert.That(Value(result.Headers, HttpHeaderNames.IfModifiedSince),
                Is.EqualTo(LastModified));
        });
    }

    [Test]
    public void WildcardAndExistingDateConditionsRemainUnchanged()
    {
        HttpRequest wildcardRequest = Request(HttpMethods.Get);
        AddField(wildcardRequest.Headers, HttpHeaderNames.IfNoneMatch, "*");
        HttpRequest dateRequest = Request(HttpMethods.Get);
        AddField(
            dateRequest.Headers,
            HttpHeaderNames.IfModifiedSince,
            "Sun, 06 Nov 1994 08:00:00 GMT");
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Headers, HttpHeaderNames.ETag, "\"stored\"");
        AddField(response.Response.Headers, HttpHeaderNames.LastModified, LastModified);

        HttpRequest wildcard = CacheRevalidationSemantics.Build(
            RequestContext(wildcardRequest),
            response);
        HttpRequest date = CacheRevalidationSemantics.Build(
            RequestContext(dateRequest),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(wildcard.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("*"));
            Assert.That(Value(date.Headers, HttpHeaderNames.IfModifiedSince),
                Is.EqualTo("Sun, 06 Nov 1994 08:00:00 GMT"));
            Assert.That(Value(date.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("\"stored\""));
        });
    }

    [Test]
    public void ExistingIfRangeIsPreservedAndTagIsAddedSeparately()
    {
        HttpRequest request = Request(HttpMethods.Get);
        AddField(request.Headers, HttpHeaderNames.Range, "bytes=0-9");
        AddField(request.Headers, HttpHeaderNames.IfRange, "\"client\"");
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Headers, HttpHeaderNames.ETag, "\"stored\"");

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(result.Headers, HttpHeaderNames.IfRange),
                Is.EqualTo("\"client\""));
            Assert.That(Value(result.Headers, HttpHeaderNames.IfNoneMatch),
                Is.EqualTo("\"stored\""));
        });
    }

    [Test]
    public void StoredResponseFieldsOtherThanValidatorsAreNotForwarded()
    {
        HttpRequest request = Request(HttpMethods.Get);
        AddField(request.Headers, HttpHeaderNames.CacheControl, "no-cache");
        HttpCacheResponseContext response = ResponseContext();
        AddField(response.Response!.Headers, HttpHeaderNames.ETag, "\"stored\"");
        AddField(response.Response.Headers, HttpHeaderNames.Age, "60");
        AddField(response.Response.Headers, HttpHeaderNames.CacheControl, "max-age=120");
        AddField(response.Response.Headers, HttpHeaderNames.ContentLength, "10");
        AddField(response.Response.Headers, HttpHeaderNames.SetCookie, "a=b");
        AddField(response.Response.Headers, "X-Stored", "not-forwarded");

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(request),
            response);

        Assert.Multiple(() =>
        {
            Assert.That(Value(result.Headers, HttpHeaderNames.CacheControl),
                Is.EqualTo("no-cache"));
            Assert.That(Fields(result.Headers, HttpHeaderNames.ETag), Is.Empty);
            Assert.That(Fields(result.Headers, HttpHeaderNames.Age), Is.Empty);
            Assert.That(Fields(result.Headers, HttpHeaderNames.ContentLength), Is.Empty);
            Assert.That(Fields(result.Headers, HttpHeaderNames.SetCookie), Is.Empty);
            Assert.That(Fields(result.Headers, "X-Stored"), Is.Empty);
        });
    }

    [Test]
    public void NoStoredValidatorProducesAnIndependentUnconditionalRequest()
    {
        HttpRequest request = Request(HttpMethods.Head);
        AddField(request.Headers, HttpHeaderNames.Accept, "text/plain");

        HttpRequest result = CacheRevalidationSemantics.Build(
            RequestContext(request),
            ResponseContext());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(request));
            Assert.That(result.Method, Is.EqualTo(HttpMethods.Head));
            Assert.That(Value(result.Headers, HttpHeaderNames.Accept),
                Is.EqualTo("text/plain"));
            Assert.That(Fields(result.Headers, HttpHeaderNames.IfNoneMatch), Is.Empty);
            Assert.That(Fields(result.Headers, HttpHeaderNames.IfModifiedSince), Is.Empty);
        });
    }

    [Test]
    public void InvalidContextsMethodsAndStoredValidatorsAreRejected()
    {
        HttpCacheRequestContext missingRequest = new HttpCacheRequestContext();
        HttpCacheResponseContext missingResponse = new HttpCacheResponseContext();
        HttpCacheResponseContext malformedTag = ResponseContext();
        AddField(malformedTag.Response!.Headers, HttpHeaderNames.ETag, "not-a-tag");
        HttpCacheResponseContext duplicateDate = ResponseContext();
        AddField(duplicateDate.Response!.Headers, HttpHeaderNames.LastModified, LastModified);
        AddField(duplicateDate.Response.Headers, HttpHeaderNames.LastModified, LastModified);

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                CacheRevalidationSemantics.Build(
                    missingRequest,
                    ResponseContext()));
            Assert.Throws<HttpProtocolException>(() =>
                CacheRevalidationSemantics.Build(
                    RequestContext(Request(HttpMethods.Get)),
                    missingResponse));
            Assert.Throws<HttpProtocolException>(() =>
                CacheRevalidationSemantics.Build(
                    RequestContext(Request(HttpMethods.Post)),
                    ResponseContext()));
            Assert.Throws<HttpProtocolException>(() =>
                CacheRevalidationSemantics.Build(
                    RequestContext(Request(HttpMethods.Get)),
                    malformedTag));
            Assert.Throws<HttpProtocolException>(() =>
                CacheRevalidationSemantics.Build(
                    RequestContext(Request(HttpMethods.Get)),
                    duplicateDate));
        });
    }

    static HttpCacheRequestContext RequestContext(HttpRequest request)
    {
        HttpCacheRequestContext context = new HttpCacheRequestContext();
        context.Request = request;
        return context;
    }

    static HttpCacheResponseContext ResponseContext()
    {
        HttpCacheResponseContext context = new HttpCacheResponseContext();
        context.Response = new HttpResponse();
        return context;
    }

    static HttpRequest Request(string method)
    {
        HttpRequest request = new HttpRequest();
        request.Method = method;
        return request;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static List<HttpHeaderField> Fields(HttpHeaders headers, string name)
    {
        return HeaderStore.FindMatchingFields(headers, name);
    }

    static string Value(HttpHeaders headers, string name)
    {
        List<HttpHeaderField> fields = Fields(headers, name);
        return fields.Count == 0 ? "" : Encoding.ASCII.GetString(fields[0].Value);
    }
}
