using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class CodewordFinalizerTests
{
    [Test]
    public void ProducesCheckedInGoldenDataCodewordsForEveryProfile()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                FinalizeText("01234567", QrSymbolKind.Model2, 1, EccBlockTable.Low),
                Is.EqualTo(new byte[]
                {
                    16, 32, 12, 86, 97, 128, 236, 17, 236, 17,
                    236, 17, 236, 17, 236, 17, 236, 17, 236
                }));
            Assert.That(
                FinalizeText(
                    "12345",
                    QrSymbolKind.Micro,
                    1,
                    MicroVersionTable.DetectionOnly),
                Is.EqualTo(new byte[] { 163, 218, 208 }));
            Assert.That(
                FinalizeText("HELLO", QrSymbolKind.Micro, 2, EccBlockTable.Low),
                Is.EqualTo(new byte[] { 214, 22, 241, 152, 0 }));
            Assert.That(
                FinalizeText("micro3", QrSymbolKind.Micro, 3, EccBlockTable.Low),
                Is.EqualTo(new byte[] { 153, 181, 165, 141, 201, 188, 204, 0, 236, 17, 0 }));
            Assert.That(
                FinalizeText("MICRO FOUR", QrSymbolKind.Micro, 4, EccBlockTable.Low),
                Is.EqualTo(new byte[]
                {
                    42, 126, 8, 222, 46, 43, 186, 194,
                    0, 236, 17, 236, 17, 236, 17, 236
                }));
            Assert.That(
                FinalizeText(
                    "12345",
                    QrSymbolKind.rMQR,
                    0,
                    RectangularMicroVersionTable.Medium),
                Is.EqualTo(new byte[] { 42, 61, 173, 0, 236, 17 }));
            Assert.That(
                FinalizeText("QR Code Model 1 ", QrSymbolKind.LegacyModel1, 2, EccBlockTable.Medium),
                Is.EqualTo(new byte[]
                {
                    4, 16, 81, 82, 32, 67, 111, 100, 101, 32,
                    77, 111, 100, 101, 108, 32, 49, 32, 0, 236,
                    17, 236, 17, 236, 17, 236, 17, 236, 17, 236
                }));
        });
    }

    [Test]
    public void ConcatenatesStructuredAppendFnc1EciAndPayloadHeadersInOrder()
    {
        QrSegment structuredAppend = new QrSegment
        {
            Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = new QrStructuredAppendInfo
            {
                Index = 1,
                Total = 3,
                Parity = 0xA5
            }
        };
        QrSegment byteSegment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[] { 0xAB },
            -1,
            1000);
        QrSegment[] segments = new QrSegment[]
        {
            structuredAppend,
            Fnc1Encoder.CreateSecondPositionControlSegment(42),
            EciEncoder.CreateControlSegment(26),
            byteSegment
        };

        byte[] result = CodewordFinalizer.CreateDataCodewords(
            segments,
            QrSymbolKind.Model2,
            1,
            EccBlockTable.Low,
            false);
        BitReader reader = new BitReader(result, result.Length * 8);

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadBits(4), Is.EqualTo(3U));
            Assert.That(reader.ReadBits(4), Is.EqualTo(1U));
            Assert.That(reader.ReadBits(4), Is.EqualTo(2U));
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xA5U));
            Assert.That(reader.ReadBits(4), Is.EqualTo(9U));
            Assert.That(reader.ReadBits(8), Is.EqualTo(42U));
            Assert.That(reader.ReadBits(4), Is.EqualTo(7U));
            Assert.That(reader.ReadBits(8), Is.EqualTo(26U));
            Assert.That(reader.ReadBits(4), Is.EqualTo(4U));
            Assert.That(reader.ReadBits(8), Is.EqualTo(1U));
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xABU));
            Assert.That(reader.ReadBits(4), Is.Zero);
            Assert.That(reader.ReadBits(4), Is.Zero);
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xECU));
            Assert.That(reader.ReadBits(8), Is.EqualTo(0x11U));
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xECU));
        });
    }

    [Test]
    public void TruncatesTerminatorToTheRemainingCapacity()
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            new string('1', 41),
            -1,
            false,
            1000);

        byte[] result = CodewordFinalizer.CreateDataCodewords(
            segments,
            QrSymbolKind.Model2,
            1,
            EccBlockTable.Low,
            false);
        BitReader reader = new BitReader(result, 152);
        reader.SkipBits(151);

        Assert.Multiple(() =>
        {
            Assert.That(result.Length, Is.EqualTo(19));
            Assert.That(reader.ReadBit(), Is.False);
            Assert.That(reader.GetRemainingBitCount(), Is.Zero);
        });
    }

    [Test]
    public void EmptyPayloadsUseProfileTerminatorAlignmentAndAlternatingPadding()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                CodewordFinalizer.CreateDataCodewords(
                    new QrSegment[0],
                    QrSymbolKind.Model2,
                    1,
                    EccBlockTable.Low,
                    false),
                Is.EqualTo(CreatePaddedBytes(19, false)));
            Assert.That(
                CodewordFinalizer.CreateDataCodewords(
                    new QrSegment[0],
                    QrSymbolKind.Micro,
                    1,
                    MicroVersionTable.DetectionOnly,
                    false),
                Is.EqualTo(new byte[] { 0, 0xEC, 0 }));
            Assert.That(
                CodewordFinalizer.CreateDataCodewords(
                    new QrSegment[0],
                    QrSymbolKind.rMQR,
                    0,
                    RectangularMicroVersionTable.Medium,
                    false),
                Is.EqualTo(CreatePaddedBytes(6, false)));
            Assert.That(
                CodewordFinalizer.CreateDataCodewords(
                    new QrSegment[0],
                    QrSymbolKind.LegacyModel1,
                    1,
                    EccBlockTable.Low,
                    false),
                Is.EqualTo(CreatePaddedBytes(19, true)));
        });
    }

    [Test]
    public void Fnc1UsesTheEscapedAlphanumericCharacterCount()
    {
        QrSegment[] dataSegments = SegmentOptimizer.CreateSingleModeTextSegments(
            "A%",
            -1,
            false,
            1000);
        QrSegment[] segments = new QrSegment[]
        {
            Fnc1Encoder.CreateFirstPositionControlSegment(),
            dataSegments[0]
        };

        byte[] result = CodewordFinalizer.CreateDataCodewords(
            segments,
            QrSymbolKind.Model2,
            1,
            EccBlockTable.Low,
            false);
        BitReader reader = new BitReader(result, result.Length * 8);

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadBits(4), Is.EqualTo(5U));
            Assert.That(reader.ReadBits(4), Is.EqualTo(2U));
            Assert.That(reader.ReadBits(9), Is.EqualTo(3U));
            Assert.That(Fnc1Decoder.DecodeFirstPositionAlphanumeric(reader, 3), Is.EqualTo("A%"));
        });
    }

    [Test]
    public void HanziWritesSubsetBeforeCharacterCountAndPayload()
    {
        QrSegment segment = HanziEncoder.CreateCompatibilitySegment("中", true);

        byte[] result = CodewordFinalizer.CreateDataCodewords(
            new QrSegment[] { segment },
            QrSymbolKind.Model2,
            1,
            EccBlockTable.Low,
            true);
        BitReader reader = new BitReader(result, result.Length * 8);

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadBits(4), Is.EqualTo(13U));
            Assert.That(reader.ReadBits(4), Is.EqualTo(1U));
            Assert.That(reader.ReadBits(8), Is.EqualTo(1U));
            Assert.That(
                reader.ReadBits(13),
                Is.EqualTo((uint)HanziEncoder.TransformGb2312Value(
                    HanziEncoder.GetGb2312Value('中'))));
        });
    }

    [Test]
    public void ReportsRequiredAndAvailableBitsWhenPayloadDoesNotFit()
    {
        QrSegment segment = SegmentOptimizer.CreateSingleModeByteSegment(
            new byte[18],
            -1,
            1000);

        QrCapacityException exception = Assert.Throws<QrCapacityException>(() =>
            CodewordFinalizer.CreateDataCodewords(
                new QrSegment[] { segment },
                QrSymbolKind.Model2,
                1,
                EccBlockTable.Low,
                false))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.SymbolKind, Is.EqualTo(QrSymbolKind.Model2));
            Assert.That(exception.RequiredBitCount, Is.EqualTo(156));
            Assert.That(exception.AvailableBitCount, Is.EqualTo(152));
        });
    }

    private static byte[] FinalizeText(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex)
    {
        QrSegment[] segments = SegmentOptimizer.CreateSingleModeTextSegments(
            text,
            -1,
            false,
            1000);
        return CodewordFinalizer.CreateDataCodewords(
            segments,
            symbolKind,
            version,
            errorCorrectionIndex,
            false);
    }

    private static byte[] CreatePaddedBytes(int length, bool legacyFirstNibble)
    {
        byte[] result = new byte[length];
        int index = 1;
        bool useFirstPadCodeword = true;
        while (index < result.Length)
        {
            result[index] = useFirstPadCodeword ? (byte)0xEC : (byte)0x11;
            useFirstPadCodeword = !useFirstPadCodeword;
            index++;
        }

        if (!legacyFirstNibble)
        {
            result[0] = 0;
        }

        return result;
    }
}
