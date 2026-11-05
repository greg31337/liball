using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RectangularMicroCapacityCalculatorTests
{
    internal static IEnumerable<TestCaseData> PublicBoundaryCases()
    {
        for (int size = 0; size < 32; size++)
            for (int level = 0; level < 2; level++)
                for (int mode = 0; mode < 4; mode++)
                {
                    int countWidth = ExpectedCountWidths[mode][size];
                    QrCapacityRequest request = CreateRequest(size, GetErrorCorrectionLevel(level), GetMode(mode), 0, 0, 0);
                    yield return new TestCaseData(request, countWidth, 3 + countWidth,
                        ExpectedDataCodewords[level][size] * 8, ExpectedDataCodewords[level][size],
                        ExpectedTotalCodewords[size], ExpectedRemainderBits[size])
                        .SetName($"PublicCapacity_R{request.Height}x{request.Width}_{request.ErrorCorrectionLevel}_{request.EncodingMode}");
                }
    }

    private static readonly int[] ExpectedHeights = new int[]
    {
        7, 7, 7, 7, 7, 9, 9, 9, 9, 9,
        11, 11, 11, 11, 11, 11, 13, 13, 13, 13, 13, 13,
        15, 15, 15, 15, 15, 17, 17, 17, 17, 17
    };

    private static readonly int[] ExpectedWidths = new int[]
    {
        43, 59, 77, 99, 139, 43, 59, 77, 99, 139,
        27, 43, 59, 77, 99, 139, 27, 43, 59, 77, 99, 139,
        43, 59, 77, 99, 139, 43, 59, 77, 99, 139
    };

    private static readonly int[] ExpectedTotalCodewords = new int[]
    {
        13, 21, 32, 44, 68, 21, 33, 49, 66, 99,
        15, 31, 47, 67, 89, 132, 21, 41, 60, 85, 113, 166,
        51, 74, 103, 136, 199, 61, 88, 122, 160, 232
    };

    private static readonly int[] ExpectedRemainderBits = new int[]
    {
        0, 3, 5, 6, 1, 2, 3, 1, 4, 5,
        2, 1, 0, 2, 7, 6, 4, 1, 6, 4, 3, 0,
        1, 4, 6, 7, 2, 1, 2, 0, 3, 4
    };

    private static readonly int[][] ExpectedCountWidths = new int[][]
    {
        new int[] { 4, 5, 6, 7, 7, 5, 6, 7, 7, 8, 4, 6, 7, 7, 8, 8, 5, 6, 7, 7, 8, 8, 7, 7, 8, 8, 9, 7, 8, 8, 8, 9 },
        new int[] { 3, 5, 5, 6, 6, 5, 5, 6, 6, 7, 4, 5, 6, 6, 7, 7, 5, 6, 6, 7, 7, 8, 6, 7, 7, 7, 8, 6, 7, 7, 8, 8 },
        new int[] { 3, 4, 5, 5, 6, 4, 5, 5, 6, 6, 3, 5, 5, 6, 6, 7, 4, 5, 6, 6, 7, 7, 6, 6, 7, 7, 7, 6, 6, 7, 7, 8 },
        new int[] { 2, 3, 4, 5, 5, 3, 4, 5, 5, 6, 2, 4, 5, 5, 6, 6, 3, 5, 5, 6, 6, 7, 5, 5, 6, 6, 7, 5, 6, 6, 6, 7 }
    };

    private static readonly int[][] ExpectedDataCodewords = new int[][]
    {
        new int[] { 6, 12, 20, 28, 44, 12, 21, 31, 42, 63, 7, 19, 31, 43, 57, 84, 12, 27, 38, 53, 73, 106, 33, 48, 67, 88, 127, 39, 56, 78, 100, 152 },
        new int[] { 3, 7, 10, 14, 24, 7, 11, 17, 22, 33, 5, 11, 15, 23, 29, 42, 7, 13, 20, 29, 35, 54, 15, 26, 31, 48, 69, 21, 28, 38, 56, 76 }
    };

    [Test]
    public void ReportsEveryStandardSizeEccAndModeCapacity()
    {
        for (int versionIndex = 0; versionIndex < 32; versionIndex++)
        {
            for (int levelIndex = 0; levelIndex < 2; levelIndex++)
            {
                for (int modeIndex = 0; modeIndex < 4; modeIndex++)
                {
                    QrCapacityRequest request = CreateRequest(
                        versionIndex,
                        GetErrorCorrectionLevel(levelIndex),
                        GetMode(modeIndex),
                        0,
                        0,
                        0);

                    QrCapacityResult result = RectangularMicroCapacityCalculator.CalculateCapacity(request);
                    int expectedDataCodewords = ExpectedDataCodewords[levelIndex][versionIndex];
                    int expectedTotalCodewords = ExpectedTotalCodewords[versionIndex];
                    int expectedRequiredBits = 3 + ExpectedCountWidths[modeIndex][versionIndex];

                    Assert.Multiple(() =>
                    {
                        Assert.That(result.Fits,
                            Is.True, GetCaseName(versionIndex, levelIndex, modeIndex));
                        Assert.That(result.RequiredBitCount,
                            Is.EqualTo(expectedRequiredBits), GetCaseName(versionIndex, levelIndex, modeIndex));
                        Assert.That(result.DataCodewordCount,
                            Is.EqualTo(expectedDataCodewords), GetCaseName(versionIndex, levelIndex, modeIndex));
                        Assert.That(result.DataBitCapacity,
                            Is.EqualTo(expectedDataCodewords * 8), GetCaseName(versionIndex, levelIndex, modeIndex));
                        Assert.That(result.ErrorCorrectionCodewordCount,
                            Is.EqualTo(expectedTotalCodewords - expectedDataCodewords),
                            GetCaseName(versionIndex, levelIndex, modeIndex));
                        Assert.That(result.TotalCodewordCount,
                            Is.EqualTo(expectedTotalCodewords), GetCaseName(versionIndex, levelIndex, modeIndex));
                        Assert.That(result.RemainderBitCount,
                            Is.EqualTo(ExpectedRemainderBits[versionIndex]),
                            GetCaseName(versionIndex, levelIndex, modeIndex));
                        Assert.That(result.UnusedBitCount,
                            Is.EqualTo(expectedDataCodewords * 8 - expectedRequiredBits),
                            GetCaseName(versionIndex, levelIndex, modeIndex));
                    });
                }
            }
        }
    }

    [Test]
    public void IndependentlyCalculatedMaximumsFitAndTheNextCharacterDoesNot()
    {
        for (int versionIndex = 0; versionIndex < 32; versionIndex++)
        {
            for (int levelIndex = 0; levelIndex < 2; levelIndex++)
            {
                int availableBits = ExpectedDataCodewords[levelIndex][versionIndex] * 8;
                for (int modeIndex = 0; modeIndex < 4; modeIndex++)
                {
                    int countWidth = ExpectedCountWidths[modeIndex][versionIndex];
                    int maximum = CalculateMaximumCharacterCount(availableBits, countWidth, modeIndex);
                    QrEncodingMode mode = GetMode(modeIndex);
                    QrCapacityResult maximumResult = RectangularMicroCapacityCalculator.CalculateCapacity(
                        CreateRequest(
                            versionIndex,
                            GetErrorCorrectionLevel(levelIndex),
                            mode,
                            maximum,
                            GetPayloadBitCount(modeIndex, maximum),
                            0));
                    int nextCount = maximum + 1;
                    Assert.Multiple(() =>
                    {
                        Assert.That(maximumResult.Fits,
                            Is.True, GetCaseName(versionIndex, levelIndex, modeIndex));
                        Assert.That(maximumResult.UnusedBitCount,
                            Is.GreaterThanOrEqualTo(0), GetCaseName(versionIndex, levelIndex, modeIndex));
                    });

                    QrCapacityRequest nextRequest = CreateRequest(
                        versionIndex,
                        GetErrorCorrectionLevel(levelIndex),
                        mode,
                        nextCount,
                        GetPayloadBitCount(modeIndex, nextCount),
                        0);
                    int maximumByCount = (1 << countWidth) - 1;
                    if (maximum == maximumByCount)
                    {
                        Assert.That(
                            () => RectangularMicroCapacityCalculator.CalculateCapacity(nextRequest),
                            Throws.TypeOf<QrInputException>(),
                            GetCaseName(versionIndex, levelIndex, modeIndex));
                    }
                    else
                    {
                        QrCapacityResult nextResult =
                            RectangularMicroCapacityCalculator.CalculateCapacity(nextRequest);
                        Assert.Multiple(() =>
                        {
                            Assert.That(nextResult.Fits,
                                Is.False, GetCaseName(versionIndex, levelIndex, modeIndex));
                            Assert.That(nextResult.UnusedBitCount,
                                Is.LessThan(0), GetCaseName(versionIndex, levelIndex, modeIndex));
                        });
                    }
                }
            }
        }
    }

    [Test]
    public void ChargesProfileSpecificControlHeaderBits()
    {
        QrCapacityResult plain = RectangularMicroCapacityCalculator.CalculateCapacity(
            CreateRequest(0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4, 0));
        QrCapacityResult fnc1First = RectangularMicroCapacityCalculator.CalculateCapacity(
            CreateRequest(0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4, 3));
        QrCapacityResult fnc1Second = RectangularMicroCapacityCalculator.CalculateCapacity(
            CreateRequest(0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4, 11));

        Assert.Multiple(() =>
        {
            Assert.That(plain.RequiredBitCount, Is.EqualTo(11));
            Assert.That(fnc1First.RequiredBitCount, Is.EqualTo(14));
            Assert.That(fnc1Second.RequiredBitCount, Is.EqualTo(22));
            Assert.That(fnc1First.UnusedBitCount, Is.EqualTo(plain.UnusedBitCount - 3));
            Assert.That(fnc1Second.UnusedBitCount, Is.EqualTo(plain.UnusedBitCount - 11));
        });
    }

    [Test]
    public void ResolvesEverySizeFromExplicitDimensions()
    {
        for (int versionIndex = 0; versionIndex < 32; versionIndex++)
        {
            QrCapacityRequest request = CreateRequest(
                versionIndex,
                QrErrorCorrectionLevel.Medium,
                QrEncodingMode.Byte,
                1,
                8,
                0);

            Assert.That(
                () => RectangularMicroCapacityCalculator.CalculateCapacity(request),
                Throws.Nothing,
                "R" + ExpectedHeights[versionIndex] + "x" + ExpectedWidths[versionIndex]);
        }
    }

    [Test]
    public void RejectsInvalidProfileSizeEccAndModeCombinations()
    {
        QrCapacityRequest wrongKind = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4, 0);
        wrongKind.SymbolKind = QrSymbolKind.Model2;
        QrCapacityRequest numberedVersion = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4, 0);
        numberedVersion.Version = 1;
        QrCapacityRequest missingDimensions = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4, 0);
        missingDimensions.Width = 0;
        missingDimensions.Height = 0;
        QrCapacityRequest invalidDimensions = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 1, 4, 0);
        invalidDimensions.Width = 44;
        QrCapacityRequest low = CreateRequest(
            0, QrErrorCorrectionLevel.Low, QrEncodingMode.Numeric, 1, 4, 0);
        QrCapacityRequest quartile = CreateRequest(
            0, QrErrorCorrectionLevel.Quartile, QrEncodingMode.Numeric, 1, 4, 0);
        QrCapacityRequest eci = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Eci, 0, 0, 0);
        QrCapacityRequest hanzi = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Hanzi, 1, 13, 0);

        Assert.Multiple(() =>
        {
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(wrongKind),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(numberedVersion),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(missingDimensions),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(invalidDimensions),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(low),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(quartile),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(eci),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(hanzi),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsInvalidCountsAndPayloadLengths()
    {
        QrCapacityRequest negativeCount = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, -1, 0, 0);
        QrCapacityRequest negativePayload = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 0, -1, 0);
        QrCapacityRequest negativeControl = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 0, 0, -1);
        QrCapacityRequest inconsistentPayload = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Numeric, 3, 9, 0);
        QrCapacityRequest countFieldOverflow = CreateRequest(
            0, QrErrorCorrectionLevel.Medium, QrEncodingMode.Byte, 8, 64, 0);

        Assert.Multiple(() =>
        {
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(negativeCount),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(negativePayload),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(negativeControl),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(inconsistentPayload),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => RectangularMicroCapacityCalculator.CalculateCapacity(countFieldOverflow),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static QrCapacityRequest CreateRequest(
        int versionIndex,
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrEncodingMode encodingMode,
        int characterCount,
        int payloadBitCount,
        int controlBitCount)
    {
        return new QrCapacityRequest
        {
            SymbolKind = QrSymbolKind.rMQR,
            Version = 0,
            Width = ExpectedWidths[versionIndex],
            Height = ExpectedHeights[versionIndex],
            ErrorCorrectionLevel = errorCorrectionLevel,
            EncodingMode = encodingMode,
            CharacterCount = characterCount,
            PayloadBitCount = payloadBitCount,
            ControlBitCount = controlBitCount
        };
    }

    private static QrErrorCorrectionLevel GetErrorCorrectionLevel(int levelIndex)
    {
        if (levelIndex == 0)
        {
            return QrErrorCorrectionLevel.Medium;
        }

        return QrErrorCorrectionLevel.High;
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

    private static int CalculateMaximumCharacterCount(
        int availableBits,
        int countWidth,
        int modeIndex)
    {
        int maximumByCount = (1 << countWidth) - 1;
        int maximum = 0;
        for (int characterCount = 0; characterCount <= maximumByCount; characterCount++)
        {
            int requiredBits = 3 + countWidth + GetPayloadBitCount(modeIndex, characterCount);
            if (requiredBits <= availableBits)
            {
                maximum = characterCount;
            }
        }

        return maximum;
    }

    private static int GetPayloadBitCount(int modeIndex, int characterCount)
    {
        if (modeIndex == 0)
        {
            int bits = characterCount / 3 * 10;
            int remainder = characterCount % 3;
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

        if (modeIndex == 1)
        {
            return characterCount / 2 * 11 + characterCount % 2 * 6;
        }

        if (modeIndex == 2)
        {
            return characterCount * 8;
        }

        return characterCount * 13;
    }

    private static string GetCaseName(int versionIndex, int levelIndex, int modeIndex)
    {
        return "version " + versionIndex + ", level " + levelIndex + ", mode " + modeIndex;
    }
}
