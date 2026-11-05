using System.Text;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class DecodedPayloadBuilderTests
{
    static IEnumerable<TestCaseData> ValidMappings()
    {
        yield return new TestCaseData(1, new byte[] { 65, 0, 233, 255 }, "A\0éÿ");
        yield return new TestCaseData(3, new byte[] { 65, 0, 233, 255 }, "A\0éÿ");
        yield return new TestCaseData(20, new byte[] { 0x8a, 0xbf, 0xa6, 0x5c, 0 }, "漢ｦ¥\0");
        yield return new TestCaseData(29, new byte[] { 0xba, 0xba, 65, 0 }, "汉A\0");
        yield return new TestCaseData(27, new byte[] { 65, 0, 127 }, "A\0\u007f");
        string text = "A€😀\uFFFD";
        foreach (int assignment in new[] { 25, 26, 33, 34, 35 })
        {
            Encoding encoding = assignment switch
            {
                25 => new UnicodeEncoding(true, false, true), 26 => new UTF8Encoding(false, true),
                33 => new UnicodeEncoding(false, false, true), 34 => new UTF32Encoding(true, false, true),
                _ => new UTF32Encoding(false, false, true)
            };
            yield return new TestCaseData(assignment, encoding.GetBytes(text), text);
        }
    }

    static IEnumerable<TestCaseData> InvalidMappings()
    {
        foreach (byte[] bytes in new byte[][] { [0xc0, 0xaf], [0xe2, 0x82], [0x80], [0xed, 0xa0, 0x80], [0xf4, 0x90, 0x80, 0x80] })
            yield return new TestCaseData(26, bytes);
        yield return new TestCaseData(25, new byte[] { 0 });
        yield return new TestCaseData(25, new byte[] { 0xd8, 0, 0, 65 });
        yield return new TestCaseData(33, new byte[] { 0, 0xdc });
        yield return new TestCaseData(34, new byte[] { 0, 0, 0xd8, 0 });
        yield return new TestCaseData(35, new byte[] { 0, 0, 0x11, 0 });
        yield return new TestCaseData(35, new byte[] { 65, 0, 0 });
        yield return new TestCaseData(27, new byte[] { 0x80 });
        yield return new TestCaseData(20, new byte[] { 0x81 });
        yield return new TestCaseData(20, new byte[] { 0xeb, 0xbf });
        yield return new TestCaseData(29, new byte[] { 0xa1 });
        yield return new TestCaseData(29, new byte[] { 0xff, 0xff });
    }

    [TestCaseSource(nameof(ValidMappings))]
    public void DecodesAllBuiltInMappingsWithoutLosingBytes(int assignment, byte[] bytes, string expected)
    {
        byte[] before = (byte[])bytes.Clone();
        QrDecodeResult result = Create(Eci(assignment), Byte(bytes));
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.Segments[1].Text, Is.EqualTo(expected));
        Assert.That(result.Segments[1].EciAssignment, Is.EqualTo(assignment));
        Assert.That(result.RawBytes, Is.EqualTo(bytes));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { assignment }));
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(EciTextTranscoder.TryDecodeBytes(bytes, assignment), Is.EqualTo(expected));
        Assert.That(bytes, Is.EqualTo(before));
    }

    [TestCaseSource(nameof(InvalidMappings))]
    public void MalformedMappingsProduceWarningsAndKeepRawData(int assignment, byte[] bytes)
    {
        Assert.That(EciTextTranscoder.TryDecodeBytes(bytes, assignment), Is.Null);
        QrDecodeResult result = Create(Eci(assignment), Byte(bytes), Text("7"));
        Assert.That(result.Text, Is.Null);
        Assert.That(result.Segments[1].Text, Is.Null);
        Assert.That(result.Segments[2].Text, Is.EqualTo("7"));
        Assert.That(result.Segments[1].Data, Is.EqualTo(bytes));
        Assert.That(result.RawBytes, Is.EqualTo(bytes.Concat(new byte[] { 55 })));
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0], Does.Contain("Segment 1").And.Contain($"ECI {assignment}"));
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(899)]
    [TestCase(1800)]
    [TestCase(999999)]
    public void UnsupportedAssignmentsRemainDistinctFromMalformedMappings(int assignment)
    {
        QrDecodeResult result = Create(Eci(assignment), Byte([0, 255, 65]));
        Assert.That(result.Text, Is.Null);
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 0, 255, 65 }));
        Assert.That(result.Warnings.Single(), Does.Contain("unsupported").And.Contain(assignment.ToString()));
        Assert.That(EciTextTranscoder.TryDecodeBytes([65], assignment), Is.Null);
        QrUnsupportedFeatureException error = Assert.Throws<QrUnsupportedFeatureException>(() => DecodedPayloadBuilder.Create(
            new PayloadParseResult { Segments = [Eci(assignment), Byte([65])] }, new QrDecodeOptions { PreserveUnsupportedEciBytes = false }))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
        Assert.That(error.ProtocolValue, Is.EqualTo(assignment));
    }

    [Test]
    public void TracksRepeatedEciControlsAndDoesNotLetStaleSegmentMetadataOverrideThem()
    {
        QrSegment stale = Byte([0xe9]);
        stale.EciAssignment = 26;
        QrDecodeResult result = Create(Eci(3), stale, Eci(26), Byte([0xc3, 0xa9]), Eci(26), Byte([65]));
        Assert.That(result.Text, Is.EqualTo("ééA"));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { 3, 26, 26 }));
        Assert.That(result.Segments.Select(s => s.Mode), Is.EqualTo(new[] { QrEncodingMode.Eci, QrEncodingMode.Byte, QrEncodingMode.Eci, QrEncodingMode.Byte, QrEncodingMode.Eci, QrEncodingMode.Byte }));
        Assert.That(result.Segments[1].EciAssignment, Is.EqualTo(3));
        Assert.That(stale.EciAssignment, Is.EqualTo(26));
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 0xe9, 0xc3, 0xa9, 65 }));
    }

    [Test]
    public void DefaultInterpretationAndOptionalInferenceAreExplicit()
    {
        PayloadParseResult parsed = new PayloadParseResult { Segments = [Byte([0xc3, 0xa9])] };
        QrDecodeResult ordinary = DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions());
        Assert.That(ordinary.Text, Is.EqualTo("Ã©"));
        Assert.That(ordinary.Segments[0].EciAssignment, Is.EqualTo(3));
        Assert.That(ordinary.EciAssignments, Is.Empty);
        QrDecodeResult inferred = DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions { EnableByteEncodingInference = true });
        Assert.That(inferred.Text, Is.EqualTo("é"));
        Assert.That(inferred.Segments[0].EciAssignment, Is.EqualTo(26));
        Assert.That(inferred.EciAssignments, Is.Empty);
        Assert.That(inferred.Warnings.Single(), Does.Contain("inferred"));
        Assert.Throws<QrDecodeException>(() => DecodedPayloadBuilder.Create(parsed,
            new QrDecodeOptions { EnableByteEncodingInference = true, RequireExplicitEci = true }));
        Assert.That(parsed.Segments[0].EciAssignment, Is.EqualTo(-1));
    }

    [TestCase(QrEncodingMode.Kanji, "8ABF", "漢")]
    [TestCase(QrEncodingMode.Hanzi, "BABA", "汉")]
    public void FixedCharacterModesUseTheirOwnMappingsRegardlessOfEci(QrEncodingMode mode, string hex, string text)
    {
        QrDecodeResult result = Create(Eci(899), new QrSegment { Mode = mode, CharacterCount = 1, Data = Convert.FromHexString(hex), IsHanziCompatibilityMode = mode == QrEncodingMode.Hanzi });
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.Segments[1].Text, Is.EqualTo(text));
        Assert.That(result.UsesHanziCompatibility, Is.EqualTo(mode == QrEncodingMode.Hanzi));
        Assert.That(result.Warnings, Is.Empty);
    }

    [TestCase(QrEncodingMode.Kanji, "EBBF")]
    [TestCase(QrEncodingMode.Hanzi, "AAFE")]
    public void UnassignedFixedCharacterMappingsAreLossless(QrEncodingMode mode, string hex)
    {
        byte[] bytes = Convert.FromHexString(hex);
        QrDecodeResult result = Create(new QrSegment { Mode = mode, CharacterCount = 1, Data = bytes, IsHanziCompatibilityMode = mode == QrEncodingMode.Hanzi });
        Assert.That(result.Text, Is.Null);
        Assert.That(result.RawBytes, Is.EqualTo(bytes));
        Assert.That(result.Warnings.Single(), Does.Contain(mode.ToString()));
    }

    [Test]
    public void PreservesControlMetadataAndReturnsIndependentMutableStorage()
    {
        QrStructuredAppendInfo info = new QrStructuredAppendInfo { Index = 1, Total = 3, Parity = 123, ApplicationIdentifier = "group" };
        QrSegment bytes = Byte([65, 0, 255]);
        PayloadParseResult parsed = new PayloadParseResult { Segments =
        [
            new QrSegment { Mode = QrEncodingMode.StructuredAppend, StructuredAppend = info },
            new QrSegment { Mode = QrEncodingMode.Fnc1Second, Fnc1ApplicationIndicator = 165, Data = [165] }, Eci(3), bytes
        ] };
        QrDecodeResult result = DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions());
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 65, 0, 255 }));
        Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(165));
        Assert.That(result.StructuredAppend!.Index, Is.EqualTo(1));
        Assert.That(result.StructuredAppend.Total, Is.EqualTo(3));
        Assert.That(result.StructuredAppend.Parity, Is.EqualTo(123));
        Assert.That(result.StructuredAppend.ApplicationIdentifier, Is.EqualTo("group"));
        result.StructuredAppend.Index = 0;
        result.Segments[3].Data[0] = 99;
        result.Segments[1].Data[0] = 0;
        result.RawBytes[1] = 100;
        Assert.That(info.Index, Is.EqualTo(1));
        Assert.That(bytes.Data, Is.EqualTo(new byte[] { 65, 0, 255 }));
        Assert.That(parsed.Segments[1].Data, Is.EqualTo(new byte[] { 165 }));
        Assert.That(result.Segments[3].Data[1], Is.Zero);
        Assert.That(result.IsConformant, Is.False);
        Assert.That(result.SymbologyIdentifier, Is.EqualTo("]Q6"));
    }

    [Test]
    public void Fnc1RetainsEncodedBytesAndInterpretedTextFromParser()
    {
        BitBuffer bits = new BitBuffer(0);
        bits.AppendBits(5, 4); bits.AppendBits(2, 4); bits.AppendBits(3, 9);
        bits.AppendBits(38 * 45 + 38, 11); bits.AppendBits(38, 6);
        PayloadParseResult parsed = PayloadParser.Parse(bits.GetBytes(), bits.GetBitLength(), QrSymbolKind.Model2, 1, new QrDecodeOptions());
        QrDecodeResult result = DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions());
        Assert.That(result.UsesFnc1First, Is.True);
        Assert.That(result.Text, Is.EqualTo("%\u001d"));
        Assert.That(result.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes("%%%")));
        Assert.That(result.Segments[1].CharacterCount, Is.EqualTo(3));
    }

    [Test]
    public void BuildsLosslessMixedEciResultsDirectlyFromParsedBits()
    {
        BitBuffer bits = new BitBuffer(0);
        bits.AppendBits(7, 4); bits.AppendBits(26, 8);
        bits.AppendBits(4, 4); bits.AppendBits(2, 8); bits.AppendBits(0xc3a9, 16);
        bits.AppendBits(7, 4); bits.AppendBits(0x8383, 16); // ECI 899.
        bits.AppendBits(4, 4); bits.AppendBits(1, 8); bits.AppendBits(255, 8);
        PayloadParseResult parsed = PayloadParser.Parse(bits.GetBytes(), bits.GetBitLength(), QrSymbolKind.Model2, 1, new QrDecodeOptions());
        QrDecodeResult result = DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions());
        Assert.That(result.Text, Is.Null);
        Assert.That(result.Segments[1].Text, Is.EqualTo("é"));
        Assert.That(result.Segments[3].Text, Is.Null);
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 0xc3, 0xa9, 255 }));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { 26, 899 }));
        Assert.That(result.Segments[2].Data, Is.EqualTo(new byte[] { 0x83, 0x83 }));
        Assert.That(result.Warnings.Single(), Does.Contain("Segment 3"));
    }

    [Test]
    public void WarningOrderIsDeterministicAndPartialTextIsNeverPresentedAsComplete()
    {
        QrSegment[] segments = [Text("1"), Eci(899), Byte([0]), Eci(26), Byte([0xc0]), Text("2")];
        QrDecodeResult first = Create(segments);
        QrDecodeResult second = Create(segments);
        Assert.That(first.Text, Is.Null);
        Assert.That(first.Warnings, Has.Length.EqualTo(2));
        Assert.That(first.Warnings[0], Does.StartWith("Segment 2:"));
        Assert.That(first.Warnings[1], Does.StartWith("Segment 4:"));
        Assert.That(first.Warnings, Is.EqualTo(second.Warnings));
        Assert.That(first.RawBytes, Is.EqualTo(new byte[] { 49, 0, 0xc0, 50 }));
    }

    [Test]
    public void EmptyAndControlOnlyPayloadsProduceEmptyTextAndBytes()
    {
        foreach (QrSegment[] segments in new QrSegment[][] { [], [Eci(26)] })
        {
            QrDecodeResult result = Create(segments);
            Assert.That(result.Text, Is.EqualTo(""));
            Assert.That(result.RawBytes, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        }
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    public void CreatesResultsFromCorrectedAndParsedEncoderOutput(QrSymbolKind kind)
    {
        QrSymbol symbol = new QrEncoder().EncodeText("12345", new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = kind });
        QrDecodeOptions options = new QrDecodeOptions();
        MatrixCodewordResult codewords = MatrixCodewordReader.Read(symbol.Matrix!, kind, options);
        PayloadParseResult parsed = PayloadParser.Parse(codewords.DataCodewords, codewords.DataBitCount, kind, codewords.Version, options);
        QrDecodeResult result = DecodedPayloadBuilder.Create(parsed, options);
        Assert.That(result.Text, Is.EqualTo("12345"));
        Assert.That(result.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes("12345")));
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void RejectsInvalidInputsAndChecksLimitsBeforeMapping()
    {
        Assert.Throws<QrInputException>(() => DecodedPayloadBuilder.Create(null!, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => DecodedPayloadBuilder.Create(new PayloadParseResult(), null!));
        foreach (QrSegment segment in new QrSegment[] { null!, new QrSegment { Mode = (QrEncodingMode)99 }, new QrSegment { Mode = QrEncodingMode.Byte, Data = null! }, Eci(-1) })
            Assert.Throws<QrInputException>(() => Create(segment));
        PayloadParseResult parsed = new PayloadParseResult { Segments = [Eci(26), Byte([65])] };
        Assert.Throws<QrResourceLimitException>(() => DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions { MaximumSegments = 1 }));
        Assert.Throws<QrResourceLimitException>(() => DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions { MaximumAllocationBytes = 64 }));
        Assert.Throws<QrInputException>(() => DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions { MaximumAllocationBytes = 0 }));
        Assert.Throws<QrInputException>(() => EciTextTranscoder.TryDecodeBytes(null!, 26));
        Assert.Throws<QrInputException>(() => EciTextTranscoder.TryDecodeBytes([], -1));
    }

    static QrSegment Eci(int assignment) => new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = assignment };
    static QrSegment Byte(byte[] bytes) => new QrSegment { Mode = QrEncodingMode.Byte, Data = bytes, CharacterCount = bytes.Length };
    static QrSegment Text(string text) => new QrSegment { Mode = QrEncodingMode.Numeric, Data = Encoding.ASCII.GetBytes(text), Text = text, CharacterCount = text.Length };
    static QrDecodeResult Create(params QrSegment[] segments) => DecodedPayloadBuilder.Create(new PayloadParseResult { Segments = segments }, new QrDecodeOptions());
}
