namespace http.Tests;

public class RangeSemanticsTests
{
    [Test]
    public void NormalizesClosedOpenAndSuffixRangesWithoutChangingSourceOrder()
    {
        HttpRangeRequest request = ByteRequest();
        request.CoalesceOverlaps = false;
        request.CoalesceAdjacent = false;
        HttpByteRange closed = Closed(20, 29);
        HttpByteRange open = Open(90);
        HttpByteRange suffix = Suffix(15);
        request.Ranges.Add(closed);
        request.Ranges.Add(open);
        request.Ranges.Add(suffix);
        request.Ranges.Add(Closed(150, 200));

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(request, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Ranges, Has.Count.EqualTo(3));
            AssertRange(result.Ranges[0], 20, 29);
            AssertRange(result.Ranges[1], 90, 99);
            AssertRange(result.Ranges[2], 85, 99);
            Assert.That(closed.IsNormalized, Is.False);
            Assert.That(open.HasEnd, Is.False);
            Assert.That(suffix.IsSuffix, Is.True);
        });
    }

    [Test]
    public void ClipsClosedRangesAndExpandsLongSuffixesToTheRepresentation()
    {
        HttpRangeRequest request = ByteRequest();
        request.CoalesceOverlaps = false;
        request.Ranges.Add(Closed(25, 200));
        request.Ranges.Add(Suffix(500));

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(request, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            AssertRange(result.Ranges[0], 25, 99);
            AssertRange(result.Ranges[1], 0, 99);
        });
    }

    [Test]
    public void DiscardsUnsatisfiableMembersWhenAnotherMemberIsSatisfiable()
    {
        HttpRangeRequest request = ByteRequest();
        request.Ranges.Add(Open(100));
        request.Ranges.Add(Suffix(0));
        request.Ranges.Add(Closed(99, 120));

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(request, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            Assert.That(result.Ranges, Has.Count.EqualTo(1));
            AssertRange(result.Ranges[0], 99, 99);
        });
    }

    [Test]
    public void ReportsUnsatisfiableWhenNoRequestedBytesCanBeSelected()
    {
        HttpRangeRequest beyond = ByteRequest();
        beyond.Ranges.Add(Open(10));
        beyond.Ranges.Add(Suffix(0));
        HttpRangeRequest empty = ByteRequest();
        empty.Ranges.Add(Suffix(10));

        HttpRangeDecision beyondResult = RangeSemantics.NormalizeByteRanges(beyond, 10);
        HttpRangeDecision emptyResult = RangeSemantics.NormalizeByteRanges(empty, 0);

        Assert.Multiple(() =>
        {
            Assert.That(beyondResult.Outcome,
                Is.EqualTo(HttpRangeOutcome.Unsatisfiable));
            Assert.That(beyondResult.Ranges, Is.Empty);
            Assert.That(emptyResult.Outcome,
                Is.EqualTo(HttpRangeOutcome.Unsatisfiable));
            Assert.That(emptyResult.Ranges, Is.Empty);
        });
    }

    [Test]
    public void PreservesNonAscendingRequestOrderWhenCoalescingIsDisabled()
    {
        HttpRangeRequest request = ByteRequest();
        request.CoalesceOverlaps = false;
        request.CoalesceAdjacent = false;
        request.Ranges.Add(Closed(80, 89));
        request.Ranges.Add(Closed(0, 9));
        request.Ranges.Add(Closed(40, 49));

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(request, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Ranges, Has.Count.EqualTo(3));
            AssertRange(result.Ranges[0], 80, 89);
            AssertRange(result.Ranges[1], 0, 9);
            AssertRange(result.Ranges[2], 40, 49);
        });
    }

    [Test]
    public void CoalescesOverlappingRangesTransitivelyAtTheirFirstSourcePosition()
    {
        HttpRangeRequest request = ByteRequest();
        request.CoalesceAdjacent = false;
        request.Ranges.Add(Closed(20, 29));
        request.Ranges.Add(Closed(0, 9));
        request.Ranges.Add(Closed(8, 22));
        request.Ranges.Add(Closed(50, 59));

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(request, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Ranges, Has.Count.EqualTo(2));
            AssertRange(result.Ranges[0], 0, 29);
            AssertRange(result.Ranges[1], 50, 59);
        });
    }

    [Test]
    public void OverlapAndAdjacencyCoalescingCanBeConfiguredIndependently()
    {
        HttpRangeRequest overlap = ByteRequest();
        overlap.CoalesceOverlaps = false;
        overlap.Ranges.Add(Closed(0, 10));
        overlap.Ranges.Add(Closed(5, 15));

        HttpRangeRequest adjacency = ByteRequest();
        adjacency.CoalesceAdjacent = false;
        adjacency.Ranges.Add(Closed(0, 9));
        adjacency.Ranges.Add(Closed(10, 19));

        HttpRangeRequest enabled = ByteRequest();
        enabled.Ranges.Add(Closed(0, 10));
        enabled.Ranges.Add(Closed(5, 15));
        enabled.Ranges.Add(Closed(16, 19));

        HttpRangeDecision overlapResult = RangeSemantics.NormalizeByteRanges(
            overlap,
            100);
        HttpRangeDecision adjacencyResult = RangeSemantics.NormalizeByteRanges(
            adjacency,
            100);
        HttpRangeDecision enabledResult = RangeSemantics.NormalizeByteRanges(
            enabled,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(overlapResult.Ranges, Has.Count.EqualTo(2));
            Assert.That(adjacencyResult.Ranges, Has.Count.EqualTo(2));
            Assert.That(enabledResult.Ranges, Has.Count.EqualTo(1));
            AssertRange(enabledResult.Ranges[0], 0, 19);
        });
    }

    [Test]
    public void EnforcesConfiguredRangeCountBeforeNormalizationWork()
    {
        HttpRangeRequest accepted = ByteRequest();
        accepted.Limits.MaximumRangeCount = 2;
        accepted.Ranges.Add(Closed(0, 9));
        accepted.Ranges.Add(Closed(20, 29));
        HttpRangeRequest excessive = ByteRequest();
        excessive.Limits.MaximumRangeCount = 2;
        excessive.Ranges.Add(Closed(0, 9));
        excessive.Ranges.Add(Closed(20, 29));
        excessive.Ranges.Add(Closed(40, 49));

        HttpRangeDecision acceptedResult = RangeSemantics.NormalizeByteRanges(
            accepted,
            100);
        HttpRangeDecision excessiveResult = RangeSemantics.NormalizeByteRanges(
            excessive,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(acceptedResult.Outcome,
                Is.EqualTo(HttpRangeOutcome.Satisfiable));
            Assert.That(excessiveResult.Outcome, Is.EqualTo(HttpRangeOutcome.Ignored));
            Assert.That(excessiveResult.Ranges, Is.Empty);
            Assert.That(excessiveResult.Reason, Does.Contain("anti-abuse"));
        });
    }

    [Test]
    public void SupportsMaximumSignedRepresentationLengthWithoutOverflow()
    {
        HttpRangeRequest request = ByteRequest();
        request.CoalesceOverlaps = false;
        request.Ranges.Add(Open(long.MaxValue - 1));
        request.Ranges.Add(Suffix(1));

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(
            request,
            long.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            AssertRange(result.Ranges[0], long.MaxValue - 1, long.MaxValue - 1);
            AssertRange(result.Ranges[1], long.MaxValue - 1, long.MaxValue - 1);
        });
    }

    [Test]
    public void DistinguishesUnsupportedUnitsAndMalformedNormalizationInput()
    {
        HttpRangeRequest extension = new HttpRangeRequest();
        extension.Unit = "custom";
        extension.OtherRanges.Add("chapter@7"u8.ToArray());
        HttpRangeRequest invalidUnit = ByteRequest();
        invalidUnit.Unit = "bad unit";
        invalidUnit.Ranges.Add(Open(0));
        HttpRangeRequest invalidRange = ByteRequest();
        invalidRange.Ranges.Add(Closed(9, 2));
        HttpRangeRequest invalidLimit = ByteRequest();
        invalidLimit.Limits.MaximumRangeCount = -1;
        invalidLimit.Ranges.Add(Open(0));

        HttpRangeDecision unsupported = RangeSemantics.NormalizeByteRanges(
            extension,
            100);
        HttpRangeDecision malformedUnit = RangeSemantics.NormalizeByteRanges(
            invalidUnit,
            100);
        HttpRangeDecision malformedRange = RangeSemantics.NormalizeByteRanges(
            invalidRange,
            100);
        HttpRangeDecision malformedLength = RangeSemantics.NormalizeByteRanges(
            invalidLimit,
            -1);

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Outcome,
                Is.EqualTo(HttpRangeOutcome.UnsupportedUnit));
            Assert.That(malformedUnit.Outcome, Is.EqualTo(HttpRangeOutcome.Malformed));
            Assert.That(malformedRange.Outcome, Is.EqualTo(HttpRangeOutcome.Malformed));
            Assert.That(malformedLength.Outcome,
                Is.EqualTo(HttpRangeOutcome.Malformed));
        });
    }

    [Test]
    public void NormalizesParsedByteRangesUsingRequestPolicy()
    {
        RangeValueParseResult parsed = FieldValueParser.ParseRangeValue(
            "bytes=0-9,10-19,-1"u8.ToArray(),
            64,
            4);
        parsed.Range.CoalesceAdjacent = true;

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(
            parsed.Range,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            Assert.That(result.Ranges, Has.Count.EqualTo(1));
            AssertRange(result.Ranges[0], 0, 19);
        });
    }

    [Test]
    public void SingleNormalizedRangeBuildsComplete206Metadata()
    {
        HttpRangeRequest request = ByteRequest();
        request.Ranges.Add(Closed(20, 200));

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(request, 100);
        HttpContentRange contentRange = result.ContentRanges.Single();
        FieldValueWriteResult serialized = FieldValueWriter.SerializeContentRange(
            contentRange,
            64);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpRangeOutcome.Satisfiable));
            Assert.That(result.Ranges, Has.Count.EqualTo(1));
            Assert.That(contentRange.Unit, Is.EqualTo(HttpRangeUnits.Bytes));
            Assert.That(contentRange.HasStart, Is.True);
            Assert.That(contentRange.Start, Is.EqualTo(20));
            Assert.That(contentRange.HasEnd, Is.True);
            Assert.That(contentRange.End, Is.EqualTo(99));
            Assert.That(contentRange.HasCompleteLength, Is.True);
            Assert.That(contentRange.CompleteLength, Is.EqualTo(100));
            Assert.That(contentRange.IsUnsatisfied, Is.False);
            Assert.That(serialized.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(System.Text.Encoding.ASCII.GetString(serialized.Bytes),
                Is.EqualTo("bytes 20-99/100"));
        });
    }

    [Test]
    public void UnsatisfiableDecisionBuilds416MetadataIncludingZeroLength()
    {
        HttpRangeRequest beyond = ByteRequest();
        beyond.Ranges.Add(Open(100));
        HttpRangeRequest empty = ByteRequest();
        empty.Ranges.Add(Open(0));

        HttpRangeDecision beyondResult = RangeSemantics.NormalizeByteRanges(
            beyond,
            100);
        HttpRangeDecision emptyResult = RangeSemantics.NormalizeByteRanges(empty, 0);
        HttpContentRange contentRange = beyondResult.ContentRanges.Single();
        FieldValueWriteResult serialized = FieldValueWriter.SerializeContentRange(
            contentRange,
            64);

        Assert.Multiple(() =>
        {
            Assert.That(beyondResult.Outcome,
                Is.EqualTo(HttpRangeOutcome.Unsatisfiable));
            Assert.That(contentRange.Unit, Is.EqualTo(HttpRangeUnits.Bytes));
            Assert.That(contentRange.IsUnsatisfied, Is.True);
            Assert.That(contentRange.HasStart, Is.False);
            Assert.That(contentRange.HasEnd, Is.False);
            Assert.That(contentRange.HasCompleteLength, Is.True);
            Assert.That(contentRange.CompleteLength, Is.EqualTo(100));
            Assert.That(System.Text.Encoding.ASCII.GetString(serialized.Bytes),
                Is.EqualTo("bytes */100"));
            Assert.That(emptyResult.ContentRanges.Single().CompleteLength, Is.Zero);
        });
    }

    [Test]
    public void ContentRangeMetadataCorrespondsToEveryRetainedRange()
    {
        HttpRangeRequest request = ByteRequest();
        request.CoalesceOverlaps = false;
        request.CoalesceAdjacent = false;
        request.Ranges.Add(Closed(80, 89));
        request.Ranges.Add(Open(100));
        request.Ranges.Add(Closed(0, 9));

        HttpRangeDecision result = RangeSemantics.NormalizeByteRanges(request, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Ranges, Has.Count.EqualTo(2));
            Assert.That(result.ContentRanges, Has.Count.EqualTo(2));
            Assert.That(result.ContentRanges[0].Start, Is.EqualTo(80));
            Assert.That(result.ContentRanges[0].End, Is.EqualTo(89));
            Assert.That(result.ContentRanges[1].Start, Is.Zero);
            Assert.That(result.ContentRanges[1].End, Is.EqualTo(9));
        });
    }

    [Test]
    public void ContentRangeConstructionRejectsUnnormalizedOrImpossibleBounds()
    {
        HttpByteRange unnormalized = Closed(0, 9);
        HttpByteRange outsideRepresentation = Closed(90, 100);
        outsideRepresentation.IsNormalized = true;

        HttpProtocolException unnormalizedError = Assert.Throws<HttpProtocolException>(() =>
            RangeSemantics.BuildContentRange(unnormalized, 100));
        HttpProtocolException outsideError = Assert.Throws<HttpProtocolException>(() =>
            RangeSemantics.BuildContentRange(outsideRepresentation, 100));
        HttpProtocolException lengthError = Assert.Throws<HttpProtocolException>(() =>
            RangeSemantics.BuildUnsatisfiedContentRange(-1));

        Assert.Multiple(() =>
        {
            Assert.That(unnormalizedError.DiagnosticCode,
                Is.EqualTo("invalid-normalized-byte-range"));
            Assert.That(outsideError.DiagnosticCode,
                Is.EqualTo("invalid-normalized-byte-range"));
            Assert.That(lengthError.DiagnosticCode,
                Is.EqualTo("invalid-representation-length"));
        });
    }

    [Test]
    public void RangeApplicationIsDefinedForGetButNotHead()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RangeSemantics.AppliesToRequestMethod(HttpMethods.Get), Is.True);
            Assert.That(RangeSemantics.AppliesToRequestMethod(HttpMethods.Head), Is.False);
            Assert.That(RangeSemantics.AppliesToRequestMethod(HttpMethods.Post), Is.False);
        });
    }

    static HttpRangeRequest ByteRequest()
    {
        HttpRangeRequest request = new HttpRangeRequest();
        request.Unit = HttpRangeUnits.Bytes;
        return request;
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
