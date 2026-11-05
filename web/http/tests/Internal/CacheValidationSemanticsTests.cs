using System.Text;

namespace http.Tests;

public class CacheValidationSemanticsTests
{
    const string FirstDate = "Sun, 06 Nov 1994 08:49:37 GMT";
    const string SecondDate = "Sun, 06 Nov 1994 09:49:37 GMT";
    const string ExpiresDate = "Sun, 06 Nov 1994 10:49:37 GMT";

    [Test]
    public void MergeReplacesMetadataAndRetainsStoredResponseSemantics()
    {
        HttpCacheResponseContext context = Context("\"same\"");
        HttpResponse stored = context.Response!;
        stored.ProtocolVersion = HttpProtocolVersion.Http11;
        stored.StatusCode = HttpStatusCodes.Ok;
        stored.HasReasonPhrase = true;
        stored.ReasonPhrase = Encoding.ASCII.GetBytes("OK");
        stored.Content = [1, 2, 3];
        stored.IsComplete = true;
        AddField(stored.Headers, HttpHeaderNames.ContentLength, "3");
        AddField(stored.Headers, HttpHeaderNames.CacheControl, "max-age=10");
        AddField(stored.Headers, "X-Metadata", "old");
        AddField(stored.Trailers, "X-Trailer", "retained");
        HttpResponse validation = NotModified("\"same\"");
        AddField(validation.Headers, HttpHeaderNames.ContentLength, "999");
        AddField(validation.Headers, HttpHeaderNames.CacheControl, "max-age=120");
        AddField(validation.Headers, "X-Metadata", "first");
        AddField(validation.Headers, "X-Metadata", "second");

        HttpCacheValidationResult result = CacheValidationSemantics.MergeNotModified(
            context,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasMerged, Is.True);
            Assert.That(result.Response, Is.SameAs(stored));
            Assert.That(result.AffectedResponses, Is.EqualTo(new[] { stored }));
            Assert.That(context.ValidationResult, Is.SameAs(result));
            Assert.That(stored.StatusCode, Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(stored.ProtocolVersion, Is.EqualTo(HttpProtocolVersion.Http11));
            Assert.That(stored.ReasonPhrase, Is.EqualTo(Encoding.ASCII.GetBytes("OK")));
            Assert.That(stored.Content, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(stored.IsComplete, Is.True);
            Assert.That(Values(stored.Headers, HttpHeaderNames.ContentLength),
                Is.EqualTo(new[] { "3" }));
            Assert.That(Values(stored.Headers, HttpHeaderNames.CacheControl),
                Is.EqualTo(new[] { "max-age=120" }));
            Assert.That(Values(stored.Headers, "X-Metadata"),
                Is.EqualTo(new[] { "first", "second" }));
            Assert.That(Value(stored.Trailers, "X-Trailer"), Is.EqualTo("retained"));
            Assert.That(Values(validation.Headers, "X-Metadata"),
                Is.EqualTo(new[] { "first", "second" }));
        });
    }

    [Test]
    public void HopByHopAndUnsafeUpdateFieldsAreExcluded()
    {
        HttpCacheResponseContext context = Context("\"same\"");
        HttpResponse stored = context.Response!;
        stored.Content = [1, 2, 3];
        AddField(stored.Headers, HttpHeaderNames.ContentLength, "3");
        AddField(stored.Headers, HttpHeaderNames.ContentRange, "bytes 0-2/10");
        HttpResponse validation = NotModified("\"same\"");
        AddField(validation.Headers, HttpHeaderNames.Connection, "X-Hop");
        AddField(validation.Headers, "X-Hop", "secret");
        AddField(validation.Headers, HttpHeaderNames.KeepAlive, "timeout=5");
        AddField(validation.Headers, HttpHeaderNames.TE, "trailers");
        AddField(validation.Headers, HttpHeaderNames.TransferEncoding, "chunked");
        AddField(validation.Headers, HttpHeaderNames.Upgrade, "example/1");
        AddField(validation.Headers, HttpHeaderNames.ProxyAuthenticate, "Basic");
        AddField(validation.Headers, HttpHeaderNames.ProxyAuthenticationInfo, "nextnonce=x");
        AddField(validation.Headers, HttpHeaderNames.ProxyAuthorization, "Basic abc");
        AddField(validation.Headers, HttpHeaderNames.ContentLength, "8");
        AddField(validation.Headers, HttpHeaderNames.ContentRange, "bytes 0-7/10");

        HttpCacheValidationResult result = CacheValidationSemantics.MergeNotModified(
            context,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasMerged, Is.True);
            Assert.That(Values(stored.Headers, HttpHeaderNames.Connection), Is.Empty);
            Assert.That(Values(stored.Headers, "X-Hop"), Is.Empty);
            Assert.That(Values(stored.Headers, HttpHeaderNames.KeepAlive), Is.Empty);
            Assert.That(Values(stored.Headers, HttpHeaderNames.TE), Is.Empty);
            Assert.That(Values(stored.Headers, HttpHeaderNames.TransferEncoding), Is.Empty);
            Assert.That(Values(stored.Headers, HttpHeaderNames.Upgrade), Is.Empty);
            Assert.That(Values(stored.Headers, HttpHeaderNames.ProxyAuthenticate), Is.Empty);
            Assert.That(Values(stored.Headers, HttpHeaderNames.ProxyAuthenticationInfo), Is.Empty);
            Assert.That(Values(stored.Headers, HttpHeaderNames.ProxyAuthorization), Is.Empty);
            Assert.That(Value(stored.Headers, HttpHeaderNames.ContentLength), Is.EqualTo("3"));
            Assert.That(Value(stored.Headers, HttpHeaderNames.ContentRange),
                Is.EqualTo("bytes 0-2/10"));
        });
    }

    [Test]
    public void StrongValidatorUpdatesEveryStronglyMatchingStoredResponse()
    {
        HttpCacheResponseContext first = Context("\"shared\"");
        HttpCacheResponseContext second = Context("\"shared\"");
        HttpCacheResponseContext different = Context("\"different\"");
        first.MatchingResponseContexts.Add(second);
        first.MatchingResponseContexts.Add(different);
        AddField(NotNull(first).Headers, "X-State", "first-old");
        AddField(NotNull(second).Headers, "X-State", "second-old");
        AddField(NotNull(different).Headers, "X-State", "different-old");
        HttpResponse validation = NotModified("\"shared\"");
        AddField(validation.Headers, "X-State", "new");

        HttpCacheValidationResult result = CacheValidationSemantics.MergeNotModified(
            first,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasMerged, Is.True);
            Assert.That(result.AffectedResponses,
                Is.EqualTo(new[] { NotNull(first), NotNull(second) }));
            Assert.That(Value(NotNull(first).Headers, "X-State"), Is.EqualTo("new"));
            Assert.That(Value(NotNull(second).Headers, "X-State"), Is.EqualTo("new"));
            Assert.That(Value(NotNull(different).Headers, "X-State"),
                Is.EqualTo("different-old"));
        });
    }

    [Test]
    public void ConflictingStrongValidatorLeavesCandidatesUntouched()
    {
        HttpCacheResponseContext context = Context("\"stored\"");
        AddField(NotNull(context).Headers, "X-State", "old");
        HttpResponse validation = NotModified("\"other\"");
        AddField(validation.Headers, "X-State", "new");

        HttpCacheValidationResult result = CacheValidationSemantics.MergeNotModified(
            context,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasMerged, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.AffectedResponses, Is.Empty);
            Assert.That(Value(NotNull(context).Headers, "X-State"), Is.EqualTo("old"));
        });
    }

    [Test]
    public void WeakValidatorUpdatesOnlyTheMostRecentMatchingResponse()
    {
        HttpCacheResponseContext older = Context("W/\"shared\"");
        HttpCacheResponseContext newer = Context("\"shared\"");
        older.HasDate = true;
        older.Date = DateTimeOffset.Parse("1994-11-06T08:49:37+00:00");
        newer.HasDate = true;
        newer.Date = DateTimeOffset.Parse("1994-11-06T09:49:37+00:00");
        older.MatchingResponseContexts.Add(newer);
        AddField(NotNull(older).Headers, "X-State", "older");
        AddField(NotNull(newer).Headers, "X-State", "newer");
        HttpResponse validation = NotModified("W/\"shared\"");
        AddField(validation.Headers, "X-State", "validated");

        HttpCacheValidationResult result = CacheValidationSemantics.MergeNotModified(
            older,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.AffectedResponses, Is.EqualTo(new[] { NotNull(newer) }));
            Assert.That(Value(NotNull(older).Headers, "X-State"), Is.EqualTo("older"));
            Assert.That(Value(NotNull(newer).Headers, "X-State"),
                Is.EqualTo("validated"));
        });
    }

    [Test]
    public void ConflictingWeakValidatorsDoNotIdentifyAStoredResponse()
    {
        HttpCacheResponseContext context = Context("W/\"shared\"");
        AddField(NotNull(context).Headers, HttpHeaderNames.LastModified, FirstDate);
        HttpResponse validation = NotModified("W/\"shared\"");
        AddField(validation.Headers, HttpHeaderNames.LastModified, SecondDate);

        HttpCacheValidationResult result = CacheValidationSemantics.MergeNotModified(
            context,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasMerged, Is.False);
            Assert.That(Value(NotNull(context).Headers, HttpHeaderNames.LastModified),
                Is.EqualTo(FirstDate));
        });
    }

    [Test]
    public void LastModifiedSelectsTheMostRecentMatchingStoredResponse()
    {
        HttpCacheResponseContext older = Context(null);
        HttpCacheResponseContext newer = Context(null);
        older.ResponseTime = DateTimeOffset.Parse("1994-11-06T08:00:00+00:00");
        newer.ResponseTime = DateTimeOffset.Parse("1994-11-06T09:00:00+00:00");
        older.MatchingResponseContexts.Add(newer);
        AddField(NotNull(older).Headers, HttpHeaderNames.LastModified, FirstDate);
        AddField(NotNull(newer).Headers, HttpHeaderNames.LastModified, FirstDate);
        AddField(NotNull(older).Headers, "X-State", "older");
        AddField(NotNull(newer).Headers, "X-State", "newer");
        HttpResponse validation = NotModified(null);
        AddField(validation.Headers, HttpHeaderNames.LastModified, FirstDate);
        AddField(validation.Headers, "X-State", "validated");

        HttpCacheValidationResult result = CacheValidationSemantics.MergeNotModified(
            older,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.AffectedResponses, Is.EqualTo(new[] { NotNull(newer) }));
            Assert.That(Value(NotNull(older).Headers, "X-State"), Is.EqualTo("older"));
            Assert.That(Value(NotNull(newer).Headers, "X-State"),
                Is.EqualTo("validated"));
        });
    }

    [Test]
    public void ValidatorlessMergeRequiresOneValidatorlessStoredResponse()
    {
        HttpCacheResponseContext single = Context(null);
        HttpResponse validation = NotModified(null);
        AddField(validation.Headers, "X-State", "new");
        HttpCacheValidationResult singleResult =
            CacheValidationSemantics.MergeNotModified(single, validation);

        HttpCacheResponseContext multiple = Context(null);
        multiple.MatchingResponseContexts.Add(Context(null));
        HttpCacheValidationResult multipleResult =
            CacheValidationSemantics.MergeNotModified(multiple, validation);

        HttpCacheResponseContext validated = Context("\"stored\"");
        HttpCacheValidationResult validatedResult =
            CacheValidationSemantics.MergeNotModified(validated, validation);

        Assert.Multiple(() =>
        {
            Assert.That(singleResult.WasMerged, Is.True);
            Assert.That(Value(NotNull(single).Headers, "X-State"), Is.EqualTo("new"));
            Assert.That(multipleResult.WasMerged, Is.False);
            Assert.That(validatedResult.WasMerged, Is.False);
        });
    }

    [Test]
    public void MergeRefreshesParsedMetadataAndValidationTimestamps()
    {
        HttpCacheResponseContext context = Context("\"same\"");
        DateTimeOffset validationTime =
            DateTimeOffset.Parse("1994-11-06T11:49:37+00:00");
        context.CurrentTime = validationTime;
        HttpResponse validation = NotModified("\"same\"");
        AddField(validation.Headers, HttpHeaderNames.CacheControl, "max-age=120");
        AddField(validation.Headers, HttpHeaderNames.Date, FirstDate);
        AddField(validation.Headers, HttpHeaderNames.Age, "7");
        AddField(validation.Headers, HttpHeaderNames.Expires, ExpiresDate);
        AddField(validation.Headers, HttpHeaderNames.LastModified, SecondDate);

        HttpCacheValidationResult result = CacheValidationSemantics.MergeNotModified(
            context,
            validation);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasMerged, Is.True);
            Assert.That(context.CacheControl.HasMaxAge, Is.True);
            Assert.That(context.CacheControl.MaxAge, Is.EqualTo(120));
            Assert.That(context.HasDate, Is.True);
            Assert.That(context.Date,
                Is.EqualTo(DateTimeOffset.Parse("1994-11-06T08:49:37+00:00")));
            Assert.That(context.HasAge, Is.True);
            Assert.That(context.Age, Is.EqualTo(7));
            Assert.That(context.HasExpires, Is.True);
            Assert.That(context.Expires,
                Is.EqualTo(DateTimeOffset.Parse("1994-11-06T10:49:37+00:00")));
            Assert.That(context.HasLastModified, Is.True);
            Assert.That(context.LastModified,
                Is.EqualTo(DateTimeOffset.Parse("1994-11-06T09:49:37+00:00")));
            Assert.That(context.RequestTime, Is.EqualTo(validationTime));
            Assert.That(context.ResponseTime, Is.EqualTo(validationTime));
            Assert.That(context.StoredTime, Is.EqualTo(validationTime));
        });
    }

    [Test]
    public void InvalidValidationInputsAreRejectedBeforeMutation()
    {
        HttpCacheResponseContext context = Context("\"same\"");
        AddField(NotNull(context).Headers, "X-State", "old");
        HttpResponse wrongStatus = NotModified("\"same\"");
        wrongStatus.StatusCode = HttpStatusCodes.Ok;
        HttpResponse content = NotModified("\"same\"");
        content.Content = [1];
        HttpResponse duplicateTag = NotModified("\"same\"");
        AddField(duplicateTag.Headers, HttpHeaderNames.ETag, "\"same\"");

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                CacheValidationSemantics.MergeNotModified(context, wrongStatus));
            Assert.Throws<HttpProtocolException>(() =>
                CacheValidationSemantics.MergeNotModified(context, content));
            Assert.Throws<HttpProtocolException>(() =>
                CacheValidationSemantics.MergeNotModified(context, duplicateTag));
            Assert.That(Value(NotNull(context).Headers, "X-State"), Is.EqualTo("old"));
        });
    }

    static HttpCacheResponseContext Context(string? entityTag)
    {
        HttpCacheResponseContext result = new HttpCacheResponseContext();
        result.Response = new HttpResponse();
        result.Response.StatusCode = HttpStatusCodes.Ok;
        if (entityTag != null)
        {
            AddField(result.Response.Headers, HttpHeaderNames.ETag, entityTag);
        }

        return result;
    }

    static HttpResponse NotModified(string? entityTag)
    {
        HttpResponse result = new HttpResponse();
        result.StatusCode = HttpStatusCodes.NotModified;
        if (entityTag != null)
        {
            AddField(result.Headers, HttpHeaderNames.ETag, entityTag);
        }

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
