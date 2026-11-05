using System.Text;

namespace http.Tests;

public class CacheResponseSemanticsTests
{
    const string BaseDate = "Sun, 06 Nov 1994 08:49:37 GMT";

    [Test]
    public void FullCachedResponseUpdatesAgeAndExposesOperationMetadata()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        request.CollapsedRequestCount = 3;
        HttpCacheResponseContext stored = ResponseContext([1, 2, 3, 4]);
        AddField(NotNull(stored).Headers, HttpHeaderNames.Age, "1");
        AddField(NotNull(stored).Headers, HttpHeaderNames.Connection, "X-Hop");
        AddField(NotNull(stored).Headers, "X-Hop", "removed");
        HttpCacheValidationResult validation = new HttpCacheValidationResult();
        validation.WasMerged = true;
        stored.ValidationResult = validation;

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(result.Response, Is.Not.SameAs(stored.Response));
            Assert.That(result.Response!.Content, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(result.Response.Content, Is.Not.SameAs(stored.Response!.Content));
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.Age), Is.EqualTo("30"));
            Assert.That(Values(result.Response.Headers, HttpHeaderNames.Connection), Is.Empty);
            Assert.That(Values(result.Response.Headers, "X-Hop"), Is.Empty);
            Assert.That(Value(stored.Response.Headers, HttpHeaderNames.Age), Is.EqualTo("1"));
            Assert.That(result.HasCurrentAge, Is.True);
            Assert.That(result.CurrentAge, Is.EqualTo(30));
            Assert.That(result.CollapsedRequestCount, Is.EqualTo(3));
            Assert.That(result.WasCollapsed, Is.True);
            Assert.That(result.WasRevalidated, Is.True);
            Assert.That(result.ValidationResult, Is.SameAs(validation));
            Assert.That(result.PreconditionDecision!.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.Proceed));
        });
    }

    [Test]
    public void HeadUsesGetMetadataWithoutStoredContentOrTrailers()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Head);
        HttpCacheResponseContext stored = ResponseContext([1, 2, 3, 4]);
        AddField(NotNull(stored).Headers, HttpHeaderNames.ContentLength, "4");
        AddField(NotNull(stored).Trailers, "X-Trailer", "stored");

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.ServeFresh));
            Assert.That(result.Response!.StatusCode, Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(result.Response.Content, Is.Empty);
            Assert.That(result.Response.Trailers.Fields, Is.Empty);
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ContentLength),
                Is.EqualTo("4"));
            Assert.That(stored.Response!.Content, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(stored.Response.Trailers.Fields, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void PermittedStaleResponseRetainsStaleOutcomeAndUpdatedAge()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        request.CacheControl.HasMaxStale = true;
        HttpCacheResponseContext stored = ResponseContext([1, 2]);
        stored.CurrentTime = stored.ResponseTime.AddSeconds(180);

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.ServeStale));
            Assert.That(result.CanServeStale, Is.True);
            Assert.That(result.CurrentAge, Is.EqualTo(180));
            Assert.That(Value(result.Response!.Headers, HttpHeaderNames.Age),
                Is.EqualTo("180"));
        });
    }

    [Test]
    public void MatchingEntityTagConditionBuildsContentFree304()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(request.Request!.Headers, HttpHeaderNames.IfNoneMatch, "W/\"tag\"");
        HttpCacheResponseContext stored = ResponseContext([1, 2, 3]);
        AddField(NotNull(stored).Headers, HttpHeaderNames.ETag, "\"tag\"");
        AddField(NotNull(stored).Headers, HttpHeaderNames.ContentLength, "3");
        AddField(NotNull(stored).Headers, HttpHeaderNames.ContentRange, "bytes 0-2/3");
        AddField(NotNull(stored).Trailers, "X-Trailer", "stored");

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.NotModified));
            Assert.That(result.Response.Content, Is.Empty);
            Assert.That(result.Response.Trailers.Fields, Is.Empty);
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ETag),
                Is.EqualTo("\"tag\""));
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ContentLength),
                Is.EqualTo("3"));
            Assert.That(Values(result.Response.Headers, HttpHeaderNames.ContentRange),
                Is.Empty);
            Assert.That(result.PreconditionDecision!.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.NotModified));
        });
    }

    [Test]
    public void NonmatchingConditionServesTheFullCachedResponse()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(request.Request!.Headers, HttpHeaderNames.IfNoneMatch, "\"other\"");
        HttpCacheResponseContext stored = ResponseContext([1, 2, 3]);
        AddField(NotNull(stored).Headers, HttpHeaderNames.ETag, "\"tag\"");

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response!.StatusCode, Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(result.Response.Content, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.PreconditionDecision!.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.Proceed));
        });
    }

    [Test]
    public void IfModifiedSinceCanBuildNotModifiedResponse()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(
            request.Request!.Headers,
            HttpHeaderNames.IfModifiedSince,
            BaseDate);
        HttpCacheResponseContext stored = ResponseContext([1]);
        stored.HasLastModified = true;
        stored.LastModified = DateTimeOffset.Parse("1994-11-06T08:49:37+00:00");
        AddField(NotNull(stored).Headers, HttpHeaderNames.LastModified, BaseDate);

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.NotModified));
            Assert.That(result.PreconditionDecision!.DecidingCondition,
                Is.EqualTo(HttpHeaderNames.IfModifiedSince));
        });
    }

    [Test]
    public void MalformedConditionalFieldMakesCachedResponseUnusable()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(request.Request!.Headers, HttpHeaderNames.IfNoneMatch, "bad-tag");
        HttpCacheResponseContext stored = ResponseContext([1]);
        AddField(NotNull(stored).Headers, HttpHeaderNames.ETag, "\"tag\"");

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(result.Response, Is.Null);
            Assert.That(result.PreconditionDecision!.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.Rejected));
        });
    }

    [Test]
    public void SingleRangeIsBuiltFromCompleteStoredContent()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(request.Request!.Headers, HttpHeaderNames.Range, "bytes=2-5");
        HttpCacheResponseContext stored = ResponseContext(
            Encoding.ASCII.GetBytes("0123456789"));

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.PartialContent));
            Assert.That(Encoding.ASCII.GetString(result.Response.Content),
                Is.EqualTo("2345"));
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ContentRange),
                Is.EqualTo("bytes 2-5/10"));
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ContentLength),
                Is.EqualTo("4"));
            Assert.That(result.RangeDecision!.Outcome,
                Is.EqualTo(HttpRangeOutcome.Satisfiable));
        });
    }

    [Test]
    public void MultipleRangesBuildMultipartResponse()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(request.Request!.Headers, HttpHeaderNames.Range, "bytes=0-1,8-9");
        HttpCacheResponseContext stored = ResponseContext(
            Encoding.ASCII.GetBytes("0123456789"));
        AddField(NotNull(stored).Headers, HttpHeaderNames.ContentType, "text/plain");

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.PartialContent));
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ContentType),
                Does.StartWith("multipart/byteranges; boundary="));
            Assert.That(Values(result.Response.Headers, HttpHeaderNames.ContentRange),
                Is.Empty);
            Assert.That(result.RangeDecision!.Multipart, Is.Not.Null);
            Assert.That(result.RangeDecision.Multipart!.Parts, Has.Count.EqualTo(2));
            Assert.That(result.Response.Content,
                Is.EqualTo(result.RangeDecision.Multipart.EncodedBody));
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ContentLength),
                Is.EqualTo(result.Response.Content.Length.ToString()));
        });
    }

    [Test]
    public void UnsatisfiableRangeBuilds416WithCompleteLength()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(request.Request!.Headers, HttpHeaderNames.Range, "bytes=20-30");
        HttpCacheResponseContext stored = ResponseContext(
            Encoding.ASCII.GetBytes("0123456789"));

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.RangeNotSatisfiable));
            Assert.That(result.Response.Content, Is.Empty);
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ContentRange),
                Is.EqualTo("bytes */10"));
            Assert.That(Value(result.Response.Headers, HttpHeaderNames.ContentLength),
                Is.EqualTo("0"));
            Assert.That(result.RangeDecision!.Outcome,
                Is.EqualTo(HttpRangeOutcome.Unsatisfiable));
        });
    }

    [Test]
    public void MalformedAndUnsupportedRangesAreIgnored()
    {
        HttpCacheRequestContext malformed = RequestContext(HttpMethods.Get);
        AddField(malformed.Request!.Headers, HttpHeaderNames.Range, "bytes=bad");
        HttpCacheRequestContext unsupported = RequestContext(HttpMethods.Get);
        AddField(unsupported.Request!.Headers, HttpHeaderNames.Range, "items=0-1");
        HttpCacheResponseContext first = ResponseContext([1, 2, 3]);
        HttpCacheResponseContext second = ResponseContext([1, 2, 3]);

        HttpCacheDecision malformedResult = CacheResponseSemantics.Build(
            malformed,
            first);
        HttpCacheDecision unsupportedResult = CacheResponseSemantics.Build(
            unsupported,
            second);

        Assert.Multiple(() =>
        {
            Assert.That(malformedResult.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(malformedResult.RangeDecision!.Outcome,
                Is.EqualTo(HttpRangeOutcome.Malformed));
            Assert.That(unsupportedResult.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(unsupportedResult.RangeDecision!.Outcome,
                Is.EqualTo(HttpRangeOutcome.UnsupportedUnit));
        });
    }

    [Test]
    public void FailedIfRangeServesFullResponseWithoutRangeProcessing()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(request.Request!.Headers, HttpHeaderNames.Range, "bytes=0-1");
        AddField(request.Request.Headers, HttpHeaderNames.IfRange, "\"other\"");
        HttpCacheResponseContext stored = ResponseContext([1, 2, 3]);
        AddField(NotNull(stored).Headers, HttpHeaderNames.ETag, "\"stored\"");

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response!.StatusCode, Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(result.Response.Content, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.PreconditionDecision!.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(result.RangeDecision, Is.Null);
        });
    }

    [Test]
    public void IncompleteStoredContentRequiresValidationForRange()
    {
        HttpCacheRequestContext request = RequestContext(HttpMethods.Get);
        AddField(request.Request!.Headers, HttpHeaderNames.Range, "bytes=0-1");
        HttpCacheResponseContext stored = ResponseContext([1, 2, 3]);
        stored.IsComplete = false;

        HttpCacheDecision result = CacheResponseSemantics.Build(request, stored);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Validate));
            Assert.That(result.RequiresValidation, Is.True);
            Assert.That(result.Response, Is.Null);
        });
    }

    [Test]
    public void IncompatibleMethodAndInvalidCollapseCountDoNotBuildResponse()
    {
        HttpCacheRequestContext post = RequestContext(HttpMethods.Post);
        HttpCacheResponseContext stored = ResponseContext([1]);
        HttpCacheDecision postResult = CacheResponseSemantics.Build(post, stored);
        HttpCacheRequestContext invalid = RequestContext(HttpMethods.Get);
        invalid.CollapsedRequestCount = 0;

        Assert.Multiple(() =>
        {
            Assert.That(postResult.Outcome, Is.EqualTo(HttpCacheOutcome.Unusable));
            Assert.That(postResult.Response, Is.Null);
            Assert.Throws<HttpProtocolException>(() =>
                CacheResponseSemantics.Build(invalid, stored));
        });
    }

    static HttpCacheRequestContext RequestContext(string method)
    {
        HttpCacheRequestContext result = new HttpCacheRequestContext();
        result.Request = new HttpRequest();
        result.Request.Method = method;
        return result;
    }

    static HttpCacheResponseContext ResponseContext(byte[] content)
    {
        DateTimeOffset start =
            DateTimeOffset.Parse("1994-11-06T08:49:37+00:00");
        HttpCacheResponseContext result = new HttpCacheResponseContext();
        result.Response = new HttpResponse();
        result.Response.StatusCode = HttpStatusCodes.Ok;
        result.Response.Content = content;
        result.Response.IsComplete = true;
        result.CacheControl.HasMaxAge = true;
        result.CacheControl.MaxAge = 120;
        result.RequestTime = start;
        result.ResponseTime = start;
        result.StoredTime = start;
        result.CurrentTime = start.AddSeconds(30);
        result.HasDate = true;
        result.Date = start;
        result.IsComplete = true;
        AddField(result.Response.Headers, HttpHeaderNames.Date, BaseDate);
        return result;
    }

    static HttpResponse NotNull(HttpCacheResponseContext context)
    {
        return context.Response!;
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
