using System.Text;

namespace http.Tests;

public class HttpFieldValueCodecTests
{
    [Test]
    public void HttpDateOperationsAcceptRfcFormatsAndEmitImfFixdate()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        DateTimeOffset expected = new DateTimeOffset(
            1994, 11, 6, 8, 49, 37, TimeSpan.Zero);

        DateTimeOffset imf = codec.ParseHttpDate(
            "Sun, 06 Nov 1994 08:49:37 GMT"u8.ToArray());
        DateTimeOffset obsolete = codec.ParseHttpDate(
            "Sunday, 06-Nov-94 08:49:37 GMT"u8.ToArray());
        DateTimeOffset asctime = codec.ParseHttpDate(
            "Sun Nov  6 08:49:37 1994"u8.ToArray());
        byte[] serialized = codec.SerializeHttpDate(
            new DateTimeOffset(1994, 11, 6, 9, 49, 37, 999, TimeSpan.FromHours(1)));

        Assert.Multiple(() =>
        {
            Assert.That(imf, Is.EqualTo(expected));
            Assert.That(obsolete, Is.EqualTo(expected));
            Assert.That(asctime, Is.EqualTo(expected));
            Assert.That(Text(serialized), Is.EqualTo("Sun, 06 Nov 1994 08:49:37 GMT"));
        });
    }

    [Test]
    public void MediaTypeOperationsCoverWildcardsQuotedValuesAndCanonicalOutput()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        HttpMediaType parsed = codec.ParseMediaType(
            "text/*; charset=\"utf-8\"; level=1"u8.ToArray());
        byte[] serialized = codec.SerializeMediaType(parsed);
        HttpMediaType any = codec.ParseMediaType("*/*"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Type, Is.EqualTo("text"));
            Assert.That(parsed.IsSubtypeWildcard, Is.True);
            Assert.That(parsed.Parameters, Has.Count.EqualTo(2));
            Assert.That(parsed.Parameters[0].Value, Is.EqualTo("utf-8"u8.ToArray()));
            Assert.That(Text(serialized), Is.EqualTo("text/*; charset=\"utf-8\"; level=1"));
            Assert.That(any.IsTypeWildcard, Is.True);
            Assert.That(any.IsSubtypeWildcard, Is.True);
        });
    }

    [Test]
    public void WeightedValueOperationsPreserveOrderParametersAndQualityBoundaries()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        List<HttpWeightedValue> parsed = codec.ParseWeightedValues(
            "gzip; note=fast; q=0.001, br; q=0, *"u8.ToArray());
        byte[] serialized = codec.SerializeWeightedValues(parsed);
        List<HttpWeightedValue> reparsed = codec.ParseWeightedValues(serialized);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Has.Count.EqualTo(3));
            Assert.That(parsed[0].Quality, Is.EqualTo(1));
            Assert.That(parsed[0].Parameters[0].Name, Is.EqualTo("note"));
            Assert.That(parsed[1].Quality, Is.Zero);
            Assert.That(parsed[2].Quality, Is.EqualTo(1000));
            Assert.That(Text(serialized), Is.EqualTo("gzip; note=fast; q=0.001, br; q=0, *"));
            Assert.That(reparsed.Select(value => value.Value),
                Is.EqualTo(new[] { "gzip", "br", "*" }));
        });
    }

    [Test]
    public void LanguageRangeOperationsUseRfc4647BasicRangesAndCanonicalWeights()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        List<HttpLanguageRange> parsed = codec.ParseLanguageRanges(
            "da, en-gb;q=0.8, en;q=0.7, *;q=0"u8.ToArray());
        byte[] serialized = codec.SerializeLanguageRanges(parsed);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Has.Count.EqualTo(4));
            Assert.That(parsed[0].Range, Is.EqualTo("da"));
            Assert.That(parsed[1].Quality, Is.EqualTo(800));
            Assert.That(parsed[1].Specificity, Is.GreaterThan(parsed[2].Specificity));
            Assert.That(parsed[3].IsWildcard, Is.True);
            Assert.That(Text(serialized), Is.EqualTo("da, en-gb; q=0.8, en; q=0.7, *; q=0"));
        });
    }

    [Test]
    public void EntityTagOperationsCoverWeakStrongAndWildcardForms()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        List<HttpEntityTag> tags = codec.ParseEntityTags(
            "W/\"weak\", \"strong\""u8.ToArray());
        byte[] serialized = codec.SerializeEntityTags(tags);
        List<HttpEntityTag> wildcard = codec.ParseEntityTags("*"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(tags, Has.Count.EqualTo(2));
            Assert.That(tags[0].IsWeak, Is.True);
            Assert.That(tags[0].OpaqueTag, Is.EqualTo("weak"u8.ToArray()));
            Assert.That(tags[1].IsWeak, Is.False);
            Assert.That(Text(serialized), Is.EqualTo("W/\"weak\", \"strong\""));
            Assert.That(wildcard, Has.Count.EqualTo(1));
            Assert.That(wildcard[0].IsWildcard, Is.True);
            Assert.That(Text(codec.SerializeEntityTags(wildcard)), Is.EqualTo("*"));
        });
    }

    [Test]
    public void RangeOperationsCoverClosedOpenSuffixAndExtensionRanges()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        HttpRangeRequest bytes = codec.ParseRangeValue(
            "bytes=0-499, 500-999, -500, 9500-"u8.ToArray());
        HttpRangeRequest extension = codec.ParseRangeValue(
            "example=chapter@7,1.2-4.3"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Ranges, Has.Count.EqualTo(4));
            Assert.That(bytes.Ranges[0].End, Is.EqualTo(499));
            Assert.That(bytes.Ranges[2].IsSuffix, Is.True);
            Assert.That(bytes.Ranges[3].HasEnd, Is.False);
            Assert.That(Text(codec.SerializeRangeValue(bytes)),
                Is.EqualTo("bytes=0-499, 500-999, -500, 9500-"));
            Assert.That(extension.OtherRanges, Has.Count.EqualTo(2));
            Assert.That(Text(codec.SerializeRangeValue(extension)),
                Is.EqualTo("example=chapter@7, 1.2-4.3"));
        });
    }

    [Test]
    public void ContentRangeOperationsCoverKnownUnknownAndUnsatisfiedLengths()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        HttpContentRange known = codec.ParseContentRange("bytes 42-1233/1234"u8.ToArray());
        HttpContentRange unknown = codec.ParseContentRange("bytes 42-1233/*"u8.ToArray());
        HttpContentRange unsatisfied = codec.ParseContentRange("bytes */1234"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(known.Start, Is.EqualTo(42));
            Assert.That(known.End, Is.EqualTo(1233));
            Assert.That(known.CompleteLength, Is.EqualTo(1234));
            Assert.That(Text(codec.SerializeContentRange(known)), Is.EqualTo("bytes 42-1233/1234"));
            Assert.That(unknown.HasCompleteLength, Is.False);
            Assert.That(Text(codec.SerializeContentRange(unknown)), Is.EqualTo("bytes 42-1233/*"));
            Assert.That(unsatisfied.IsUnsatisfied, Is.True);
            Assert.That(Text(codec.SerializeContentRange(unsatisfied)), Is.EqualTo("bytes */1234"));
        });
    }

    [Test]
    public void AuthenticationOperationsCoverRfcChallengeAndToken68Examples()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        List<HttpAuthenticationValue> challenges = codec.ParseAuthenticationValues(
            "Basic realm=\"simple\", Newauth realm=\"apps\", type=1, title=\"Login to \\\"apps\\\"\""u8.ToArray());
        List<HttpAuthenticationValue> token = codec.ParseAuthenticationValues(
            "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ=="u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(challenges, Has.Count.EqualTo(2));
            Assert.That(challenges[1].Parameters, Has.Count.EqualTo(3));
            Assert.That(challenges[1].Parameters[2].Value,
                Is.EqualTo("Login to \"apps\""u8.ToArray()));
            Assert.That(Text(codec.SerializeAuthenticationValues(challenges)),
                Is.EqualTo("Basic realm=\"simple\", Newauth realm=\"apps\", type=1, title=\"Login to \\\"apps\\\"\""));
            Assert.That(token[0].Token68,
                Is.EqualTo("QWxhZGRpbjpvcGVuIHNlc2FtZQ=="u8.ToArray()));
            Assert.That(Text(codec.SerializeAuthenticationValues(token)),
                Is.EqualTo("Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ=="));
        });
    }

    [Test]
    public void ViaOperationsCoverRfcProtocolsPseudonymsPortsAndComments()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        List<HttpViaEntry> entries = codec.ParseViaEntries(
            "1.0 fred, 1.1 p.example.net:8080 (edge \\(west\\))"u8.ToArray());
        byte[] serialized = codec.SerializeViaEntries(entries);

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0].ReceivedProtocol, Is.EqualTo("1.0"));
            Assert.That(entries[0].ReceivedBy, Is.EqualTo("fred"));
            Assert.That(entries[1].HasPort, Is.True);
            Assert.That(entries[1].Port, Is.EqualTo(8080));
            Assert.That(entries[1].Comment, Is.EqualTo("edge (west)"u8.ToArray()));
            Assert.That(Text(serialized),
                Is.EqualTo("1.0 fred, 1.1 p.example.net:8080 (edge \\(west\\))"));
        });
    }

    [Test]
    public void CacheControlOperationsCoverStandardExtensionsAndCanonicalOrder()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        HttpCacheControl value = codec.ParseCacheControlValue(
            "MAX-AGE=00060, private=\"ETag, Set-Cookie\", no-transform, ext=\"x y\""u8.ToArray());
        byte[] serialized = codec.SerializeCacheControlValue(value);
        HttpCacheControl reparsed = codec.ParseCacheControlValue(serialized);

        Assert.Multiple(() =>
        {
            Assert.That(value.HasMaxAge, Is.True);
            Assert.That(value.MaxAge, Is.EqualTo(60));
            Assert.That(value.PrivateFields, Is.EqualTo(new[] { "ETag", "Set-Cookie" }));
            Assert.That(Text(serialized), Is.EqualTo(
                "no-transform, private=\"ETag, Set-Cookie\", max-age=60, ext=\"x y\""));
            Assert.That(reparsed.NoTransform, Is.True);
            Assert.That(reparsed.Extensions[0].Value, Is.EqualTo("x y"u8.ToArray()));
        });
    }

    [Test]
    public void PriorityOperationsCoverRfcDefaultsKnownValuesAndExtensions()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        HttpPriority defaults = codec.ParsePriority([]);
        HttpPriority value = codec.ParsePriority("u=1, i, progress=\"streaming\""u8.ToArray());
        byte[] serialized = codec.SerializePriority(value);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.HasUrgency, Is.False);
            Assert.That(defaults.Urgency, Is.EqualTo(3));
            Assert.That(codec.SerializePriority(defaults), Is.Empty);
            Assert.That(value.HasUrgency, Is.True);
            Assert.That(value.Urgency, Is.EqualTo(1));
            Assert.That(value.Incremental, Is.True);
            Assert.That(value.UnknownParameters, Has.Count.EqualTo(1));
            Assert.That(Text(serialized), Is.EqualTo("u=1, i, progress=\"streaming\""));
        });
    }

    [Test]
    public void EveryParseMethodRejectsMalformedInputThroughThePublicContract()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpParseException>(() => codec.ParseHttpDate("not a date"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseMediaType("*/json"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseWeightedValues("gzip;q=0.1234"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseLanguageRanges("en-*-US"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseEntityTags("W/weak"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseRangeValue("bytes=9-1"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseContentRange("bytes 0-1/1"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseAuthenticationValues(
                "Basic realm=\"unterminated"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseViaEntries("1.1proxy"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParseCacheControlValue("max-age=-1"u8.ToArray()));
            Assert.Throws<HttpParseException>(() => codec.ParsePriority("u="u8.ToArray()));
        });
    }

    [Test]
    public void EveryModelBasedSerializerRejectsInconsistentState()
    {
        IHttpFieldValueCodec codec = new HttpFieldValueCodec();
        HttpMediaType media = new HttpMediaType();
        media.IsTypeWildcard = true;
        media.Subtype = "json";
        HttpWeightedValue weighted = new HttpWeightedValue();
        weighted.Value = "gzip";
        weighted.Quality = 1001;
        HttpLanguageRange language = new HttpLanguageRange();
        language.Range = "en-*-US";
        HttpEntityTag tag = new HttpEntityTag();
        tag.IsWildcard = true;
        tag.IsWeak = true;
        HttpRangeRequest range = new HttpRangeRequest();
        range.Unit = "bytes";
        HttpContentRange contentRange = new HttpContentRange();
        contentRange.Unit = "bytes";
        contentRange.HasStart = true;
        contentRange.Start = 2;
        contentRange.HasEnd = true;
        contentRange.End = 1;
        HttpAuthenticationValue authentication = new HttpAuthenticationValue();
        authentication.Scheme = "Basic";
        authentication.Token68 = "ab=c"u8.ToArray();
        HttpViaEntry via = new HttpViaEntry();
        via.ReceivedProtocol = "1.1";
        via.ReceivedBy = "bad host";
        HttpCacheControl cache = new HttpCacheControl();
        cache.HasMaxAge = true;
        cache.MaxAge = -1;
        HttpPriority priority = new HttpPriority();
        priority.HasUrgency = true;
        priority.Urgency = 8;

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpFieldException>(() => codec.SerializeMediaType(media));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeWeightedValues(new List<HttpWeightedValue> { weighted }));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeLanguageRanges(new List<HttpLanguageRange> { language }));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeEntityTags(new List<HttpEntityTag> { tag }));
            Assert.Throws<HttpFieldException>(() => codec.SerializeRangeValue(range));
            Assert.Throws<HttpFieldException>(() => codec.SerializeContentRange(contentRange));
            Assert.Throws<HttpFieldException>(() => codec.SerializeAuthenticationValues(
                new List<HttpAuthenticationValue> { authentication }));
            Assert.Throws<HttpFieldException>(() =>
                codec.SerializeViaEntries(new List<HttpViaEntry> { via }));
            Assert.Throws<HttpFieldException>(() => codec.SerializeCacheControlValue(cache));
            Assert.Throws<HttpFieldException>(() => codec.SerializePriority(priority));
        });
    }

    [Test]
    public void EveryParseAndSerializeMethodHonorsTheFieldValueSizeBoundary()
    {
        IHttpFieldValueCodec normal = new HttpFieldValueCodec();
        HttpFieldValueCodec limited = new HttpFieldValueCodec();
        limited.limits.MaximumFieldValueSize = 0;
        HttpMediaType media = normal.ParseMediaType("text/plain"u8.ToArray());
        List<HttpWeightedValue> weighted = normal.ParseWeightedValues("gzip"u8.ToArray());
        List<HttpLanguageRange> languages = normal.ParseLanguageRanges("en"u8.ToArray());
        List<HttpEntityTag> tags = normal.ParseEntityTags("\"x\""u8.ToArray());
        HttpRangeRequest range = normal.ParseRangeValue("bytes=0-1"u8.ToArray());
        HttpContentRange contentRange = normal.ParseContentRange("bytes 0-1/2"u8.ToArray());
        List<HttpAuthenticationValue> authentication = normal.ParseAuthenticationValues(
            "Basic realm=\"x\""u8.ToArray());
        List<HttpViaEntry> via = normal.ParseViaEntries("1.1 proxy"u8.ToArray());
        HttpCacheControl cache = normal.ParseCacheControlValue("max-age=1"u8.ToArray());
        HttpPriority priority = normal.ParsePriority("u=1"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseHttpDate(
                "Sun, 06 Nov 1994 08:49:37 GMT"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeHttpDate(
                new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero)));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseMediaType("text/plain"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeMediaType(media));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseWeightedValues("gzip"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeWeightedValues(weighted));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseLanguageRanges("en"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeLanguageRanges(languages));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseEntityTags("\"x\""u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeEntityTags(tags));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseRangeValue("bytes=0-1"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeRangeValue(range));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseContentRange("bytes 0-1/2"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeContentRange(contentRange));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseAuthenticationValues(
                "Basic realm=\"x\""u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeAuthenticationValues(authentication));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseViaEntries("1.1 proxy"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeViaEntries(via));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParseCacheControlValue("max-age=1"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializeCacheControlValue(cache));
            Assert.Throws<HttpLimitExceededException>(() => limited.ParsePriority("u=1"u8.ToArray()));
            Assert.Throws<HttpLimitExceededException>(() => limited.SerializePriority(priority));
        });
    }

    [Test]
    public void ListAndParameterLimitsAreReportedSeparatelyFromMalformedSyntax()
    {
        HttpFieldValueCodec codec = new HttpFieldValueCodec();
        codec.limits.MaximumListMemberCount = 1;
        codec.limits.MaximumParameterCount = 1;
        HttpLimitExceededException members = Assert.Throws<HttpLimitExceededException>(() =>
            codec.ParseWeightedValues("gzip, br"u8.ToArray()))!;
        HttpLimitExceededException parameters = Assert.Throws<HttpLimitExceededException>(() =>
            codec.ParseMediaType("text/plain;a=1;b=2"u8.ToArray()))!;

        Assert.Multiple(() =>
        {
            Assert.That(members.LimitName, Is.EqualTo("maximum-list-member-count"));
            Assert.That(members.Limit, Is.EqualTo(1));
            Assert.That(parameters.LimitName, Is.EqualTo("maximum-parameter-count"));
            Assert.That(parameters.Limit, Is.EqualTo(1));
        });
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
