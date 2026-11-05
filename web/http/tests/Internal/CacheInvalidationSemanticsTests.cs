using System.Text;

namespace http.Tests;

public class CacheInvalidationSemanticsTests
{
    [Test]
    public void SuccessfulUnsafeRequestInvalidatesItsNormalizedTarget()
    {
        HttpRequest request = AbsoluteRequest(
            HttpMethods.Put,
            "HTTPS://Example.TEST:443/a/./b/../item?x=%7e");
        HttpResponse response = Response(HttpStatusCodes.NoContent);

        HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
            request,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Invalidate));
            Assert.That(result.InvalidationTargets,
                Is.EqualTo(new[] { "https://example.test/a/item?x=~" }));
            Assert.That(result.Response, Is.SameAs(response));
        });
    }

    [Test]
    public void RedirectionAfterUnsafeRequestAlsoInvalidatesTarget()
    {
        HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
            AbsoluteRequest(HttpMethods.Post, "https://example.test/form"),
            Response(HttpStatusCodes.SeeOther));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Invalidate));
            Assert.That(result.InvalidationTargets,
                Is.EqualTo(new[] { "https://example.test/form" }));
        });
    }

    [Test]
    public void SafeMethodsNeverCauseInvalidation()
    {
        string[] methods =
        [
            HttpMethods.Get,
            HttpMethods.Head,
            HttpMethods.Options,
            HttpMethods.Trace
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < methods.Length)
            {
                HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
                    AbsoluteRequest(methods[index], "https://example.test/item"),
                    Response(HttpStatusCodes.Ok));
                Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
                Assert.That(result.InvalidationTargets, Is.Empty);
                index = index + 1;
            }
        });
    }

    [Test]
    public void ErrorResponsesDoNotCauseInvalidation()
    {
        int[] statuses =
        [
            HttpStatusCodes.BadRequest,
            HttpStatusCodes.InternalServerError,
            700
        ];

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < statuses.Length)
            {
                HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
                    AbsoluteRequest(HttpMethods.Delete, "https://example.test/item"),
                    Response(statuses[index]));
                Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
                Assert.That(result.InvalidationTargets, Is.Empty);
                index = index + 1;
            }
        });
    }

    [Test]
    public void UnknownMethodIsConservativelyTreatedAsUnsafe()
    {
        HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
            AbsoluteRequest("CUSTOM", "https://example.test/item"),
            Response(HttpStatusCodes.Ok));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Invalidate));
            Assert.That(result.InvalidationTargets,
                Is.EqualTo(new[] { "https://example.test/item" }));
        });
    }

    [Test]
    public void SameOriginLocationFieldsAddNormalizedInvalidationTargets()
    {
        HttpRequest request = AbsoluteRequest(
            HttpMethods.Patch,
            "https://example.test/current");
        HttpResponse response = Response(HttpStatusCodes.Ok);
        AddField(
            response.Headers,
            HttpHeaderNames.Location,
            " https://EXAMPLE.test:443/next#part ");
        AddField(
            response.Headers,
            HttpHeaderNames.ContentLocation,
            "/content?q=%7e");

        HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
            request,
            response);

        Assert.That(result.InvalidationTargets, Is.EqualTo(new[]
        {
            "https://example.test/current",
            "https://example.test/next",
            "https://example.test/content?q=~"
        }));
    }

    [Test]
    public void RelativeLocationReferencesResolveAgainstTheRequestTarget()
    {
        HttpRequest request = OriginRequest(HttpMethods.Delete, "/a/b?old=1");
        HttpResponse response = Response(HttpStatusCodes.Ok);
        AddField(response.Headers, HttpHeaderNames.Location, "../next?q=1#part");
        AddField(response.Headers, HttpHeaderNames.ContentLocation, "child");

        HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
            request,
            response);

        Assert.That(result.InvalidationTargets, Is.EqualTo(new[]
        {
            "https://example.test/a/b?old=1",
            "https://example.test/next?q=1",
            "https://example.test/a/child"
        }));
    }

    [Test]
    public void CrossOriginLocationFieldsAreNotInvalidated()
    {
        HttpRequest request = AbsoluteRequest(
            HttpMethods.Delete,
            "https://example.test/item");
        HttpResponse response = Response(HttpStatusCodes.Ok);
        AddField(
            response.Headers,
            HttpHeaderNames.Location,
            "http://example.test/other");
        AddField(
            response.Headers,
            HttpHeaderNames.ContentLocation,
            "https://example.test:8443/other");

        HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
            request,
            response);

        Assert.That(result.InvalidationTargets,
            Is.EqualTo(new[] { "https://example.test/item" }));
    }

    [Test]
    public void DuplicateAndMalformedLocationFieldsAreIgnoredSafely()
    {
        HttpRequest request = AbsoluteRequest(
            HttpMethods.Delete,
            "https://example.test/item");
        HttpResponse response = Response(HttpStatusCodes.Ok);
        AddField(response.Headers, HttpHeaderNames.Location, "/first");
        AddField(response.Headers, HttpHeaderNames.Location, "/second");
        AddField(response.Headers, HttpHeaderNames.ContentLocation, "/bad#fragment");

        HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
            request,
            response);

        Assert.Multiple(() =>
        {
            Assert.That(result.InvalidationTargets,
                Is.EqualTo(new[] { "https://example.test/item" }));
            Assert.That(Values(response.Headers, HttpHeaderNames.Location),
                Is.EqualTo(new[] { "/first", "/second" }));
            Assert.That(Value(response.Headers, HttpHeaderNames.ContentLocation),
                Is.EqualTo("/bad#fragment"));
        });
    }

    [Test]
    public void EquivalentLocationTargetsAreDeduplicated()
    {
        HttpRequest request = AbsoluteRequest(
            HttpMethods.Put,
            "https://example.test/a/item");
        HttpResponse response = Response(HttpStatusCodes.Ok);
        AddField(
            response.Headers,
            HttpHeaderNames.Location,
            "https://EXAMPLE.test:443/a/./item#section");
        AddField(
            response.Headers,
            HttpHeaderNames.ContentLocation,
            "/a/item");

        HttpCacheDecision result = CacheInvalidationSemantics.Evaluate(
            request,
            response);

        Assert.That(result.InvalidationTargets,
            Is.EqualTo(new[] { "https://example.test/a/item" }));
    }

    [Test]
    public void InvalidInputsAreRejectedWithoutMutatingMessages()
    {
        HttpRequest invalidMethod = AbsoluteRequest(
            "bad method",
            "https://example.test/item");
        HttpRequest invalidTarget = AbsoluteRequest(
            HttpMethods.Put,
            "https://example.test/bad target");
        HttpResponse invalidStatus = Response(99);
        HttpResponse malformedField = Response(HttpStatusCodes.Ok);
        HttpHeaderField field = new HttpHeaderField();
        field.Name = "Bad Name";
        malformedField.Headers.Fields.Add(field);

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                CacheInvalidationSemantics.Evaluate(
                    invalidMethod,
                    Response(HttpStatusCodes.Ok)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheInvalidationSemantics.Evaluate(
                    invalidTarget,
                    Response(HttpStatusCodes.Ok)));
            Assert.Throws<HttpProtocolException>(() =>
                CacheInvalidationSemantics.Evaluate(
                    AbsoluteRequest(HttpMethods.Put, "https://example.test/item"),
                    invalidStatus));
            Assert.Throws<HttpProtocolException>(() =>
                CacheInvalidationSemantics.Evaluate(
                    AbsoluteRequest(HttpMethods.Put, "https://example.test/item"),
                    malformedField));
            Assert.That(malformedField.Headers.Fields, Has.Count.EqualTo(1));
        });
    }

    static HttpRequest AbsoluteRequest(string method, string target)
    {
        HttpRequest result = new HttpRequest();
        result.ProtocolVersion = HttpProtocolVersion.Http11;
        result.Method = method;
        result.Target = target;
        result.Scheme = "https";
        AddField(result.Headers, HttpHeaderNames.Host, "example.test");
        return result;
    }

    static HttpRequest OriginRequest(string method, string target)
    {
        HttpRequest result = new HttpRequest();
        result.ProtocolVersion = HttpProtocolVersion.Http11;
        result.Method = method;
        result.Target = target;
        result.Scheme = "https";
        AddField(result.Headers, HttpHeaderNames.Host, "example.test");
        return result;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse result = new HttpResponse();
        result.StatusCode = statusCode;
        return result;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static string Value(HttpHeaders headers, string name)
    {
        List<string> values = Values(headers, name);
        return values.Count == 0 ? "" : values[0];
    }

    static List<string> Values(HttpHeaders headers, string name)
    {
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(headers, name);
        List<string> result = new List<string>();
        int index = 0;
        while (index < fields.Count)
        {
            result.Add(Encoding.ASCII.GetString(fields[index].Value));
            index = index + 1;
        }

        return result;
    }
}
