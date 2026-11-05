using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ControlSegmentOptimizerTests
{
    [Test]
    public void PrependsControlSegmentsInStandardsOrderAndCountsTheirHeaders()
    {
        QrStructuredAppendInfo append = CreateStructuredAppendInfo();

        QrSegment[] segments = Optimize(
            "a€",
            QrSymbolKind.Model2,
            1,
            new int[] { QrEciAssignments.Utf8, QrEciAssignments.Utf16BigEndian },
            true,
            -1,
            append,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(GetModes(segments), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.StructuredAppend,
                QrEncodingMode.Fnc1First,
                QrEncodingMode.Eci,
                QrEncodingMode.Byte
            }));
            Assert.That(segments[0].StructuredAppend, Is.Not.SameAs(append));
            Assert.That(segments[0].StructuredAppend!.Index, Is.EqualTo(1));
            Assert.That(segments[2].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(CalculateBits(segments, QrSymbolKind.Model2, 1), Is.EqualTo(80));
        });
    }

    [Test]
    public void ChargesEachFnc1HeaderForModel2AndRmqr()
    {
        QrSegment[] plainModel2 = Optimize(
            "123", QrSymbolKind.Model2, 1, new int[0], false, -1, null, 10);
        QrSegment[] firstModel2 = Optimize(
            "123", QrSymbolKind.Model2, 1, new int[0], true, -1, null, 10);
        QrSegment[] secondModel2 = Optimize(
            "123", QrSymbolKind.Model2, 1, new int[0], false, 42, null, 10);
        QrSegment[] firstRmqr = Optimize(
            "123", QrSymbolKind.rMQR, 0, new int[0], true, -1, null, 10);
        QrSegment[] secondRmqr = Optimize(
            "123", QrSymbolKind.rMQR, 0, new int[0], false, 42, null, 10);

        Assert.Multiple(() =>
        {
            Assert.That(CalculateBits(plainModel2, QrSymbolKind.Model2, 1), Is.EqualTo(24));
            Assert.That(CalculateBits(firstModel2, QrSymbolKind.Model2, 1), Is.EqualTo(28));
            Assert.That(CalculateBits(secondModel2, QrSymbolKind.Model2, 1), Is.EqualTo(36));
            Assert.That(CalculateBits(firstRmqr, QrSymbolKind.rMQR, 0), Is.EqualTo(20));
            Assert.That(CalculateBits(secondRmqr, QrSymbolKind.rMQR, 0), Is.EqualTo(28));
        });
    }

    [Test]
    public void ChargesStructuredAppendEciAndHanziHeadersExactly()
    {
        QrSegment[] structured = Optimize(
            "123",
            QrSymbolKind.Model2,
            1,
            new int[0],
            false,
            -1,
            CreateStructuredAppendInfo(),
            10);
        QrSegment eci = EciEncoder.CreateControlSegment(200);
        QrSegment hanzi = HanziEncoder.CreateCompatibilitySegment("中", true);

        Assert.Multiple(() =>
        {
            Assert.That(CalculateBits(structured, QrSymbolKind.Model2, 1), Is.EqualTo(44));
            Assert.That(CalculateBits(new QrSegment[] { eci }, QrSymbolKind.Model2, 1), Is.EqualTo(20));
            Assert.That(SegmentOptimizer.CalculateOptimizedBitLength(
                new QrSegment[] { hanzi }, QrSymbolKind.Model2, 1, true), Is.EqualTo(29));
        });
    }

    [Test]
    public void RejectsFnc1ForProfilesThatDoNotDefineIt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Optimize(
                "123", QrSymbolKind.Micro, 4, new int[0], true, -1, null, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "123", QrSymbolKind.LegacyModel1, 1, new int[0], false, 42, null, 10),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsStructuredAppendOutsideModel2()
    {
        QrStructuredAppendInfo append = CreateStructuredAppendInfo();

        Assert.Multiple(() =>
        {
            Assert.That(() => Optimize(
                "123", QrSymbolKind.Micro, 4, new int[0], false, -1, append, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "123", QrSymbolKind.rMQR, 0, new int[0], false, -1, append, 10),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsConflictingOrInvalidControlMetadata()
    {
        QrStructuredAppendInfo invalidTotal = CreateStructuredAppendInfo();
        invalidTotal.Total = 1;
        QrStructuredAppendInfo invalidIndex = CreateStructuredAppendInfo();
        invalidIndex.Index = invalidIndex.Total;
        QrStructuredAppendInfo invalidParity = CreateStructuredAppendInfo();
        invalidParity.Parity = 256;

        Assert.Multiple(() =>
        {
            Assert.That(() => Optimize(
                "123", QrSymbolKind.Model2, 1, new int[0], true, 42, null, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "123", QrSymbolKind.Model2, 1, new int[0], false, -2, null, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "123", QrSymbolKind.Model2, 1, new int[0], false, 100, null, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "123", QrSymbolKind.Model2, 1, new int[0], false, -1, invalidTotal, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "123", QrSymbolKind.Model2, 1, new int[0], false, -1, invalidIndex, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "123", QrSymbolKind.Model2, 1, new int[0], false, -1, invalidParity, 10),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void CountsControlsTowardTheConfiguredSegmentLimit()
    {
        Assert.That(() => Optimize(
            "123",
            QrSymbolKind.Model2,
            1,
            new int[0],
            true,
            -1,
            CreateStructuredAppendInfo(),
            2), Throws.TypeOf<QrResourceLimitException>());
    }

    private static QrSegment[] Optimize(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int[] priorities,
        bool useFnc1First,
        int fnc1SecondApplicationIndicator,
        QrStructuredAppendInfo? structuredAppend,
        int maximumSegments)
    {
        return SegmentOptimizer.OptimizeTextSegmentsWithControls(
            text,
            symbolKind,
            version,
            priorities,
            useFnc1First,
            fnc1SecondApplicationIndicator,
            structuredAppend,
            maximumSegments,
            1000);
    }

    private static int CalculateBits(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version)
    {
        return SegmentOptimizer.CalculateOptimizedBitLength(
            segments,
            symbolKind,
            version,
            false);
    }

    private static QrStructuredAppendInfo CreateStructuredAppendInfo()
    {
        return new QrStructuredAppendInfo
        {
            Index = 1,
            Total = 3,
            Parity = 0xA5,
            ApplicationIdentifier = "batch",
            IsComplete = true
        };
    }

    private static QrEncodingMode[] GetModes(QrSegment[] segments)
    {
        QrEncodingMode[] modes = new QrEncodingMode[segments.Length];
        for (int index = 0; index < segments.Length; index++)
        {
            modes[index] = segments[index].Mode;
        }

        return modes;
    }
}
