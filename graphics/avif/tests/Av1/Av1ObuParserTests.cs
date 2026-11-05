namespace avif.Tests;

public class Av1ObuParserTests
{
    [Test]
    public void LowOverheadParsingReadsHeadersExtensionsPayloadsAndTemporalUnits()
    {
        byte[] source =
        [
            0x12, 0,
            0x0A, 1, 0x80,
            0x1E, 0x48, 1, 0x80,
            0x4A, 1, 0x80,
            0x12, 0,
            0x2A, 1, 0x80
        ];

        Av1ObuParseResult result = Av1ObuParser.ParseLowOverhead(
            source,
            new AvifSecurityLimits(),
            516);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Obus, Has.Count.EqualTo(6));
            Assert.That(result.TemporalUnits, Has.Count.EqualTo(2));
            Assert.That(result.TemporalUnits[0].Obus, Has.Count.EqualTo(4));
            Assert.That(result.TemporalUnits[1].Obus, Has.Count.EqualTo(2));
            Assert.That(result.TemporalUnits[0].Offset, Is.Zero);
            Assert.That(result.TemporalUnits[0].Size, Is.EqualTo(12));
            Assert.That(result.TemporalUnits[1].Offset, Is.EqualTo(12));
            Assert.That(result.Obus[1].Header.Type, Is.EqualTo(Av1ObuType.SequenceHeader));
            Assert.That(result.Obus[1].Header.HasSizeField, Is.True);
            Assert.That(result.Obus[1].Offset, Is.EqualTo(2));
            Assert.That(result.Obus[1].PayloadOffset, Is.EqualTo(4));
            Assert.That(result.Obus[1].GetPayload(), Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(result.Obus[2].Header.HasExtension, Is.True);
            Assert.That(result.Obus[2].Header.TemporalId, Is.EqualTo(2));
            Assert.That(result.Obus[2].Header.SpatialId, Is.EqualTo(1));
            Assert.That((int)result.Obus[3].Header.Type, Is.EqualTo(9));
        });

        byte[] payload = result.Obus[1].GetPayload();
        payload[0] = 0;
        Assert.That(result.Obus[1].GetPayload()[0], Is.EqualTo(0x80));
    }

    [Test]
    public void OperatingPointFilteringUsesBothLayerMasksAndKeepsGlobalObus()
    {
        byte[] source =
        [
            0x12, 0,
            0x0A, 1, 0x80,
            0x1E, 0x48, 1, 0x80,
            0x1E, 0x00, 1, 0x80
        ];

        Av1ObuParseResult result = Av1ObuParser.ParseLowOverhead(
            source,
            new AvifSecurityLimits(),
            (1 << 2) | (1 << 9));
        List<Av1Obu> included = result.GetIncludedObus();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.DroppedObuCount, Is.EqualTo(1));
            Assert.That(included, Has.Count.EqualTo(3));
            Assert.That(result.Obus[0].InOperatingPoint, Is.True);
            Assert.That(result.Obus[1].InOperatingPoint, Is.True);
            Assert.That(result.Obus[2].InOperatingPoint, Is.True);
            Assert.That(result.Obus[3].InOperatingPoint, Is.False);
            Assert.That(Av1ObuParser.ShouldIncludeObu(result.Obus[3].Header, 0), Is.True);
        });
    }

    [Test]
    public void LowOverheadParsingRejectsIllegalHeadersSizesAndTruncation()
    {
        byte[][] invalidHeaders =
        [
            [0x8A, 0],
            [0x0B, 0],
            [0x0E, 0, 0],
            [0x1E, 0x01, 0],
            [0x46, 0, 0]
        ];

        int index = 0;
        while (index < invalidHeaders.Length)
        {
            Av1ObuParseResult invalid = Av1ObuParser.ParseLowOverhead(
                invalidHeaders[index],
                new AvifSecurityLimits(),
                0);
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(invalid.Diagnostics.Diagnostics, Has.Count.EqualTo(1));
            index++;
        }

        Av1ObuParseResult missingSize = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x08 },
            new AvifSecurityLimits(),
            0);
        Av1ObuParseResult unterminatedSize = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x0A, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 },
            new AvifSecurityLimits(),
            0);
        Av1ObuParseResult truncatedPayload = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x0A, 2, 0x80 },
            new AvifSecurityLimits(),
            0);
        Av1ObuParseResult truncatedExtension = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x1E },
            new AvifSecurityLimits(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(missingSize.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(unterminatedSize.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(truncatedPayload.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncatedExtension.Code, Is.EqualTo(AvifResultCode.TruncatedData));
        });
    }

    [Test]
    public void LowOverheadParsingSupportsCanonicalMultiByteObuSizes()
    {
        byte[] source = new byte[131];
        source[0] = 0x0A;
        source[1] = 0x80;
        source[2] = 0x01;
        source[130] = 0x80;

        Av1ObuParseResult result = Av1ObuParser.ParseLowOverhead(
            source,
            new AvifSecurityLimits(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Obus, Has.Count.EqualTo(1));
            Assert.That(result.Obus[0].SizeFieldSize, Is.EqualTo(2));
            Assert.That(result.Obus[0].PayloadSize, Is.EqualTo(128));
            Assert.That(result.Obus[0].TotalSize, Is.EqualTo(131));
            Assert.That(result.Obus[0].ValidateTrailingBits(127 * 8),
                Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void ParsingChargesRetainedPayloadsToTemporaryStorage()
    {
        byte[] source = [0x2A, 2, 0x04, 0x80];
        AvifSecurityLimits limited = new AvifSecurityLimits();
        limited.MaxTemporaryBytes = 1;
        AvifSecurityLimits allowed = new AvifSecurityLimits();
        allowed.MaxTemporaryBytes = 2;

        Av1ObuParseResult rejected = Av1ObuParser.ParseLowOverhead(
            source,
            limited,
            0);
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            source,
            allowed,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Code,
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(rejected.Obus, Is.Empty);
            Assert.That(rejected.Resources.TemporaryBytes.Current,
                Is.Zero);
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Obus, Has.Count.EqualTo(1));
            Assert.That(parsed.Resources.TemporaryBytes.Current,
                Is.EqualTo(2));
            Assert.That(parsed.Obus[0].GetPayload(),
                Is.EqualTo(new byte[] { 0x04, 0x80 }));
        });
    }

    [Test]
    public void Av1SizeFieldsAllowPaddedLeb128Encodings()
    {
        byte[] source = [0x0A, 0x81, 0, 0x80];

        Av1ObuParseResult result = Av1ObuParser.ParseLowOverhead(
            source,
            new AvifSecurityLimits(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Obus, Has.Count.EqualTo(1));
            Assert.That(result.Obus[0].SizeFieldSize, Is.EqualTo(2));
            Assert.That(result.Obus[0].PayloadSize, Is.EqualTo(1));
            Assert.That(result.Obus[0].GetPayload(), Is.EqualTo(new byte[] { 0x80 }));
        });
    }

    [Test]
    public void LengthDelimitedParsingBuildsTemporalAndFrameUnitHierarchy()
    {
        byte[] source =
        [
            11,
            5,
            1, 0x10,
            2, 0x08, 0x80,
            4,
            3, 0x1A, 1, 0x80,
            3,
            2,
            1, 0x10
        ];

        Av1ObuParseResult result = Av1ObuParser.ParseLengthDelimited(
            source,
            new AvifSecurityLimits(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.TemporalUnits, Has.Count.EqualTo(2));
            Assert.That(result.TemporalUnits[0].Offset, Is.EqualTo(1));
            Assert.That(result.TemporalUnits[0].Size, Is.EqualTo(11));
            Assert.That(result.TemporalUnits[0].FrameUnits, Has.Count.EqualTo(2));
            Assert.That(result.TemporalUnits[0].FrameUnits[0].Offset, Is.EqualTo(2));
            Assert.That(result.TemporalUnits[0].FrameUnits[0].Size, Is.EqualTo(5));
            Assert.That(result.TemporalUnits[0].FrameUnits[0].Obus, Has.Count.EqualTo(2));
            Assert.That(result.TemporalUnits[0].FrameUnits[1].Obus, Has.Count.EqualTo(1));
            Assert.That(result.TemporalUnits[1].Offset, Is.EqualTo(13));
            Assert.That(result.TemporalUnits[1].Size, Is.EqualTo(3));
            Assert.That(result.TemporalUnits[1].FrameUnits, Has.Count.EqualTo(1));
            Assert.That(result.Obus, Has.Count.EqualTo(4));
            Assert.That(result.Obus[0].Header.Type,
                Is.EqualTo(Av1ObuType.TemporalDelimiter));
            Assert.That(result.Obus[0].Header.HasSizeField, Is.False);
            Assert.That(result.Obus[1].Header.Type,
                Is.EqualTo(Av1ObuType.SequenceHeader));
            Assert.That(result.Obus[1].GetPayload(), Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(result.Obus[2].Header.HasSizeField, Is.True);
            Assert.That(result.Obus[2].SizeFieldSize, Is.EqualTo(1));
            Assert.That(result.Resources.FrameCount.Current, Is.EqualTo(3));
        });
    }

    [Test]
    public void LengthDelimitedParsingEnforcesNestedBoundsAndDelimiterPlacement()
    {
        byte[] inconsistentInnerSize = [6, 5, 4, 0x1A, 1, 0x80, 0];
        byte[] missingFirstDelimiter = [4, 3, 2, 0x08, 0x80];
        byte[] misplacedDelimiter = [6, 5, 2, 0x08, 0x80, 1, 0x10];
        byte[] frameCrossesTemporalUnit = [2, 3, 0];
        byte[] temporalCrossesSource = [4, 1, 1];
        byte[] emptyTemporalUnit = [0];

        Av1ObuParseResult inconsistent = ParseLengthDelimited(inconsistentInnerSize);
        Av1ObuParseResult missing = ParseLengthDelimited(missingFirstDelimiter);
        Av1ObuParseResult misplaced = ParseLengthDelimited(misplacedDelimiter);
        Av1ObuParseResult nested = ParseLengthDelimited(frameCrossesTemporalUnit);
        Av1ObuParseResult truncated = ParseLengthDelimited(temporalCrossesSource);
        Av1ObuParseResult empty = ParseLengthDelimited(emptyTemporalUnit);

        Assert.Multiple(() =>
        {
            Assert.That(inconsistent.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(missing.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(misplaced.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(nested.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(empty.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });
    }

    [Test]
    public void TrailingBitValidationAcceptsPaddingAndRejectsMalformedPatterns()
    {
        Av1ObuParseResult padded = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x0A, 2, 0xA0, 0 },
            new AvifSecurityLimits(),
            0);
        Av1ObuParseResult noTrailing = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x0A, 1, 0xA0 },
            new AvifSecurityLimits(),
            0);
        Av1ObuParseResult nonzeroPadding = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x0A, 1, 0xA1 },
            new AvifSecurityLimits(),
            0);
        Av1ObuParseResult badDelimiter = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x12, 1, 0 },
            new AvifSecurityLimits(),
            0);
        Av1ObuParseResult headerOnlyDelimiter = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x12, 0 },
            new AvifSecurityLimits(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(padded.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(padded.Obus[0].ValidateTrailingBits(2),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(padded.Obus[0].ValidateTrailingBits(1),
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(padded.Obus[0].ValidateTrailingBits(16),
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(padded.Obus[0].ValidateTrailingBits(17),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(noTrailing.Obus[0].ValidateTrailingBits(8),
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(nonzeroPadding.Obus[0].ValidateTrailingBits(2),
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(badDelimiter.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(headerOnlyDelimiter.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void ParserValidatesArgumentsAndSourceResourceLimit()
    {
        Av1ObuParseResult invalidOperatingPoint = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x12, 0 },
            new AvifSecurityLimits(),
            4096);
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 1;
        Av1ObuParseResult limited = Av1ObuParser.ParseLowOverhead(
            new byte[] { 0x12, 0 },
            limits,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(invalidOperatingPoint.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That((limited.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Resource));
        });
    }

    static Av1ObuParseResult ParseLengthDelimited(byte[] source)
    {
        return Av1ObuParser.ParseLengthDelimited(
            source,
            new AvifSecurityLimits(),
            0);
    }
}
