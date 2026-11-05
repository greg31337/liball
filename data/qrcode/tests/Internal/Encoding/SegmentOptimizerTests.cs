using System.Text;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class SegmentOptimizerTests
{
    [TestCase("0")]
    [TestCase("01234567890123456789")]
    public void SelectsNumericModeForDigitOnlyText(string text)
    {
        QrSegment segment = CreateTextSegment(text, -1, false);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(segment.CharacterCount, Is.EqualTo(text.Length));
            Assert.That(segment.Data, Is.EqualTo(Encoding.ASCII.GetBytes(text)));
            Assert.That(segment.Text, Is.EqualTo(text));
            Assert.That(segment.EciAssignment, Is.EqualTo(-1));
        });
    }

    [TestCase("HELLO WORLD")]
    [TestCase("A1 $%*+-./:")]
    public void SelectsAlphanumericModeForTheExactQrRepertoire(string text)
    {
        QrSegment segment = CreateTextSegment(text, -1, false);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Alphanumeric));
            Assert.That(segment.CharacterCount, Is.EqualTo(text.Length));
            Assert.That(segment.Data, Is.EqualTo(Encoding.ASCII.GetBytes(text)));
            Assert.That(segment.Text, Is.EqualTo(text));
        });
    }

    [Test]
    public void SelectsStandardKanjiAheadOfByteAndHanziCompatibilityModes()
    {
        QrSegment segment = CreateTextSegment("漢字", -1, true);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Kanji));
            Assert.That(segment.CharacterCount, Is.EqualTo(2));
            Assert.That(segment.Data, Is.EqualTo(new byte[] { 0x8A, 0xBF, 0x8E, 0x9A }));
            Assert.That(segment.Text, Is.EqualTo("漢字"));
            Assert.That(segment.IsHanziCompatibilityMode, Is.False);
        });
    }

    [Test]
    public void SelectsHanziOnlyWhenCompatibilityIsEnabled()
    {
        QrSegment enabled = CreateTextSegment("汉", -1, true);
        QrSegment disabled = CreateTextSegment("汉", -1, false);

        Assert.Multiple(() =>
        {
            Assert.That(enabled.Mode, Is.EqualTo(QrEncodingMode.Hanzi));
            Assert.That(enabled.Data, Is.EqualTo(new byte[] { 0xBA, 0xBA }));
            Assert.That(enabled.IsHanziCompatibilityMode, Is.True);
            Assert.That(disabled.Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(disabled.Data, Is.EqualTo(Encoding.UTF8.GetBytes("汉")));
            Assert.That(disabled.EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
        });
    }

    [Test]
    public void UsesDefaultLatin1OrUtf8ForRemainingTextDeterministically()
    {
        QrSegment latin1 = CreateTextSegment("café", -1, false);
        QrSegment utf8 = CreateTextSegment("a😀", -1, false);

        Assert.Multiple(() =>
        {
            Assert.That(latin1.Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(latin1.Data, Is.EqualTo(new byte[] { 0x63, 0x61, 0x66, 0xE9 }));
            Assert.That(latin1.CharacterCount, Is.EqualTo(4));
            Assert.That(latin1.EciAssignment, Is.EqualTo(-1));
            Assert.That(utf8.Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(utf8.Data, Is.EqualTo(new byte[] { 0x61, 0xF0, 0x9F, 0x98, 0x80 }));
            Assert.That(utf8.CharacterCount, Is.EqualTo(5));
            Assert.That(utf8.EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
        });
    }

    [Test]
    public void HonorsAnExplicitSupportedByteEciAssignment()
    {
        QrSegment segment = CreateTextSegment("A😀", QrEciAssignments.Utf16BigEndian, false);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(segment.Data, Is.EqualTo(new byte[] { 0x00, 0x41, 0xD8, 0x3D, 0xDE, 0x00 }));
            Assert.That(segment.CharacterCount, Is.EqualTo(6));
            Assert.That(segment.Text, Is.EqualTo("A😀"));
            Assert.That(segment.EciAssignment, Is.EqualTo(QrEciAssignments.Utf16BigEndian));
        });
    }

    [Test]
    public void CreatesAByteSegmentForBinaryInputAndDefensivelyCopiesIt()
    {
        byte[] input = new byte[] { 0, 1, 255 };

        QrSegment segment = SegmentOptimizer.CreateSingleModeByteSegment(
            input,
            QrEciAssignments.Binary,
            3);
        input[1] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(segment.CharacterCount, Is.EqualTo(3));
            Assert.That(segment.Data, Is.EqualTo(new byte[] { 0, 1, 255 }));
            Assert.That(segment.Data, Is.Not.SameAs(input));
            Assert.That(segment.Text, Is.Null);
            Assert.That(segment.EciAssignment, Is.EqualTo(QrEciAssignments.Binary));
        });
    }

    [Test]
    public void HandlesEmptyTextAndEmptyBinaryInputExplicitly()
    {
        QrSegment[] textSegments = SegmentOptimizer.CreateSingleModeTextSegments("", -1, false, 0);
        QrSegment binarySegment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[0],
            -1,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(textSegments, Is.Empty);
            Assert.That(binarySegment.Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(binarySegment.CharacterCount, Is.Zero);
            Assert.That(binarySegment.Data, Is.Empty);
        });
    }

    [Test]
    public void EnforcesNullAssignmentEncodingAndResourceConstraints()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => SegmentOptimizer.CreateSingleModeTextSegments(
                null!, -1, false, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentOptimizer.CreateSingleModeByteSegment(
                null!, -1, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentOptimizer.CreateSingleModeTextSegments(
                "a", -2, false, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentOptimizer.CreateSingleModeTextSegments(
                "a", 999998, false, 1), Throws.TypeOf<QrUnsupportedFeatureException>());
            Assert.That(() => SegmentOptimizer.CreateSingleModeTextSegments(
                "12", -1, false, 1), Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => SegmentOptimizer.CreateSingleModeByteSegment(
                new byte[] { 1, 2 }, -1, 1), Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => SegmentOptimizer.CreateSingleModeByteSegment(
                new byte[0], -2, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentOptimizer.CreateSingleModeTextSegments(
                "", -1, false, -1), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void ProducesSegmentsAcceptedByManualValidation()
    {
        QrSegment[] numeric = SegmentOptimizer.CreateSingleModeTextSegments(
            "12345", -1, false, 10);
        QrSegment[] utf8 = SegmentOptimizer.CreateSingleModeTextSegments(
            "hello 😀", -1, false, 20);
        QrSegment binary = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 0, 255 }, -1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(() => SegmentValidator.ValidateSegments(
                numeric, QrSymbolKind.Model2, 1, 2, 10, false), Throws.Nothing);
            Assert.That(() => SegmentValidator.ValidateSegments(
                utf8, QrSymbolKind.Model2, 1, 2, 20, false), Throws.Nothing);
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { binary }, QrSymbolKind.Model2, 1, 2, 2, false), Throws.Nothing);
        });
    }

    [TestCase("A1b23456789C")]
    [TestCase("123ABC4567xyz890")]
    [TestCase("A1234b56789012C")]
    public void MixedAsciiSegmentationHasTheGlobalMinimumBitLength(string text)
    {
        QrSegment[] segments = Optimize(text, QrSymbolKind.Model2, 1, -1, 20, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(CalculateSegmentBits(segments, QrSymbolKind.Model2, 1),
                Is.EqualTo(CalculateBruteForceAsciiMinimum(text, 1)));
            Assert.That(JoinSegmentText(segments), Is.EqualTo(text));
        });
    }

    [Test]
    public void Model2CharacterCountGroupsChangeTheOptimalBreakEvenPoint()
    {
        string text = "a12345678a";

        QrSegment[] groupOne = Optimize(text, QrSymbolKind.Model2, 1, -1, 20, 1000);
        QrSegment[] groupTwo = Optimize(text, QrSymbolKind.Model2, 10, -1, 20, 1000);
        QrSegment[] groupThree = Optimize(text, QrSymbolKind.Model2, 27, -1, 20, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(GetModes(groupOne), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Byte,
                QrEncodingMode.Numeric,
                QrEncodingMode.Byte
            }));
            Assert.That(GetModes(groupTwo), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Byte,
                QrEncodingMode.Numeric,
                QrEncodingMode.Byte
            }));
            Assert.That(GetModes(groupThree), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Byte
            }));
            Assert.That(CalculateSegmentBits(groupOne, QrSymbolKind.Model2, 1), Is.EqualTo(81));
            Assert.That(CalculateSegmentBits(groupTwo, QrSymbolKind.Model2, 10), Is.EqualTo(99));
            Assert.That(CalculateSegmentBits(groupThree, QrSymbolKind.Model2, 27), Is.EqualTo(100));
        });
    }

    [Test]
    public void SeparatesKanjiRunsOnlyWhenTheirHeaderCostIsRecovered()
    {
        QrSegment[] segments = Optimize(
            "a漢字漢字a",
            QrSymbolKind.Model2,
            1,
            -1,
            20,
            1000);

        Assert.Multiple(() =>
        {
            Assert.That(GetModes(segments), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Byte,
                QrEncodingMode.Kanji,
                QrEncodingMode.Byte
            }));
            Assert.That(segments[1].Text, Is.EqualTo("漢字漢字"));
            Assert.That(segments[1].Data, Is.EqualTo(new byte[]
            {
                0x8A, 0xBF, 0x8E, 0x9A, 0x8A, 0xBF, 0x8E, 0x9A
            }));
            Assert.That(CalculateSegmentBits(segments, QrSymbolKind.Model2, 1), Is.EqualTo(104));
        });
    }

    [Test]
    public void MicroProfilesRestrictModesAndSplitAtCountFieldLimits()
    {
        QrSegment[] numeric = Optimize("12345678", QrSymbolKind.Micro, 1, -1, 20, 1000);
        QrSegment[] bytes = Optimize("abc", QrSymbolKind.Micro, 3, -1, 20, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(numeric.Length, Is.EqualTo(2));
            Assert.That(numeric[0].Mode, Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(numeric[1].Mode, Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(numeric[0].CharacterCount, Is.LessThanOrEqualTo(7));
            Assert.That(numeric[1].CharacterCount, Is.LessThanOrEqualTo(7));
            Assert.That(CalculateSegmentBits(numeric, QrSymbolKind.Micro, 1), Is.EqualTo(33));
            Assert.That(bytes.Length, Is.EqualTo(1));
            Assert.That(bytes[0].Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(() => Optimize("abc", QrSymbolKind.Micro, 2, -1, 20, 1000),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RmqrByteCountSplitsOnlyAtUnicodeScalarBoundaries()
    {
        QrSegment[] segments = Optimize("😀😀😀", QrSymbolKind.rMQR, 0, -1, 20, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(segments.Length, Is.EqualTo(3));
            Assert.That(GetModes(segments), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.Byte,
                QrEncodingMode.Byte,
                QrEncodingMode.Byte
            }));
            Assert.That(segments[0].Text, Is.EqualTo("😀"));
            Assert.That(segments[1].Text, Is.EqualTo("😀"));
            Assert.That(segments[2].Text, Is.EqualTo("😀"));
            Assert.That(segments[0].CharacterCount, Is.EqualTo(4));
            Assert.That(segments[1].CharacterCount, Is.EqualTo(4));
            Assert.That(segments[2].CharacterCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void EnforcesOptimizationInputsAndResultLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Optimize("", QrSymbolKind.Model2, 1, -1, 1, 0), Is.Empty);
            Assert.That(() => Optimize(null!, QrSymbolKind.Model2, 1, -1, 1, 1),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize("a12345678a", QrSymbolKind.Model2, 1, -1, 2, 100),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => Optimize("12", QrSymbolKind.Model2, 1, -1, 2, 1),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => Optimize("\uD800", QrSymbolKind.Model2, 1, -1, 2, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize("abc", QrSymbolKind.Model2, 0, -1, 2, 10),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Optimize("abc", QrSymbolKind.Model2, 1, 999998, 2, 10),
                Throws.TypeOf<QrUnsupportedFeatureException>());
        });
    }

    private static QrSegment[] Optimize(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int byteEciAssignment,
        int maximumSegments,
        int maximumInputBytes)
    {
        return SegmentOptimizer.OptimizeTextSegments(
            text,
            symbolKind,
            version,
            byteEciAssignment,
            maximumSegments,
            maximumInputBytes);
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

    private static string JoinSegmentText(QrSegment[] segments)
    {
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < segments.Length; index++)
        {
            builder.Append(segments[index].Text);
        }

        return builder.ToString();
    }

    private static int CalculateBruteForceAsciiMinimum(string text, int version)
    {
        int[] minimumBits = new int[text.Length + 1];
        for (int end = 1; end <= text.Length; end++)
        {
            minimumBits[end] = int.MaxValue;
            bool numeric = true;
            bool alphanumeric = true;
            for (int start = end - 1; start >= 0; start--)
            {
                char character = text[start];
                numeric = numeric && character >= '0' && character <= '9';
                alphanumeric = alphanumeric
                    && QrCharacterSets.Alphanumeric.IndexOf(character) >= 0;
                int length = end - start;
                int byteBits = minimumBits[start]
                    + 4
                    + Model2VersionTable.GetCharacterCountBitWidth(2, version)
                    + ByteEncoder.CalculateBitLength(length);
                if (byteBits < minimumBits[end])
                {
                    minimumBits[end] = byteBits;
                }

                if (alphanumeric)
                {
                    int alphanumericBits = minimumBits[start]
                        + 4
                        + Model2VersionTable.GetCharacterCountBitWidth(1, version)
                        + AlphanumericEncoder.CalculateBitLength(length);
                    if (alphanumericBits < minimumBits[end])
                    {
                        minimumBits[end] = alphanumericBits;
                    }
                }

                if (numeric)
                {
                    int numericBits = minimumBits[start]
                        + 4
                        + Model2VersionTable.GetCharacterCountBitWidth(0, version)
                        + NumericEncoder.CalculateBitLength(length);
                    if (numericBits < minimumBits[end])
                    {
                        minimumBits[end] = numericBits;
                    }
                }
            }
        }

        return minimumBits[text.Length];
    }

    private static int CalculateSegmentBits(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version)
    {
        int total = 0;
        int modeWidth = 4;
        if (symbolKind == QrSymbolKind.Micro)
        {
            modeWidth = MicroVersionTable.GetModeIndicatorBitWidth(version);
        }
        else if (symbolKind == QrSymbolKind.rMQR)
        {
            modeWidth = RectangularMicroVersionTable.ModeIndicatorBitWidth;
        }

        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            int modeIndex = GetModeIndex(segment.Mode);
            int countWidth;
            if (symbolKind == QrSymbolKind.Model2)
            {
                countWidth = Model2VersionTable.GetCharacterCountBitWidth(modeIndex, version);
            }
            else if (symbolKind == QrSymbolKind.Micro)
            {
                countWidth = MicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
            }
            else if (symbolKind == QrSymbolKind.rMQR)
            {
                countWidth = RectangularMicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
            }
            else
            {
                countWidth = LegacyModel1Table.GetCharacterCountBitWidth(version, modeIndex);
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

    private static QrSegment CreateTextSegment(
        string text,
        int byteEciAssignment,
        bool enableHanziCompatibility)
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            text,
            byteEciAssignment,
            enableHanziCompatibility,
            1000);
        return segments[0];
    }
}
