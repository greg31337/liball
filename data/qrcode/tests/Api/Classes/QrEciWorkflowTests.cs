using System.Text;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrEciWorkflowTests
{
    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.rMQR)]
    public void DesignatorBoundariesSurviveEncodingAndDecodingWithoutLosingUnknownAssignments(QrSymbolKind kind)
    {
        (int Assignment, string Hex)[] cases = [(0, "00"), (127, "7F"), (128, "8080"),
            (16383, "BFFF"), (16384, "C04000"), (999999, "CF423F")];
        IQrEncoder encoder = new QrEncoder();
        IQrDecoder decoder = new QrDecoder();
        foreach (var test in cases)
        {
            QrEncodeOptions options = Options(kind);
            options.EciAssignment = test.Assignment;
            byte[] payload = [0, 0x80, 0xff];
            QrSymbol symbol = encoder.EncodeBytes(payload, options);
            byte[] expected = Convert.FromHexString(test.Hex);
            Assert.That(symbol.Segments[0].Data, Is.EqualTo(expected), test.Assignment.ToString());
            QrDecodeResult decoded = decoder.DecodeMatrix(symbol.Matrix!, new QrDecodeOptions { EnableByteEncodingInference = true });
            Assert.That(decoded.EciAssignments, Is.EqualTo(new[] { test.Assignment }));
            Assert.That(decoded.Segments[0].Data, Is.EqualTo(expected));
            Assert.That(decoded.Segments[1].EciAssignment, Is.EqualTo(test.Assignment));
            Assert.That(decoded.RawBytes, Is.EqualTo(payload));
            Assert.That(decoded.Text, Is.Null);
            Assert.That(decoded.Warnings.Single(), Does.Contain("unsupported ECI"));
            Assert.That(decoded.SymbologyIdentifier, Is.EqualTo("]Q2"));
            QrDecodeOptions strictMapping = new QrDecodeOptions { PreserveUnsupportedEciBytes = false };
            QrAttemptResult attempt = decoder.TryDecodeMatrix(symbol.Matrix!, strictMapping);
            Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
            Assert.That(attempt.Result, Is.Null);
            Assert.That(attempt.RawBytes, Is.Empty);
            Assert.That(attempt.Segments, Is.Empty);
            Assert.That(Assert.Throws<QrDecodeException>(() => decoder.DecodeMatrix(symbol.Matrix!, strictMapping))!.FailureReason,
                Is.EqualTo(attempt.FailureReason));
        }
    }

    [TestCase(1, "4100E9FF", "A\0éÿ")]
    [TestCase(3, "4100E9FF", "A\0éÿ")]
    [TestCase(20, "00415C815F7EA6DF82A08ABF", "\0A¥\\‾ｦﾟあ漢")]
    [TestCase(25, "0041000020ACD83DDE00", "A\0€😀")]
    [TestCase(26, "4100E282ACF09F9880", "A\0€😀")]
    [TestCase(27, "41007F", "A\0\u007f")]
    [TestCase(29, "0041D6D0CEC4A1A4", "\0A中文・")]
    [TestCase(33, "41000000AC203DD800DE", "A\0€😀")]
    [TestCase(34, "0000004100000000000020AC0001F600", "A\0€😀")]
    [TestCase(35, "4100000000000000AC20000000F60100", "A\0€😀")]
    public void CommonMappingsRoundTripExactBytesThroughBothEciFamilies(int assignment, string hex, string expectedText)
    {
        byte[] expectedBytes = Convert.FromHexString(hex);
        IQrSegmenter segmenter = new QrSegmenter();
        QrSegment segment = segmenter.CreateTextSegments(expectedText, assignment, false, 1000).Single();
        Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Byte));
        Assert.That(segment.Data, Is.EqualTo(expectedBytes));
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.rMQR })
        {
            QrEncodeOptions options = Options(kind);
            options.EciAssignment = assignment;
            options.OptimizeSegments = false;
            QrSymbol symbol = new QrEncoder().EncodeText(expectedText, options);
            QrDecodeResult result = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
            Assert.That(result.Text, Is.EqualTo(expectedText));
            Assert.That(result.RawBytes, Is.EqualTo(expectedBytes));
            Assert.That(result.EciAssignments, Is.EqualTo(new[] { assignment }));
            Assert.That(result.Segments[1].Text, Is.EqualTo(expectedText));
            Assert.That(result.Segments[1].EciAssignment, Is.EqualTo(assignment));
            Assert.That(result.Warnings, Is.Empty);
        }
    }

    [TestCase(0x00, "00")]
    [TestCase(0x7f, "7F")]
    [TestCase(0x80, "C280")]
    [TestCase(0x7ff, "DFBF")]
    [TestCase(0x800, "E0A080")]
    [TestCase(0xd7ff, "ED9FBF")]
    [TestCase(0xe000, "EE8080")]
    [TestCase(0xffff, "EFBFBF")]
    [TestCase(0x10000, "F0908080")]
    [TestCase(0x10ffff, "F48FBFBF")]
    [TestCase(0xfffd, "EFBFBD")]
    [TestCase(0xfeff, "EFBBBF")]
    public void Utf8ScalarBoundariesRetainExactTextWithoutDroppingBomOrReplacementCharacter(int scalar, string hex)
    {
        byte[] bytes = Convert.FromHexString(hex);
        QrEncodeOptions options = Options(QrSymbolKind.Model2);
        options.EciAssignment = 26;
        QrSymbol symbol = new QrEncoder().EncodeBytes(bytes, options);
        QrDecodeResult result = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo(char.ConvertFromUtf32(scalar)));
        Assert.That(result.RawBytes, Is.EqualTo(bytes));
        Assert.That(result.Warnings, Is.Empty);
        QrSegment segment = new QrSegmenter().CreateTextSegments(char.ConvertFromUtf32(scalar), 26, false, 4).Single();
        Assert.That(segment.Data, Is.EqualTo(bytes));
    }

    [TestCaseSource(nameof(MalformedMappings))]
    public void MalformedTextRemainsLosslessWithoutFabricatedReplacementOrPartialText(int assignment, string hex)
    {
        byte[] bad = Convert.FromHexString(hex);
        QrSegment[] segments = [Eci(3), Byte([65]), Eci(assignment), Byte(bad), Eci(3), Byte([66])];
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.rMQR })
        {
            QrSymbol symbol = new QrEncoder().EncodeSegments(segments, Options(kind));
            QrDecodeResult result = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions
            {
                Strict = true, EnableByteEncodingInference = true, PreserveUnsupportedEciBytes = false
            });
            Assert.That(result.Text, Is.Null);
            Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 65 }.Concat(bad).Append((byte)66).ToArray()));
            Assert.That(result.Segments[1].Text, Is.EqualTo("A"));
            Assert.That(result.Segments[3].Text, Is.Null);
            Assert.That(result.Segments[3].Data, Is.EqualTo(bad));
            Assert.That(result.Segments[3].EciAssignment, Is.EqualTo(assignment));
            Assert.That(result.Segments[5].Text, Is.EqualTo("B"));
            Assert.That(result.Warnings.Single(), Does.Contain("malformed or unmapped"));
            Assert.That(result.EciAssignments, Is.EqualTo(new[] { 3, assignment, 3 }));
        }
    }

    static IEnumerable<TestCaseData> MalformedMappings()
    {
        foreach (string hex in new[] { "80", "BF", "C0AF", "C1BF", "C2", "E08080", "E282", "EDA080", "EDBFBF", "F0808080", "F09F98", "F4908080", "F5808080", "FF", "E228A1" })
            yield return new TestCaseData(26, hex);
        foreach (string hex in new[] { "81", "817F", "81FD", "EBBF", "8740", "F040", "80", "A0", "FD" })
            yield return new TestCaseData(20, hex);
        yield return new TestCaseData(25, "D8000041");
        yield return new TestCaseData(33, "00DC");
        yield return new TestCaseData(34, "00110000");
        yield return new TestCaseData(35, "00D80000");
        yield return new TestCaseData(27, "80");
        yield return new TestCaseData(29, "D6");
    }

    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void EciSwitchesAndFnc1InterpretationStayIndependent(QrSymbolKind kind, bool second)
    {
        QrSegment fnc1 = second ? new QrSegment { Mode = QrEncodingMode.Fnc1Second, Fnc1ApplicationIndicator = 165, Data = [165] }
            : new QrSegment { Mode = QrEncodingMode.Fnc1First };
        QrSegment[] segments =
        [
            fnc1, Eci(3), Byte([0xe9, 0x25]),
            new QrSegment { Mode = QrEncodingMode.Alphanumeric, Text = "A%B\u001dC", CharacterCount = 5 },
            Eci(26), Byte([0xe2, 0x82, 0xac, 0x1d]),
            new QrSegment { Mode = QrEncodingMode.Kanji, Text = "漢", CharacterCount = 1 },
            Byte([0xc3, 0xa9]), Eci(3), Byte([0xff]), Eci(3), Byte([65])
        ];
        QrSymbol symbol = new QrEncoder().EncodeSegments(segments, Options(kind));
        QrDecodeResult result = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo("é%A%B\u001dC€\u001d漢éÿA"));
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 0xe9, 0x25 }
            .Concat(Encoding.ASCII.GetBytes("A%%B%C")).Concat(new byte[] { 0xe2, 0x82, 0xac, 0x1d, 0x8a, 0xbf, 0xc3, 0xa9, 0xff, 65 }).ToArray()));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { 3, 26, 3, 3 }));
        Assert.That(result.Segments[7].EciAssignment, Is.EqualTo(26));
        Assert.That(result.Segments[6].Text, Is.EqualTo("漢"));
        Assert.That(result.Segments[2].Text, Is.EqualTo("é%"));
        Assert.That(result.UsesFnc1First, Is.EqualTo(!second));
        Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(second ? 165 : -1));
        Assert.That(result.SymbologyIdentifier, Is.EqualTo(second ? "]Q6" : "]Q4"));
        Assert.That(result.Warnings, Is.Empty);
    }

    [TestCase("8140", "\u3000")]
    [TestCase("A1DF", "｡ﾟ")]
    [TestCase("5C815F7E", "¥\\‾")]
    public void ShiftJisSingleAndDoubleByteEdgesRetainTheirDistinctMappings(string hex, string text)
    {
        byte[] bytes = Convert.FromHexString(hex);
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.rMQR })
        {
            QrEncodeOptions options = Options(kind);
            options.EciAssignment = 20;
            QrSymbol symbol = new QrEncoder().EncodeBytes(bytes, options);
            QrDecodeResult result = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
            Assert.That(result.Text, Is.EqualTo(text));
            Assert.That(result.RawBytes, Is.EqualTo(bytes));
            Assert.That(result.Warnings, Is.Empty);
        }
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.rMQR)]
    public void UnsupportedAssignmentsDoNotPreventLaterSwitchesBackToKnownMappings(QrSymbolKind kind)
    {
        QrSegment[] segments =
        [
            Eci(3), Byte([0xe9]),
            new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = 128, Data = [0x80, 0x80] }, Byte([0x41]),
            Eci(26), Byte([0xe2, 0x82, 0xac]),
            new QrSegment { Mode = QrEncodingMode.Eci, EciAssignment = 999999, Data = [0xcf, 0x42, 0x3f] }, Byte([0xff]),
            Eci(20), Byte([0x82, 0xa0])
        ];
        QrSymbol symbol = new QrEncoder().EncodeSegments(segments, Options(kind));
        QrDecodeResult result = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions { EnableByteEncodingInference = true });
        Assert.That(result.Text, Is.Null);
        Assert.That(result.RawBytes, Is.EqualTo(Convert.FromHexString("E941E282ACFF82A0")));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { 3, 128, 26, 999999, 20 }));
        Assert.That(result.Segments.Where(s => s.Mode == QrEncodingMode.Byte).Select(s => s.Text),
            Is.EqualTo(new string?[] { "é", null, "€", null, "あ" }));
        Assert.That(result.Segments.Where(s => s.Mode == QrEncodingMode.Byte).Select(s => s.EciAssignment),
            Is.EqualTo(result.EciAssignments));
        Assert.That(result.Segments[2].Data, Is.EqualTo(new byte[] { 0x80, 0x80 }));
        Assert.That(result.Segments[6].Data, Is.EqualTo(new byte[] { 0xcf, 0x42, 0x3f }));
        Assert.That(result.Warnings.Length, Is.EqualTo(2));
        Assert.That(result.Warnings[0], Does.Contain("unsupported ECI 128"));
        Assert.That(result.Warnings[1], Does.Contain("unsupported ECI 999999"));
    }

    [TestCase(QrSymbolKind.Model2, false, "A\u001dB")]
    [TestCase(QrSymbolKind.rMQR, false, "A\u001dB")]
    [TestCase(QrSymbolKind.Model2, true, "A\nB")]
    [TestCase(QrSymbolKind.rMQR, true, "A\nB")]
    [TestCase(QrSymbolKind.Model2, true, "abc")]
    [TestCase(QrSymbolKind.rMQR, true, "abc")]
    public void Fnc1ValidationOnlyPermitsGroupSeparatorsWhenFnc1IsActive(QrSymbolKind kind, bool usesFnc1, string text)
    {
        List<QrSegment> segments = new List<QrSegment>();
        if (usesFnc1) segments.Add(new QrSegment { Mode = QrEncodingMode.Fnc1First });
        segments.Add(Eci(26));
        segments.Add(new QrSegment { Mode = QrEncodingMode.Alphanumeric, Text = text, CharacterCount = text.Length });
        Assert.Throws<QrInputException>(() => new QrEncoder().EncodeSegments(segments.ToArray(), Options(kind)));
    }

    static QrSegment Eci(int assignment) => new QrSegment
    {
        Mode = QrEncodingMode.Eci, EciAssignment = assignment, Data = [(byte)assignment]
    };

    static QrSegment Byte(byte[] data) => new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = data.Length, Data = data };

    static QrEncodeOptions Options(QrSymbolKind kind) => new QrEncodeOptions
    {
        ForceSymbolKind = true, SymbolKind = kind, ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium,
        BoostErrorCorrectionLevel = false, QuietZoneModules = 0
    };
}
