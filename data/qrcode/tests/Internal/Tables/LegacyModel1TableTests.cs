using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class LegacyModel1TableTests
{
    private static readonly int[] ExpectedTotalCodewords = new int[]
    {
        26, 46, 72, 100, 134, 170, 212,
        256, 306, 358, 416, 476, 542, 610
    };

    private static readonly int[] ExpectedFunctionModules = new int[]
    {
        206, 230, 238, 262, 270, 294, 302,
        326, 334, 358, 366, 390, 398, 422
    };

    private static readonly int[][] ExpectedDataCodewords = new int[][]
    {
        new int[] { 19, 36, 57, 80, 108, 136, 170, 208, 246, 290, 336, 384, 432, 489 },
        new int[] { 16, 30, 44, 60, 82, 106, 132, 160, 186, 222, 256, 292, 332, 368 },
        new int[] { 13, 24, 36, 50, 68, 86, 108, 128, 156, 183, 208, 244, 276, 310 },
        new int[] { 9, 16, 24, 34, 46, 58, 72, 87, 102, 124, 145, 165, 192, 210 }
    };

    private static readonly int[][] ExpectedBlockCounts = new int[][]
    {
        new int[] { 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3 },
        new int[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 4, 4, 4, 4 },
        new int[] { 1, 1, 1, 1, 1, 2, 2, 2, 3, 3, 4, 4, 4, 5 },
        new int[] { 1, 1, 1, 1, 2, 2, 3, 3, 3, 4, 5, 5, 6, 6 }
    };

    private static readonly int[][] ExpectedRemainderCodewords = new int[][]
    {
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1 },
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2 },
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 2, 0 },
        new int[] { 0, 0, 0, 0, 0, 0, 2, 1, 0, 2, 1, 1, 2, 4 }
    };

    [Test]
    public void GeometryMatchesEveryLegacyVersion()
    {
        for (int version = 1; version <= 14; version++)
        {
            int dimension = 17 + version * 4;
            int encodingModules = ExpectedTotalCodewords[version - 1] * 8 - 4;
            Assert.That(LegacyModel1Table.GetDimension(version), Is.EqualTo(dimension));
            Assert.That(LegacyModel1Table.GetTotalCodewordCount(version), Is.EqualTo(ExpectedTotalCodewords[version - 1]));
            Assert.That(LegacyModel1Table.GetRawEncodingModuleCount(version), Is.EqualTo(encodingModules));
            Assert.That(LegacyModel1Table.GetFunctionPatternModuleCount(version), Is.EqualTo(ExpectedFunctionModules[version - 1]));
            Assert.That(
                LegacyModel1Table.GetFunctionPatternModuleCount(version)
                    + LegacyModel1Table.FormatInformationModuleCount
                    + LegacyModel1Table.GetRawEncodingModuleCount(version),
                Is.EqualTo(dimension * dimension));
        }
    }

    [Test]
    public void ErrorCorrectionConfigurationsReconstructEverySymbol()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 14; version++)
            {
                int data = ExpectedDataCodewords[level][version - 1];
                int blocks = ExpectedBlockCounts[level][version - 1];
                int remainder = ExpectedRemainderCodewords[level][version - 1];
                int ecc = ExpectedTotalCodewords[version - 1] - data - remainder;
                Assert.That(LegacyModel1Table.GetDataCodewordCount(version, level), Is.EqualTo(data));
                Assert.That(LegacyModel1Table.GetDataBitCount(version, level), Is.EqualTo(data * 8 - 4));
                Assert.That(LegacyModel1Table.GetErrorCorrectionBlockCount(version, level), Is.EqualTo(blocks));
                Assert.That(LegacyModel1Table.GetRemainderCodewordCount(version, level), Is.EqualTo(remainder));
                Assert.That(LegacyModel1Table.GetErrorCorrectionCodewordCount(version, level), Is.EqualTo(ecc));
                Assert.That(LegacyModel1Table.GetDataCodewordCountPerBlock(version, level) * blocks, Is.EqualTo(data));
                Assert.That(LegacyModel1Table.GetErrorCorrectionCodewordCountPerBlock(version, level) * blocks, Is.EqualTo(ecc));

                for (int block = 0; block < blocks; block++)
                {
                    Assert.That(
                        LegacyModel1Table.GetDataBlockStart(version, level, block),
                        Is.EqualTo(block * data / blocks));
                    Assert.That(
                        LegacyModel1Table.GetErrorCorrectionBlockStart(version, level, block),
                        Is.EqualTo(data + block * ecc / blocks));
                }
            }
        }
    }

    [Test]
    public void FormatCodesUseTheLegacyRecognitionMask()
    {
        int[] levelBits = new int[] { 1, 0, 3, 2 };
        for (int level = 0; level < 4; level++)
        {
            for (int mask = 0; mask < 8; mask++)
            {
                int data = levelBits[level] << 3 | mask;
                int expected = CreateBchCode(data, 10, 0x537) ^ 0x2825;
                int actual = LegacyModel1Table.GetFormatCode(level, mask);
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(LegacyModel1Table.IsExactFormatCode(actual), Is.True);
                Assert.That(actual ^ FormatCodeTable.GetCode(level, mask), Is.EqualTo(0x7C37));
            }
        }

        Assert.That(LegacyModel1Table.IsExactFormatCode(-1), Is.False);
        Assert.That(LegacyModel1Table.IsExactFormatCode(0x8000), Is.False);
        Assert.That(LegacyModel1Table.IsExactFormatCode(0x0001), Is.False);
    }

    [Test]
    public void ExtensionPatternsMatchOddAndEvenVersionPlacement()
    {
        Assert.That(GetExtensionStarts(13), Is.EqualTo(new int[] { 17, 25, 33, 41, 49, 57 }));
        Assert.That(GetExtensionStarts(14), Is.EqualTo(new int[] { 13, 21, 29, 37, 45, 53, 61 }));

        for (int version = 1; version <= 14; version++)
        {
            int dimension = LegacyModel1Table.GetDimension(version);
            int extensionModules = 0;
            int standardDarkModules = 0;
            int earlyAdditionalDarkModules = 0;
            for (int row = 0; row < dimension; row++)
            {
                for (int column = 0; column < dimension; column++)
                {
                    if (LegacyModel1Table.IsExtensionModule(version, row, column))
                    {
                        extensionModules++;
                    }

                    if (LegacyModel1Table.IsStandardExtensionDarkModule(version, row, column))
                    {
                        standardDarkModules++;
                    }

                    if (LegacyModel1Table.IsEarlyExtensionAdditionalDarkModule(version, row, column))
                    {
                        earlyAdditionalDarkModules++;
                    }
                }
            }

            int blocksPerSide = version / 2;
            Assert.That(extensionModules, Is.EqualTo(4 + blocksPerSide * 16));
            Assert.That(standardDarkModules, Is.EqualTo(1 + blocksPerSide * 8));
            Assert.That(earlyAdditionalDarkModules, Is.EqualTo(blocksPerSide * 4));
            Assert.That(LegacyModel1Table.IsExtensionModule(version, dimension - 1, dimension - 1), Is.True);
            Assert.That(LegacyModel1Table.IsStandardExtensionDarkModule(version, dimension - 1, dimension - 1), Is.True);
        }
    }

    [Test]
    public void FormatAndFixedModuleCoordinatesMatchLegacyLayout()
    {
        int[] expectedPrimaryRows = new int[] { 0, 1, 2, 3, 4, 5, 7, 8, 8, 8, 8, 8, 8, 8, 8 };
        int[] expectedPrimaryColumns = new int[] { 8, 8, 8, 8, 8, 8, 8, 8, 7, 5, 4, 3, 2, 1, 0 };
        int[] expectedSecondaryRows = new int[] { 8, 8, 8, 8, 8, 8, 8, 8, 66, 67, 68, 69, 70, 71, 72 };
        int[] expectedSecondaryColumns = new int[] { 72, 71, 70, 69, 68, 67, 66, 65, 8, 8, 8, 8, 8, 8, 8 };

        for (int bit = 0; bit < 15; bit++)
        {
            Assert.That(LegacyModel1Table.GetFormatPrimaryRow(bit), Is.EqualTo(expectedPrimaryRows[bit]));
            Assert.That(LegacyModel1Table.GetFormatPrimaryColumn(bit), Is.EqualTo(expectedPrimaryColumns[bit]));
            Assert.That(LegacyModel1Table.GetFormatSecondaryRow(14, bit), Is.EqualTo(expectedSecondaryRows[bit]));
            Assert.That(LegacyModel1Table.GetFormatSecondaryColumn(14, bit), Is.EqualTo(expectedSecondaryColumns[bit]));
        }

        Assert.That(LegacyModel1Table.GetDarkModuleRow(1), Is.EqualTo(13));
        Assert.That(LegacyModel1Table.GetDarkModuleRow(14), Is.EqualTo(65));
        Assert.That(LegacyModel1Table.GetDarkModuleColumn(14), Is.EqualTo(8));
    }

    [Test]
    public void SymbolCharacterPlacementDescribesLegacyBlockOrdering()
    {
        Assert.That(LegacyModel1Table.IsVerticalSymbolCharacterColumn(0, 10), Is.True);
        Assert.That(LegacyModel1Table.IsVerticalSymbolCharacterColumn(1, 10), Is.True);
        Assert.That(LegacyModel1Table.IsVerticalSymbolCharacterColumn(2, 10), Is.False);
        Assert.That(LegacyModel1Table.IsVerticalSymbolCharacterColumn(5, 10), Is.False);
        Assert.That(LegacyModel1Table.IsVerticalSymbolCharacterColumn(6, 10), Is.True);
        Assert.That(LegacyModel1Table.IsVerticalSymbolCharacterColumn(9, 10), Is.True);

        int[,] firstExpected = new int[,] { { 0, 1 }, { 2, 3 } };
        for (int bit = 0; bit < 4; bit++)
        {
            int row = LegacyModel1Table.GetSymbolCharacterBitRow(bit, false, true);
            int column = LegacyModel1Table.GetSymbolCharacterBitColumn(bit, false, true);
            Assert.That(firstExpected[row, column], Is.EqualTo(bit));
        }

        for (int bit = 0; bit < 8; bit++)
        {
            int verticalRow = LegacyModel1Table.GetSymbolCharacterBitRow(bit, true, false);
            int verticalColumn = LegacyModel1Table.GetSymbolCharacterBitColumn(bit, true, false);
            int horizontalRow = LegacyModel1Table.GetSymbolCharacterBitRow(bit, false, false);
            int horizontalColumn = LegacyModel1Table.GetSymbolCharacterBitColumn(bit, false, false);
            Assert.That(verticalRow * 2 + verticalColumn, Is.EqualTo(bit));
            Assert.That(horizontalRow * 4 + horizontalColumn, Is.EqualTo(bit));
        }
    }

    [Test]
    public void DecodeProfileCapturesLegacyRestrictionsAndCountWidths()
    {
        Assert.That(LegacyModel1Table.HasAlignmentPatterns, Is.False);
        Assert.That(LegacyModel1Table.HasVersionInformation, Is.False);
        Assert.That(LegacyModel1Table.SupportsEci, Is.False);
        Assert.That(LegacyModel1Table.SupportsMirrorImaging, Is.False);
        Assert.That(LegacyModel1Table.SupportsReflectanceReversal, Is.False);
        Assert.That(LegacyModel1Table.InterleavesErrorCorrectionBlocks, Is.False);
        Assert.That(LegacyModel1Table.FirstSymbolCharacterBitCount, Is.EqualTo(4));
        Assert.That(LegacyModel1Table.SymbolCharacterBitCount, Is.EqualTo(8));
        Assert.That(LegacyModel1Table.QuietZoneWidth, Is.EqualTo(4));

        int[,] expectedWidths = new int[,]
        {
            { 10, 12 },
            { 9, 11 },
            { 8, 16 },
            { 8, 10 }
        };
        for (int mode = 0; mode < 4; mode++)
        {
            Assert.That(LegacyModel1Table.GetCharacterCountBitWidth(1, mode), Is.EqualTo(expectedWidths[mode, 0]));
            Assert.That(LegacyModel1Table.GetCharacterCountBitWidth(9, mode), Is.EqualTo(expectedWidths[mode, 0]));
            Assert.That(LegacyModel1Table.GetCharacterCountBitWidth(10, mode), Is.EqualTo(expectedWidths[mode, 1]));
            Assert.That(LegacyModel1Table.GetCharacterCountBitWidth(14, mode), Is.EqualTo(expectedWidths[mode, 1]));
        }
    }

    [Test]
    public void InvalidInputsAreRejected()
    {
        Assert.That(() => LegacyModel1Table.GetDimension(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetDimension(15), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetDataCodewordCount(1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetDataCodewordCount(1, 4), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetCharacterCountBitWidth(1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetCharacterCountBitWidth(1, 4), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetFormatCode(0, 8), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetExtensionBlockStart(1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.IsExtensionModule(1, -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.IsExtensionModule(1, 0, 21), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.IsVerticalSymbolCharacterColumn(0, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.IsVerticalSymbolCharacterColumn(10, 10), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetSymbolCharacterBitRow(4, false, true), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetSymbolCharacterBitColumn(8, false, false), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => LegacyModel1Table.GetDataBlockStart(1, 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static int[] GetExtensionStarts(int version)
    {
        int count = LegacyModel1Table.GetExtensionBlockCountPerSide(version);
        int[] starts = new int[count];
        for (int index = 0; index < count; index++)
        {
            starts[index] = LegacyModel1Table.GetExtensionBlockStart(version, index);
        }

        return starts;
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
