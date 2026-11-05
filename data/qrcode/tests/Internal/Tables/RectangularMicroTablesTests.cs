using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class RectangularMicroTablesTests
{
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

    private static readonly int[] ExpectedFunctionModules = new int[]
    {
        161, 206, 242, 299, 392, 181, 228, 264, 323, 418,
        139, 188, 237, 273, 334, 431, 143, 194, 245, 281, 344, 443,
        200, 253, 289, 354, 455, 206, 261, 297, 364, 467
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

    private static readonly int[][] ExpectedBlockCounts = new int[][]
    {
        new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 2, 2, 3, 1, 1, 2, 2, 3, 1, 2, 2, 3, 4 },
        new int[] { 1, 1, 1, 1, 2, 1, 1, 2, 2, 3, 1, 1, 2, 2, 2, 3, 1, 1, 2, 2, 3, 4, 2, 2, 3, 4, 5, 2, 2, 3, 4, 6 }
    };

    private static readonly int[][] ExpectedAlignmentColumns = new int[][]
    {
        new int[] { },
        new int[] { 21 },
        new int[] { 19, 39 },
        new int[] { 25, 51 },
        new int[] { 23, 49, 75 },
        new int[] { 27, 55, 83, 111 }
    };

    [Test]
    public void GeometryAndCodewordCapacityMatchEveryVersion()
    {
        for (int version = 0; version < 32; version++)
        {
            int height = ExpectedHeights[version];
            int width = ExpectedWidths[version];
            Assert.That(RectangularMicroVersionTable.GetHeight(version), Is.EqualTo(height));
            Assert.That(RectangularMicroVersionTable.GetWidth(version), Is.EqualTo(width));
            Assert.That(RectangularMicroVersionTable.GetVersionName(version), Is.EqualTo($"R{height}x{width}"));
            Assert.That(RectangularMicroVersionTable.GetVersionIndex(height, width), Is.EqualTo(version));
            Assert.That(RectangularMicroVersionTable.GetTotalCodewordCount(version), Is.EqualTo(ExpectedTotalCodewords[version]));
            Assert.That(RectangularMicroVersionTable.GetRemainderBitCount(version), Is.EqualTo(ExpectedRemainderBits[version]));
            Assert.That(RectangularMicroVersionTable.GetFunctionPatternModuleCount(version), Is.EqualTo(ExpectedFunctionModules[version]));

            int reconstructedArea = ExpectedFunctionModules[version]
                + RectangularMicroVersionTable.FormatInformationModuleCount
                + ExpectedTotalCodewords[version] * 8
                + ExpectedRemainderBits[version];
            Assert.That(reconstructedArea, Is.EqualTo(height * width));
        }

        Assert.That(RectangularMicroVersionTable.QuietZoneWidth, Is.EqualTo(2));
    }

    [Test]
    public void ModeIndicatorsAndCountWidthsMatchEveryVersion()
    {
        Assert.That(RectangularMicroVersionTable.ModeIndicatorBitWidth, Is.EqualTo(3));
        Assert.That(RectangularMicroVersionTable.TerminatorBitCount, Is.EqualTo(3));
        Assert.That(RectangularMicroVersionTable.TerminatorModeIndicator, Is.EqualTo(0));
        Assert.That(RectangularMicroVersionTable.Fnc1FirstModeIndicator, Is.EqualTo(5));
        Assert.That(RectangularMicroVersionTable.Fnc1SecondModeIndicator, Is.EqualTo(6));
        Assert.That(RectangularMicroVersionTable.EciModeIndicator, Is.EqualTo(7));

        for (int mode = 0; mode < 4; mode++)
        {
            Assert.That(RectangularMicroVersionTable.GetModeIndicator(mode), Is.EqualTo(mode + 1));
            for (int version = 0; version < 32; version++)
            {
                Assert.That(RectangularMicroVersionTable.GetCharacterCountBitWidth(version, mode), Is.EqualTo(ExpectedCountWidths[mode][version]));
            }
        }
    }

    [Test]
    public void EccParametersAndDataCapacitiesMatchEveryVersionAndLevel()
    {
        for (int level = 0; level < 2; level++)
        {
            for (int version = 0; version < 32; version++)
            {
                int data = ExpectedDataCodewords[level][version];
                int blocks = ExpectedBlockCounts[level][version];
                int ecc = ExpectedTotalCodewords[version] - data;
                Assert.That(RectangularMicroVersionTable.GetDataCodewordCount(version, level), Is.EqualTo(data));
                Assert.That(RectangularMicroVersionTable.GetDataBitCapacity(version, level), Is.EqualTo(data * 8));
                Assert.That(RectangularMicroVersionTable.GetEccCodewordCount(version, level), Is.EqualTo(ecc));
                Assert.That(RectangularMicroVersionTable.GetBlockCount(version, level), Is.EqualTo(blocks));
                Assert.That(RectangularMicroVersionTable.GetEccCodewordCountPerBlock(version, level), Is.EqualTo(ecc / blocks));
                Assert.That(ecc % blocks, Is.EqualTo(0));
                AssertBlockGroupsReconstructData(version, level);
            }
        }
    }

    [Test]
    public void MaximumInputCapacitiesMatchIndependentBitCalculations()
    {
        for (int version = 0; version < 32; version++)
        {
            for (int level = 0; level < 2; level++)
            {
                int availableBits = ExpectedDataCodewords[level][version] * 8;
                for (int mode = 0; mode < 4; mode++)
                {
                    int countWidth = ExpectedCountWidths[mode][version];
                    int expected = CalculateMaximumCharacterCount(availableBits, countWidth, mode);
                    Assert.That(RectangularMicroVersionTable.GetMaximumCharacterCount(version, level, mode), Is.EqualTo(expected));
                }
            }
        }

        Assert.That(RectangularMicroVersionTable.GetMaximumCharacterCount(0, 0, 0), Is.EqualTo(12));
        Assert.That(RectangularMicroVersionTable.GetMaximumCharacterCount(31, 0, 0), Is.EqualTo(361));
        Assert.That(RectangularMicroVersionTable.GetMaximumCharacterCount(31, 0, 3), Is.EqualTo(92));
        Assert.That(RectangularMicroVersionTable.GetMaximumCharacterCount(31, 1, 2), Is.EqualTo(74));
    }

    [Test]
    public void FormatCodesMatchIndependentBchCalculation()
    {
        for (int level = 0; level < 2; level++)
        {
            for (int version = 0; version < 32; version++)
            {
                int data = level << 5 | version;
                int unmasked = CreateBchCode(data, 12, 0x1F25);
                Assert.That(RectangularMicroFormatCodeTable.GetFormatData(version, level), Is.EqualTo(data));
                Assert.That(RectangularMicroFormatCodeTable.GetFinderSideCode(version, level), Is.EqualTo(unmasked ^ 0x1FAB2));
                Assert.That(RectangularMicroFormatCodeTable.GetSubfinderSideCode(version, level), Is.EqualTo(unmasked ^ 0x20A7B));
            }
        }

        Assert.That(RectangularMicroFormatCodeTable.GetFinderSideCode(0, 0), Is.EqualTo(0x1FAB2));
        Assert.That(RectangularMicroFormatCodeTable.GetSubfinderSideCode(31, 1), Is.EqualTo(0x1F1FE));
    }

    [Test]
    public void FixedMaskMatchesReferenceGrid()
    {
        string expected = "111000111000000111000111111000111000";
        int index = 0;
        for (int row = 0; row < 6; row++)
        {
            for (int column = 0; column < 6; column++)
            {
                Assert.That(RectangularMicroMaskTable.IsInverted(row, column), Is.EqualTo(expected[index] == '1'));
                index++;
            }
        }
    }

    [Test]
    public void AlignmentColumnsMatchEveryWidthAndAreCopied()
    {
        int[] widths = new int[] { 27, 43, 59, 77, 99, 139 };
        for (int index = 0; index < widths.Length; index++)
        {
            Assert.That(RectangularMicroAlignmentTable.GetCenterColumns(widths[index]), Is.EqualTo(ExpectedAlignmentColumns[index]));
        }

        int[] first = RectangularMicroAlignmentTable.GetCenterColumns(139);
        first[0] = 99;
        Assert.That(RectangularMicroAlignmentTable.GetCenterColumns(139)[0], Is.EqualTo(27));
    }

    [Test]
    public void PlacementRulesMatchFunctionAndDataCoordinates()
    {
        Assert.That(RectangularMicroPlacementTable.FinderPatternWidth, Is.EqualTo(7));
        Assert.That(RectangularMicroPlacementTable.SeparatorWidth, Is.EqualTo(1));
        Assert.That(RectangularMicroPlacementTable.AlignmentPatternWidth, Is.EqualTo(3));
        Assert.That(RectangularMicroPlacementTable.SubfinderPatternWidth, Is.EqualTo(5));
        Assert.That(RectangularMicroPlacementTable.CornerPatternWidth, Is.EqualTo(3));
        Assert.That(RectangularMicroPlacementTable.QuietZoneWidth, Is.EqualTo(2));

        for (int version = 0; version < 32; version++)
        {
            int height = ExpectedHeights[version];
            int width = ExpectedWidths[version];
            Assert.That(RectangularMicroPlacementTable.GetFinderCenterRow(version), Is.EqualTo(3));
            Assert.That(RectangularMicroPlacementTable.GetFinderCenterColumn(version), Is.EqualTo(3));
            Assert.That(RectangularMicroPlacementTable.GetSubfinderCenterRow(version), Is.EqualTo(height - 3));
            Assert.That(RectangularMicroPlacementTable.GetSubfinderCenterColumn(version), Is.EqualTo(width - 3));
            Assert.That(RectangularMicroPlacementTable.HasBottomSeparator(version), Is.EqualTo(height > 7));
            Assert.That(RectangularMicroPlacementTable.HasLowerLeftCornerPattern(version), Is.EqualTo(height > 7));
            Assert.That(RectangularMicroPlacementTable.HasLeftVerticalTimingPattern(version), Is.EqualTo(height >= 13));
            Assert.That(RectangularMicroPlacementTable.HasRightVerticalTimingPattern(version), Is.EqualTo(height >= 11));

            for (int column = 0; column < width; column++)
            {
                Assert.That(RectangularMicroPlacementTable.GetHorizontalTimingModuleValue(version, column), Is.EqualTo(column % 2 == 0));
            }

            for (int row = 0; row < height; row++)
            {
                Assert.That(RectangularMicroPlacementTable.GetVerticalTimingModuleValue(version, row), Is.EqualTo(row % 2 == 0));
            }

            int pairCount = (width - 1) / 2;
            Assert.That(RectangularMicroPlacementTable.GetDataColumnPairCount(version), Is.EqualTo(pairCount));
            for (int pair = 0; pair < pairCount; pair++)
            {
                Assert.That(RectangularMicroPlacementTable.GetDataColumnPairRight(version, pair), Is.EqualTo(width - 2 - pair * 2));
                Assert.That(RectangularMicroPlacementTable.IsDataColumnPairUpward(version, pair), Is.EqualTo(pair % 2 == 0));
            }

            for (int bit = 0; bit < 18; bit++)
            {
                int expectedLeftRow = bit < 15 ? bit % 5 + 1 : bit - 14;
                int expectedLeftColumn = bit < 15 ? bit / 5 + 8 : 11;
                int expectedRightRow = bit < 15 ? height - 6 + bit % 5 : height - 6;
                int expectedRightColumn = bit < 15 ? width - 8 + bit / 5 : width + bit - 20;
                Assert.That(RectangularMicroPlacementTable.GetFinderSideFormatRow(bit), Is.EqualTo(expectedLeftRow));
                Assert.That(RectangularMicroPlacementTable.GetFinderSideFormatColumn(bit), Is.EqualTo(expectedLeftColumn));
                Assert.That(RectangularMicroPlacementTable.GetSubfinderSideFormatRow(version, bit), Is.EqualTo(expectedRightRow));
                Assert.That(RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(version, bit), Is.EqualTo(expectedRightColumn));
            }
        }
    }

    [Test]
    public void InvalidInputsAreRejected()
    {
        Assert.That(() => RectangularMicroVersionTable.GetHeight(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroVersionTable.GetHeight(32), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroVersionTable.GetVersionIndex(7, 27), Throws.TypeOf<ArgumentException>());
        Assert.That(() => RectangularMicroVersionTable.GetModeIndicator(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroVersionTable.GetModeIndicator(4), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroVersionTable.GetDataCodewordCount(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroVersionTable.GetDataCodewordCount(0, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroFormatCodeTable.GetFinderSideCode(0, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroMaskTable.IsInverted(-1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroMaskTable.IsInverted(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroAlignmentTable.GetCenterColumns(28), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroPlacementTable.GetHorizontalTimingModuleValue(0, 43), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroPlacementTable.GetVerticalTimingModuleValue(0, 7), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroPlacementTable.GetFinderSideFormatRow(18), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RectangularMicroPlacementTable.GetDataColumnPairRight(0, 21), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static void AssertBlockGroupsReconstructData(int version, int level)
    {
        int shortBlocks = RectangularMicroVersionTable.GetShortBlockCount(version, level);
        int longBlocks = RectangularMicroVersionTable.GetLongBlockCount(version, level);
        int shortData = RectangularMicroVersionTable.GetShortDataCodewordCount(version, level);
        int longData = RectangularMicroVersionTable.GetLongDataCodewordCount(version, level);
        Assert.That(shortBlocks + longBlocks, Is.EqualTo(ExpectedBlockCounts[level][version]));
        Assert.That(shortBlocks * shortData + longBlocks * longData, Is.EqualTo(ExpectedDataCodewords[level][version]));
    }

    private static int CalculateMaximumCharacterCount(int availableBits, int countWidth, int mode)
    {
        int maximumCount = (1 << countWidth) - 1;
        int count = 0;
        while (count < maximumCount && 3 + countWidth + GetPayloadBitCount(mode, count + 1) <= availableBits)
        {
            count++;
        }

        return count;
    }

    private static int GetPayloadBitCount(int mode, int count)
    {
        if (mode == 0)
        {
            int finalBits = 0;
            if (count % 3 == 1)
            {
                finalBits = 4;
            }
            else if (count % 3 == 2)
            {
                finalBits = 7;
            }

            return count / 3 * 10 + finalBits;
        }

        if (mode == 1)
        {
            return count / 2 * 11 + count % 2 * 6;
        }

        if (mode == 2)
        {
            return count * 8;
        }

        return count * 13;
    }

    private static int CreateBchCode(int data, int parityBits, int generator)
    {
        int remainder = data;
        for (int index = 0; index < parityBits; index++)
        {
            int topBit = remainder >> (parityBits - 1);
            remainder <<= 1;
            if (topBit != 0)
            {
                remainder ^= generator;
            }
        }

        return data << parityBits | remainder;
    }
}
