using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class StructuredAppendGrouperTests
{
    [Test]
    public void GroupsInterleavedDecodedMessagesByExactIdentityInArrivalOrder()
    {
        QrDecodeResult a1 = DecodePart(1, 2, 42, "invoice:A");
        QrDecodeResult a0 = DecodePart(0, 2, 42, "invoice:A", 2, QrErrorCorrectionLevel.High, 26);
        QrDecodeResult b0 = DecodePart(0, 2, 42, "invoice:B");
        QrDecodeResult c0 = DecodePart(0, 3, 42, "invoice:A");
        QrDecodeResult d0 = DecodePart(0, 2, 43, "invoice:A");
        QrDecodeResult ordinary = new QrDecoder().DecodeMatrix(new QrEncoder().EncodeText("ordinary", new QrEncodeOptions()).Matrix!, new QrDecodeOptions());
        QrDecodeResult[] input = [a1, ordinary, b0, c0, a0, d0];
        StructuredAppendGroupingResult result = Group(input);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(result.Groups, Has.Length.EqualTo(4));
        Assert.That(result.Groups[0].Parts, Is.EqualTo(new[] { a1, a0 }));
        Assert.That(result.Groups[1].Parts, Is.EqualTo(new[] { b0 }));
        Assert.That(result.Groups[2].Total, Is.EqualTo(3));
        Assert.That(result.Groups[3].Parity, Is.EqualTo(43));
        Assert.That(result.OrdinaryResults, Is.EqualTo(new[] { ordinary }));
        Assert.That(result.RejectedResults, Is.Empty);
        Assert.That(result.Groups[0].ApplicationIdentifier, Is.EqualTo("invoice:A"));
        Assert.That(a0.EciAssignments, Is.EqualTo(new[] { 26 }));
        Assert.That(a1.Symbol!.Version, Is.Not.EqualTo(a0.Symbol!.Version));
        Assert.That(Group(input).Groups.Select(g => g.ApplicationIdentifier), Is.EqualTo(result.Groups.Select(g => g.ApplicationIdentifier)));
    }

    [Test]
    public void ApplicationIdentityIsOrdinalAndDistinguishesMissingEmptyAndDelimiterText()
    {
        string?[] applications = [null, "", "N", "S", "A", "a", "2:42:N", "é", "e\u0301"];
        QrDecodeResult[] parts = applications.Select(id => Part(0, 2, 42, id)).ToArray();
        StructuredAppendGroupingResult result = Group(parts.Concat(parts).ToArray());
        Assert.That(result.Groups, Has.Length.EqualTo(applications.Length));
        Assert.That(result.Groups.Select(g => g.ApplicationIdentifier), Is.EqualTo(applications));
        Assert.That(result.Groups.All(g => g.Parts.Length == 2), Is.True);
    }

    [TestCase(2, 0)]
    [TestCase(16, 255)]
    public void LegalHeaderBoundariesAndDuplicatesRemainAvailableForAssembly(int total, int parity)
    {
        QrDecodeResult last = Part(total - 1, total, parity, null);
        last.StructuredAppend!.IsComplete = true;
        StructuredAppendGroup group = StructuredAppendGrouper.RequireSingleGroup([last, last], 256, 1_000_000);
        Assert.That(group.Total, Is.EqualTo(total));
        Assert.That(group.Parity, Is.EqualTo(parity));
        Assert.That(group.Parts, Is.EqualTo(new[] { last, last }));
        Assert.That(group.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(last.StructuredAppend.Index, Is.EqualTo(total - 1));
        Assert.That(last.StructuredAppend.IsComplete, Is.True);
    }

    [TestCase(0, 1, 0)]
    [TestCase(0, 17, 0)]
    [TestCase(-1, 2, 0)]
    [TestCase(2, 2, 0)]
    [TestCase(0, 2, -1)]
    [TestCase(0, 2, 256)]
    public void RejectsOutOfRangeHeadersWithoutLosingOtherGroups(int index, int total, int parity)
    {
        QrDecodeResult invalid = Part(index, total, parity, null);
        QrDecodeResult valid = Part(0, 2, 7, "valid");
        StructuredAppendGroupingResult result = Group([invalid, valid]);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
        Assert.That(result.RejectedResults, Is.EqualTo(new[] { invalid }));
        Assert.That(result.Groups.Single().Parts, Is.EqualTo(new[] { valid }));
        Assert.Throws<QrStructuredAppendException>(() => StructuredAppendGrouper.RequireSingleGroup([invalid, valid], 256, 1_000_000));
    }

    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    [TestCase(QrSymbolKind.LegacyModel1)]
    [TestCase((QrSymbolKind)99)]
    public void RejectsStructuredAppendOnUnsupportedSymbolFamilies(QrSymbolKind kind)
    {
        QrDecodeResult part = Part(0, 2, 7, null);
        part.Symbol = new QrSymbol { SymbolKind = kind };
        Assert.That(Group([part]).RejectedResults, Is.EqualTo(new[] { part }));
    }

    [TestCase(false, -1, true, -1)]
    [TestCase(true, -1, false, 165)]
    [TestCase(false, 165, false, 166)]
    public void ConflictingApplicationControlsPoisonTheWholeIdentity(bool firstA, int secondA, bool firstB, int secondB)
    {
        QrDecodeResult a = Part(0, 2, 7, "same");
        QrDecodeResult b = Part(1, 2, 7, "same");
        a.UsesFnc1First = firstA;
        a.Fnc1SecondApplicationIndicator = secondA;
        b.UsesFnc1First = firstB;
        b.Fnc1SecondApplicationIndicator = secondB;
        foreach (QrDecodeResult[] order in new[] { new[] { a, b }, new[] { b, a } })
        {
            StructuredAppendGroupingResult result = Group(order);
            Assert.That(result.Groups, Has.Length.EqualTo(1));
            Assert.That(result.Groups[0].FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
            Assert.That(result.Groups[0].Parts, Is.EqualTo(order));
            QrStructuredAppendException error = Assert.Throws<QrStructuredAppendException>(() => StructuredAppendGrouper.RequireSingleGroup(order, 256, 1_000_000))!;
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
            Assert.That(error.ExpectedPartCount, Is.EqualTo(2));
            Assert.That(error.ActualPartCount, Is.EqualTo(2));
            Assert.That(error.Parity, Is.EqualTo(7));
        }
    }

    [Test]
    public void SingleSequenceRequestRejectsDifferentKeysAndOrdinarySymbols()
    {
        QrDecodeResult a = Part(0, 2, 7, "A");
        foreach (QrDecodeResult other in new[] { Part(1, 2, 8, "A"), Part(1, 3, 7, "A"), Part(1, 2, 7, "B"), new QrDecodeResult() })
        {
            QrStructuredAppendException error = Assert.Throws<QrStructuredAppendException>(() => StructuredAppendGrouper.RequireSingleGroup([a, other], 256, 1_000_000))!;
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ConflictingSequence));
            Assert.That(error.Parity, Is.EqualTo(other.StructuredAppend?.Parity == 7 ? 7 : -1));
        }
    }

    [Test]
    public void RejectsContradictingOrMisplacedRetainedHeadersAndFnc1Controls()
    {
        QrDecodeResult decoded = DecodePart(0, 2, 42, "external application");
        Assert.That(Group([decoded]).RejectedResults, Is.Empty); // Application identity is not an on-wire field.
        decoded.StructuredAppend = new QrStructuredAppendInfo { Index = 0, Total = 2, Parity = 43 };
        Assert.That(Group([decoded]).RejectedResults, Has.Length.EqualTo(1));
        decoded = DecodePart(0, 2, 42, null);
        decoded.StructuredAppend = null;
        Assert.That(Group([decoded]).RejectedResults, Has.Length.EqualTo(1));
        decoded = DecodePart(0, 2, 42, null);
        Array.Reverse(decoded.Segments);
        Assert.That(Group([decoded]).RejectedResults, Has.Length.EqualTo(1));
        decoded = DecodePart(0, 2, 42, null);
        decoded.UsesFnc1First = true;
        Assert.That(Group([decoded]).RejectedResults, Has.Length.EqualTo(1));
        decoded = DecodePart(0, 2, 42, null);
        decoded.Segments = [decoded.Segments[0], new QrSegment { Mode = QrEncodingMode.Fnc1Second, Fnc1ApplicationIndicator = -1 }];
        Assert.That(Group([decoded]).RejectedResults, Has.Length.EqualTo(1));
    }

    [TestCase(true, 165)]
    [TestCase(false, -2)]
    [TestCase(false, 100)]
    public void RejectsInvalidApplicationControlMetadata(bool first, int second)
    {
        QrDecodeResult part = Part(0, 2, 7, null);
        part.UsesFnc1First = first;
        part.Fnc1SecondApplicationIndicator = second;
        Assert.That(Group([part]).RejectedResults, Has.Length.EqualTo(1));
    }

    [TestCase(-1)]
    [TestCase(99)]
    [TestCase(165)]
    public void CompatibleDecodedFnc1PartsRemainInOneGroup(int fnc1)
    {
        QrDecodeResult a = DecodePart(0, 2, 7, "application", fnc1: fnc1);
        QrDecodeResult b = DecodePart(1, 2, 7, "application", eci: 26, fnc1: fnc1);
        StructuredAppendGroup group = StructuredAppendGrouper.RequireSingleGroup([b, a], 256, 1_000_000);
        Assert.That(group.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(group.UsesFnc1First, Is.EqualTo(fnc1 == -1));
        Assert.That(group.Fnc1SecondApplicationIndicator, Is.EqualTo(fnc1));
        Assert.That(group.Parts, Is.EqualTo(new[] { b, a }));
    }

    [Test]
    public void GroupingDoesNotChangeOrReorderCallerOwnedData()
    {
        QrDecodeResult part = DecodePart(1, 2, 42, "A");
        QrDecodeResult[] input = [part];
        byte[] before = (byte[])part.RawBytes.Clone();
        StructuredAppendGroup group = Group(input).Groups.Single();
        group.ApplicationIdentifier = "changed";
        group.Parts[0] = new QrDecodeResult();
        Assert.That(input[0], Is.SameAs(part));
        Assert.That(part.StructuredAppend!.ApplicationIdentifier, Is.EqualTo("A"));
        Assert.That(part.StructuredAppend.Index, Is.EqualTo(1));
        Assert.That(part.RawBytes, Is.EqualTo(before));
    }

    [Test]
    public void ValidatesCollectionAndResourceLimitsBeforeGrouping()
    {
        Assert.That(Group([]).Groups, Is.Empty);
        Assert.Throws<QrInputException>(() => StructuredAppendGrouper.RequireSingleGroup([], 1, 4096));
        Assert.Throws<QrInputException>(() => Group(null!));
        Assert.Throws<QrInputException>(() => Group([null!]));
        Assert.Throws<QrInputException>(() => Group([new QrDecodeResult { Segments = null! }]));
        Assert.Throws<QrInputException>(() => Group([new QrDecodeResult { Segments = [null!] }]));
        Assert.Throws<QrInputException>(() => StructuredAppendGrouper.Group([], 0, 4096));
        Assert.Throws<QrInputException>(() => StructuredAppendGrouper.Group([], 1, 0));
        Assert.Throws<QrResourceLimitException>(() => StructuredAppendGrouper.Group([Part(0, 2, 0, null), Part(1, 2, 0, null)], 1, 4096));
        Assert.Throws<QrResourceLimitException>(() => StructuredAppendGrouper.Group([Part(0, 2, 0, new string('x', 1024))], 1, 4096));
        Assert.Throws<QrResourceLimitException>(() => StructuredAppendGrouper.Group([Part(0, 2, 0, null)], 1, 511));
    }

    static StructuredAppendGroupingResult Group(QrDecodeResult[] parts) => StructuredAppendGrouper.Group(parts, 256, 1_000_000);
    static QrDecodeResult Part(int index, int total, int parity, string? application) => new QrDecodeResult
    {
        StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = total, Parity = parity, ApplicationIdentifier = application }
    };

    static QrDecodeResult DecodePart(int index, int total, int parity, string? application, int version = 1,
        QrErrorCorrectionLevel level = QrErrorCorrectionLevel.Low, int eci = -1, int fnc1 = -2)
    {
        List<QrSegment> segments = [new QrSegment { Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = total, Parity = parity } }];
        if (fnc1 == -1) segments.Add(Fnc1Encoder.CreateFirstPositionControlSegment());
        if (fnc1 >= 0) segments.Add(Fnc1Encoder.CreateSecondPositionControlSegment(fnc1));
        if (eci >= 0) segments.Add(EciEncoder.CreateControlSegment(eci));
        segments.Add(new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = [(byte)(65 + index)], EciAssignment = eci });
        QrSymbol symbol = new QrEncoder().EncodeSegments(segments.ToArray(), new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = version, MaximumVersion = version,
            ErrorCorrectionLevel = level, QuietZoneModules = 0
        });
        QrDecodeResult result = new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions());
        result.StructuredAppend!.ApplicationIdentifier = application;
        return result;
    }
}
