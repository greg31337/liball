namespace barcode.Tests;

public class DataMatrixSymbolInfoTests
{
    static readonly int[][] ExpectedSymbols = new int[][]
    {
        new int[] { 1, 0, 10, 10, 8, 8, 1, 1, 3, 5, 1, 3, 5 },
        new int[] { 2, 0, 12, 12, 10, 10, 1, 1, 5, 7, 1, 5, 7 },
        new int[] { 3, 0, 14, 14, 12, 12, 1, 1, 8, 10, 1, 8, 10 },
        new int[] { 4, 0, 16, 16, 14, 14, 1, 1, 12, 12, 1, 12, 12 },
        new int[] { 5, 0, 18, 18, 16, 16, 1, 1, 18, 14, 1, 18, 14 },
        new int[] { 6, 0, 20, 20, 18, 18, 1, 1, 22, 18, 1, 22, 18 },
        new int[] { 7, 0, 22, 22, 20, 20, 1, 1, 30, 20, 1, 30, 20 },
        new int[] { 8, 0, 24, 24, 22, 22, 1, 1, 36, 24, 1, 36, 24 },
        new int[] { 9, 0, 26, 26, 24, 24, 1, 1, 44, 28, 1, 44, 28 },
        new int[] { 10, 0, 32, 32, 14, 14, 2, 2, 62, 36, 1, 62, 36 },
        new int[] { 11, 0, 36, 36, 16, 16, 2, 2, 86, 42, 1, 86, 42 },
        new int[] { 12, 0, 40, 40, 18, 18, 2, 2, 114, 48, 1, 114, 48 },
        new int[] { 13, 0, 44, 44, 20, 20, 2, 2, 144, 56, 1, 144, 56 },
        new int[] { 14, 0, 48, 48, 22, 22, 2, 2, 174, 68, 1, 174, 68 },
        new int[] { 15, 0, 52, 52, 24, 24, 2, 2, 204, 84, 2, 102, 42 },
        new int[] { 16, 0, 64, 64, 14, 14, 4, 4, 280, 112, 2, 140, 56 },
        new int[] { 17, 0, 72, 72, 16, 16, 4, 4, 368, 144, 4, 92, 36 },
        new int[] { 18, 0, 80, 80, 18, 18, 4, 4, 456, 192, 4, 114, 48 },
        new int[] { 19, 0, 88, 88, 20, 20, 4, 4, 576, 224, 4, 144, 56 },
        new int[] { 20, 0, 96, 96, 22, 22, 4, 4, 696, 272, 4, 174, 68 },
        new int[] { 21, 0, 104, 104, 24, 24, 4, 4, 816, 336, 6, 136, 56 },
        new int[] { 22, 0, 120, 120, 18, 18, 6, 6, 1050, 408, 6, 175, 68 },
        new int[] { 23, 0, 132, 132, 20, 20, 6, 6, 1304, 496, 8, 163, 62 },
        new int[] { 24, 0, 144, 144, 22, 22, 6, 6, 1558, 620, 10, 0, 62 },
        new int[] { 25, 1, 8, 18, 6, 16, 1, 1, 5, 7, 1, 5, 7 },
        new int[] { 26, 1, 8, 32, 6, 14, 1, 2, 10, 11, 1, 10, 11 },
        new int[] { 27, 1, 12, 26, 10, 24, 1, 1, 16, 14, 1, 16, 14 },
        new int[] { 28, 1, 12, 36, 10, 16, 1, 2, 22, 18, 1, 22, 18 },
        new int[] { 29, 1, 16, 36, 14, 16, 1, 2, 32, 24, 1, 32, 24 },
        new int[] { 30, 1, 16, 48, 14, 22, 1, 2, 49, 28, 1, 49, 28 }
    };

    static readonly int[] AutomaticSelectionVersions = new int[]
    {
        1, 2, 3, 26, 4, 27, 5, 6, 7, 29, 8, 9, 30, 10, 11, 12, 13, 14, 15,
        16, 17, 18, 19, 20, 21, 22, 23, 24
    };

    [Test]
    public void CompleteStandardTableMatchesIndependentSymbolOracle()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixSymbolInfoTable.GetSymbolCount(), Is.EqualTo(30));
            Assert.That(DataMatrixSymbolInfoTable.SquareSymbolCount, Is.EqualTo(24));
            Assert.That(DataMatrixSymbolInfoTable.StandardRectangularSymbolCount, Is.EqualTo(6));
            Assert.That(DataMatrixSymbolInfoTable.MaximumDataCodewordCapacity, Is.EqualTo(1558));
            Assert.That(ExpectedSymbols, Has.Length.EqualTo(30));
        });

        int index = 0;
        while (index < ExpectedSymbols.Length)
        {
            int[] expected = ExpectedSymbols[index];
            DataMatrixSymbolInfo actual = DataMatrixSymbolInfoTable.GetSymbolByVersion(expected[0]);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Version, Is.EqualTo(expected[0]), $"Version {expected[0]}");
                Assert.That(actual.IsRectangular, Is.EqualTo(expected[1] == 1), $"Version {expected[0]}");
                Assert.That(actual.SymbolRows, Is.EqualTo(expected[2]), $"Version {expected[0]}");
                Assert.That(actual.SymbolColumns, Is.EqualTo(expected[3]), $"Version {expected[0]}");
                Assert.That(actual.DataRegionRows, Is.EqualTo(expected[4]), $"Version {expected[0]}");
                Assert.That(actual.DataRegionColumns, Is.EqualTo(expected[5]), $"Version {expected[0]}");
                Assert.That(actual.VerticalDataRegionCount, Is.EqualTo(expected[6]), $"Version {expected[0]}");
                Assert.That(actual.HorizontalDataRegionCount, Is.EqualTo(expected[7]), $"Version {expected[0]}");
                Assert.That(actual.DataCodewordCapacity, Is.EqualTo(expected[8]), $"Version {expected[0]}");
                Assert.That(actual.ErrorCodewordCount, Is.EqualTo(expected[9]), $"Version {expected[0]}");
                Assert.That(actual.InterleavedBlockCount, Is.EqualTo(expected[10]), $"Version {expected[0]}");
            });

            if (actual.Version != 24)
            {
                Assert.That(
                    actual.CopyDataCodewordsPerBlock(),
                    Is.EqualTo(CreateRepeatedArray(expected[10], expected[11])),
                    $"Version {expected[0]} data blocks");
            }

            Assert.That(
                actual.CopyErrorCodewordsPerBlock(),
                Is.EqualTo(CreateRepeatedArray(expected[10], expected[12])),
                $"Version {expected[0]} error blocks");
            index++;
        }
    }

    [Test]
    public void EverySymbolGeometryAndBlockDistributionIsInternallyConsistent()
    {
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            int[] dataBlocks = symbol.CopyDataCodewordsPerBlock();
            int[] errorBlocks = symbol.CopyErrorCodewordsPerBlock();
            Assert.Multiple(() =>
            {
                Assert.That(symbol.DataRegionCount, Is.EqualTo(symbol.VerticalDataRegionCount * symbol.HorizontalDataRegionCount));
                Assert.That(symbol.MappingMatrixRows, Is.EqualTo(symbol.DataRegionRows * symbol.VerticalDataRegionCount));
                Assert.That(symbol.MappingMatrixColumns, Is.EqualTo(symbol.DataRegionColumns * symbol.HorizontalDataRegionCount));
                Assert.That(symbol.SymbolRows, Is.EqualTo(symbol.MappingMatrixRows + (2 * symbol.VerticalDataRegionCount)));
                Assert.That(symbol.SymbolColumns, Is.EqualTo(symbol.MappingMatrixColumns + (2 * symbol.HorizontalDataRegionCount)));
                Assert.That(symbol.TotalCodewordCount, Is.EqualTo(symbol.DataCodewordCapacity + symbol.ErrorCodewordCount));
                Assert.That(dataBlocks, Has.Length.EqualTo(symbol.InterleavedBlockCount));
                Assert.That(errorBlocks, Has.Length.EqualTo(symbol.InterleavedBlockCount));
                Assert.That(dataBlocks.Sum(), Is.EqualTo(symbol.DataCodewordCapacity));
                Assert.That(errorBlocks.Sum(), Is.EqualTo(symbol.ErrorCodewordCount));
            });

            int block = 0;
            while (block < symbol.InterleavedBlockCount)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(symbol.GetDataCodewordCountForBlock(block), Is.EqualTo(dataBlocks[block]));
                    Assert.That(symbol.GetErrorCodewordCountForBlock(block), Is.EqualTo(errorBlocks[block]));
                    Assert.That(DataMatrixReedSolomon.IsSupportedErrorCorrectionCount(errorBlocks[block]), Is.True);
                });
                block++;
            }

            version++;
        }
    }

    [Test]
    public void LargestSquareUsesTheRequiredAsymmetricDataBlockDistribution()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(24);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolRows, Is.EqualTo(144));
            Assert.That(symbol.SymbolColumns, Is.EqualTo(144));
            Assert.That(symbol.CopyDataCodewordsPerBlock(), Is.EqualTo(new int[]
            {
                156, 156, 156, 156, 156, 156, 156, 156, 155, 155
            }));
            Assert.That(symbol.CopyErrorCodewordsPerBlock(), Is.EqualTo(new int[]
            {
                62, 62, 62, 62, 62, 62, 62, 62, 62, 62
            }));
        });
    }

    [Test]
    public void DimensionLookupFindsEveryStandardSymbolAndExcludesDmreSizes()
    {
        int index = 0;
        while (index < ExpectedSymbols.Length)
        {
            int[] expected = ExpectedSymbols[index];
            DataMatrixSymbolInfo actual = DataMatrixSymbolInfoTable.FindSymbolByDimensions(expected[2], expected[3])!;
            Assert.That(actual, Is.Not.Null, $"{expected[2]}x{expected[3]}");
            Assert.That(actual.Version, Is.EqualTo(expected[0]), $"{expected[2]}x{expected[3]}");
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolByDimensions(8, 48), Is.Null);
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolByDimensions(12, 64), Is.Null);
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolByDimensions(20, 64), Is.Null);
        });
    }

    [Test]
    public void ShapeSpecificSelectionUsesEveryCapacityBoundary()
    {
        int previousCapacity = 0;
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SquareSymbolCount)
        {
            DataMatrixSymbolInfo expected = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            DataMatrixSymbolInfo selected = DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(
                previousCapacity + 1,
                DataMatrixShape.Square)!;
            Assert.That(selected.Version, Is.EqualTo(version), $"Square capacity after {previousCapacity}");
            Assert.That(
                DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(expected.DataCodewordCapacity, DataMatrixShape.Square)!.Version,
                Is.EqualTo(version));
            previousCapacity = expected.DataCodewordCapacity;
            version++;
        }

        previousCapacity = 0;
        version = 25;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo expected = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            DataMatrixSymbolInfo selected = DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(
                previousCapacity + 1,
                DataMatrixShape.StandardRectangular)!;
            Assert.That(selected.Version, Is.EqualTo(version), $"Rectangular capacity after {previousCapacity}");
            Assert.That(
                DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(
                    expected.DataCodewordCapacity,
                    DataMatrixShape.StandardRectangular)!.Version,
                Is.EqualTo(version));
            previousCapacity = expected.DataCodewordCapacity;
            version++;
        }
    }

    [Test]
    public void AutomaticSelectionUsesSmallestCapacityAndPrefersSquareOnTies()
    {
        int previousCapacity = 0;
        int index = 0;
        while (index < AutomaticSelectionVersions.Length)
        {
            int expectedVersion = AutomaticSelectionVersions[index];
            DataMatrixSymbolInfo expected = DataMatrixSymbolInfoTable.GetSymbolByVersion(expectedVersion);
            DataMatrixSymbolInfo selected = DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(
                previousCapacity + 1,
                DataMatrixShape.Automatic)!;
            Assert.That(selected.Version, Is.EqualTo(expectedVersion), $"Automatic capacity after {previousCapacity}");
            previousCapacity = expected.DataCodewordCapacity;
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(5, DataMatrixShape.Automatic)!.Version, Is.EqualTo(2));
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(9, DataMatrixShape.Automatic)!.Version, Is.EqualTo(26));
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(22, DataMatrixShape.Automatic)!.Version, Is.EqualTo(6));
        });
    }

    [Test]
    public void LookupsRejectInvalidInputsAndReportCapacityExhaustion()
    {
        DataMatrixSymbolInfo singleBlock = DataMatrixSymbolInfoTable.GetSymbolByVersion(1);
        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixSymbolInfoTable.GetSymbolByVersion(0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixSymbolInfoTable.GetSymbolByVersion(31), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixSymbolInfoTable.FindSymbolByDimensions(0, 10), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixSymbolInfoTable.FindSymbolByDimensions(10, -1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(0, DataMatrixShape.Automatic), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(1, (DataMatrixShape)99), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => singleBlock.GetDataCodewordCountForBlock(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => singleBlock.GetErrorCodewordCountForBlock(1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(50, DataMatrixShape.StandardRectangular), Is.Null);
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(1559, DataMatrixShape.Square), Is.Null);
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(1559, DataMatrixShape.Automatic), Is.Null);
        });
    }

    [Test]
    public void ReturnedSymbolsAndBlockArraysCannotMutateTheCanonicalTable()
    {
        DataMatrixSymbolInfo first = DataMatrixSymbolInfoTable.GetSymbolByVersion(24);
        int[] copiedBlocks = first.CopyDataCodewordsPerBlock();
        first.SymbolRows = 1;
        first.DataCodewordsPerBlock[0] = 1;
        copiedBlocks[1] = 1;

        DataMatrixSymbolInfo second = DataMatrixSymbolInfoTable.GetSymbolByVersion(24);
        Assert.Multiple(() =>
        {
            Assert.That(second.SymbolRows, Is.EqualTo(144));
            Assert.That(second.GetDataCodewordCountForBlock(0), Is.EqualTo(156));
            Assert.That(second.GetDataCodewordCountForBlock(1), Is.EqualTo(156));
            Assert.That(DataMatrixSymbolInfoTable.FindSymbolByDimensions(144, 144)!.Version, Is.EqualTo(24));
        });
    }

    static int[] CreateRepeatedArray(int count, int value)
    {
        int[] result = new int[count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = value;
            index++;
        }

        return result;
    }
}
