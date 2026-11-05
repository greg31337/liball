using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class Model2CapacityCalculatorTests
{
    internal static IEnumerable<TestCaseData> PublicBoundaryCases()
    {
        QrEncodingMode[] modes = [QrEncodingMode.Numeric, QrEncodingMode.Alphanumeric, QrEncodingMode.Byte, QrEncodingMode.Kanji, QrEncodingMode.Hanzi];
        int[][] countWidths = [[10, 9, 8, 8, 8], [12, 11, 16, 10, 10], [14, 13, 16, 12, 12]];
        for (int version = 1; version <= 40; version++)
            for (int level = 0; level < 4; level++)
                for (int mode = 0; mode < modes.Length; mode++)
                {
                    int countWidth = countWidths[version < 10 ? 0 : version < 27 ? 1 : 2][mode];
                    QrCapacityRequest request = CreateRequest(version, (QrErrorCorrectionLevel)level, modes[mode], 0, 0, 0);
                    yield return new TestCaseData(request, countWidth, 4 + countWidth + (mode == 4 ? 4 : 0),
                        ExpectedDataCodewords[level][version - 1] * 8, ExpectedDataCodewords[level][version - 1],
                        ExpectedTotalCodewords[version - 1], ExpectedRemainderBits[version - 1])
                        .SetName($"PublicCapacity_Model2_V{version}_{(QrErrorCorrectionLevel)level}_{modes[mode]}");
                }
    }

    private static readonly int[] ExpectedTotalCodewords = new int[]
    {
        26, 44, 70, 100, 134, 172, 196, 242, 292, 346,
        404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
        1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185,
        2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706
    };

    private static readonly int[] ExpectedRemainderBits = new int[]
    {
        0, 7, 7, 7, 7, 7, 0, 0, 0, 0,
        0, 0, 0, 3, 3, 3, 3, 3, 3, 3,
        4, 4, 4, 4, 4, 4, 4, 3, 3, 3,
        3, 3, 3, 3, 0, 0, 0, 0, 0, 0
    };

    private static readonly int[][] ExpectedDataCodewords = new int[][]
    {
        new int[] { 19, 34, 55, 80, 108, 136, 156, 194, 232, 274, 324, 370, 428, 461, 523, 589, 647, 721, 795, 861, 932, 1006, 1094, 1174, 1276, 1370, 1468, 1531, 1631, 1735, 1843, 1955, 2071, 2191, 2306, 2434, 2566, 2702, 2812, 2956 },
        new int[] { 16, 28, 44, 64, 86, 108, 124, 154, 182, 216, 254, 290, 334, 365, 415, 453, 507, 563, 627, 669, 714, 782, 860, 914, 1000, 1062, 1128, 1193, 1267, 1373, 1455, 1541, 1631, 1725, 1812, 1914, 1992, 2102, 2216, 2334 },
        new int[] { 13, 22, 34, 48, 62, 76, 88, 110, 132, 154, 180, 206, 244, 261, 295, 325, 367, 397, 445, 485, 512, 568, 614, 664, 718, 754, 808, 871, 911, 985, 1033, 1115, 1171, 1231, 1286, 1354, 1426, 1502, 1582, 1666 },
        new int[] { 9, 16, 26, 36, 46, 60, 66, 86, 100, 122, 140, 158, 180, 197, 223, 253, 283, 313, 341, 385, 406, 442, 464, 514, 538, 596, 628, 661, 701, 745, 793, 845, 901, 961, 986, 1054, 1096, 1142, 1222, 1276 }
    };

    [Test]
    public void ReportsEveryStandardVersionAndErrorCorrectionCapacity()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 40; version++)
            {
                QrCapacityRequest request = CreateRequest(
                    version,
                    (QrErrorCorrectionLevel)level,
                    QrEncodingMode.Numeric,
                    0,
                    0,
                    0);

                QrCapacityResult result = Model2CapacityCalculator.CalculateCapacity(request);
                int expectedDataCodewords = ExpectedDataCodewords[level][version - 1];
                int expectedTotalCodewords = ExpectedTotalCodewords[version - 1];
                int expectedRequiredBits = 4 + GetNumericCountWidth(version);

                Assert.Multiple(() =>
                {
                    Assert.That(result.Fits, Is.True, GetCaseName(version, level));
                    Assert.That(result.RequiredBitCount, Is.EqualTo(expectedRequiredBits), GetCaseName(version, level));
                    Assert.That(result.DataCodewordCount, Is.EqualTo(expectedDataCodewords), GetCaseName(version, level));
                    Assert.That(result.DataBitCapacity, Is.EqualTo(expectedDataCodewords * 8), GetCaseName(version, level));
                    Assert.That(result.ErrorCorrectionCodewordCount,
                        Is.EqualTo(expectedTotalCodewords - expectedDataCodewords), GetCaseName(version, level));
                    Assert.That(result.TotalCodewordCount,
                        Is.EqualTo(expectedTotalCodewords), GetCaseName(version, level));
                    Assert.That(result.RemainderBitCount,
                        Is.EqualTo(ExpectedRemainderBits[version - 1]), GetCaseName(version, level));
                    Assert.That(result.UnusedBitCount,
                        Is.EqualTo(expectedDataCodewords * 8 - expectedRequiredBits), GetCaseName(version, level));
                });
            }
        }
    }

    [Test]
    public void UsesOnlyDataCodewordsAsPayloadCapacity()
    {
        QrCapacityResult versionTwoLow = Model2CapacityCalculator.CalculateCapacity(
            CreateRequest(2, QrErrorCorrectionLevel.Low, QrEncodingMode.Byte, 1, 8, 0));

        Assert.Multiple(() =>
        {
            Assert.That(versionTwoLow.DataCodewordCount, Is.EqualTo(34));
            Assert.That(versionTwoLow.DataBitCapacity, Is.EqualTo(272));
            Assert.That(versionTwoLow.ErrorCorrectionCodewordCount, Is.EqualTo(10));
            Assert.That(versionTwoLow.TotalCodewordCount, Is.EqualTo(44));
            Assert.That(versionTwoLow.RemainderBitCount, Is.EqualTo(7));
            Assert.That(versionTwoLow.DataBitCapacity,
                Is.Not.EqualTo(versionTwoLow.TotalCodewordCount * 8 + versionTwoLow.RemainderBitCount));
        });
    }

    [Test]
    public void DistinguishesExactFitsFromCapacityOverflow()
    {
        QrCapacityResult fits = Model2CapacityCalculator.CalculateCapacity(
            CreateRequest(1, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 41, 137, 1));
        QrCapacityResult overflows = Model2CapacityCalculator.CalculateCapacity(
            CreateRequest(1, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 42, 140, 0));

        Assert.Multiple(() =>
        {
            Assert.That(fits.RequiredBitCount, Is.EqualTo(152));
            Assert.That(fits.DataBitCapacity, Is.EqualTo(152));
            Assert.That(fits.UnusedBitCount, Is.Zero);
            Assert.That(fits.Fits, Is.True);
            Assert.That(overflows.RequiredBitCount, Is.EqualTo(154));
            Assert.That(overflows.UnusedBitCount, Is.EqualTo(-2));
            Assert.That(overflows.Fits, Is.False);
        });
    }

    [Test]
    public void ChargesModeCountControlAndHanziSubsetBits()
    {
        QrCapacityResult alphanumeric = Model2CapacityCalculator.CalculateCapacity(
            CreateRequest(10, QrErrorCorrectionLevel.Low, QrEncodingMode.Alphanumeric, 2, 11, 20));
        QrCapacityResult bytes = Model2CapacityCalculator.CalculateCapacity(
            CreateRequest(10, QrErrorCorrectionLevel.Low, QrEncodingMode.Byte, 2, 16, 0));
        QrCapacityResult kanji = Model2CapacityCalculator.CalculateCapacity(
            CreateRequest(10, QrErrorCorrectionLevel.Low, QrEncodingMode.Kanji, 1, 13, 0));
        QrCapacityResult hanzi = Model2CapacityCalculator.CalculateCapacity(
            CreateRequest(10, QrErrorCorrectionLevel.Low, QrEncodingMode.Hanzi, 1, 13, 0));

        Assert.Multiple(() =>
        {
            Assert.That(alphanumeric.RequiredBitCount, Is.EqualTo(46));
            Assert.That(bytes.RequiredBitCount, Is.EqualTo(36));
            Assert.That(kanji.RequiredBitCount, Is.EqualTo(27));
            Assert.That(hanzi.RequiredBitCount, Is.EqualTo(31));
        });
    }

    [Test]
    public void AcceptsDerivedOrExactDimensionsAndRejectsMismatches()
    {
        QrCapacityRequest derived = CreateRequest(
            7, QrErrorCorrectionLevel.Medium, QrEncodingMode.Byte, 1, 8, 0);
        QrCapacityRequest exact = CreateRequest(
            7, QrErrorCorrectionLevel.Medium, QrEncodingMode.Byte, 1, 8, 0);
        exact.Width = 45;
        exact.Height = 45;
        QrCapacityRequest wrongWidth = CreateRequest(
            7, QrErrorCorrectionLevel.Medium, QrEncodingMode.Byte, 1, 8, 0);
        wrongWidth.Width = 44;
        QrCapacityRequest wrongHeight = CreateRequest(
            7, QrErrorCorrectionLevel.Medium, QrEncodingMode.Byte, 1, 8, 0);
        wrongHeight.Height = 46;

        Assert.Multiple(() =>
        {
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(derived), Throws.Nothing);
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(exact), Throws.Nothing);
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(wrongWidth),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(wrongHeight),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsInvalidRequestsAndInconsistentPayloadLengths()
    {
        QrCapacityRequest wrongKind = CreateRequest(
            1, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4, 0);
        wrongKind.SymbolKind = QrSymbolKind.Micro;
        QrCapacityRequest wrongLevel = CreateRequest(
            1, QrErrorCorrectionLevel.None, QrEncodingMode.Numeric, 1, 4, 0);
        QrCapacityRequest wrongMode = CreateRequest(
            1, QrErrorCorrectionLevel.Low, QrEncodingMode.Eci, 0, 0, 0);
        QrCapacityRequest wrongPayload = CreateRequest(
            1, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 3, 9, 0);
        QrCapacityRequest negativeControl = CreateRequest(
            1, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4, -1);
        QrCapacityRequest countOverflow = CreateRequest(
            1, QrErrorCorrectionLevel.Low, QrEncodingMode.Byte, 256, 2048, 0);

        Assert.Multiple(() =>
        {
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(wrongKind),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(wrongLevel),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(wrongMode),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(wrongPayload),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(negativeControl),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => Model2CapacityCalculator.CalculateCapacity(countOverflow),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static QrCapacityRequest CreateRequest(
        int version,
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrEncodingMode encodingMode,
        int characterCount,
        int payloadBitCount,
        int controlBitCount)
    {
        return new QrCapacityRequest
        {
            SymbolKind = QrSymbolKind.Model2,
            Version = version,
            ErrorCorrectionLevel = errorCorrectionLevel,
            EncodingMode = encodingMode,
            CharacterCount = characterCount,
            PayloadBitCount = payloadBitCount,
            ControlBitCount = controlBitCount
        };
    }

    private static int GetNumericCountWidth(int version)
    {
        if (version < 10)
        {
            return 10;
        }

        if (version < 27)
        {
            return 12;
        }

        return 14;
    }

    private static string GetCaseName(int version, int level)
    {
        return "version " + version + ", level " + level;
    }
}
