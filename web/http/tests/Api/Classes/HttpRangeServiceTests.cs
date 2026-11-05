using System.Text;

namespace http.Tests;

public class HttpRangeServiceTests
{
    [Test]
    public void PublicServiceParsesAndNormalizesByteRangesWithCallerLimits()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpLimits limits = new HttpLimits();
        limits.MaximumRangeCount = 3;

        HttpRangeRequest request = service.ParseRangeRequest(
            "bytes=0-2,7-"u8.ToArray(),
            limits);
        request.CoalesceAdjacent = false;
        HttpRangeDecision decision = service.NormalizeByteRanges(request, 10);

        Assert.Multiple(() =>
        {
            Assert.That(request.Limits, Is.SameAs(limits));
            Assert.That(request.Ranges, Has.Count.EqualTo(2));
            Assert.That(decision.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            Assert.That(decision.Ranges, Has.Count.EqualTo(2));
            Assert.That(decision.Ranges[0].Start, Is.Zero);
            Assert.That(decision.Ranges[0].End, Is.EqualTo(2));
            Assert.That(decision.Ranges[1].Start, Is.EqualTo(7));
            Assert.That(decision.Ranges[1].End, Is.EqualTo(9));
            Assert.That(service.BuildContentRange(decision.Ranges[0], 10).End,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void MultipartBuilderProducesExactCanonicalPayloadAndPartMetadata()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRepresentation representation = TextRepresentation("0123456789");
        List<HttpByteRange> ranges = NormalizedRanges(service, 10, 0, 2, 7, 9);

        HttpMultipartRange result = service.BuildMultipartRanges(
            representation,
            ranges,
            "simple");
        string expected =
            "--simple\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Range: bytes 0-2/10\r\n" +
            "\r\n" +
            "012\r\n" +
            "--simple\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Range: bytes 7-9/10\r\n" +
            "\r\n" +
            "789\r\n" +
            "--simple--\r\n";

        Assert.Multiple(() =>
        {
            Assert.That(result.Boundary, Is.EqualTo("simple"));
            Assert.That(result.ContentType,
                Is.EqualTo("multipart/byteranges; boundary=simple"));
            Assert.That(Encoding.ASCII.GetString(result.EncodedBody),
                Is.EqualTo(expected));
            Assert.That(result.EncodedLength, Is.EqualTo(result.EncodedBody.LongLength));
            Assert.That(result.Parts, Has.Count.EqualTo(2));
            Assert.That(Encoding.ASCII.GetString(result.Parts[0].Content),
                Is.EqualTo("012"));
            Assert.That(Encoding.ASCII.GetString(result.Parts[1].Content),
                Is.EqualTo("789"));
            Assert.That(result.Parts[0].MediaType?.Type, Is.EqualTo("text"));
            Assert.That(result.Parts[0].Charset, Is.EqualTo("utf-8"));
            Assert.That(result.Parts[1].ContentRange?.Start, Is.EqualTo(7));
        });
    }

    [Test]
    public void MultipartParserRoundTripsContentTypeContentRangeAndContent()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRepresentation representation = TextRepresentation("0123456789");
        List<HttpByteRange> ranges = NormalizedRanges(service, 10, 1, 3, 6, 8);
        HttpMultipartRange built = service.BuildMultipartRanges(
            representation,
            ranges,
            "roundtrip");

        HttpMultipartRange parsed = service.ParseMultipartRanges(
            built.EncodedBody,
            built.Boundary,
            new HttpLimits());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.EncodedBody, Is.EqualTo(built.EncodedBody));
            Assert.That(parsed.EncodedBody, Is.Not.SameAs(built.EncodedBody));
            Assert.That(parsed.EncodedLength, Is.EqualTo(built.EncodedLength));
            Assert.That(parsed.Parts, Has.Count.EqualTo(2));
            Assert.That(parsed.Parts[0].MediaType?.Subtype, Is.EqualTo("plain"));
            Assert.That(parsed.Parts[0].Charset, Is.EqualTo("utf-8"));
            Assert.That(parsed.Parts[0].ContentRange?.Start, Is.EqualTo(1));
            Assert.That(parsed.Parts[0].ContentRange?.End, Is.EqualTo(3));
            Assert.That(parsed.Parts[0].ContentRange?.CompleteLength, Is.EqualTo(10));
            Assert.That(Encoding.ASCII.GetString(parsed.Parts[0].Content),
                Is.EqualTo("123"));
            Assert.That(Encoding.ASCII.GetString(parsed.Parts[1].Content),
                Is.EqualTo("678"));
        });
    }

    [Test]
    public void EmptyBoundaryGeneratesAValidCollisionFreeBoundary()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRepresentation representation = TextRepresentation(
            "first\r\n--ordinary\r\nsecond");
        List<HttpByteRange> ranges = NormalizedRanges(
            service,
            representation.Content.LongLength,
            0,
            4,
            20,
            25);

        HttpMultipartRange result = service.BuildMultipartRanges(
            representation,
            ranges,
            "");
        HttpMultipartRange parsed = service.ParseMultipartRanges(
            result.EncodedBody,
            result.Boundary,
            new HttpLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Boundary, Does.StartWith("http-range-"));
            Assert.That(result.Boundary.Length, Is.LessThanOrEqualTo(70));
            Assert.That(result.ContentType,
                Is.EqualTo("multipart/byteranges; boundary=" + result.Boundary));
            Assert.That(parsed.Parts, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void BoundaryWithMimePunctuationIsQuotedInContentType()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRepresentation representation = TextRepresentation("0123456789");
        List<HttpByteRange> ranges = NormalizedRanges(service, 10, 0, 1, 8, 9);

        HttpMultipartRange result = service.BuildMultipartRanges(
            representation,
            ranges,
            "safe boundary:1");

        Assert.That(result.ContentType,
            Is.EqualTo("multipart/byteranges; boundary=\"safe boundary:1\""));
    }

    [Test]
    public void ParserAcceptsPreambleEpilogueTransportPaddingAndUnknownFields()
    {
        IHttpRangeService service = new HttpRangeService();
        byte[] content = Encoding.ASCII.GetBytes(
            "ignored preamble\r\n" +
            "--b \t\r\n" +
            "X-Trace: retained-elsewhere\r\n" +
            "Content-Range: bytes 0-1/4\r\n" +
            "\r\n" +
            "ab\r\n" +
            "--b\r\n" +
            "Content-Type: application/octet-stream\r\n" +
            "Content-Range: bytes 2-3/4\r\n" +
            "\r\n" +
            "cd\r\n" +
            "--b-- \t\r\n" +
            "ignored epilogue");

        HttpMultipartRange result = service.ParseMultipartRanges(
            content,
            "b",
            new HttpLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Parts, Has.Count.EqualTo(2));
            Assert.That(result.Parts[0].MediaType, Is.Null);
            Assert.That(Encoding.ASCII.GetString(result.Parts[0].Content),
                Is.EqualTo("ab"));
            Assert.That(result.Parts[1].MediaType?.Type, Is.EqualTo("application"));
            Assert.That(Encoding.ASCII.GetString(result.Parts[1].Content),
                Is.EqualTo("cd"));
        });
    }

    [Test]
    public void BuilderRejectsUnsafeBoundariesCollisionsAndInvalidRangeSets()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRepresentation representation = TextRepresentation(
            "--collision\r\n0123456789");
        List<HttpByteRange> collidingRanges = NormalizedRanges(
            service,
            representation.Content.LongLength,
            0,
            10,
            13,
            15);
        List<HttpByteRange> oneRange = new List<HttpByteRange>();
        oneRange.Add(collidingRanges[0]);

        HttpProtocolException invalid = Assert.Throws<HttpProtocolException>(() =>
            service.BuildMultipartRanges(representation, collidingRanges, "bad@boundary"));
        HttpProtocolException collision = Assert.Throws<HttpProtocolException>(() =>
            service.BuildMultipartRanges(representation, collidingRanges, "collision"));
        HttpProtocolException insufficient = Assert.Throws<HttpProtocolException>(() =>
            service.BuildMultipartRanges(representation, oneRange, "safe"));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.DiagnosticCode, Is.EqualTo("invalid-multipart-boundary"));
            Assert.That(collision.DiagnosticCode,
                Is.EqualTo("multipart-boundary-collision"));
            Assert.That(insufficient.DiagnosticCode,
                Is.EqualTo("insufficient-multipart-ranges"));
        });
    }

    [Test]
    public void ParserRejectsMissingMetadataDuplicateMetadataAndWrongPartLength()
    {
        IHttpRangeService service = new HttpRangeService();
        byte[] missing = Encoding.ASCII.GetBytes(
            "--b\r\nContent-Type: text/plain\r\n\r\na\r\n--b--\r\n");
        byte[] duplicate = Encoding.ASCII.GetBytes(
            "--b\r\n" +
            "Content-Range: bytes 0-0/2\r\n" +
            "Content-Range: bytes 1-1/2\r\n\r\na\r\n--b--\r\n");
        byte[] wrongLength = Encoding.ASCII.GetBytes(
            "--b\r\nContent-Range: bytes 0-2/4\r\n\r\nab\r\n--b--\r\n");

        HttpParseException missingError = Assert.Throws<HttpParseException>(() =>
            service.ParseMultipartRanges(missing, "b", new HttpLimits()));
        HttpParseException duplicateError = Assert.Throws<HttpParseException>(() =>
            service.ParseMultipartRanges(duplicate, "b", new HttpLimits()));
        HttpParseException lengthError = Assert.Throws<HttpParseException>(() =>
            service.ParseMultipartRanges(wrongLength, "b", new HttpLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(missingError.DiagnosticCode, Is.EqualTo("missing-content-range"));
            Assert.That(duplicateError.DiagnosticCode,
                Is.EqualTo("duplicate-part-content-range"));
            Assert.That(lengthError.DiagnosticCode,
                Is.EqualTo("inconsistent-part-length"));
        });
    }

    [Test]
    public void ParserEnforcesContentHeaderAndPartCountLimits()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRepresentation representation = TextRepresentation("0123456789");
        List<HttpByteRange> ranges = NormalizedRanges(service, 10, 0, 1, 8, 9);
        HttpMultipartRange built = service.BuildMultipartRanges(
            representation,
            ranges,
            "limits");

        HttpLimits contentLimits = new HttpLimits();
        contentLimits.MaximumContentSize = built.EncodedBody.Length - 1;
        HttpLimits partLimits = new HttpLimits();
        partLimits.MaximumRangeCount = 1;
        HttpLimits fieldLimits = new HttpLimits();
        fieldLimits.MaximumFieldLineSize = 10;

        HttpLimitExceededException contentError =
            Assert.Throws<HttpLimitExceededException>(() =>
                service.ParseMultipartRanges(
                    built.EncodedBody,
                    built.Boundary,
                    contentLimits));
        HttpLimitExceededException partError =
            Assert.Throws<HttpLimitExceededException>(() =>
                service.ParseMultipartRanges(
                    built.EncodedBody,
                    built.Boundary,
                    partLimits));
        HttpLimitExceededException fieldError =
            Assert.Throws<HttpLimitExceededException>(() =>
                service.ParseMultipartRanges(
                    built.EncodedBody,
                    built.Boundary,
                    fieldLimits));

        Assert.Multiple(() =>
        {
            Assert.That(contentError.LimitName, Is.EqualTo("maximum-content-size"));
            Assert.That(partError.LimitName, Is.EqualTo("maximum-range-count"));
            Assert.That(fieldError.LimitName,
                Is.EqualTo("maximum-field-line-size"));
        });
    }

    [Test]
    public void RangeParserDistinguishesMalformedInputAndConfiguredLimits()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpLimits limits = new HttpLimits();
        limits.MaximumRangeCount = 1;

        HttpParseException malformed = Assert.Throws<HttpParseException>(() =>
            service.ParseRangeRequest("bytes=4-2"u8.ToArray(), limits));
        HttpLimitExceededException excessive =
            Assert.Throws<HttpLimitExceededException>(() =>
                service.ParseRangeRequest("bytes=0-1,2-3"u8.ToArray(), limits));

        Assert.Multiple(() =>
        {
            Assert.That(malformed.DiagnosticCode, Is.EqualTo("invalid-range-value"));
            Assert.That(excessive.LimitName, Is.EqualTo("maximum-range-count"));
        });
    }

    [Test]
    public void RangeParserCoversClosedOpenSuffixEmptyMembersAndMaximumOffsets()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpLimits limits = new HttpLimits();
        limits.MaximumRangeCount = 5;

        HttpRangeRequest request = service.ParseRangeRequest(
            "Bytes= 0-499, 500-, , -0500, 9223372036854775807-,"u8.ToArray(),
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(request.Unit, Is.EqualTo("Bytes"));
            Assert.That(request.Limits, Is.SameAs(limits));
            Assert.That(request.Ranges, Has.Count.EqualTo(4));
            Assert.That(request.Ranges[0].Start, Is.Zero);
            Assert.That(request.Ranges[0].End, Is.EqualTo(499));
            Assert.That(request.Ranges[1].Start, Is.EqualTo(500));
            Assert.That(request.Ranges[1].HasEnd, Is.False);
            Assert.That(request.Ranges[2].IsSuffix, Is.True);
            Assert.That(request.Ranges[2].SuffixLength, Is.EqualTo(500));
            Assert.That(request.Ranges[3].Start, Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void RangeParserPreservesExtensionUnitsAndSpecifierOctets()
    {
        IHttpRangeService service = new HttpRangeService();

        HttpRangeRequest request = service.ParseRangeRequest(
            "Example-Unit=1.2-4.3,chapter@7,token=value"u8.ToArray(),
            new HttpLimits());
        HttpRangeDecision decision = service.NormalizeByteRanges(request, 100);

        Assert.Multiple(() =>
        {
            Assert.That(request.Unit, Is.EqualTo("Example-Unit"));
            Assert.That(request.Ranges, Is.Empty);
            Assert.That(request.OtherRanges, Has.Count.EqualTo(3));
            Assert.That(request.OtherRanges[0], Is.EqualTo("1.2-4.3"u8.ToArray()));
            Assert.That(request.OtherRanges[1], Is.EqualTo("chapter@7"u8.ToArray()));
            Assert.That(request.OtherRanges[2], Is.EqualTo("token=value"u8.ToArray()));
            Assert.That(decision.Outcome, Is.EqualTo(HttpRangeOutcome.UnsupportedUnit));
        });
    }

    [Test]
    public void RangeParserRejectsUndefinedReversedSpacedAndOverflowingMembers()
    {
        IHttpRangeService service = new HttpRangeService();
        string[] malformed =
        [
            "bytes=",
            "bytes=,,",
            "bytes=500-499",
            "bytes=-",
            "bytes=1.2-4.3",
            "bytes=0 - 1",
            "bytes=+1-2",
            "bytes=9223372036854775808-",
            "bytes=-9223372036854775808",
            "bytes=0-9223372036854775808"
        ];

        Assert.Multiple(() =>
        {
            foreach (string value in malformed)
            {
                HttpParseException error = Assert.Throws<HttpParseException>(() =>
                    service.ParseRangeRequest(Encoding.ASCII.GetBytes(value), new HttpLimits()),
                    value)!;
                Assert.That(error.DiagnosticCode, Is.EqualTo("invalid-range-value"), value);
            }

            Assert.Throws<HttpParseException>(() =>
                service.ParseRangeRequest(null!, new HttpLimits()));
        });
    }

    [Test]
    public void RangeParserReportsFieldSizeRangeCountAndInvalidLimits()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpLimits size = new HttpLimits();
        size.MaximumFieldValueSize = 8;
        HttpLimits count = new HttpLimits();
        count.MaximumRangeCount = 1;
        HttpLimits invalidSize = new HttpLimits();
        invalidSize.MaximumFieldValueSize = -1;
        HttpLimits invalidCount = new HttpLimits();
        invalidCount.MaximumRangeCount = -1;

        HttpLimitExceededException sizeError = Assert.Throws<HttpLimitExceededException>(() =>
            service.ParseRangeRequest("bytes=0-1"u8.ToArray(), size));
        HttpLimitExceededException countError = Assert.Throws<HttpLimitExceededException>(() =>
            service.ParseRangeRequest("bytes=0-1,2-3"u8.ToArray(), count));

        Assert.Multiple(() =>
        {
            Assert.That(sizeError.LimitName, Is.EqualTo("maximum-field-value-size"));
            Assert.That(sizeError.Limit, Is.EqualTo(8));
            Assert.That(sizeError.Actual, Is.EqualTo(9));
            Assert.That(countError.LimitName, Is.EqualTo("maximum-range-count"));
            Assert.That(countError.Limit, Is.EqualTo(1));
            Assert.That(countError.Actual, Is.EqualTo(2));
            Assert.Throws<HttpProtocolException>(() =>
                service.ParseRangeRequest(Array.Empty<byte>(), null!));
            Assert.Throws<HttpProtocolException>(() =>
                service.ParseRangeRequest(Array.Empty<byte>(), invalidSize));
            Assert.Throws<HttpProtocolException>(() =>
                service.ParseRangeRequest(Array.Empty<byte>(), invalidCount));
        });
    }

    [Test]
    public void NormalizationCoversClosedOpenSuffixClippingAndUnsatisfiedMembers()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRangeRequest request = ByteRequest();
        request.CoalesceOverlaps = false;
        request.CoalesceAdjacent = false;
        request.Ranges.Add(Closed(20, 29));
        request.Ranges.Add(Open(90));
        request.Ranges.Add(Suffix(15));
        request.Ranges.Add(Closed(25, 200));
        request.Ranges.Add(Open(100));
        request.Ranges.Add(Suffix(0));

        HttpRangeDecision result = service.NormalizeByteRanges(request, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Ranges, Has.Count.EqualTo(4));
            AssertRange(result.Ranges[0], 20, 29);
            AssertRange(result.Ranges[1], 90, 99);
            AssertRange(result.Ranges[2], 85, 99);
            AssertRange(result.Ranges[3], 25, 99);
            Assert.That(result.ContentRanges, Has.Count.EqualTo(4));
            Assert.That(request.Ranges[1].HasEnd, Is.False);
            Assert.That(request.Ranges[2].IsNormalized, Is.False);
        });
    }

    [Test]
    public void NormalizationBuildsUnsatisfied416MetadataIncludingEmptyRepresentations()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRangeRequest beyond = ByteRequest();
        beyond.Ranges.Add(Open(100));
        beyond.Ranges.Add(Suffix(0));
        HttpRangeRequest empty = ByteRequest();
        empty.Ranges.Add(Suffix(10));

        HttpRangeDecision beyondResult = service.NormalizeByteRanges(beyond, 100);
        HttpRangeDecision emptyResult = service.NormalizeByteRanges(empty, 0);

        Assert.Multiple(() =>
        {
            Assert.That(beyondResult.Outcome, Is.EqualTo(HttpRangeOutcome.Unsatisfiable));
            Assert.That(beyondResult.Ranges, Is.Empty);
            Assert.That(beyondResult.ContentRanges, Has.Count.EqualTo(1));
            Assert.That(beyondResult.ContentRanges[0].IsUnsatisfied, Is.True);
            Assert.That(beyondResult.ContentRanges[0].CompleteLength, Is.EqualTo(100));
            Assert.That(emptyResult.Outcome, Is.EqualTo(HttpRangeOutcome.Unsatisfiable));
            Assert.That(emptyResult.ContentRanges[0].CompleteLength, Is.Zero);
        });
    }

    [Test]
    public void NormalizationCoalescesOverlapAndAdjacencyIndependentlyAndTransitively()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRangeRequest overlapOnly = ByteRequest();
        overlapOnly.CoalesceAdjacent = false;
        overlapOnly.Ranges.Add(Closed(20, 29));
        overlapOnly.Ranges.Add(Closed(0, 9));
        overlapOnly.Ranges.Add(Closed(8, 22));
        overlapOnly.Ranges.Add(Closed(30, 39));
        HttpRangeRequest neither = ByteRequest();
        neither.CoalesceOverlaps = false;
        neither.CoalesceAdjacent = false;
        neither.Ranges.Add(Closed(0, 10));
        neither.Ranges.Add(Closed(5, 15));
        neither.Ranges.Add(Closed(16, 19));
        HttpRangeRequest both = ByteRequest();
        both.Ranges.Add(Closed(0, 10));
        both.Ranges.Add(Closed(5, 15));
        both.Ranges.Add(Closed(16, 19));

        HttpRangeDecision overlap = service.NormalizeByteRanges(overlapOnly, 100);
        HttpRangeDecision separate = service.NormalizeByteRanges(neither, 100);
        HttpRangeDecision merged = service.NormalizeByteRanges(both, 100);

        Assert.Multiple(() =>
        {
            Assert.That(overlap.Ranges, Has.Count.EqualTo(2));
            AssertRange(overlap.Ranges[0], 0, 29);
            AssertRange(overlap.Ranges[1], 30, 39);
            Assert.That(separate.Ranges, Has.Count.EqualTo(3));
            Assert.That(merged.Ranges, Has.Count.EqualTo(1));
            AssertRange(merged.Ranges[0], 0, 19);
        });
    }

    [Test]
    public void NormalizationHandlesSignedMaximumWithoutOverflowAndEnforcesAbuseLimit()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRangeRequest maximum = ByteRequest();
        maximum.CoalesceOverlaps = false;
        maximum.Ranges.Add(Open(long.MaxValue - 1));
        maximum.Ranges.Add(Suffix(1));
        HttpRangeRequest excessive = ByteRequest();
        excessive.Limits.MaximumRangeCount = 1;
        excessive.Ranges.Add(Closed(0, 1));
        excessive.Ranges.Add(Closed(2, 3));

        HttpRangeDecision maximumResult = service.NormalizeByteRanges(maximum, long.MaxValue);
        HttpRangeDecision excessiveResult = service.NormalizeByteRanges(excessive, 10);

        Assert.Multiple(() =>
        {
            Assert.That(maximumResult.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            AssertRange(maximumResult.Ranges[0], long.MaxValue - 1, long.MaxValue - 1);
            AssertRange(maximumResult.Ranges[1], long.MaxValue - 1, long.MaxValue - 1);
            Assert.That(excessiveResult.Outcome, Is.EqualTo(HttpRangeOutcome.Ignored));
            Assert.That(excessiveResult.Ranges, Is.Empty);
            Assert.That(excessiveResult.Reason, Does.Contain("anti-abuse"));
        });
    }

    [Test]
    public void NormalizationRejectsMalformedContextsRangesAndMixedUnits()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRangeRequest invalidUnit = ByteRequest();
        invalidUnit.Unit = "bad unit";
        invalidUnit.Ranges.Add(Open(0));
        HttpRangeRequest reversed = ByteRequest();
        reversed.Ranges.Add(Closed(9, 2));
        HttpRangeRequest mixed = ByteRequest();
        mixed.Ranges.Add(Open(0));
        mixed.OtherRanges.Add("custom"u8.ToArray());
        HttpRangeRequest missingLimits = ByteRequest();
        missingLimits.Limits = null!;
        missingLimits.Ranges.Add(Open(0));

        Assert.Multiple(() =>
        {
            Assert.That(service.NormalizeByteRanges(invalidUnit, 10).Outcome,
                Is.EqualTo(HttpRangeOutcome.Malformed));
            Assert.That(service.NormalizeByteRanges(reversed, 10).Outcome,
                Is.EqualTo(HttpRangeOutcome.Malformed));
            Assert.That(service.NormalizeByteRanges(mixed, 10).Outcome,
                Is.EqualTo(HttpRangeOutcome.Malformed));
            Assert.That(service.NormalizeByteRanges(missingLimits, 10).Outcome,
                Is.EqualTo(HttpRangeOutcome.Malformed));
            Assert.That(service.NormalizeByteRanges(null!, 10).Outcome,
                Is.EqualTo(HttpRangeOutcome.Malformed));
            Assert.That(service.NormalizeByteRanges(ByteRequest(), -1).Outcome,
                Is.EqualTo(HttpRangeOutcome.Malformed));
        });
    }

    [Test]
    public void ContentRangeBuilderProduces206MetadataAndRejectsImpossibleBounds()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpByteRange normalized = Closed(20, 99);
        normalized.IsNormalized = true;
        HttpContentRange result = service.BuildContentRange(normalized, 100);
        HttpByteRange unnormalized = Closed(0, 9);
        HttpByteRange outside = Closed(90, 100);
        outside.IsNormalized = true;

        Assert.Multiple(() =>
        {
            Assert.That(result.Unit, Is.EqualTo(HttpRangeUnits.Bytes));
            Assert.That(result.Start, Is.EqualTo(20));
            Assert.That(result.End, Is.EqualTo(99));
            Assert.That(result.HasCompleteLength, Is.True);
            Assert.That(result.CompleteLength, Is.EqualTo(100));
            Assert.That(result.IsUnsatisfied, Is.False);
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildContentRange(unnormalized, 100));
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildContentRange(outside, 100));
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildContentRange(normalized, -1));
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildContentRange(null!, 100));
        });
    }

    [Test]
    public void MultipartBuilderPreservesBinarySlicesAndOmitsAbsentContentType()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRepresentation representation = new HttpRepresentation();
        representation.Content = new byte[] { 0x00, 0x0d, 0x0a, 0x80, 0xff, 0x41 };
        representation.HasContentLength = true;
        representation.ContentLength = representation.Content.LongLength;
        List<HttpByteRange> ranges = NormalizedRanges(service, 6, 0, 1, 3, 5);

        HttpMultipartRange built = service.BuildMultipartRanges(
            representation, ranges, "binary");
        HttpMultipartRange parsed = service.ParseMultipartRanges(
            built.EncodedBody, built.Boundary, new HttpLimits());

        Assert.Multiple(() =>
        {
            Assert.That(built.EncodedLength, Is.EqualTo(built.EncodedBody.LongLength));
            Assert.That(built.Parts[0].Content, Is.EqualTo(new byte[] { 0x00, 0x0d }));
            Assert.That(built.Parts[1].Content, Is.EqualTo(new byte[] { 0x80, 0xff, 0x41 }));
            Assert.That(built.Parts[0].MediaType, Is.Null);
            Assert.That(parsed.Parts[0].Content, Is.EqualTo(built.Parts[0].Content));
            Assert.That(parsed.Parts[1].Content, Is.EqualTo(built.Parts[1].Content));
        });
    }

    [Test]
    public void MultipartBuilderRejectsIncompleteRepresentationsAndRanges()
    {
        IHttpRangeService service = new HttpRangeService();
        HttpRepresentation representation = TextRepresentation("0123456789");
        List<HttpByteRange> ranges = NormalizedRanges(service, 10, 0, 1, 8, 9);
        HttpRepresentation missingContent = TextRepresentation("data");
        missingContent.Content = null!;
        HttpRepresentation inconsistent = TextRepresentation("data");
        inconsistent.ContentLength = 3;
        HttpByteRange unnormalized = Closed(0, 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildMultipartRanges(null!, ranges, "safe"));
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildMultipartRanges(missingContent, ranges, "safe"));
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildMultipartRanges(inconsistent, ranges, "safe"));
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildMultipartRanges(representation, null!, "safe"));
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildMultipartRanges(representation, [unnormalized, ranges[1]], "safe"));
            Assert.Throws<HttpProtocolException>(() =>
                service.BuildMultipartRanges(representation, ranges, null!));
        });
    }

    [Test]
    public void MultipartParserRejectsMalformedDelimiterAndHeaderForms()
    {
        IHttpRangeService service = new HttpRangeService();
        string[] malformed =
        [
            "--wrong\r\nContent-Range: bytes 0-0/2\r\n\r\na\r\n--wrong--\r\n",
            "--b\nContent-Range: bytes 0-0/2\n\na\n--b--\n",
            "--b\r\nContent-Range: bytes 0-0/2\r\n\r\na",
            "--b\r\nBad Field: x\r\nContent-Range: bytes 0-0/2\r\n\r\na\r\n--b--\r\n",
            "--b\r\nContent-Range bytes 0-0/2\r\n\r\na\r\n--b--\r\n",
            "--b\r\nContent-Range: items 0-0/2\r\n\r\na\r\n--b--\r\n",
            "--b\r\nContent-Range: bytes */2\r\n\r\na\r\n--b--\r\n"
        ];

        Assert.Multiple(() =>
        {
            foreach (string value in malformed)
            {
                Assert.Throws<HttpParseException>(() => service.ParseMultipartRanges(
                    Encoding.ASCII.GetBytes(value), "b", new HttpLimits()), value);
            }

            Assert.Throws<HttpProtocolException>(() =>
                service.ParseMultipartRanges(Array.Empty<byte>(), "", new HttpLimits()));
            Assert.Throws<HttpProtocolException>(() =>
                service.ParseMultipartRanges(Array.Empty<byte>(), "bad@boundary", new HttpLimits()));
            Assert.Throws<HttpProtocolException>(() =>
                service.ParseMultipartRanges(null!, "b", new HttpLimits()));
        });
    }

    [Test]
    public void MultipartParserRejectsConflictingLengthsAndDuplicateContentTypes()
    {
        IHttpRangeService service = new HttpRangeService();
        byte[] conflicting = Encoding.ASCII.GetBytes(
            "--b\r\nContent-Range: bytes 0-0/2\r\n\r\na\r\n" +
            "--b\r\nContent-Range: bytes 1-1/3\r\n\r\nb\r\n--b--\r\n");
        byte[] duplicateType = Encoding.ASCII.GetBytes(
            "--b\r\nContent-Type: text/plain\r\nContent-Type: text/plain\r\n" +
            "Content-Range: bytes 0-0/2\r\n\r\na\r\n--b--\r\n");

        HttpParseException length = Assert.Throws<HttpParseException>(() =>
            service.ParseMultipartRanges(conflicting, "b", new HttpLimits()));
        HttpParseException type = Assert.Throws<HttpParseException>(() =>
            service.ParseMultipartRanges(duplicateType, "b", new HttpLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(length.DiagnosticCode, Is.EqualTo("conflicting-complete-length"));
            Assert.That(type.DiagnosticCode, Is.EqualTo("duplicate-part-content-type"));
        });
    }

    [Test]
    public void MultipartParserEnforcesEveryRelevantHeaderLimit()
    {
        IHttpRangeService service = new HttpRangeService();
        byte[] body = Encoding.ASCII.GetBytes(
            "--b\r\nContent-Type: text/plain;charset=utf-8\r\n" +
            "Content-Range: bytes 0-0/2\r\nX-Trace: value\r\n\r\na\r\n--b--\r\n");
        HttpLimits section = new HttpLimits();
        section.MaximumFieldSectionSize = 20;
        HttpLimits count = new HttpLimits();
        count.MaximumFieldCount = 2;
        HttpLimits value = new HttpLimits();
        value.MaximumFieldValueSize = 5;
        HttpLimits parameter = new HttpLimits();
        parameter.MaximumParameterCount = 0;

        HttpLimitExceededException sectionError = Assert.Throws<HttpLimitExceededException>(() =>
            service.ParseMultipartRanges(body, "b", section));
        HttpLimitExceededException countError = Assert.Throws<HttpLimitExceededException>(() =>
            service.ParseMultipartRanges(body, "b", count));
        HttpLimitExceededException valueError = Assert.Throws<HttpLimitExceededException>(() =>
            service.ParseMultipartRanges(body, "b", value));
        HttpLimitExceededException parameterError = Assert.Throws<HttpLimitExceededException>(() =>
            service.ParseMultipartRanges(body, "b", parameter));

        Assert.Multiple(() =>
        {
            Assert.That(sectionError.LimitName, Is.EqualTo("maximum-field-section-size"));
            Assert.That(countError.LimitName, Is.EqualTo("maximum-field-count"));
            Assert.That(valueError.LimitName, Is.EqualTo("maximum-field-value-size"));
            Assert.That(parameterError.LimitName, Is.EqualTo("maximum-parameter-count"));
        });
    }

    static HttpRepresentation TextRepresentation(string content)
    {
        HttpMediaType mediaType = new HttpMediaType();
        mediaType.Type = "text";
        mediaType.Subtype = "plain";
        HttpRepresentation representation = new HttpRepresentation();
        representation.MediaType = mediaType;
        representation.Charset = "utf-8";
        representation.HasContentLength = true;
        representation.Content = Encoding.ASCII.GetBytes(content);
        representation.ContentLength = representation.Content.LongLength;
        return representation;
    }

    static List<HttpByteRange> NormalizedRanges(
        IHttpRangeService service,
        long completeLength,
        long firstStart,
        long firstEnd,
        long secondStart,
        long secondEnd)
    {
        HttpRangeRequest request = new HttpRangeRequest();
        request.Unit = HttpRangeUnits.Bytes;
        request.CoalesceOverlaps = false;
        request.CoalesceAdjacent = false;
        request.Ranges.Add(Closed(firstStart, firstEnd));
        request.Ranges.Add(Closed(secondStart, secondEnd));
        return service.NormalizeByteRanges(request, completeLength).Ranges;
    }

    static HttpByteRange Closed(long start, long end)
    {
        HttpByteRange range = new HttpByteRange();
        range.HasStart = true;
        range.Start = start;
        range.HasEnd = true;
        range.End = end;
        return range;
    }

    static HttpRangeRequest ByteRequest()
    {
        HttpRangeRequest request = new HttpRangeRequest();
        request.Unit = HttpRangeUnits.Bytes;
        return request;
    }

    static HttpByteRange Open(long start)
    {
        HttpByteRange range = new HttpByteRange();
        range.HasStart = true;
        range.Start = start;
        return range;
    }

    static HttpByteRange Suffix(long length)
    {
        HttpByteRange range = new HttpByteRange();
        range.IsSuffix = true;
        range.SuffixLength = length;
        return range;
    }

    static void AssertRange(HttpByteRange range, long start, long end)
    {
        Assert.Multiple(() =>
        {
            Assert.That(range.HasStart, Is.True);
            Assert.That(range.Start, Is.EqualTo(start));
            Assert.That(range.HasEnd, Is.True);
            Assert.That(range.End, Is.EqualTo(end));
            Assert.That(range.IsSuffix, Is.False);
            Assert.That(range.IsNormalized, Is.True);
        });
    }
}
