using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class EciSegmentOptimizerTests
{
    [Test]
    public void SwitchesEciOnlyWhenTheSavedPayloadBitsRecoverEveryHeader()
    {
        QrSegment[] switched = Optimize(
            "ééééé😀ééééé",
            QrSymbolKind.Model2,
            1,
            new int[] { QrEciAssignments.Iso8859_1, QrEciAssignments.Utf8 },
            20);
        QrSegment[] singleEci = Optimize(
            "é😀é",
            QrSymbolKind.Model2,
            1,
            new int[] { QrEciAssignments.Iso8859_1, QrEciAssignments.Utf8 },
            20);

        Assert.Multiple(() =>
        {
            Assert.That(GetModes(switched), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Byte,
                QrEncodingMode.Eci,
                QrEncodingMode.Byte,
                QrEncodingMode.Eci,
                QrEncodingMode.Byte
            }));
            Assert.That(switched[1].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(switched[3].EciAssignment, Is.EqualTo(QrEciAssignments.Iso8859_1));
            Assert.That(CalculateBits(switched, QrSymbolKind.Model2, 1), Is.EqualTo(172));
            Assert.That(GetModes(singleEci), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Eci,
                QrEncodingMode.Byte
            }));
            Assert.That(singleEci[0].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(CalculateBits(singleEci, QrSymbolKind.Model2, 1), Is.EqualTo(88));
        });
    }

    [Test]
    public void UsesCallerPriorityOnlyToBreakEqualCostEncodingChoices()
    {
        QrSegment[] utf16First = Optimize(
            "a€",
            QrSymbolKind.Model2,
            1,
            new int[] { QrEciAssignments.Utf16BigEndian, QrEciAssignments.Utf8 },
            10);
        QrSegment[] utf8First = Optimize(
            "a€",
            QrSymbolKind.Model2,
            1,
            new int[] { QrEciAssignments.Utf8, QrEciAssignments.Utf16BigEndian },
            10);

        Assert.Multiple(() =>
        {
            Assert.That(utf16First[0].Mode, Is.EqualTo(QrEncodingMode.Eci));
            Assert.That(utf16First[0].EciAssignment, Is.EqualTo(QrEciAssignments.Utf16BigEndian));
            Assert.That(utf16First[1].Data, Is.EqualTo(new byte[] { 0x00, 0x61, 0x20, 0xAC }));
            Assert.That(utf8First[0].Mode, Is.EqualTo(QrEncodingMode.Eci));
            Assert.That(utf8First[0].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(utf8First[1].Data, Is.EqualTo(new byte[] { 0x61, 0xE2, 0x82, 0xAC }));
            Assert.That(CalculateBits(utf16First, QrSymbolKind.Model2, 1), Is.EqualTo(56));
            Assert.That(CalculateBits(utf8First, QrSymbolKind.Model2, 1), Is.EqualTo(56));
        });
    }

    [Test]
    public void PreservesActiveEciAcrossNonByteSegments()
    {
        QrSegment[] segments = Optimize(
            "😀1234567890😀",
            QrSymbolKind.Model2,
            1,
            new int[] { QrEciAssignments.Utf8, QrEciAssignments.Iso8859_1 },
            20);

        Assert.Multiple(() =>
        {
            Assert.That(GetModes(segments), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Eci,
                QrEncodingMode.Byte,
                QrEncodingMode.Numeric,
                QrEncodingMode.Byte
            }));
            Assert.That(segments[0].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(segments[1].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(segments[3].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(CalculateBits(segments, QrSymbolKind.Model2, 1), Is.EqualTo(148));
        });
    }

    [Test]
    public void ChargesTheProfileSpecificEciModeIndicatorWidth()
    {
        int[] priorities = new int[]
        {
            QrEciAssignments.Utf16BigEndian,
            QrEciAssignments.Utf8
        };
        QrSegment[] model2 = Optimize("a€", QrSymbolKind.Model2, 1, priorities, 10);
        QrSegment[] rmqr = Optimize("a€", QrSymbolKind.rMQR, 0, priorities, 10);

        Assert.Multiple(() =>
        {
            Assert.That(CalculateBits(model2, QrSymbolKind.Model2, 1), Is.EqualTo(56));
            Assert.That(CalculateBits(rmqr, QrSymbolKind.rMQR, 0), Is.EqualTo(47));
            Assert.That(GetModes(rmqr), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Byte,
                QrEncodingMode.Eci,
                QrEncodingMode.Byte
            }));
            Assert.That(rmqr[1].Data, Is.EqualTo(new byte[] { 25 }));
        });
    }

    [Test]
    public void ProfilesWithoutEciUseOnlyTheDefaultInterpretation()
    {
        QrSegment[] micro = Optimize(
            "café",
            QrSymbolKind.Micro,
            3,
            new int[] { QrEciAssignments.Utf8, QrEciAssignments.Iso8859_1 },
            10);

        Assert.Multiple(() =>
        {
            Assert.That(micro.Length, Is.EqualTo(1));
            Assert.That(micro[0].Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(micro[0].Data, Is.EqualTo(new byte[] { 0x63, 0x61, 0x66, 0xE9 }));
            Assert.That(() => Optimize(
                "a€",
                QrSymbolKind.Micro,
                3,
                new int[] { QrEciAssignments.Utf8 },
                10), Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "a€",
                QrSymbolKind.LegacyModel1,
                1,
                new int[] { QrEciAssignments.Utf8 },
                10), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void NormalizesDefaultAndDuplicatePrioritiesDeterministically()
    {
        QrSegment[] emptyPriority = Optimize(
            "café",
            QrSymbolKind.Model2,
            1,
            new int[0],
            10);
        QrSegment[] duplicates = Optimize(
            "a€",
            QrSymbolKind.Model2,
            1,
            new int[]
            {
                QrEciAssignments.Utf8,
                QrEciAssignments.Utf8,
                QrEciAssignments.Utf16BigEndian
            },
            10);

        Assert.Multiple(() =>
        {
            Assert.That(emptyPriority.Length, Is.EqualTo(1));
            Assert.That(emptyPriority[0].Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(emptyPriority[0].EciAssignment, Is.EqualTo(QrEciAssignments.Iso8859_1));
            Assert.That(duplicates[0].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
        });
    }

    [Test]
    public void ValidatesPrioritiesInputsAndControlSegmentLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => SegmentOptimizer.OptimizeTextSegmentsAcrossEci(
                "a", QrSymbolKind.Model2, 1, null!, 10, 100),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "a", QrSymbolKind.Model2, 1, new int[] { 999998 }, 10),
                Throws.TypeOf<QrUnsupportedFeatureException>());
            Assert.That(() => Optimize(
                "a", QrSymbolKind.Model2, 1, new int[] { -2 }, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize(
                "a€", QrSymbolKind.Model2, 1, new int[] { QrEciAssignments.Utf8 }, 1),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    [Test]
    public void ProducesControlAndDataSegmentsAcceptedByManualValidation()
    {
        QrSegment[] segments = Optimize(
            "ééééé😀ééééé",
            QrSymbolKind.Model2,
            1,
            new int[] { QrEciAssignments.Iso8859_1, QrEciAssignments.Utf8 },
            20);

        Assert.That(() => SegmentValidator.ValidateSegments(
            segments,
            QrSymbolKind.Model2,
            1,
            20,
            100,
            false), Throws.Nothing);
    }

    private static QrSegment[] Optimize(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int[] priorities,
        int maximumSegments)
    {
        return SegmentOptimizer.OptimizeTextSegmentsAcrossEci(
            text,
            symbolKind,
            version,
            priorities,
            maximumSegments,
            1000);
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

    private static int CalculateBits(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version)
    {
        int modeWidth = symbolKind == QrSymbolKind.rMQR ? 3 : 4;
        int total = 0;
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            if (segment.Mode == QrEncodingMode.Eci)
            {
                total += modeWidth + EciEncoder.CalculateBitLength(segment.EciAssignment);
                continue;
            }

            int modeIndex = GetModeIndex(segment.Mode);
            int countWidth;
            if (symbolKind == QrSymbolKind.Model2)
            {
                countWidth = Model2VersionTable.GetCharacterCountBitWidth(modeIndex, version);
            }
            else
            {
                countWidth = RectangularMicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
            }

            int payloadBits;
            if (modeIndex == 0)
            {
                payloadBits = NumericEncoder.CalculateBitLength(segment.CharacterCount);
            }
            else if (modeIndex == 1)
            {
                payloadBits = AlphanumericEncoder.CalculateBitLength(segment.CharacterCount);
            }
            else if (modeIndex == 2)
            {
                payloadBits = ByteEncoder.CalculateBitLength(segment.CharacterCount);
            }
            else
            {
                payloadBits = KanjiEncoder.CalculateBitLength(segment.CharacterCount);
            }

            total += modeWidth + countWidth + payloadBits;
        }

        return total;
    }

    private static int GetModeIndex(QrEncodingMode mode)
    {
        if (mode == QrEncodingMode.Numeric)
        {
            return 0;
        }

        if (mode == QrEncodingMode.Alphanumeric)
        {
            return 1;
        }

        if (mode == QrEncodingMode.Byte)
        {
            return 2;
        }

        return 3;
    }
}
