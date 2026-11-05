using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MicroTablesTests
{
    private static readonly int[] ExpectedDimensions = new int[] { 11, 13, 15, 17 };
    private static readonly int[] ExpectedTotalEncodingBits = new int[] { 36, 80, 132, 192 };
    private static readonly int[] ExpectedSymbolCharacters = new int[] { 5, 10, 17, 24 };
    private static readonly int[] ExpectedFinalDataWidths = new int[] { 4, 8, 4, 8 };
    private static readonly int[] ExpectedTerminators = new int[] { 3, 5, 7, 9 };
    private static readonly int[] ExpectedModeIndicatorWidths = new int[] { 0, 1, 2, 3 };

    private static readonly int[][] ExpectedCountWidths = new int[][]
    {
        new int[] { 3, 0, 0, 0 },
        new int[] { 4, 3, 0, 0 },
        new int[] { 5, 4, 4, 3 },
        new int[] { 6, 5, 5, 4 }
    };

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

    private static readonly int[][] ExpectedMaximumCharacters = new int[][]
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
    public void VersionGeometryAndHeadersMatchEveryMicroVersion()
    {
        for (int version = 1; version <= 4; version++)
        {
            int dimension = ExpectedDimensions[version - 1];
            Assert.That(MicroVersionTable.GetDimension(version), Is.EqualTo(dimension));
            Assert.That(MicroVersionTable.GetQuietZoneWidth(version), Is.EqualTo(2));
            Assert.That(MicroVersionTable.GetTotalEncodingBitCount(version), Is.EqualTo(ExpectedTotalEncodingBits[version - 1]));
            Assert.That(MicroVersionTable.GetSymbolCharacterCount(version), Is.EqualTo(ExpectedSymbolCharacters[version - 1]));
            Assert.That(MicroVersionTable.GetFinalDataSymbolBitWidth(version), Is.EqualTo(ExpectedFinalDataWidths[version - 1]));
            Assert.That(MicroVersionTable.GetTerminatorBitCount(version), Is.EqualTo(ExpectedTerminators[version - 1]));
            Assert.That(MicroVersionTable.GetModeIndicatorBitWidth(version), Is.EqualTo(ExpectedModeIndicatorWidths[version - 1]));

            int expectedEncodingBits = dimension * dimension - 64 - 2 * (dimension - 8) - 15;
            Assert.That(MicroVersionTable.GetTotalEncodingBitCount(version), Is.EqualTo(expectedEncodingBits));
        }
    }

    [Test]
    public void ModeSupportAndCharacterCountWidthsMatchEveryMicroVersion()
    {
        for (int version = 1; version <= 4; version++)
        {
            for (int mode = 0; mode < 4; mode++)
            {
                int expectedWidth = ExpectedCountWidths[version - 1][mode];
                Assert.That(MicroVersionTable.IsModeSupported(version, mode), Is.EqualTo(expectedWidth != 0));
                if (expectedWidth != 0)
                {
                    Assert.That(MicroVersionTable.GetCharacterCountBitWidth(version, mode), Is.EqualTo(expectedWidth));
                    Assert.That(MicroVersionTable.GetModeIndicator(version, mode), Is.EqualTo(mode));
                }
            }
        }
    }

    [Test]
    public void CapacityAndEccMatchEveryStandardSymbolConfiguration()
    {
        for (int symbolNumber = 0; symbolNumber < SymbolConfigurations.Length; symbolNumber++)
        {
            int[] configuration = SymbolConfigurations[symbolNumber];
            int version = configuration[0];
            int level = configuration[1];
            int dataBits = configuration[2];
            int dataCharacters = configuration[3];
            int eccCodewords = configuration[4];

            Assert.That(MicroVersionTable.IsErrorCorrectionSupported(version, level), Is.True);
            Assert.That(MicroVersionTable.GetSymbolNumber(version, level), Is.EqualTo(symbolNumber));
            Assert.That(MicroVersionTable.GetDataBitCapacity(version, level), Is.EqualTo(dataBits));
            Assert.That(MicroVersionTable.GetDataSymbolCharacterCount(version, level), Is.EqualTo(dataCharacters));
            Assert.That(MicroVersionTable.GetEccCodewordCount(version, level), Is.EqualTo(eccCodewords));
            Assert.That(MicroVersionTable.GetErrorCorrectionBlockCount(version, level), Is.EqualTo(1));

            int reconstructedDataBits = (dataCharacters - 1) * 8 + MicroVersionTable.GetFinalDataSymbolBitWidth(version);
            Assert.That(dataBits, Is.EqualTo(reconstructedDataBits));
            Assert.That(dataCharacters + eccCodewords, Is.EqualTo(MicroVersionTable.GetSymbolCharacterCount(version)));
            Assert.That(dataBits + eccCodewords * 8, Is.EqualTo(MicroVersionTable.GetTotalEncodingBitCount(version)));

            for (int mode = 0; mode < 4; mode++)
            {
                int expected = ExpectedMaximumCharacters[symbolNumber][mode];
                if (expected != 0)
                {
                    Assert.That(MicroVersionTable.GetMaximumCharacterCount(version, level, mode), Is.EqualTo(expected));
                }
            }
        }

        Assert.That(MicroVersionTable.IsErrorCorrectionSupported(1, 0), Is.False);
        Assert.That(MicroVersionTable.IsErrorCorrectionSupported(2, -1), Is.False);
        Assert.That(MicroVersionTable.IsErrorCorrectionSupported(3, 2), Is.False);
    }

    [Test]
    public void FormatCodesMatchIndependentBchCalculation()
    {
        for (int symbolNumber = 0; symbolNumber < SymbolConfigurations.Length; symbolNumber++)
        {
            int version = SymbolConfigurations[symbolNumber][0];
            int level = SymbolConfigurations[symbolNumber][1];
            for (int mask = 0; mask < 4; mask++)
            {
                int data = symbolNumber << 2 | mask;
                int expected = CreateBchCode(data, 10, 0x537) ^ 0x4445;
                Assert.That(MicroFormatCodeTable.GetCode(version, level, mask), Is.EqualTo(expected));
            }
        }

        Assert.That(MicroFormatCodeTable.GetCode(1, -1, 0), Is.EqualTo(0x4445));
        Assert.That(MicroFormatCodeTable.GetCode(4, 2, 3), Is.EqualTo(0x3BBA));
    }

    [Test]
    public void MaskFormulasMatchReferenceGrids()
    {
        string[] expected = new string[]
        {
            "111111000000111111000000111111000000",
            "111000111000000111000111111000111000",
            "111111111000110110101010101101100011",
            "101010000111100011010101111000011100"
        };

        for (int mask = 0; mask < 4; mask++)
        {
            int index = 0;
            for (int row = 0; row < 6; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    Assert.That(MicroMaskTable.IsInverted(mask, row, column), Is.EqualTo(expected[mask][index] == '1'));
                    index++;
                }
            }
        }
    }

    [Test]
    public void PlacementRulesMatchFunctionAndZigzagCoordinates()
    {
        Assert.That(MicroPlacementTable.FinderPatternWidth, Is.EqualTo(7));
        Assert.That(MicroPlacementTable.SeparatorWidth, Is.EqualTo(1));
        Assert.That(MicroPlacementTable.TimingStartIndex, Is.EqualTo(8));
        Assert.That(MicroPlacementTable.FormatAxisIndex, Is.EqualTo(8));
        Assert.That(MicroPlacementTable.QuietZoneWidth, Is.EqualTo(2));

        for (int version = 1; version <= 4; version++)
        {
            Assert.That(MicroPlacementTable.GetFinderCenterRow(version), Is.EqualTo(3));
            Assert.That(MicroPlacementTable.GetFinderCenterColumn(version), Is.EqualTo(3));
            int dimension = MicroVersionTable.GetDimension(version);
            for (int index = 8; index < dimension; index++)
            {
                Assert.That(MicroPlacementTable.GetTimingModuleValue(version, index), Is.EqualTo(index % 2 == 0));
            }

            int pairCount = (dimension - 1) / 2;
            Assert.That(MicroPlacementTable.GetDataColumnPairCount(version), Is.EqualTo(pairCount));
            for (int pair = 0; pair < pairCount; pair++)
            {
                Assert.That(MicroPlacementTable.GetDataColumnPairRight(version, pair), Is.EqualTo(dimension - 1 - pair * 2));
                Assert.That(MicroPlacementTable.IsDataColumnPairUpward(version, pair), Is.EqualTo(pair % 2 == 0));
            }
        }

        for (int bit = 0; bit <= 7; bit++)
        {
            Assert.That(MicroPlacementTable.GetFormatVerticalRow(bit), Is.EqualTo(bit + 1));
            Assert.That(MicroPlacementTable.GetFormatVerticalColumn(bit), Is.EqualTo(8));
        }

        for (int bit = 7; bit <= 14; bit++)
        {
            Assert.That(MicroPlacementTable.GetFormatHorizontalRow(bit), Is.EqualTo(8));
            Assert.That(MicroPlacementTable.GetFormatHorizontalColumn(bit), Is.EqualTo(15 - bit));
        }
    }

    [Test]
    public void InvalidAndUnsupportedInputsAreRejected()
    {
        Assert.That(() => MicroVersionTable.GetDimension(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroVersionTable.GetDimension(5), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroVersionTable.IsModeSupported(1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroVersionTable.IsModeSupported(1, 4), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroVersionTable.GetCharacterCountBitWidth(1, 1), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MicroVersionTable.GetSymbolNumber(1, 0), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MicroVersionTable.GetSymbolNumber(2, -1), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MicroVersionTable.GetSymbolNumber(3, 2), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MicroVersionTable.GetSymbolNumber(4, 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroFormatCodeTable.GetCode(1, -1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroFormatCodeTable.GetCode(1, -1, 4), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroMaskTable.IsInverted(4, 0, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroMaskTable.IsInverted(0, -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroMaskTable.IsInverted(0, 0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroPlacementTable.GetTimingModuleValue(1, 7), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroPlacementTable.GetTimingModuleValue(1, 11), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroPlacementTable.GetFormatVerticalRow(8), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroPlacementTable.GetFormatHorizontalColumn(6), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MicroPlacementTable.GetDataColumnPairRight(1, 5), Throws.TypeOf<ArgumentOutOfRangeException>());
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
