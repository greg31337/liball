using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PayloadParserTests
{
    static IEnumerable<TestCaseData> ModesAndProfiles()
    {
        foreach (int version in new[] { 1, 9, 10, 26, 27, 40 })
            foreach (QrEncodingMode mode in DataModes()) yield return new TestCaseData(QrSymbolKind.Model2, version, mode);
        foreach (int version in new[] { 1, 9, 10, 14 })
            foreach (QrEncodingMode mode in DataModes()) yield return new TestCaseData(QrSymbolKind.LegacyModel1, version, mode);
        for (int version = 1; version <= 4; version++)
            for (int mode = 0; mode < (version == 1 ? 1 : version == 2 ? 2 : 4); mode++)
                yield return new TestCaseData(QrSymbolKind.Micro, version, DataModes()[mode]);
        for (int version = 0; version < 32; version++)
            foreach (QrEncodingMode mode in DataModes()) yield return new TestCaseData(QrSymbolKind.rMQR, version, mode);
    }

    [TestCaseSource(nameof(ModesAndProfiles))]
    public void ParsesAllDataModesAcrossProfilesAndCountWidthBoundaries(QrSymbolKind kind, int version, QrEncodingMode mode)
    {
        QrSegment segment = Segment(mode);
        int level = kind == QrSymbolKind.Micro && version == 1 ? -1 : 0;
        byte[] data = CodewordFinalizer.CreateDataCodewords([segment], kind, version, level, false);
        int bits = kind == QrSymbolKind.LegacyModel1 ? LegacyModel1Table.GetDataBitCount(version, level) : Capacity(kind, version, level);
        byte[] before = (byte[])data.Clone();
        PayloadParseResult result = PayloadParser.Parse(data, bits, kind, version, new QrDecodeOptions());
        Assert.That(result.SymbolKind, Is.EqualTo(kind));
        Assert.That(result.Segments, Has.Length.EqualTo(1));
        Assert.That(result.Segments[0].Mode, Is.EqualTo(mode));
        Assert.That(result.Segments[0].CharacterCount, Is.EqualTo(segment.CharacterCount));
        Assert.That(result.HasTerminator, Is.True);
        Assert.That(result.PayloadBitCount, Is.EqualTo(CodewordFinalizer.CalculateRequiredBitCount([segment], kind, version, false, 10, 100)));
        if (mode == QrEncodingMode.Numeric || mode == QrEncodingMode.Alphanumeric)
            Assert.That(result.Segments[0].Text, Is.EqualTo(segment.Text));
        else if (mode == QrEncodingMode.Byte)
        {
            Assert.That(result.Segments[0].Data, Is.EqualTo(segment.Data));
            Assert.That(result.Segments[0].Text, Is.Null);
        }
        else
        {
            Assert.That(result.Segments[0].Data, Is.EqualTo(new byte[] { 0x8a, 0xbf }));
            Assert.That(result.Segments[0].Text, Is.Null);
        }
        Assert.That(data, Is.EqualTo(before));
    }

    [Test]
    public void ReadsIndependentMixedModeBitstreamAndTracksEciChanges()
    {
        // Numeric '7', ECI 26, byte 00 FF, ECI 3, byte 41, terminator.
        BitBuffer bits = Bits((1, 4), (1, 10), (7, 4), (7, 4), (26, 8),
            (4, 4), (2, 8), (0, 8), (255, 8), (7, 4), (3, 8), (4, 4), (1, 8), (65, 8), (0, 4));
        PayloadParseResult result = Parse(bits);
        Assert.That(result.Segments.Select(s => s.Mode), Is.EqualTo(new[] { QrEncodingMode.Numeric, QrEncodingMode.Eci, QrEncodingMode.Byte, QrEncodingMode.Eci, QrEncodingMode.Byte }));
        Assert.That(result.Segments[0].Text, Is.EqualTo("7"));
        Assert.That(result.Segments[2].Data, Is.EqualTo(new byte[] { 0, 255 }));
        Assert.That(result.Segments[2].EciAssignment, Is.EqualTo(26));
        Assert.That(result.Segments[4].EciAssignment, Is.EqualTo(3));
        Assert.That(result.ActiveEciAssignment, Is.EqualTo(3));
        Assert.That(result.HasTerminator, Is.True);
    }

    [TestCase(127)]
    [TestCase(128)]
    [TestCase(16383)]
    [TestCase(16384)]
    [TestCase(999999)]
    public void PreservesAllEciDesignatorWidths(int assignment)
    {
        BitBuffer bits = Bits((7, 4));
        if (assignment < 128) bits.AppendBits((uint)assignment, 8);
        else if (assignment < 16384) bits.AppendBits((uint)(0x8000 | assignment), 16);
        else bits.AppendBits((uint)(0xc00000 | assignment), 24);
        PayloadParseResult result = Parse(bits);
        Assert.That(result.Segments.Single().EciAssignment, Is.EqualTo(assignment));
        Assert.That(result.ActiveEciAssignment, Is.EqualTo(assignment));
        Assert.That(result.HasTerminator, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Fnc1UnescapesAlphanumericWhileRetainingEncodedData(bool second)
    {
        BitBuffer bits = Bits((second ? 9 : 5, 4));
        if (second) bits.AppendBits(165, 8);
        // '%%%' represents literal percent followed by GS.
        bits.AppendBits(2, 4); bits.AppendBits(3, 9);
        bits.AppendBits(38 * 45 + 38, 11); bits.AppendBits(38, 6);
        PayloadParseResult result = Parse(bits);
        Assert.That(result.Segments[0].Mode, Is.EqualTo(second ? QrEncodingMode.Fnc1Second : QrEncodingMode.Fnc1First));
        Assert.That(result.Segments[0].Fnc1ApplicationIndicator, Is.EqualTo(second ? 165 : -1));
        Assert.That(result.Segments[1].Text, Is.EqualTo("%\u001d"));
        Assert.That(result.Segments[1].Data, Is.EqualTo(new byte[] { 37, 37, 37 }));
        Assert.That(result.Segments[1].CharacterCount, Is.EqualTo(3));
    }

    [Test]
    public void StructuredAppendMayPrecedeFnc1AndEci()
    {
        PayloadParseResult result = Parse(Bits((3, 4), (1, 4), (2, 4), (0xa5, 8), (5, 4), (7, 4), (26, 8)));
        QrStructuredAppendInfo info = result.Segments[0].StructuredAppend!;
        Assert.That(info.Index, Is.EqualTo(1));
        Assert.That(info.Total, Is.EqualTo(3));
        Assert.That(info.Parity, Is.EqualTo(0xa5));
        Assert.That(info.IsComplete, Is.False);
        Assert.That(result.Segments, Has.Length.EqualTo(3));
    }

    [Test]
    public void RectangularControlIndicatorsUseThreeBits()
    {
        BitBuffer bits = Bits((6, 3), (165, 8), (7, 3), (26, 8), (3, 3), (1, 3), (0xab, 8), (0, 3));
        PayloadParseResult result = Parse(bits, QrSymbolKind.rMQR, 0);
        Assert.That(result.Segments.Select(s => s.Mode), Is.EqualTo(new[] { QrEncodingMode.Fnc1Second, QrEncodingMode.Eci, QrEncodingMode.Byte }));
        Assert.That(result.Segments[2].Data, Is.EqualTo(new byte[] { 0xab }));
        Assert.That(result.Segments[2].EciAssignment, Is.EqualTo(26));
    }

    [Test]
    public void ReadsHanziSubsetBeforeCountAndRetainsGb2312Bytes()
    {
        QrSegment segment = HanziEncoder.CreateCompatibilitySegment("汉", true);
        byte[] data = CodewordFinalizer.CreateDataCodewords([segment], QrSymbolKind.Model2, 1, 0, true);
        PayloadParseResult result = PayloadParser.Parse(data, data.Length * 8, QrSymbolKind.Model2, 1, new QrDecodeOptions { EnableHanziCompatibility = true });
        Assert.That(result.Segments.Single().Mode, Is.EqualTo(QrEncodingMode.Hanzi));
        Assert.That(result.Segments[0].Data, Is.EqualTo(new byte[] { 0xba, 0xba }));
        Assert.That(result.Segments[0].IsHanziCompatibilityMode, Is.True);
        Assert.That(result.Segments[0].Text, Is.Null);
        Assert.Throws<QrDecodeException>(() => PayloadParser.Parse(data, data.Length * 8, QrSymbolKind.Model2, 1, new QrDecodeOptions()));
    }

    [Test]
    public void RetainsInRangeKanjiBytesWithoutRequiringACharacterMapping()
    {
        PayloadParseResult result = Parse(Bits((8, 4), (1, 8), (0x1fff, 13)));
        Assert.That(result.Segments.Single().Data, Is.EqualTo(new byte[] { 0xeb, 0xbf }));
        Assert.That(result.Segments[0].Text, Is.Null);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void AcceptsFullCapacityAndTruncatedTerminators(int zeros)
    {
        BitBuffer bits = Bits((1, 4), (1, 10), (7, 4));
        bits.AppendBits(0, zeros);
        PayloadParseResult result = Parse(bits);
        Assert.That(result.Segments[0].Text, Is.EqualTo("7"));
        Assert.That(result.PayloadBitCount, Is.EqualTo(18));
        Assert.That(result.HasTerminator, Is.EqualTo(zeros != 0));
    }

    [TestCase(22)]
    [TestCase(24)]
    [TestCase(32)]
    public void StrictPaddingRejectsNonzeroAlignmentAndWrongPadWords(int bit)
    {
        byte[] data = CodewordFinalizer.CreateDataCodewords([new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = 1, Text = "7" }], QrSymbolKind.Model2, 1, 0, false);
        data[bit / 8] ^= (byte)(1 << (7 - bit % 8));
        Assert.Throws<QrDecodeException>(() => PayloadParser.Parse(data, data.Length * 8, QrSymbolKind.Model2, 1, new QrDecodeOptions()));
        Assert.That(PayloadParser.Parse(data, data.Length * 8, QrSymbolKind.Model2, 1, new QrDecodeOptions { Strict = false }).Segments.Single().Text, Is.EqualTo("7"));
    }

    [Test]
    public void MicroFinalPartialPadByteMustBeZero()
    {
        byte[] data = CodewordFinalizer.CreateDataCodewords([new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = 1, Text = "7" }], QrSymbolKind.Micro, 1, -1, false);
        data[2] |= 0x10;
        Assert.Throws<QrDecodeException>(() => PayloadParser.Parse(data, 20, QrSymbolKind.Micro, 1, new QrDecodeOptions()));
        Assert.That(PayloadParser.Parse(data, 20, QrSymbolKind.Micro, 1, new QrDecodeOptions { Strict = false }).Segments[0].Text, Is.EqualTo("7"));
    }

    [TestCase(6)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(14)]
    [TestCase(15)]
    public void ReservedModel2ModesAreRejected(int mode)
    {
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => Parse(Bits((mode, 4))))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void ReservedMicroM4ModesAreRejected(int mode)
    {
        Assert.Throws<QrDecodeException>(() => Parse(Bits((mode, 3), (1, 6)), QrSymbolKind.Micro, 4));
    }

    [Test]
    public void RejectsTruncatedSegmentsInvalidValuesAndMalformedControls()
    {
        BitBuffer[] invalid =
        [
            Bits((1, 2)), Bits((1, 4)), Bits((4, 4), (2, 8), (65, 8)),
            Bits((1, 4), (3, 10), (1000, 10)), Bits((1, 4), (2, 10), (100, 7)), Bits((1, 4), (1, 10), (10, 4)),
            Bits((2, 4), (2, 9), (2025, 11)), Bits((2, 4), (1, 9), (45, 6)),
            Bits((8, 4), (1, 8), (30 * 192 + 189, 13)),
            Bits((7, 4), (0xe0, 8)), Bits((7, 4), (0x807f, 16)), Bits((7, 4), (0xc0007f, 24)), Bits((7, 4), (0x80, 8)),
            Bits((9, 4), (100, 8)), Bits((9, 4), (1, 3)),
            Bits((3, 4), (0, 4), (0, 4), (0, 8)), Bits((3, 4), (2, 4), (1, 4), (0, 8)), Bits((3, 4), (0, 4)),
            Bits((5, 4), (5, 4)), Bits((7, 4), (26, 8), (5, 4)),
            Bits((1, 4), (1, 10), (7, 4), (9, 4), (165, 8)), Bits((5, 4), (3, 4), (0, 4), (1, 4), (0, 8))
        ];
        foreach (BitBuffer bits in invalid) Assert.Throws<QrDecodeException>(() => Parse(bits), $"Invalid stream {Convert.ToHexString(bits.GetBytes())}");
        Assert.Throws<QrDecodeException>(() => Parse(Bits((13, 4), (2, 4), (1, 8), (0, 13)), QrSymbolKind.Model2, 1, new QrDecodeOptions { EnableHanziCompatibility = true }));
        Assert.Throws<QrDecodeException>(() => Parse(Bits((13, 4), (1, 4), (1, 8), (0x1fff, 13)), QrSymbolKind.Model2, 1, new QrDecodeOptions { EnableHanziCompatibility = true }));
    }

    [Test]
    public void LimitsIncludeControlSegmentsAndCheckAllocationBeforeLargeCounts()
    {
        BitBuffer bits = Bits((7, 4), (26, 8), (7, 4), (3, 8));
        Assert.Throws<QrResourceLimitException>(() => Parse(bits, QrSymbolKind.Model2, 1, new QrDecodeOptions { MaximumSegments = 1 }));
        Assert.Throws<QrResourceLimitException>(() => Parse(bits, QrSymbolKind.Model2, 1, new QrDecodeOptions { MaximumAllocationBytes = 300 }));
        Assert.Throws<QrDecodeException>(() => Parse(Bits((4, 4), (65535, 16)), QrSymbolKind.Model2, 27));
        Assert.Throws<QrInputException>(() => Parse(bits, QrSymbolKind.Model2, 1, new QrDecodeOptions { MaximumSegments = 0 }));
        Assert.Throws<QrInputException>(() => PayloadParser.Parse(null!, 0, QrSymbolKind.Model2, 1, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => PayloadParser.Parse([], 0, QrSymbolKind.Model2, 1, null!));
        Assert.Throws<QrInputException>(() => PayloadParser.Parse([0], 16, QrSymbolKind.Model2, 1, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => PayloadParser.Parse([], -1, QrSymbolKind.Model2, 1, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => PayloadParser.Parse([], 0, QrSymbolKind.LegacyModel1, 1, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => PayloadParser.Parse([], 0, QrSymbolKind.Micro, 5, new QrDecodeOptions()));
        Assert.Throws<QrResourceLimitException>(() => Parse(bits, QrSymbolKind.Model2, 1, new QrDecodeOptions { MaximumMatrixDimension = 20 }));
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    public void ParsesCorrectedEncoderPayload(QrSymbolKind kind)
    {
        QrSymbol symbol = new QrEncoder().EncodeText("12345", new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = kind });
        MatrixCodewordResult codewords = MatrixCodewordReader.Read(symbol.Matrix!, kind, new QrDecodeOptions());
        PayloadParseResult result = PayloadParser.Parse(codewords.DataCodewords, codewords.DataBitCount, kind, codewords.Version, new QrDecodeOptions());
        Assert.That(result.Segments.Single().Text, Is.EqualTo("12345"));
    }

    static QrEncodingMode[] DataModes() => [QrEncodingMode.Numeric, QrEncodingMode.Alphanumeric, QrEncodingMode.Byte, QrEncodingMode.Kanji];

    static QrSegment Segment(QrEncodingMode mode) => mode switch
    {
        QrEncodingMode.Numeric => new QrSegment { Mode = mode, CharacterCount = 2, Text = "12" },
        QrEncodingMode.Alphanumeric => new QrSegment { Mode = mode, CharacterCount = 1, Text = "A" },
        QrEncodingMode.Byte => new QrSegment { Mode = mode, CharacterCount = 1, Data = [0x80] },
        _ => new QrSegment { Mode = mode, CharacterCount = 1, Text = "漢" }
    };

    static int Capacity(QrSymbolKind kind, int version, int level) => kind == QrSymbolKind.Model2 ? EccBlockTable.GetDataCodewordCount(version, level) * 8
        : kind == QrSymbolKind.Micro ? MicroVersionTable.GetDataBitCapacity(version, level) : RectangularMicroVersionTable.GetDataBitCapacity(version, level);

    static BitBuffer Bits(params (int value, int count)[] fields)
    {
        BitBuffer buffer = new BitBuffer(0);
        foreach ((int value, int count) in fields) buffer.AppendBits((uint)value, count);
        return buffer;
    }

    static PayloadParseResult Parse(BitBuffer bits, QrSymbolKind kind = QrSymbolKind.Model2, int version = 1, QrDecodeOptions? options = null)
        => PayloadParser.Parse(bits.GetBytes(), bits.GetBitLength(), kind, version, options ?? new QrDecodeOptions());
}
