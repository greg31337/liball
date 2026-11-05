using System.Text;

namespace http.Tests;

public class CacheControlCodecTests
{
    [Test]
    public void ParserRecognizesEveryValuelessStandardDirective()
    {
        CacheControlParseResult result = FieldValueParser.ParseCacheControlValue(
            "no-cache, no-store, no-transform, only-if-cached, must-revalidate, proxy-revalidate, must-understand, private, public, immutable"u8.ToArray(),
            256,
            16,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Value.NoCache, Is.True);
            Assert.That(result.Value.NoStore, Is.True);
            Assert.That(result.Value.NoTransform, Is.True);
            Assert.That(result.Value.OnlyIfCached, Is.True);
            Assert.That(result.Value.MustRevalidate, Is.True);
            Assert.That(result.Value.ProxyRevalidate, Is.True);
            Assert.That(result.Value.MustUnderstand, Is.True);
            Assert.That(result.Value.Private, Is.True);
            Assert.That(result.Value.Public, Is.True);
            Assert.That(result.Value.Immutable, Is.True);
            Assert.That(result.Value.Extensions, Is.Empty);
            Assert.That(result.Value.HasConflicts, Is.False);
        });
    }

    [Test]
    public void ParserRecognizesNumericDirectivesInTokenAndQuotedForms()
    {
        CacheControlParseResult result = FieldValueParser.ParseCacheControlValue(
            "max-age=60, s-maxage=\"30\", max-stale=15, min-fresh=5, stale-if-error=120, stale-while-revalidate=10"u8.ToArray(),
            192,
            12,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Value.HasMaxAge, Is.True);
            Assert.That(result.Value.MaxAge, Is.EqualTo(60));
            Assert.That(result.Value.HasSharedMaxAge, Is.True);
            Assert.That(result.Value.SharedMaxAge, Is.EqualTo(30));
            Assert.That(result.Value.HasMaxStale, Is.True);
            Assert.That(result.Value.HasMaxStaleValue, Is.True);
            Assert.That(result.Value.MaxStale, Is.EqualTo(15));
            Assert.That(result.Value.HasMinFresh, Is.True);
            Assert.That(result.Value.MinFresh, Is.EqualTo(5));
            Assert.That(result.Value.HasStaleIfError, Is.True);
            Assert.That(result.Value.StaleIfError, Is.EqualTo(120));
            Assert.That(result.Value.HasStaleWhileRevalidate, Is.True);
            Assert.That(result.Value.StaleWhileRevalidate, Is.EqualTo(10));
        });
    }

    [Test]
    public void UnvaluedMaxStaleIsDistinctFromAnExplicitZero()
    {
        CacheControlParseResult unvalued = FieldValueParser.ParseCacheControlValue(
            "max-stale"u8.ToArray(),
            32,
            4,
            4);
        CacheControlParseResult zero = FieldValueParser.ParseCacheControlValue(
            "max-stale=0"u8.ToArray(),
            32,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(unvalued.Value.HasMaxStale, Is.True);
            Assert.That(unvalued.Value.HasMaxStaleValue, Is.False);
            Assert.That(zero.Value.HasMaxStale, Is.True);
            Assert.That(zero.Value.HasMaxStaleValue, Is.True);
            Assert.That(zero.Value.MaxStale, Is.Zero);
        });
    }

    [Test]
    public void QualifiedNoCacheAndPrivateParseQuotedFieldNameLists()
    {
        CacheControlParseResult result = FieldValueParser.ParseCacheControlValue(
            "no-cache=\"Set-Cookie, Authorization\", private=ETag"u8.ToArray(),
            128,
            8,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Value.NoCache, Is.True);
            Assert.That(result.Value.NoCacheFields,
                Is.EqualTo(new[] { "Set-Cookie", "Authorization" }));
            Assert.That(result.Value.Private, Is.True);
            Assert.That(result.Value.PrivateFields, Is.EqualTo(new[] { "ETag" }));
        });
    }

    [Test]
    public void DuplicateRecognizedValuesRetainFirstAndReportOnlyDifferences()
    {
        CacheControlParseResult identical = FieldValueParser.ParseCacheControlValue(
            "max-age=60, MAX-AGE=\"60\", no-cache=\"ETag\", NO-CACHE=etag"u8.ToArray(),
            128,
            8,
            8);
        CacheControlParseResult conflicting = FieldValueParser.ParseCacheControlValue(
            "max-age=60, max-age=30, max-stale, max-stale=0, private, private=\"Set-Cookie\""u8.ToArray(),
            160,
            10,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(identical.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(identical.Value.HasConflicts, Is.False);
            Assert.That(identical.Value.MaxAge, Is.EqualTo(60));
            Assert.That(identical.Value.NoCacheFields, Is.EqualTo(new[] { "ETag" }));
            Assert.That(conflicting.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(conflicting.Value.HasConflicts, Is.True);
            Assert.That(conflicting.Value.MaxAge, Is.EqualTo(60));
            Assert.That(conflicting.Value.HasMaxStaleValue, Is.False);
            Assert.That(conflicting.Value.PrivateFields, Is.Empty);
        });
    }

    [Test]
    public void DeltaSecondsSaturatesValuesBeyondSignedStorage()
    {
        CacheControlParseResult maximum = FieldValueParser.ParseCacheControlValue(
            "max-age=9223372036854775807"u8.ToArray(),
            64,
            4,
            4);
        CacheControlParseResult overflow = FieldValueParser.ParseCacheControlValue(
            "max-age=999999999999999999999999999999999999999"u8.ToArray(),
            96,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Value.MaxAge, Is.EqualTo(long.MaxValue));
            Assert.That(overflow.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(overflow.Value.MaxAge, Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void UnknownExtensionsPreserveOrderCaseBytesAndValueForm()
    {
        CacheControlParseResult parsed = FieldValueParser.ParseCacheControlValue(
            "Foo=bar, Quoted=\"a\\\"b\", Flag, Foo=second"u8.ToArray(),
            128,
            8,
            4);
        FieldValueWriteResult written = FieldValueWriter.SerializeCacheControlValue(
            parsed.Value,
            128,
            8,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Value.Extensions, Has.Count.EqualTo(4));
            Assert.That(parsed.Value.Extensions[0].Name, Is.EqualTo("Foo"));
            Assert.That(parsed.Value.Extensions[0].Value, Is.EqualTo("bar"u8.ToArray()));
            Assert.That(parsed.Value.Extensions[0].IsQuoted, Is.False);
            Assert.That(parsed.Value.Extensions[1].Value, Is.EqualTo("a\"b"u8.ToArray()));
            Assert.That(parsed.Value.Extensions[1].IsQuoted, Is.True);
            Assert.That(parsed.Value.Extensions[2].HasValue, Is.False);
            Assert.That(parsed.Value.Extensions[3].Value, Is.EqualTo("second"u8.ToArray()));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("Foo=bar, Quoted=\"a\\\"b\", Flag, Foo=second"));
        });
    }

    [Test]
    public void EmptyElementsAreIgnoredByTheCacheDirectiveListGrammar()
    {
        CacheControlParseResult result = FieldValueParser.ParseCacheControlValue(
            ", no-store, , max-age=5,"u8.ToArray(),
            64,
            8,
            4);
        CacheControlParseResult empty = FieldValueParser.ParseCacheControlValue(
            Array.Empty<byte>(),
            16,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Value.NoStore, Is.True);
            Assert.That(result.Value.MaxAge, Is.EqualTo(5));
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
        });
    }

    [Test]
    public void ParserRejectsMalformedKnownAndGenericDirectives()
    {
        string[] malformed =
        [
            "max-age",
            "max-age=-1",
            "max-age=1.5",
            "no-store=yes",
            "immutable=?1",
            "no-cache=\"\"",
            "private=\"bad name\"",
            "no-cache =ETag",
            "unknown=",
            "unknown=\"unterminated",
            "unknown value"
        ];

        foreach (string input in malformed)
        {
            CacheControlParseResult result = FieldValueParser.ParseCacheControlValue(
                Encoding.ASCII.GetBytes(input),
                128,
                8,
                8);
            Assert.That(result.IsMalformed, Is.True, input);
        }
    }

    [Test]
    public void ParserDistinguishesConfiguredSizeDirectiveAndFieldNameLimits()
    {
        CacheControlParseResult size = FieldValueParser.ParseCacheControlValue(
            "no-store"u8.ToArray(),
            4,
            4,
            4);
        CacheControlParseResult directives = FieldValueParser.ParseCacheControlValue(
            "no-store, public"u8.ToArray(),
            64,
            1,
            4);
        CacheControlParseResult fields = FieldValueParser.ParseCacheControlValue(
            "no-cache=\"one, two\""u8.ToArray(),
            64,
            4,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(directives.IsLimitExceeded, Is.True);
            Assert.That(fields.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void SerializerProducesDeterministicCanonicalOrderAndSyntax()
    {
        HttpCacheControl value = new HttpCacheControl();
        value.NoCache = true;
        value.NoCacheFields.Add("Set-Cookie");
        value.NoCacheFields.Add("Authorization");
        value.NoStore = true;
        value.NoTransform = true;
        value.OnlyIfCached = true;
        value.MustRevalidate = true;
        value.ProxyRevalidate = true;
        value.MustUnderstand = true;
        value.Private = true;
        value.PrivateFields.Add("X-Secret");
        value.Public = true;
        value.Immutable = true;
        value.HasMaxAge = true;
        value.MaxAge = 60;
        value.HasSharedMaxAge = true;
        value.SharedMaxAge = 30;
        value.HasMaxStale = true;
        value.HasMaxStaleValue = true;
        value.MaxStale = 15;
        value.HasMinFresh = true;
        value.MinFresh = 5;
        value.HasStaleIfError = true;
        value.StaleIfError = 120;
        value.HasStaleWhileRevalidate = true;
        value.StaleWhileRevalidate = 10;

        FieldValueWriteResult result = FieldValueWriter.SerializeCacheControlValue(
            value,
            512,
            24,
            8);

        Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
            "no-cache=\"Set-Cookie, Authorization\", no-store, no-transform, only-if-cached, must-revalidate, proxy-revalidate, must-understand, private=\"X-Secret\", public, immutable, max-age=60, s-maxage=30, max-stale=15, min-fresh=5, stale-if-error=120, stale-while-revalidate=10"));
    }

    [Test]
    public void SerializerRejectsInconsistentPresenceNegativeValuesAndBadExtensions()
    {
        HttpCacheControl fieldsWithoutDirective = new HttpCacheControl();
        fieldsWithoutDirective.NoCacheFields.Add("ETag");
        HttpCacheControl negative = new HttpCacheControl();
        negative.HasMaxAge = true;
        negative.MaxAge = -1;
        HttpCacheControl knownExtension = new HttpCacheControl();
        knownExtension.Extensions.Add(new HttpParameter
        {
            Name = "MAX-AGE",
            HasValue = true,
            Value = "5"u8.ToArray()
        });
        HttpCacheControl unicodeExtension = new HttpCacheControl();
        unicodeExtension.Extensions.Add(new HttpParameter
        {
            Name = "café"
        });

        Assert.Multiple(() =>
        {
            Assert.That(FieldValueWriter.SerializeCacheControlValue(
                fieldsWithoutDirective, 64, 4, 4).IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeCacheControlValue(
                negative, 64, 4, 4).IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeCacheControlValue(
                knownExtension, 64, 4, 4).IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeCacheControlValue(
                unicodeExtension, 64, 4, 4).IsMalformed, Is.True);
        });
    }

    [Test]
    public void SerializerEnforcesOutputDirectiveAndQualifiedFieldLimits()
    {
        HttpCacheControl value = new HttpCacheControl();
        value.NoCache = true;
        value.NoCacheFields.Add("one");
        value.NoCacheFields.Add("two");
        value.NoStore = true;

        FieldValueWriteResult size = FieldValueWriter.SerializeCacheControlValue(value, 8, 4, 4);
        FieldValueWriteResult directives = FieldValueWriter.SerializeCacheControlValue(
            value,
            128,
            1,
            4);
        FieldValueWriteResult fields = FieldValueWriter.SerializeCacheControlValue(
            value,
            128,
            4,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(directives.IsLimitExceeded, Is.True);
            Assert.That(fields.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void ParseSerializeRoundTripRetainsRecognizedSemanticsAndExtensions()
    {
        CacheControlParseResult parsed = FieldValueParser.ParseCacheControlValue(
            "MAX-AGE=00060, PRIVATE=\"ETag, Set-Cookie\", no-transform, ext=\"x y\""u8.ToArray(),
            160,
            8,
            8);
        FieldValueWriteResult written = FieldValueWriter.SerializeCacheControlValue(
            parsed.Value,
            160,
            8,
            8);
        CacheControlParseResult reparsed = FieldValueParser.ParseCacheControlValue(
            written.Bytes,
            160,
            8,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(written.Bytes), Is.EqualTo(
                "no-transform, private=\"ETag, Set-Cookie\", max-age=60, ext=\"x y\""));
            Assert.That(reparsed.Value.MaxAge, Is.EqualTo(60));
            Assert.That(reparsed.Value.PrivateFields,
                Is.EqualTo(new[] { "ETag", "Set-Cookie" }));
            Assert.That(reparsed.Value.NoTransform, Is.True);
            Assert.That(reparsed.Value.Extensions[0].Value, Is.EqualTo("x y"u8.ToArray()));
        });
    }
}
