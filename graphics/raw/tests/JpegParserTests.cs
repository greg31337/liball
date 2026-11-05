using raw;

namespace raw.Tests;

public class JpegParserTests
{
    [Test]
    public void BaselineHeadersTablesStuffingAndRestartsAreDescribed()
    {
        byte[] source = BuildBaselineStream();

        JpegDocument document = JpegParser.Parse(source, new RawLimits(), CancellationToken.None);
        JpegFrameHeader frame = document.Frames.Single();
        JpegScanHeader scan = document.Scans.Single();

        Assert.Multiple(() =>
        {
            Assert.That(document.Source.ToArray(), Is.EqualTo(source));
            Assert.That(document.Markers.Select(marker => marker.Code), Is.EqualTo(new byte[] { 0xd8, 0xe1, 0xdb, 0xc4, 0xc0, 0xdd, 0xda, 0xd0, 0xd1, 0xd9 }));
            Assert.That(document.QuantizationTables.Count, Is.EqualTo(2));
            Assert.That(document.HuffmanTables.Count, Is.EqualTo(2));
            Assert.That(document.RestartIntervals.Single().Interval, Is.EqualTo(4));
            Assert.That(frame.ProcessMarker, Is.EqualTo(0xc0));
            Assert.That(frame.Precision, Is.EqualTo(8));
            Assert.That(frame.Width, Is.EqualTo(3));
            Assert.That(frame.Height, Is.EqualTo(2));
            Assert.That(frame.Components.Count, Is.EqualTo(3));
            Assert.That(frame.Components[0].HorizontalSampling, Is.EqualTo(2));
            Assert.That(frame.Components[0].VerticalSampling, Is.EqualTo(2));
            Assert.That(frame.Components[1].QuantizationTable, Is.EqualTo(1));
            Assert.That(scan.Components.Count, Is.EqualTo(3));
            Assert.That(scan.Components[1].DcTable, Is.EqualTo(1));
            Assert.That(scan.Components[1].AcTable, Is.EqualTo(1));
            Assert.That(scan.SpectralStart, Is.Zero);
            Assert.That(scan.SpectralEnd, Is.EqualTo(63));
            Assert.That(scan.RestartInterval, Is.EqualTo(4));
            Assert.That(scan.RestartMarkers.Select(marker => marker.Code), Is.EqualTo(new byte[] { 0xd0, 0xd1 }));
            Assert.That(scan.EntropySegments.Count, Is.EqualTo(3));
            Assert.That(scan.EntropySegments.Select(segment => segment.DecodedLength), Is.EqualTo(new int[] { 3, 3, 1 }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(JpegEntropyCodec.Unstuff(document.Source, scan.EntropySegments[0], new RawLimits(), CancellationToken.None), Is.EqualTo(new byte[] { 0x12, 0xff, 0x34 }));
            Assert.That(JpegEntropyCodec.Unstuff(document.Source, scan.EntropySegments[1], new RawLimits(), CancellationToken.None), Is.EqualTo(new byte[] { 0x56, 0xff, 0x78 }));
            Assert.That(JpegEntropyCodec.Unstuff(document.Source, scan.EntropySegments[2], new RawLimits(), CancellationToken.None), Is.EqualTo(new byte[] { 0x9a }));
        });
    }

    [Test]
    public void QuantizationAndHuffmanDefinitionsRetainExactValues()
    {
        JpegDocument document = JpegParser.Parse(BuildBaselineStream(), new RawLimits(), CancellationToken.None);
        JpegQuantizationTable eightBit = document.QuantizationTables[0];
        JpegQuantizationTable sixteenBit = document.QuantizationTables[1];
        JpegHuffmanTableDefinition dc = document.HuffmanTables[0];
        JpegHuffmanTableDefinition ac = document.HuffmanTables[1];

        Assert.Multiple(() =>
        {
            Assert.That(eightBit.Precision, Is.EqualTo(8));
            Assert.That(eightBit.Identifier, Is.Zero);
            Assert.That(eightBit.Values, Is.EqualTo(Enumerable.Range(1, 64).Select(value => (ushort)value)));
            Assert.That(sixteenBit.Precision, Is.EqualTo(16));
            Assert.That(sixteenBit.Identifier, Is.EqualTo(1));
            Assert.That((sixteenBit.Values ?? throw new System.InvalidOperationException("A required value is unavailable."))[0], Is.EqualTo(0x0100));
            Assert.That(sixteenBit.Values[63], Is.EqualTo(0x013f));
            Assert.That(dc.TableClass, Is.Zero);
            Assert.That(dc.Identifier, Is.Zero);
            Assert.That(((dc.CodeLengthCounts ?? throw new System.InvalidOperationException("A required value is unavailable.")).Take(2) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(new byte[] { 1, 1 }));
            Assert.That(dc.Symbols, Is.EqualTo(new byte[] { 0, 1 }));
            Assert.That((dc.Table ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetSymbolCount(), Is.EqualTo(2));
            Assert.That(ac.TableClass, Is.EqualTo(1));
            Assert.That(ac.Identifier, Is.EqualTo(1));
            Assert.That(ac.Symbols, Is.EqualTo(new byte[] { 0 }));
        });
    }

    [Test]
    public void UnknownAndFillPrefixedMarkersArePreservedByteForByte()
    {
        byte[] source = BuildBaselineStream();

        JpegDocument document = JpegParser.Parse(source, new RawLimits(), CancellationToken.None);
        JpegMarker application = document.Markers.Single(marker => marker.Code == 0xe1);

        Assert.Multiple(() =>
        {
            Assert.That(application.RawBytes, Is.EqualTo(new byte[] { 0xff, 0xff, 0xe1, 0x00, 0x05, 0x01, 0xff, 0x02 }));
            Assert.That(application.GetPayload().ToArray(), Is.EqualTo(new byte[] { 0x01, 0xff, 0x02 }));
            Assert.That(application.DeclaredLength, Is.EqualTo(5));
            Assert.That(document.PreservedMarkerBytes, Is.EqualTo(document.Markers.Sum(marker => (marker.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)));
        });

        source[application.Offset + 5] = 0x99;
        Assert.That(application.RawBytes, Is.EqualTo(new byte[] { 0xff, 0xff, 0xe1, 0x00, 0x05, 0x01, 0xff, 0x02 }));
    }

    [Test]
    public void LosslessFrameAndPredictorScanFieldsAreDescribed()
    {
        byte[] frame = Segment(0xc3, new byte[] { 14, 0, 2, 0, 4, 1, 7, 0x11, 0 });
        byte[] scan = Segment(0xda, new byte[] { 1, 7, 0x20, 7, 0, 0 });
        byte[] source = Join(new byte[] { 0xff, 0xd8 }, frame, scan, new byte[] { 0x40 }, new byte[] { 0xff, 0xd9 });

        JpegDocument document = JpegParser.Parse(source, new RawLimits(), CancellationToken.None);
        JpegFrameHeader description = document.Frames.Single();
        JpegScanHeader scanDescription = document.Scans.Single();

        Assert.Multiple(() =>
        {
            Assert.That(description.ProcessMarker, Is.EqualTo(0xc3));
            Assert.That(description.Precision, Is.EqualTo(14));
            Assert.That(description.Width, Is.EqualTo(4));
            Assert.That(description.Height, Is.EqualTo(2));
            Assert.That(description.Components.Single().Identifier, Is.EqualTo(7));
            Assert.That(scanDescription.Components.Single().DcTable, Is.EqualTo(2));
            Assert.That(scanDescription.Components.Single().AcTable, Is.Zero);
            Assert.That(scanDescription.SpectralStart, Is.EqualTo(7));
            Assert.That(scanDescription.SpectralEnd, Is.Zero);
            Assert.That(scanDescription.SuccessiveApproximationHigh, Is.Zero);
            Assert.That(scanDescription.SuccessiveApproximationLow, Is.Zero);
        });
    }

    [Test]
    public void MultipleScansAndAbbreviatedTableStreamsAreAccepted()
    {
        byte[] frame = Segment(0xc0, new byte[] { 8, 0, 1, 0, 1, 1, 1, 0x11, 0 });
        byte[] scan = Segment(0xda, new byte[] { 1, 1, 0, 0, 63, 0 });
        byte[] counts = new byte[16];
        counts[0] = 1;
        byte[] table = Segment(0xc4, Join(new byte[] { 0 }, counts, new byte[] { 0 }));
        byte[] source = Join(new byte[] { 0xff, 0xd8 }, frame, scan, new byte[] { 1 }, table, scan, new byte[] { 2 }, new byte[] { 0xff, 0xd9 });

        JpegDocument document = JpegParser.Parse(source, new RawLimits(), CancellationToken.None);
        byte[] tablesOnly = Join(new byte[] { 0xff, 0xd8 }, table, new byte[] { 0xff, 0xd9 });
        JpegDocument abbreviated = JpegParser.Parse(tablesOnly, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(document.Scans.Count, Is.EqualTo(2));
            Assert.That(document.HuffmanTables.Count, Is.EqualTo(1));
            Assert.That(JpegEntropyCodec.Unstuff(document.Source, document.Scans[0].EntropySegments.Single(), new RawLimits(), CancellationToken.None), Is.EqualTo(new byte[] { 1 }));
            Assert.That(JpegEntropyCodec.Unstuff(document.Source, document.Scans[1].EntropySegments.Single(), new RawLimits(), CancellationToken.None), Is.EqualTo(new byte[] { 2 }));
            Assert.That(abbreviated.Frames, Is.Empty);
            Assert.That(abbreviated.Scans, Is.Empty);
            Assert.That(abbreviated.HuffmanTables.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void MissingSignaturesLengthsAndTerminatorsAreRejected()
    {
        RawFormatException? shortSignature = Assert.Throws<RawFormatException>(() => JpegParser.Parse(new byte[] { 0xff }, new RawLimits(), CancellationToken.None));
        RawFormatException? badSignature = Assert.Throws<RawFormatException>(() => JpegParser.Parse(new byte[] { 0x00, 0xd8 }, new RawLimits(), CancellationToken.None));
        RawFormatException? notSoi = Assert.Throws<RawFormatException>(() => JpegParser.Parse(new byte[] { 0xff, 0xd9 }, new RawLimits(), CancellationToken.None));
        RawFormatException? invalidLength = Assert.Throws<RawFormatException>(() => JpegParser.Parse(new byte[] { 0xff, 0xd8, 0xff, 0xe0, 0x00, 0x01 }, new RawLimits(), CancellationToken.None));
        RawFormatException? truncatedPayload = Assert.Throws<RawFormatException>(() => JpegParser.Parse(new byte[] { 0xff, 0xd8, 0xff, 0xe0, 0x00, 0x05, 1 }, new RawLimits(), CancellationToken.None));
        RawFormatException? reservedMarker = Assert.Throws<RawFormatException>(() => JpegParser.Parse(new byte[] { 0xff, 0xd8, 0xff, 0x02, 0x00, 0x02, 0xff, 0xd9 }, new RawLimits(), CancellationToken.None));
        RawFormatException? missingEoi = Assert.Throws<RawFormatException>(() => JpegParser.Parse(new byte[] { 0xff, 0xd8 }, new RawLimits(), CancellationToken.None));
        RawFormatException? trailing = Assert.Throws<RawFormatException>(() => JpegParser.Parse(new byte[] { 0xff, 0xd8, 0xff, 0xd9, 0 }, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((shortSignature ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((badSignature ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidSignature));
            Assert.That((notSoi ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidSignature));
            Assert.That((invalidLength ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((truncatedPayload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((reservedMarker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((missingEoi ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((trailing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void InvalidTableDefinitionsAreRejected()
    {
        byte[] counts = new byte[16];
        counts[0] = 3;
        byte[] completeCounts = new byte[16];
        completeCounts[0] = 2;
        RawFormatException? dqtPrecision = ParseHeaderFailure(Segment(0xdb, Join(new byte[] { 0x20 }, new byte[64])));
        RawFormatException? dqtTruncated = ParseHeaderFailure(Segment(0xdb, Join(new byte[] { 0 }, new byte[63])));
        RawFormatException? dqtZero = ParseHeaderFailure(Segment(0xdb, Join(new byte[] { 0 }, new byte[64])));
        RawFormatException? dhtClass = ParseHeaderFailure(Segment(0xc4, Join(new byte[] { 0x20 }, new byte[16], new byte[] { 0 })));
        RawFormatException? dhtTruncated = ParseHeaderFailure(Segment(0xc4, Join(new byte[] { 0 }, new byte[15])));
        RawFormatException? dhtOversubscribed = ParseHeaderFailure(Segment(0xc4, Join(new byte[] { 0 }, counts, new byte[] { 1, 2, 3 })));
        RawFormatException? dhtAllOnes = ParseHeaderFailure(Segment(0xc4, Join(new byte[] { 0 }, completeCounts, new byte[] { 1, 2 })));

        Assert.Multiple(() =>
        {
            Assert.That((dqtPrecision ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((dqtTruncated ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((dqtZero ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((dhtClass ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((dhtTruncated ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((dhtOversubscribed ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((dhtAllOnes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void InvalidDriFrameAndScanDescriptionsAreRejected()
    {
        RawFormatException? dri = ParseHeaderFailure(Segment(0xdd, new byte[] { 1 }));
        RawFormatException? shortFrame = ParseHeaderFailure(Segment(0xc0, new byte[] { 8, 0, 1 }));
        RawFormatException? duplicateFrameComponent = ParseHeaderFailure(Segment(0xc0, new byte[] { 8, 0, 1, 0, 1, 2, 1, 0x11, 0, 1, 0x11, 0 }));
        byte[] scan = Segment(0xda, new byte[] { 1, 2, 0, 0, 63, 0 });
        RawFormatException? missingFrame = ParseHeaderFailure(scan);
        byte[] frame = Segment(0xc0, new byte[] { 8, 0, 1, 0, 1, 1, 1, 0x11, 0 });
        RawFormatException? absentComponent = Assert.Throws<RawFormatException>(() => JpegParser.Parse(Join(new byte[] { 0xff, 0xd8 }, frame, scan, new byte[] { 1 }, new byte[] { 0xff, 0xd9 }), new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((dri ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((shortFrame ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((duplicateFrameComponent ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((missingFrame ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
            Assert.That((absentComponent ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void RestartMarkersRequireDriAndStrictModuloEightOrder()
    {
        byte[] frame = Segment(0xc0, new byte[] { 8, 0, 1, 0, 1, 1, 1, 0x11, 0 });
        byte[] scan = Segment(0xda, new byte[] { 1, 1, 0, 0, 63, 0 });
        byte[] dri = Segment(0xdd, new byte[] { 0, 1 });
        byte[] withoutDri = Join(new byte[] { 0xff, 0xd8 }, frame, scan, new byte[] { 1, 0xff, 0xd0, 2, 0xff, 0xd9 });
        byte[] wrongOrder = Join(new byte[] { 0xff, 0xd8 }, frame, dri, scan, new byte[] { 1, 0xff, 0xd1, 2, 0xff, 0xd9 });
        byte[] outsideScan = Join(new byte[] { 0xff, 0xd8, 0xff, 0xd0, 0xff, 0xd9 });

        RawFormatException? missingDri = Assert.Throws<RawFormatException>(() => JpegParser.Parse(withoutDri, new RawLimits(), CancellationToken.None));
        RawFormatException? sequence = Assert.Throws<RawFormatException>(() => JpegParser.Parse(wrongOrder, new RawLimits(), CancellationToken.None));
        RawFormatException? outside = Assert.Throws<RawFormatException>(() => JpegParser.Parse(outsideScan, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((missingDri ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((sequence ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((outside ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void InvalidEntropyStuffingAndMissingScanDataAreRejected()
    {
        byte[] frame = Segment(0xc0, new byte[] { 8, 0, 1, 0, 1, 1, 1, 0x11, 0 });
        byte[] scan = Segment(0xda, new byte[] { 1, 1, 0, 0, 63, 0 });
        byte[] fillBeforeStuff = Join(new byte[] { 0xff, 0xd8 }, frame, scan, new byte[] { 1, 0xff, 0xff, 0x00, 2, 0xff, 0xd9 });
        byte[] emptyScan = Join(new byte[] { 0xff, 0xd8 }, frame, scan, new byte[] { 0xff, 0xd9 });
        byte[] unterminated = Join(new byte[] { 0xff, 0xd8 }, frame, scan, new byte[] { 1, 2, 3 });

        RawFormatException? stuffing = Assert.Throws<RawFormatException>(() => JpegParser.Parse(fillBeforeStuff, new RawLimits(), CancellationToken.None));
        RawFormatException? empty = Assert.Throws<RawFormatException>(() => JpegParser.Parse(emptyScan, new RawLimits(), CancellationToken.None));
        RawFormatException? end = Assert.Throws<RawFormatException>(() => JpegParser.Parse(unterminated, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((stuffing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((empty ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((end ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
        });
    }

    [Test]
    public void EntropyUnstuffingValidatesItsParsedBoundsAndContents()
    {
        byte[] encoded = new byte[] { 1, 0xff, 0, 2 };
        JpegEntropySegment valid = new JpegEntropySegment { Offset = 0, EncodedLength = 4, DecodedLength = 3 };
        JpegEntropySegment unescaped = new JpegEntropySegment { Offset = 0, EncodedLength = 2, DecodedLength = 2 };
        JpegEntropySegment wrongLength = new JpegEntropySegment { Offset = 0, EncodedLength = 4, DecodedLength = 4 };

        byte[] decoded = JpegEntropyCodec.Unstuff(encoded, valid, new RawLimits(), CancellationToken.None);
        RawFormatException? marker = Assert.Throws<RawFormatException>(() => JpegEntropyCodec.Unstuff(new byte[] { 0xff, 0xd0 }, unescaped, new RawLimits(), CancellationToken.None));
        RawFormatException? length = Assert.Throws<RawFormatException>(() => JpegEntropyCodec.Unstuff(encoded, wrongLength, new RawLimits(), CancellationToken.None));
        RawFormatException? missing = Assert.Throws<RawFormatException>(() => JpegEntropyCodec.Unstuff(encoded, null, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(new byte[] { 1, 0xff, 2 }));
            Assert.That((marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((length ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((missing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
        });
    }

    [Test]
    public void FileMarkerAllocationAndDimensionLimitsAreEnforcedBeforeRetention()
    {
        byte[] source = BuildBaselineStream();
        RawLimits fileLimits = new RawLimits();
        fileLimits.MaxFileSizeBytes = source.Length - 1;
        RawLimits markerLimits = new RawLimits();
        markerLimits.MaxMetadataBytes = 1;
        RawLimits allocationLimits = new RawLimits();
        allocationLimits.MaxAllocationBytes = 1;
        RawLimits dimensionLimits = new RawLimits();
        dimensionLimits.MaxWidth = 2;

        RawLimitException? file = Assert.Throws<RawLimitException>(() => JpegParser.Parse(source, fileLimits, CancellationToken.None));
        RawLimitException? marker = Assert.Throws<RawLimitException>(() => JpegParser.Parse(source, markerLimits, CancellationToken.None));
        RawLimitException? allocation = Assert.Throws<RawLimitException>(() => JpegParser.Parse(source, allocationLimits, CancellationToken.None));
        RawLimitException? dimension = Assert.Throws<RawLimitException>(() => JpegParser.Parse(source, dimensionLimits, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((file ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((allocation ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((dimension ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void ParsingAndUnstuffingHonorPreCanceledTokens()
    {
        byte[] source = BuildBaselineStream();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        JpegEntropySegment segment = new JpegEntropySegment { Offset = 0, EncodedLength = 1, DecodedLength = 1 };

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => JpegParser.Parse(source, new RawLimits(), cancellation.Token));
            Assert.Throws<OperationCanceledException>(() => JpegEntropyCodec.Unstuff(new byte[] { 1 }, segment, new RawLimits(), cancellation.Token));
        });
    }

    private static RawFormatException? ParseHeaderFailure(byte[] marker)
    {
        return Assert.Throws<RawFormatException>(() => JpegParser.Parse(Join(new byte[] { 0xff, 0xd8 }, marker, new byte[] { 0xff, 0xd9 }), new RawLimits(), CancellationToken.None));
    }

    private static byte[] BuildBaselineStream()
    {
        List<byte> quantization = new List<byte>();
        quantization.Add(0);
        for (int index = 1; index <= 64; index++)
        {
            quantization.Add((byte)index);
        }
        quantization.Add(0x11);
        for (int index = 0; index < 64; index++)
        {
            quantization.Add(0x01);
            quantization.Add((byte)index);
        }

        byte[] dcCounts = new byte[16];
        dcCounts[0] = 1;
        dcCounts[1] = 1;
        byte[] acCounts = new byte[16];
        acCounts[0] = 1;
        byte[] huffman = Join(new byte[] { 0 }, dcCounts, new byte[] { 0, 1 }, new byte[] { 0x11 }, acCounts, new byte[] { 0 });
        byte[] frame = new byte[]
        {
            8, 0, 2, 0, 3, 3,
            1, 0x22, 0,
            2, 0x11, 1,
            3, 0x11, 1
        };
        byte[] scan = new byte[]
        {
            3,
            1, 0x00,
            2, 0x11,
            3, 0x11,
            0, 63, 0
        };
        byte[] entropy = new byte[] { 0x12, 0xff, 0x00, 0x34, 0xff, 0xd0, 0x56, 0xff, 0x00, 0x78, 0xff, 0xd1, 0x9a };
        return Join(
            new byte[] { 0xff, 0xd8 },
            new byte[] { 0xff, 0xff, 0xe1, 0x00, 0x05, 0x01, 0xff, 0x02 },
            Segment(0xdb, quantization.ToArray()),
            Segment(0xc4, huffman),
            Segment(0xc0, frame),
            Segment(0xdd, new byte[] { 0, 4 }),
            Segment(0xda, scan),
            entropy,
            new byte[] { 0xff, 0xd9 });
    }

    private static byte[] Segment(byte marker, byte[] payload)
    {
        int length = payload.Length + 2;
        return Join(new byte[] { 0xff, marker, (byte)(length >> 8), (byte)length }, payload);
    }

    private static byte[] Join(params byte[][] arrays)
    {
        int length = arrays.Sum(array => array.Length);
        byte[] result = new byte[length];
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            array.CopyTo(result, offset);
            offset += array.Length;
        }
        return result;
    }
}
