using System.Text;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrFnc1WorkflowTests
{
    static IEnumerable<TestCaseData> Gs1Vectors()
    {
        (string Text, string Escaped)[] vectors =
        [
            ("01095060001343521727010110ABC123\u001d21999", "01095060001343521727010110ABC123%21999"),
            ("10LOT%25\u001d21SERIAL", "10LOT%%25%21SERIAL"),
            ("10%%\u001d21999", "10%%%%%21999"),
            ("%", "%%"),
            ("\u001d", "%"),
            ("A%\u001dB%", "A%%%B%%")
        ];
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.rMQR })
            foreach (int indicator in new[] { -1, 0, 99, 165, 190 })
                for (int index = 0; index < vectors.Length; index++)
                    yield return new TestCaseData(kind, indicator, vectors[index].Text, vectors[index].Escaped)
                        .SetName($"Gs1Vector_{kind}_{indicator}_{index}");
    }

    [TestCaseSource(nameof(Gs1Vectors))]
    public void ManualAlphanumericGs1VectorsPreserveLogicalTextAndEscapedRawBytes(
        QrSymbolKind kind, int indicator, string text, string escaped)
    {
        QrSegment[] segments = [Control(indicator), Alpha(text)];
        QrSymbol symbol = new QrEncoder().EncodeSegments(segments, Options(kind));
        QrDecodeResult result = Decode(symbol);
        Assert.That(result.Text, Is.EqualTo(text));
        Assert.That(result.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes(escaped)));
        Assert.That(result.Segments[1].CharacterCount, Is.EqualTo(escaped.Length));
        Assert.That(result.Segments[1].Text, Is.EqualTo(text));
        Assert.That(segments[1].Text, Is.EqualTo(text));
        Assert.That(segments[1].CharacterCount, Is.EqualTo(text.Length));
        AssertMetadata(result, indicator, false);
    }

    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void TextAndByteEntryPointsPreserveGs1DataWithAndWithoutOptimization(QrSymbolKind kind, bool optimize)
    {
        const string text = "010950600013435210LOT%25\u001d21SERIAL";
        foreach (int indicator in new[] { -1, 42, 177 })
            foreach (bool eci in new[] { false, true })
            {
                QrEncodeOptions options = Options(kind);
                options.UseFnc1First = indicator == -1;
                options.Fnc1SecondApplicationIndicator = indicator;
                options.OptimizeSegments = optimize;
                options.EciAssignment = eci ? 26 : -1;
                IQrEncoder encoder = new QrEncoder();
                QrSymbol symbol = encoder.EncodeText(text, options);
                QrDecodeResult decodedText = Decode(symbol);
                Assert.That(decodedText.Text, Is.EqualTo(text));
                // The optimizer can use the default mapping for this ASCII payload without an ECI switch.
                AssertMetadata(decodedText, indicator, eci && !optimize);
                QrEncodeAttemptResult attempt = encoder.TryEncodeText(text, options);
                Assert.That(attempt.Symbol, Is.Not.Null);
                Assert.That(Decode(attempt.Symbol!).Text, Is.EqualTo(text));
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                QrDecodeResult decodedBytes = Decode(encoder.EncodeBytes(bytes, options));
                Assert.That(decodedBytes.Text, Is.EqualTo(text));
                Assert.That(decodedBytes.RawBytes, Is.EqualTo(bytes));
                AssertMetadata(decodedBytes, indicator, eci);
            }
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.rMQR)]
    public void EveryAssignedApplicationIndicatorRoundTripsAsMetadata(QrSymbolKind kind)
    {
        foreach (int indicator in Enumerable.Range(0, 100).Concat(Enumerable.Range(165, 26)))
        {
            QrEncodeOptions options = Options(kind);
            options.Fnc1SecondApplicationIndicator = indicator;
            QrSymbol symbol = new QrEncoder().EncodeBytes([65, 37, 29, 66], options);
            QrDecodeResult result = Decode(symbol);
            Assert.That(result.Segments[0].Data, Is.EqualTo(new byte[] { (byte)indicator }));
            Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 65, 37, 29, 66 }));
            Assert.That(result.Text, Is.EqualTo("A%\u001dB"));
            AssertMetadata(result, indicator, false);
        }
    }

    [TestCase(QrSymbolKind.Model2, -1)]
    [TestCase(QrSymbolKind.Model2, 165)]
    [TestCase(QrSymbolKind.rMQR, -1)]
    [TestCase(QrSymbolKind.rMQR, 165)]
    public void PercentEscapesDoNotCrossSegmentBoundariesOrAffectByteSegments(QrSymbolKind kind, int indicator)
    {
        QrSegment[] segments =
        [
            Control(indicator), Alpha("A\u001d"), Alpha("\u001d"), Alpha("%B"),
            new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 4, Data = [37, 37, 29, 37] },
            Alpha("C%\u001d")
        ];
        QrDecodeResult result = Decode(new QrEncoder().EncodeSegments(segments, Options(kind)));
        Assert.That(result.Text, Is.EqualTo("A\u001d\u001d%B%%\u001d%C%\u001d"));
        Assert.That(result.Segments[1].Text, Is.EqualTo("A\u001d"));
        Assert.That(result.Segments[2].Text, Is.EqualTo("\u001d"));
        Assert.That(result.Segments[3].Text, Is.EqualTo("%B"));
        Assert.That(result.Segments[4].Data, Is.EqualTo(new byte[] { 37, 37, 29, 37 }));
        AssertMetadata(result, indicator, false);
    }

    [TestCase("\u001d%", TestName = "RejectSeparatorBeforePercent")]
    [TestCase("\u001d\u001d", TestName = "RejectAdjacentSeparators")]
    [TestCase("A\u001d%%B", TestName = "RejectSeparatorBeforePercentRun")]
    [TestCase("%\u001d%B", TestName = "RejectSeparatorInsidePercentRun")]
    public void AmbiguousManualAlphanumericEscapesAreRejectedInsteadOfChangingPayload(string text)
    {
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.rMQR })
            foreach (int indicator in new[] { -1, 165 })
            {
                QrSegment[] segments = [Control(indicator), Alpha(text)];
                QrInputException error = Assert.Throws<QrInputException>(() =>
                    new QrEncoder().EncodeSegments(segments, Options(kind)))!;
                Assert.That(error.InputName, Is.EqualTo("text"));
                // Automatic text encoding can retain these sequences in byte mode.
                QrEncodeOptions options = Options(kind);
                options.UseFnc1First = indicator == -1;
                options.Fnc1SecondApplicationIndicator = indicator;
                Assert.That(Decode(new QrEncoder().EncodeText(text, options)).Text, Is.EqualTo(text));
            }
    }

    [TestCase(QrSymbolKind.Model2, -1)]
    [TestCase(QrSymbolKind.Model2, 42)]
    [TestCase(QrSymbolKind.rMQR, -1)]
    [TestCase(QrSymbolKind.rMQR, 42)]
    public void BitLengthIncludesEscapedPercentsAcrossMultipleSegments(QrSymbolKind kind, int indicator)
    {
        QrSegment[] segments = [Control(indicator), Alpha("A%B"), Alpha("%%\u001d")];
        int version = kind == QrSymbolKind.Model2 ? 1 : 31;
        int modeWidth = kind == QrSymbolKind.Model2 ? 4 : 3;
        int countWidth = kind == QrSymbolKind.Model2 ? 9 : 8;
        // Escaped lengths are 4 and 5; alphanumeric pairs use 11 bits, a final character 6.
        int expected = modeWidth + (indicator == -1 ? 0 : 8) + 2 * (modeWidth + countWidth) + 22 + 28;
        Assert.That(ControlSegmentOptimizer.CalculateBitLength(segments, kind, version, false), Is.EqualTo(expected));
        Assert.That(CodewordFinalizer.CalculateRequiredBitCount(segments, kind, version, false, 10, 100), Is.EqualTo(expected));
    }

    static QrSegment Alpha(string text) => new QrSegment { Mode = QrEncodingMode.Alphanumeric, Text = text, CharacterCount = text.Length };

    [TestCase(-1, 116, "520902D7934FF581070B73EA689D70EC11EC11")]
    [TestCase(42, 124, "92A20902D7934FF581070B73EA689D70EC11EC")]
    [TestCase(165, 124, "9A520902D7934FF581070B73EA689D70EC11EC")]
    public void Model2Gs1CodewordsMatchIndependentBitVectors(int indicator, int bitCount, string hex)
    {
        // FNC1 header (0101 or 1001 + indicator), alphanumeric mode 0010, 9-bit count 18,
        // and base-45 pairs for 10LOT%%25%21SERIAL, followed by terminator and EC/11 padding.
        const string text = "10LOT%25\u001d21SERIAL";
        byte[] golden = Convert.FromHexString(hex);
        QrSegment[] segments = [Control(indicator), Alpha(text)];
        Assert.That(CodewordFinalizer.CalculateRequiredBitCount(segments, QrSymbolKind.Model2, 1, false, 10, 100), Is.EqualTo(bitCount));
        Assert.That(CodewordFinalizer.CreateDataCodewords(segments, QrSymbolKind.Model2, 1, 0, false), Is.EqualTo(golden));
        QrDecodeOptions options = new QrDecodeOptions();
        QrDecodeResult decoded = DecodedPayloadBuilder.Create(PayloadParser.Parse(golden, 152, QrSymbolKind.Model2, 1, options), options);
        Assert.That(decoded.Text, Is.EqualTo(text));
        Assert.That(decoded.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes("10LOT%%25%21SERIAL")));
        AssertMetadata(decoded, indicator, false);
    }

    static QrSegment Control(int indicator) => indicator == -1
        ? new QrSegment { Mode = QrEncodingMode.Fnc1First }
        : new QrSegment { Mode = QrEncodingMode.Fnc1Second, Fnc1ApplicationIndicator = indicator, Data = [(byte)indicator] };

    static QrEncodeOptions Options(QrSymbolKind kind) => new QrEncodeOptions
    {
        ForceSymbolKind = true, SymbolKind = kind, ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium,
        BoostErrorCorrectionLevel = false, QuietZoneModules = 0
    };

    static QrDecodeResult Decode(QrSymbol symbol) => new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());

    static void AssertMetadata(QrDecodeResult result, int indicator, bool eci)
    {
        Assert.That(result.UsesFnc1First, Is.EqualTo(indicator == -1));
        Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(indicator));
        Assert.That(result.SymbologyIdentifier, Is.EqualTo(indicator == -1 ? (eci ? "]Q4" : "]Q3") : (eci ? "]Q6" : "]Q5")));
        Assert.That(result.EciAssignments, Is.EqualTo(eci ? new[] { 26 } : Array.Empty<int>()));
        Assert.That(result.Warnings, Is.Empty);
    }
}
