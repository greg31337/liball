using System.Text;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class SymbologyIdentifierSelectorTests
{
    static IEnumerable<TestCaseData> ModernStates()
    {
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.rMQR })
        {
            yield return new TestCaseData(kind, false, 0, "]Q1");
            yield return new TestCaseData(kind, true, 0, "]Q2");
            yield return new TestCaseData(kind, false, 1, "]Q3");
            yield return new TestCaseData(kind, true, 1, "]Q4");
            yield return new TestCaseData(kind, false, 2, "]Q5");
            yield return new TestCaseData(kind, true, 2, "]Q6");
        }
    }

    [TestCaseSource(nameof(ModernStates))]
    public void SelectsEveryModernModifierFromParsedControlsWithoutChangingPayload(
        QrSymbolKind kind, bool eci, int fnc1, string expected)
    {
        List<QrSegment> segments = new List<QrSegment>();
        if (fnc1 == 1) segments.Add(new QrSegment { Mode = QrEncodingMode.Fnc1First });
        if (fnc1 == 2) segments.Add(Fnc1Encoder.CreateSecondPositionControlSegment(0));
        if (eci) segments.Add(EciEncoder.CreateControlSegment(3));
        segments.Add(new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 4, Data = [93, 81, 49, 65] });
        QrDecodeResult result = Decode(segments.ToArray(), kind);
        Assert.That(result.SymbologyIdentifier, Is.EqualTo(expected));
        Assert.That(result.Text, Is.EqualTo("]Q1A"));
        Assert.That(result.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes("]Q1A")));
        Assert.That(result.Segments, Has.Length.EqualTo(segments.Count));
        Assert.That(result.EciAssignments, Is.EqualTo(eci ? new[] { 3 } : Array.Empty<int>()));
        Assert.That(result.UsesFnc1First, Is.EqualTo(fnc1 == 1));
        Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(fnc1 == 2 ? 0 : -1));
    }

    [TestCase(QrSymbolKind.Micro, "]Q1")]
    [TestCase(QrSymbolKind.LegacyModel1, "]Q0")]
    public void UsesFixedIdentifierForRestrictedProfiles(QrSymbolKind kind, string expected)
    {
        QrDecodeResult result = Decode([new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = 1, Text = "7" }], kind);
        Assert.That(result.Text, Is.EqualTo("7"));
        Assert.That(result.SymbologyIdentifier, Is.EqualTo(expected));
    }

    [TestCase(QrSymbolKind.Model2, "]Q1")]
    [TestCase(QrSymbolKind.Micro, "]Q1")]
    [TestCase(QrSymbolKind.rMQR, "]Q1")]
    [TestCase(QrSymbolKind.LegacyModel1, "]Q0")]
    public void EmptyPayloadStillIdentifiesTheParsedProfile(QrSymbolKind kind, string expected)
    {
        QrDecodeResult result = Decode([], kind);
        Assert.That(result.SymbologyIdentifier, Is.EqualTo(expected));
        Assert.That(result.RawBytes, Is.Empty);
        Assert.That(result.Text, Is.Empty);
    }

    [TestCase(3)]
    [TestCase(26)]
    [TestCase(899)]
    [TestCase(999999)]
    public void ExplicitEciRemainsActiveAfterReturningToDefault(int assignment)
    {
        QrDecodeResult result = Decode([
            EciEncoder.CreateControlSegment(assignment),
            new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = [65] },
            EciEncoder.CreateControlSegment(3),
            new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = [66] }
        ], QrSymbolKind.Model2);
        Assert.That(result.SymbologyIdentifier, Is.EqualTo("]Q2"));
        Assert.That(result.EciAssignments, Is.EqualTo(new[] { assignment, 3 }));
        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 65, 66 }));
    }

    [Test]
    public void InferredAndFixedCharacterMappingsDoNotCountAsExplicitEci()
    {
        QrSegment kanji = new QrSegment { Mode = QrEncodingMode.Kanji, CharacterCount = 1, Text = "漢" };
        Assert.That(Decode([kanji], QrSymbolKind.Model2).SymbologyIdentifier, Is.EqualTo("]Q1"));
        PayloadParseResult parsed = new PayloadParseResult { Segments =
            [new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 2, Data = [0xc3, 0xa9] }] };
        QrDecodeResult result = DecodedPayloadBuilder.Create(parsed, new QrDecodeOptions { EnableByteEncodingInference = true });
        Assert.That(result.Text, Is.EqualTo("é"));
        Assert.That(result.Segments[0].EciAssignment, Is.EqualTo(26));
        Assert.That(result.EciAssignments, Is.Empty);
        Assert.That(result.SymbologyIdentifier, Is.EqualTo("]Q1"));
    }

    [Test]
    public void StructuredAppendAndFnc1EscapingPreserveIdentifierAndRawData()
    {
        QrDecodeResult result = Decode([
            new QrSegment { Mode = QrEncodingMode.StructuredAppend, StructuredAppend = new QrStructuredAppendInfo { Index = 0, Total = 2, Parity = 45 } },
            new QrSegment { Mode = QrEncodingMode.Fnc1First },
            new QrSegment { Mode = QrEncodingMode.Alphanumeric, Text = "A%B", CharacterCount = 3 }
        ], QrSymbolKind.Model2);
        Assert.That(result.SymbologyIdentifier, Is.EqualTo("]Q3"));
        Assert.That(result.Text, Is.EqualTo("A%B"));
        Assert.That(result.RawBytes, Is.EqualTo(Encoding.ASCII.GetBytes("A%%B")));
        Assert.That(result.StructuredAppend!.Parity, Is.EqualTo(45));
    }

    [TestCase(0)]
    [TestCase(99)]
    [TestCase(165)]
    [TestCase(190)]
    public void SecondPositionApplicationValueDoesNotChangeModifier(int application)
    {
        Assert.That(SymbologyIdentifierSelector.GetIdentifier(QrSymbolKind.Model2, false, false, application), Is.EqualTo("]Q5"));
        Assert.That(SymbologyIdentifierSelector.GetIdentifier(QrSymbolKind.rMQR, true, false, application), Is.EqualTo("]Q6"));
    }

    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.LegacyModel1)]
    public void RejectsControlsUnsupportedByProfile(QrSymbolKind kind)
    {
        foreach ((bool eci, bool first, int second) in new[] { (true, false, -1), (false, true, -1), (false, false, 0) })
        {
            QrDecodeException error = Assert.Throws<QrDecodeException>(() => SymbologyIdentifierSelector.GetIdentifier(kind, eci, first, second))!;
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
        }
    }

    [Test]
    public void RejectsUndefinedProfilesAndConflictingOrInvalidFnc1State()
    {
        Assert.Throws<QrInputException>(() => SymbologyIdentifierSelector.GetIdentifier((QrSymbolKind)99, false, false, -1));
        Assert.Throws<QrDecodeException>(() => SymbologyIdentifierSelector.GetIdentifier(QrSymbolKind.Model2, false, true, 0));
        foreach (int invalid in new[] { -2, 100, 164, 191, 196, 223, 256 })
            Assert.Throws<QrInputException>(() => SymbologyIdentifierSelector.GetIdentifier(QrSymbolKind.Model2, false, false, invalid));
    }

    [Test]
    public void MetadataHelpersRejectConflictingPositionsWithoutPartialMutation()
    {
        QrDecodeResult first = new QrDecodeResult();
        Fnc1Decoder.ApplyFirstPositionMetadata(first, true);
        Assert.Throws<QrDecodeException>(() => Fnc1Decoder.ApplySecondPositionMetadata(first, 0, true));
        Assert.That(first.SymbologyIdentifier, Is.EqualTo("]Q4"));
        Assert.That(first.Fnc1SecondApplicationIndicator, Is.EqualTo(-1));
        QrDecodeResult second = new QrDecodeResult();
        Fnc1Decoder.ApplySecondPositionMetadata(second, 0, false);
        Assert.Throws<QrDecodeException>(() => Fnc1Decoder.ApplyFirstPositionMetadata(second, false));
        Assert.That(second.SymbologyIdentifier, Is.EqualTo("]Q5"));
        Assert.That(second.UsesFnc1First, Is.False);
    }

    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.LegacyModel1)]
    public void MetadataHelpersRespectRetainedSymbolProfile(QrSymbolKind kind)
    {
        QrDecodeResult result = new QrDecodeResult { Symbol = new QrSymbol { SymbolKind = kind } };
        Assert.Throws<QrDecodeException>(() => Fnc1Decoder.ApplyFirstPositionMetadata(result, false));
        Assert.Throws<QrDecodeException>(() => Fnc1Decoder.ApplySecondPositionMetadata(result, 0, false));
        Assert.That(result.UsesFnc1First, Is.False);
        Assert.That(result.Fnc1SecondApplicationIndicator, Is.EqualTo(-1));
        Assert.That(result.SymbologyIdentifier, Is.Null);
    }

    static QrDecodeResult Decode(QrSegment[] segments, QrSymbolKind kind)
    {
        int version = kind == QrSymbolKind.rMQR ? 31 : kind == QrSymbolKind.Micro ? 4 : 2;
        byte[] data = CodewordFinalizer.CreateDataCodewords(segments, kind, version, 0, false);
        int bits = kind == QrSymbolKind.LegacyModel1 ? data.Length * 8 - 4 : data.Length * 8;
        QrDecodeOptions options = new QrDecodeOptions();
        return DecodedPayloadBuilder.Create(PayloadParser.Parse(data, bits, kind, version, options), options);
    }
}
