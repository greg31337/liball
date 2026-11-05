using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class Model2TablesTests
{
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

    private static readonly int[][] ExpectedAlignmentPositions = new int[][]
    {
        new int[] { },
        new int[] { 6, 18 }, new int[] { 6, 22 }, new int[] { 6, 26 },
        new int[] { 6, 30 }, new int[] { 6, 34 }, new int[] { 6, 22, 38 },
        new int[] { 6, 24, 42 }, new int[] { 6, 26, 46 }, new int[] { 6, 28, 50 },
        new int[] { 6, 30, 54 }, new int[] { 6, 32, 58 }, new int[] { 6, 34, 62 },
        new int[] { 6, 26, 46, 66 }, new int[] { 6, 26, 48, 70 },
        new int[] { 6, 26, 50, 74 }, new int[] { 6, 30, 54, 78 },
        new int[] { 6, 30, 56, 82 }, new int[] { 6, 30, 58, 86 },
        new int[] { 6, 34, 62, 90 }, new int[] { 6, 28, 50, 72, 94 },
        new int[] { 6, 26, 50, 74, 98 }, new int[] { 6, 30, 54, 78, 102 },
        new int[] { 6, 28, 54, 80, 106 }, new int[] { 6, 32, 58, 84, 110 },
        new int[] { 6, 30, 58, 86, 114 }, new int[] { 6, 34, 62, 90, 118 },
        new int[] { 6, 26, 50, 74, 98, 122 }, new int[] { 6, 30, 54, 78, 102, 126 },
        new int[] { 6, 26, 52, 78, 104, 130 }, new int[] { 6, 30, 56, 82, 108, 134 },
        new int[] { 6, 34, 60, 86, 112, 138 }, new int[] { 6, 30, 58, 86, 114, 142 },
        new int[] { 6, 34, 62, 90, 118, 146 }, new int[] { 6, 30, 54, 78, 102, 126, 150 },
        new int[] { 6, 24, 50, 76, 102, 128, 154 }, new int[] { 6, 28, 54, 80, 106, 132, 158 },
        new int[] { 6, 32, 58, 84, 110, 136, 162 }, new int[] { 6, 26, 54, 82, 110, 138, 166 },
        new int[] { 6, 30, 58, 86, 114, 142, 170 }
    };

    private static readonly int[][] ExpectedDataCodewords = new int[][]
    {
        new int[] { 19, 34, 55, 80, 108, 136, 156, 194, 232, 274, 324, 370, 428, 461, 523, 589, 647, 721, 795, 861, 932, 1006, 1094, 1174, 1276, 1370, 1468, 1531, 1631, 1735, 1843, 1955, 2071, 2191, 2306, 2434, 2566, 2702, 2812, 2956 },
        new int[] { 16, 28, 44, 64, 86, 108, 124, 154, 182, 216, 254, 290, 334, 365, 415, 453, 507, 563, 627, 669, 714, 782, 860, 914, 1000, 1062, 1128, 1193, 1267, 1373, 1455, 1541, 1631, 1725, 1812, 1914, 1992, 2102, 2216, 2334 },
        new int[] { 13, 22, 34, 48, 62, 76, 88, 110, 132, 154, 180, 206, 244, 261, 295, 325, 367, 397, 445, 485, 512, 568, 614, 664, 718, 754, 808, 871, 911, 985, 1033, 1115, 1171, 1231, 1286, 1354, 1426, 1502, 1582, 1666 },
        new int[] { 9, 16, 26, 36, 46, 60, 66, 86, 100, 122, 140, 158, 180, 197, 223, 253, 283, 313, 341, 385, 406, 442, 464, 514, 538, 596, 628, 661, 701, 745, 793, 845, 901, 961, 986, 1054, 1096, 1142, 1222, 1276 }
    };

    private static readonly int[][] ExpectedEccCodewordsPerBlock = new int[][]
    {
        new int[] { 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new int[] { 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
        new int[] { 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new int[] { 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }
    };

    private static readonly int[][] ExpectedBlockCounts = new int[][]
    {
        new int[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
        new int[] { 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
        new int[] { 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
        new int[] { 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 }
    };

    [Test]
    public void VersionGeometryMatchesAllStandardVersions()
    {
        for (int version = 1; version <= 40; version++)
        {
            Assert.That(Model2VersionTable.GetDimension(version), Is.EqualTo(17 + version * 4));
            Assert.That(Model2VersionTable.GetTotalCodewordCount(version), Is.EqualTo(ExpectedTotalCodewords[version - 1]));
            Assert.That(Model2VersionTable.GetRemainderBitCount(version), Is.EqualTo(ExpectedRemainderBits[version - 1]));
            int expectedModules = ExpectedTotalCodewords[version - 1] * 8 + ExpectedRemainderBits[version - 1];
            Assert.That(Model2VersionTable.GetRawDataModuleCount(version), Is.EqualTo(expectedModules));
        }
    }

    [Test]
    public void AlignmentPositionsMatchAllStandardVersionsAndAreCopied()
    {
        for (int version = 1; version <= 40; version++)
        {
            int[] actual = AlignmentPositionTable.GetPositions(version);
            Assert.That(actual, Is.EqualTo(ExpectedAlignmentPositions[version - 1]));
        }

        int[] first = AlignmentPositionTable.GetPositions(40);
        first[0] = 99;
        Assert.That(AlignmentPositionTable.GetPositions(40)[0], Is.EqualTo(6));
    }

    [Test]
    public void EccParametersAndDataCapacitiesMatchEveryVersionAndLevel()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 40; version++)
            {
                Assert.That(EccBlockTable.GetEccCodewordCountPerBlock(version, level), Is.EqualTo(ExpectedEccCodewordsPerBlock[level][version - 1]));
                Assert.That(EccBlockTable.GetBlockCount(version, level), Is.EqualTo(ExpectedBlockCounts[level][version - 1]));
                Assert.That(EccBlockTable.GetDataCodewordCount(version, level), Is.EqualTo(ExpectedDataCodewords[level][version - 1]));
                AssertBlockGroupsReconstructSymbol(version, level);
            }
        }
    }

    [TestCase(0, 1, 10)]
    [TestCase(0, 9, 10)]
    [TestCase(0, 10, 12)]
    [TestCase(0, 26, 12)]
    [TestCase(0, 27, 14)]
    [TestCase(1, 1, 9)]
    [TestCase(1, 10, 11)]
    [TestCase(1, 27, 13)]
    [TestCase(2, 1, 8)]
    [TestCase(2, 10, 16)]
    [TestCase(2, 27, 16)]
    [TestCase(3, 1, 8)]
    [TestCase(3, 10, 10)]
    [TestCase(3, 27, 12)]
    public void CharacterCountWidthsMatchModeAndVersionGroups(int mode, int version, int expected)
    {
        Assert.That(Model2VersionTable.GetCharacterCountBitWidth(mode, version), Is.EqualTo(expected));
    }

    [Test]
    public void FormatCodesMatchIndependentBchCalculation()
    {
        int[] levelBits = new int[] { 1, 0, 3, 2 };
        for (int level = 0; level < 4; level++)
        {
            for (int mask = 0; mask < 8; mask++)
            {
                int data = levelBits[level] << 3 | mask;
                int expected = CreateBchCode(data, 10, 0x537) ^ 0x5412;
                Assert.That(FormatCodeTable.GetCode(level, mask), Is.EqualTo(expected));
            }
        }
    }

    [Test]
    public void VersionCodesMatchIndependentBchCalculation()
    {
        for (int version = 7; version <= 40; version++)
        {
            int expected = CreateBchCode(version, 12, 0x1F25);
            Assert.That(VersionCodeTable.GetCode(version), Is.EqualTo(expected));
        }

        Assert.That(VersionCodeTable.GetCode(7), Is.EqualTo(0x07C94));
        Assert.That(VersionCodeTable.GetCode(40), Is.EqualTo(0x28C69));
    }

    [Test]
    public void MaskFormulasMatchReferenceGrids()
    {
        string[] expected = new string[]
        {
            "101010010101101010010101101010010101",
            "111111000000111111000000111111000000",
            "100100100100100100100100100100100100",
            "100100001001010010100100001001010010",
            "111000111000000111000111111000111000",
            "111111100000100100101010100100100000",
            "111111111000110110101010101101100011",
            "101010000111100011010101111000011100"
        };

        for (int mask = 0; mask < 8; mask++)
        {
            int index = 0;
            for (int row = 0; row < 6; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    bool expectedValue = expected[mask][index] == '1';
                    Assert.That(Model2MaskTable.IsInverted(mask, row, column), Is.EqualTo(expectedValue));
                    index++;
                }
            }
        }
    }

    [Test]
    public void PlacementRulesMatchFormatVersionAndZigzagCoordinates()
    {
        Assert.That(Model2PlacementTable.FinderPatternWidth, Is.EqualTo(7));
        Assert.That(Model2PlacementTable.SeparatorWidth, Is.EqualTo(1));
        Assert.That(Model2PlacementTable.AlignmentPatternWidth, Is.EqualTo(5));
        Assert.That(Model2PlacementTable.TimingPatternIndex, Is.EqualTo(6));
        Assert.That(Model2PlacementTable.QuietZoneWidth, Is.EqualTo(4));
        Assert.That(Model2PlacementTable.GetFinderCenterRow(1, 0), Is.EqualTo(3));
        Assert.That(Model2PlacementTable.GetFinderCenterColumn(1, 0), Is.EqualTo(3));
        Assert.That(Model2PlacementTable.GetFinderCenterRow(40, 1), Is.EqualTo(3));
        Assert.That(Model2PlacementTable.GetFinderCenterColumn(40, 1), Is.EqualTo(173));
        Assert.That(Model2PlacementTable.GetFinderCenterRow(40, 2), Is.EqualTo(173));
        Assert.That(Model2PlacementTable.GetFinderCenterColumn(40, 2), Is.EqualTo(3));

        int[] expectedPrimaryRows = new int[] { 0, 1, 2, 3, 4, 5, 7, 8, 8, 8, 8, 8, 8, 8, 8 };
        int[] expectedPrimaryColumns = new int[] { 8, 8, 8, 8, 8, 8, 8, 8, 7, 5, 4, 3, 2, 1, 0 };
        int[] expectedSecondaryRows = new int[] { 8, 8, 8, 8, 8, 8, 8, 8, 14, 15, 16, 17, 18, 19, 20 };
        int[] expectedSecondaryColumns = new int[] { 20, 19, 18, 17, 16, 15, 14, 13, 8, 8, 8, 8, 8, 8, 8 };

        for (int bit = 0; bit < 15; bit++)
        {
            Assert.That(Model2PlacementTable.GetFormatPrimaryRow(bit), Is.EqualTo(expectedPrimaryRows[bit]));
            Assert.That(Model2PlacementTable.GetFormatPrimaryColumn(bit), Is.EqualTo(expectedPrimaryColumns[bit]));
            Assert.That(Model2PlacementTable.GetFormatSecondaryRow(1, bit), Is.EqualTo(expectedSecondaryRows[bit]));
            Assert.That(Model2PlacementTable.GetFormatSecondaryColumn(1, bit), Is.EqualTo(expectedSecondaryColumns[bit]));
        }

        Assert.That(Model2PlacementTable.GetDarkModuleRow(1), Is.EqualTo(13));
        Assert.That(Model2PlacementTable.GetDarkModuleColumn(40), Is.EqualTo(8));
        Assert.That(Model2PlacementTable.GetVersionPrimaryRow(7, 0), Is.EqualTo(0));
        Assert.That(Model2PlacementTable.GetVersionPrimaryColumn(7, 0), Is.EqualTo(34));
        Assert.That(Model2PlacementTable.GetVersionSecondaryRow(7, 17), Is.EqualTo(36));
        Assert.That(Model2PlacementTable.GetVersionSecondaryColumn(7, 17), Is.EqualTo(5));

        int[] expectedPairRights = new int[] { 20, 18, 16, 14, 12, 10, 8, 5, 3, 1 };
        for (int pair = 0; pair < expectedPairRights.Length; pair++)
        {
            Assert.That(Model2PlacementTable.GetDataColumnPairRight(1, pair), Is.EqualTo(expectedPairRights[pair]));
            Assert.That(Model2PlacementTable.IsDataColumnPairUpward(1, pair), Is.EqualTo(pair % 2 == 0));
        }

        Assert.That(Model2PlacementTable.GetDataColumnPairCount(40), Is.EqualTo(88));
        Assert.That(Model2PlacementTable.GetDataColumnPairRight(40, 87), Is.EqualTo(1));
    }

    [Test]
    public void InvalidTableInputsAreRejected()
    {
        Assert.That(() => Model2VersionTable.GetDimension(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2VersionTable.GetDimension(41), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2VersionTable.GetCharacterCountBitWidth(-1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2VersionTable.GetCharacterCountBitWidth(4, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => AlignmentPositionTable.GetPositions(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EccBlockTable.GetBlockCount(1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EccBlockTable.GetBlockCount(1, 4), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => FormatCodeTable.GetCode(0, 8), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => VersionCodeTable.GetCode(6), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => VersionCodeTable.GetCode(41), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2MaskTable.IsInverted(-1, 0, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2MaskTable.IsInverted(0, -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2MaskTable.IsInverted(0, 0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2PlacementTable.GetFinderCenterRow(1, 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2PlacementTable.GetDataColumnPairRight(1, 10), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2PlacementTable.GetFormatPrimaryRow(15), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2PlacementTable.GetVersionPrimaryRow(6, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Model2PlacementTable.GetVersionPrimaryRow(7, 18), Throws.TypeOf<ArgumentOutOfRangeException>());
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

    private static void AssertBlockGroupsReconstructSymbol(int version, int level)
    {
        int blocks = EccBlockTable.GetBlockCount(version, level);
        int shortBlocks = EccBlockTable.GetShortBlockCount(version, level);
        int longBlocks = EccBlockTable.GetLongBlockCount(version, level);
        int ecc = EccBlockTable.GetEccCodewordCountPerBlock(version, level);
        int shortData = EccBlockTable.GetShortDataCodewordCount(version, level);
        int longData = EccBlockTable.GetLongDataCodewordCount(version, level);

        Assert.That(shortBlocks + longBlocks, Is.EqualTo(blocks));
        int reconstructedData = shortBlocks * shortData + longBlocks * longData;
        Assert.That(reconstructedData, Is.EqualTo(EccBlockTable.GetDataCodewordCount(version, level)));
        int reconstructedTotal = reconstructedData + blocks * ecc;
        Assert.That(reconstructedTotal, Is.EqualTo(Model2VersionTable.GetTotalCodewordCount(version)));
    }
}
