using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MicroCapacityCalculatorTests
{
    internal static IEnumerable<TestCaseData> PublicBoundaryCases()
    {
        foreach (int[] configuration in SymbolConfigurations)
            for (int mode = 0; mode < 4; mode++)
            {
                int version = configuration[0];
                int countWidth = CountWidths[version - 1][mode];
                if (countWidth == 0) continue;
                QrCapacityRequest request = CreateRequest(version, (QrErrorCorrectionLevel)configuration[1], GetMode(mode), 0, 0);
                yield return new TestCaseData(request, countWidth, version - 1 + countWidth,
                    configuration[2], configuration[3], configuration[3] + configuration[4], 0)
                    .SetName($"PublicCapacity_Micro_M{version}_{request.ErrorCorrectionLevel}_{request.EncodingMode}");
            }
    }

    private static readonly int[][] SymbolConfigurations = new int[][]
    {
        new int[] { 1, -1, 20, 3, 2 },
        new int[] { 2, 0, 40, 5, 5 },
        new int[] { 2, 1, 32, 4, 6 },
        new int[] { 3, 0, 84, 11, 6 },
        new int[] { 3, 1, 68, 9, 8 },
        new int[] { 4, 0, 128, 16, 8 },
        new int[] { 4, 1, 112, 14, 10 },
        new int[] { 4, 2, 80, 10, 14 }
    };

    private static readonly int[][] CountWidths = new int[][]
    {
        new int[] { 3, 0, 0, 0 },
        new int[] { 4, 3, 0, 0 },
        new int[] { 5, 4, 4, 3 },
        new int[] { 6, 5, 5, 4 }
    };

    private static readonly int[][] MaximumCharacterCounts = new int[][]
    {
        new int[] { 5, 0, 0, 0 },
        new int[] { 10, 6, 0, 0 },
        new int[] { 8, 5, 0, 0 },
        new int[] { 23, 14, 9, 6 },
        new int[] { 18, 11, 7, 4 },
        new int[] { 35, 21, 15, 9 },
        new int[] { 30, 18, 13, 8 },
        new int[] { 21, 13, 9, 5 }
    };

    [Test]
    public void ReportsEveryStandardVersionEccAndModeCapacity()
    {
        for (int symbolNumber = 0; symbolNumber < SymbolConfigurations.Length; symbolNumber++)
        {
            int[] configuration = SymbolConfigurations[symbolNumber];
            int version = configuration[0];
            int level = configuration[1];
            int dataBitCapacity = configuration[2];
            int dataCodewordCount = configuration[3];
            int eccCodewordCount = configuration[4];
            for (int modeIndex = 0; modeIndex < 4; modeIndex++)
            {
                int countWidth = CountWidths[version - 1][modeIndex];
                if (countWidth == 0)
                {
                    continue;
                }

                QrCapacityRequest request = CreateRequest(
                    version,
                    (QrErrorCorrectionLevel)level,
                    GetMode(modeIndex),
                    0,
                    0);
                QrCapacityResult result = MicroCapacityCalculator.CalculateCapacity(request);
                int requiredBits = version - 1 + countWidth;

                Assert.Multiple(() =>
                {
                    Assert.That(result.Fits, Is.True, GetCaseName(symbolNumber, modeIndex));
                    Assert.That(result.RequiredBitCount,
                        Is.EqualTo(requiredBits), GetCaseName(symbolNumber, modeIndex));
                    Assert.That(result.DataBitCapacity,
                        Is.EqualTo(dataBitCapacity), GetCaseName(symbolNumber, modeIndex));
                    Assert.That(result.UnusedBitCount,
                        Is.EqualTo(dataBitCapacity - requiredBits), GetCaseName(symbolNumber, modeIndex));
                    Assert.That(result.DataCodewordCount,
                        Is.EqualTo(dataCodewordCount), GetCaseName(symbolNumber, modeIndex));
                    Assert.That(result.ErrorCorrectionCodewordCount,
                        Is.EqualTo(eccCodewordCount), GetCaseName(symbolNumber, modeIndex));
                    Assert.That(result.TotalCodewordCount,
                        Is.EqualTo(dataCodewordCount + eccCodewordCount), GetCaseName(symbolNumber, modeIndex));
                    Assert.That(result.RemainderBitCount,
                        Is.Zero, GetCaseName(symbolNumber, modeIndex));
                });
            }
        }
    }

    [Test]
    public void PublishedModeMaximumsFitAndTheNextCharacterDoesNot()
    {
        for (int symbolNumber = 0; symbolNumber < SymbolConfigurations.Length; symbolNumber++)
        {
            int[] configuration = SymbolConfigurations[symbolNumber];
            for (int modeIndex = 0; modeIndex < 4; modeIndex++)
            {
                int maximum = MaximumCharacterCounts[symbolNumber][modeIndex];
                if (maximum == 0)
                {
                    continue;
                }

                QrEncodingMode mode = GetMode(modeIndex);
                QrCapacityResult maximumResult = MicroCapacityCalculator.CalculateCapacity(
                    CreateRequest(
                        configuration[0],
                        (QrErrorCorrectionLevel)configuration[1],
                        mode,
                        maximum,
                        GetPayloadBitCount(mode, maximum)));
                int nextCount = maximum + 1;
                QrCapacityResult nextResult = MicroCapacityCalculator.CalculateCapacity(
                    CreateRequest(
                        configuration[0],
                        (QrErrorCorrectionLevel)configuration[1],
                        mode,
                        nextCount,
                        GetPayloadBitCount(mode, nextCount)));

                Assert.Multiple(() =>
                {
                    Assert.That(maximumResult.Fits,
                        Is.True, GetCaseName(symbolNumber, modeIndex));
                    Assert.That(maximumResult.UnusedBitCount,
                        Is.GreaterThanOrEqualTo(0), GetCaseName(symbolNumber, modeIndex));
                    Assert.That(nextResult.Fits,
                        Is.False, GetCaseName(symbolNumber, modeIndex));
                    Assert.That(nextResult.UnusedBitCount,
                        Is.LessThan(0), GetCaseName(symbolNumber, modeIndex));
                });
            }
        }
    }

    [Test]
    public void PreservesPartialFinalDataCodewordCapacity()
    {
        QrCapacityResult m1 = MicroCapacityCalculator.CalculateCapacity(
            CreateRequest(1, QrErrorCorrectionLevel.None, QrEncodingMode.Numeric, 5, 17));
        QrCapacityResult m3Low = MicroCapacityCalculator.CalculateCapacity(
            CreateRequest(3, QrErrorCorrectionLevel.Low, QrEncodingMode.Byte, 9, 72));
        QrCapacityResult m3Medium = MicroCapacityCalculator.CalculateCapacity(
            CreateRequest(3, QrErrorCorrectionLevel.Medium, QrEncodingMode.Byte, 7, 56));

        Assert.Multiple(() =>
        {
            Assert.That(m1.DataCodewordCount, Is.EqualTo(3));
            Assert.That(m1.DataBitCapacity, Is.EqualTo(20));
            Assert.That(m1.RequiredBitCount, Is.EqualTo(20));
            Assert.That(m3Low.DataCodewordCount, Is.EqualTo(11));
            Assert.That(m3Low.DataBitCapacity, Is.EqualTo(84));
            Assert.That(m3Medium.DataCodewordCount, Is.EqualTo(9));
            Assert.That(m3Medium.DataBitCapacity, Is.EqualTo(68));
        });
    }

    [Test]
    public void AcceptsDerivedOrExactSquareDimensions()
    {
        QrCapacityRequest derived = CreateRequest(
            4, QrErrorCorrectionLevel.Quartile, QrEncodingMode.Byte, 1, 8);
        QrCapacityRequest exact = CreateRequest(
            4, QrErrorCorrectionLevel.Quartile, QrEncodingMode.Byte, 1, 8);
        exact.Width = 17;
        exact.Height = 17;
        QrCapacityRequest wrongWidth = CreateRequest(
            4, QrErrorCorrectionLevel.Quartile, QrEncodingMode.Byte, 1, 8);
        wrongWidth.Width = 16;
        QrCapacityRequest wrongHeight = CreateRequest(
            4, QrErrorCorrectionLevel.Quartile, QrEncodingMode.Byte, 1, 8);
        wrongHeight.Height = 18;

        Assert.Multiple(() =>
        {
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(derived), Throws.Nothing);
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(exact), Throws.Nothing);
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(wrongWidth),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(wrongHeight),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsUnsupportedVersionEccModeAndControlCombinations()
    {
        QrCapacityRequest wrongKind = CreateRequest(
            1, QrErrorCorrectionLevel.None, QrEncodingMode.Numeric, 1, 4);
        wrongKind.SymbolKind = QrSymbolKind.Model2;
        QrCapacityRequest wrongVersion = CreateRequest(
            5, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4);
        QrCapacityRequest m1WithEcc = CreateRequest(
            1, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4);
        QrCapacityRequest m2WithoutEcc = CreateRequest(
            2, QrErrorCorrectionLevel.None, QrEncodingMode.Numeric, 1, 4);
        QrCapacityRequest highEcc = CreateRequest(
            4, QrErrorCorrectionLevel.High, QrEncodingMode.Numeric, 1, 4);
        QrCapacityRequest m2Byte = CreateRequest(
            2, QrErrorCorrectionLevel.Low, QrEncodingMode.Byte, 1, 8);
        QrCapacityRequest hanzi = CreateRequest(
            4, QrErrorCorrectionLevel.Low, QrEncodingMode.Hanzi, 1, 13);
        QrCapacityRequest control = CreateRequest(
            4, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4);
        control.ControlBitCount = 1;

        Assert.Multiple(() =>
        {
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(wrongKind),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(wrongVersion),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(m1WithEcc),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(m2WithoutEcc),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(highEcc),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(m2Byte),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(hanzi),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(control),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsInvalidCountsAndPayloadLengths()
    {
        QrCapacityRequest negativeCount = CreateRequest(
            4, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, -1, 0);
        QrCapacityRequest negativePayload = CreateRequest(
            4, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 0, -1);
        QrCapacityRequest inconsistentPayload = CreateRequest(
            4, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 3, 9);
        QrCapacityRequest countFieldOverflow = CreateRequest(
            2, QrErrorCorrectionLevel.Low, QrEncodingMode.Alphanumeric, 8, 44);

        Assert.Multiple(() =>
        {
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(negativeCount),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(negativePayload),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(inconsistentPayload),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => MicroCapacityCalculator.CalculateCapacity(countFieldOverflow),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static QrCapacityRequest CreateRequest(
        int version,
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrEncodingMode encodingMode,
        int characterCount,
        int payloadBitCount)
    {
        return new QrCapacityRequest
        {
            SymbolKind = QrSymbolKind.Micro,
            Version = version,
            ErrorCorrectionLevel = errorCorrectionLevel,
            EncodingMode = encodingMode,
            CharacterCount = characterCount,
            PayloadBitCount = payloadBitCount
        };
    }

    private static QrEncodingMode GetMode(int modeIndex)
    {
        if (modeIndex == 0)
        {
            return QrEncodingMode.Numeric;
        }

        if (modeIndex == 1)
        {
            return QrEncodingMode.Alphanumeric;
        }

        if (modeIndex == 2)
        {
            return QrEncodingMode.Byte;
        }

        return QrEncodingMode.Kanji;
    }

    private static int GetPayloadBitCount(QrEncodingMode mode, int characterCount)
    {
        if (mode == QrEncodingMode.Numeric)
        {
            int groups = characterCount / 3;
            int remainder = characterCount % 3;
            int bits = groups * 10;
            if (remainder == 1)
            {
                bits += 4;
            }
            else if (remainder == 2)
            {
                bits += 7;
            }

            return bits;
        }

        if (mode == QrEncodingMode.Alphanumeric)
        {
            return characterCount / 2 * 11 + characterCount % 2 * 6;
        }

        if (mode == QrEncodingMode.Byte)
        {
            return characterCount * 8;
        }

        return characterCount * 13;
    }

    private static string GetCaseName(int symbolNumber, int modeIndex)
    {
        return "symbol " + symbolNumber + ", mode " + modeIndex;
    }
}
