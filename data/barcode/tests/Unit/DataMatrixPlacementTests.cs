namespace barcode.Tests;

public class DataMatrixPlacementTests
{
    [Test]
    public void EveryStandardSizePlacesAndExtractsEveryCodewordBitExactlyOnce()
    {
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            DataMatrixPlacementMap map = DataMatrixPlacement.CreatePlacementMap(symbol);
            int[] codewords = CreateCodewords(symbol.TotalCodewordCount, version);
            ModuleMatrix matrix = DataMatrixPlacement.PlaceCodewords(codewords, symbol);
            int[] extracted = DataMatrixPlacement.ExtractCodewords(matrix, symbol);
            bool[] seen = new bool[symbol.TotalCodewordCount * 8];
            int fixedCount = 0;
            int row = 0;
            while (row < map.Rows)
            {
                int column = 0;
                while (column < map.Columns)
                {
                    int assignment = map.GetAssignment(column, row);
                    if (assignment >= 0)
                    {
                        Assert.That(seen[assignment], Is.False, $"Version {version}, assignment {assignment}");
                        seen[assignment] = true;
                    }
                    else
                    {
                        Assert.That(map.IsFixed(column, row), Is.True, $"Version {version}, fixed {column},{row}");
                        fixedCount++;
                    }

                    column++;
                }

                row++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(map.Rows, Is.EqualTo(symbol.MappingMatrixRows), $"Version {version}");
                Assert.That(map.Columns, Is.EqualTo(symbol.MappingMatrixColumns), $"Version {version}");
                Assert.That(map.CodewordCount, Is.EqualTo(symbol.TotalCodewordCount), $"Version {version}");
                Assert.That(seen, Has.All.True, $"Version {version}");
                Assert.That(fixedCount, Is.EqualTo(map.Assignments.Length - (symbol.TotalCodewordCount * 8)), $"Version {version}");
                Assert.That(fixedCount, Is.AnyOf(0, 4), $"Version {version}");
                Assert.That(matrix.Width, Is.EqualTo(symbol.MappingMatrixColumns), $"Version {version}");
                Assert.That(matrix.Height, Is.EqualTo(symbol.MappingMatrixRows), $"Version {version}");
                Assert.That(extracted, Is.EqualTo(codewords), $"Version {version}");
            });
            version++;
        }
    }

    [Test]
    public void FirstUtahPatternUsesTheStandardWrappedCoordinatesAndBitOrder()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(1);
        DataMatrixPlacementMap map = DataMatrixPlacement.CreatePlacementMap(symbol);
        int[] codewords = new int[symbol.TotalCodewordCount];
        codewords[0] = 0xA5;

        ModuleMatrix matrix = DataMatrixPlacement.PlaceCodewords(codewords, symbol);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetAssignment(6, 2), Is.EqualTo(0));
            Assert.That(map.GetAssignment(7, 2), Is.EqualTo(1));
            Assert.That(map.GetAssignment(6, 3), Is.EqualTo(2));
            Assert.That(map.GetAssignment(7, 3), Is.EqualTo(3));
            Assert.That(map.GetAssignment(0, 3), Is.EqualTo(4));
            Assert.That(map.GetAssignment(6, 4), Is.EqualTo(5));
            Assert.That(map.GetAssignment(7, 4), Is.EqualTo(6));
            Assert.That(map.GetAssignment(0, 4), Is.EqualTo(7));
            Assert.That(matrix.GetModule(6, 2), Is.True);
            Assert.That(matrix.GetModule(7, 2), Is.False);
            Assert.That(matrix.GetModule(6, 3), Is.True);
            Assert.That(matrix.GetModule(7, 3), Is.False);
            Assert.That(matrix.GetModule(0, 3), Is.False);
            Assert.That(matrix.GetModule(6, 4), Is.True);
            Assert.That(matrix.GetModule(7, 4), Is.False);
            Assert.That(matrix.GetModule(0, 4), Is.True);
        });
    }

    [Test]
    public void ExceptionalCornerPatternsUseTheirSpecifiedCoordinates()
    {
        DataMatrixPlacementMap corner1 = DataMatrixPlacement.CreatePlacementMap(
            DataMatrixSymbolInfoTable.GetSymbolByVersion(3));
        DataMatrixPlacementMap corner2 = DataMatrixPlacement.CreatePlacementMap(
            DataMatrixSymbolInfoTable.GetSymbolByVersion(4));
        DataMatrixPlacementMap corner3 = DataMatrixPlacement.CreatePlacementMap(
            DataMatrixSymbolInfoTable.GetSymbolByVersion(26));
        DataMatrixPlacementMap corner4 = DataMatrixPlacement.CreatePlacementMap(
            DataMatrixSymbolInfoTable.GetSymbolByVersion(25));

        AssertPattern(
            corner1,
            new int[] { 0, 11, 1, 11, 2, 11, 10, 0, 11, 0, 11, 1, 11, 2, 11, 3 });
        AssertPattern(
            corner2,
            new int[] { 0, 11, 0, 12, 0, 13, 10, 0, 11, 0, 12, 0, 13, 0, 13, 1 });
        AssertPattern(
            corner3,
            new int[] { 0, 3, 0, 4, 0, 5, 26, 0, 27, 0, 27, 1, 27, 2, 27, 3 });
        AssertPattern(
            corner4,
            new int[] { 0, 5, 15, 5, 13, 0, 14, 0, 15, 0, 13, 1, 14, 1, 15, 1 });
    }

    [Test]
    public void FixedLowerRightPatternIsValidatedForEveryApplicableSize()
    {
        int applicableCount = 0;
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            DataMatrixPlacementMap map = DataMatrixPlacement.CreatePlacementMap(symbol);
            int fixedCount = CountFixedModules(map);
            if (fixedCount > 0)
            {
                applicableCount++;
                ModuleMatrix matrix = DataMatrixPlacement.PlaceCodewords(
                    new int[symbol.TotalCodewordCount],
                    symbol);
                int right = map.Columns - 1;
                int bottom = map.Rows - 1;
                Assert.Multiple(() =>
                {
                    Assert.That(fixedCount, Is.EqualTo(4), $"Version {version}");
                    Assert.That(map.GetFixedValue(right, bottom), Is.True, $"Version {version}");
                    Assert.That(map.GetFixedValue(right - 1, bottom - 1), Is.True, $"Version {version}");
                    Assert.That(map.GetFixedValue(right, bottom - 1), Is.False, $"Version {version}");
                    Assert.That(map.GetFixedValue(right - 1, bottom), Is.False, $"Version {version}");
                    Assert.That(() => DataMatrixPlacement.ValidateFixedModules(matrix, symbol), Throws.Nothing);
                });

                matrix.SetModule(right, bottom, false);
                Assert.That(
                    () => DataMatrixPlacement.ExtractCodewords(matrix, symbol),
                    Throws.TypeOf<BarcodeDecodingException>(),
                    $"Version {version} fixed dark");
                matrix.SetModule(right, bottom, true);
                matrix.SetModule(right, bottom - 1, true);
                Assert.That(
                    () => DataMatrixPlacement.ValidateFixedModules(matrix, symbol),
                    Throws.TypeOf<BarcodeDecodingException>(),
                    $"Version {version} fixed light");
            }

            version++;
        }

        Assert.That(applicableCount, Is.GreaterThan(0));
    }

    [Test]
    public void ChangingOneDataModuleChangesOnlyItsAssignedCodewordBit()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(30);
        int[] codewords = CreateCodewords(symbol.TotalCodewordCount, 30);
        ModuleMatrix matrix = DataMatrixPlacement.PlaceCodewords(codewords, symbol);
        DataMatrixPlacementMap map = DataMatrixPlacement.CreatePlacementMap(symbol);
        int[] coordinates = FindAssignment(map, 0);
        bool original = matrix.GetModule(coordinates[0], coordinates[1]);
        matrix.SetModule(coordinates[0], coordinates[1], !original);

        int[] extracted = DataMatrixPlacement.ExtractCodewords(matrix, symbol);
        int expectedFirst = codewords[0] ^ 0x80;

        Assert.Multiple(() =>
        {
            Assert.That(extracted[0], Is.EqualTo(expectedFirst));
            Assert.That(CopyRange(extracted, 1), Is.EqualTo(CopyRange(codewords, 1)));
        });
    }

    [Test]
    public void PlacementRejectsInvalidCodewordsMatricesSymbolsAndMapQueries()
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(2);
        int[] valid = new int[symbol.TotalCodewordCount];
        int[] negative = new int[symbol.TotalCodewordCount];
        negative[0] = -1;
        int[] high = new int[symbol.TotalCodewordCount];
        high[0] = 256;
        DataMatrixSymbolInfo badArea = symbol.CopySymbolInfo();
        badArea.MappingMatrixRows = 9;
        DataMatrixSymbolInfo badCount = symbol.CopySymbolInfo();
        badCount.TotalCodewordCount = 1;
        DataMatrixPlacementMap map = DataMatrixPlacement.CreatePlacementMap(symbol);
        ModuleMatrix wrongWidth = new ModuleMatrix(symbol.MappingMatrixColumns - 1, symbol.MappingMatrixRows);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => DataMatrixPlacement.PlaceCodewords(null!, symbol),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixPlacement.PlaceCodewords(new int[valid.Length - 1], symbol),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixPlacement.PlaceCodewords(negative, symbol),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixPlacement.PlaceCodewords(high, symbol),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixPlacement.ExtractCodewords(null!, symbol),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => DataMatrixPlacement.ExtractCodewords(wrongWidth, symbol),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => DataMatrixPlacement.CreatePlacementMap(null!),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixPlacement.CreatePlacementMap(badArea),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixPlacement.CreatePlacementMap(badCount),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => map.GetAssignment(-1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => map.GetAssignment(map.Columns, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => map.GetFixedValue(0, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => map.GetCodewordIndex(map.Columns - 1, map.Rows - 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => map.GetBitIndex(map.Columns - 1, map.Rows - 1), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static void AssertPattern(DataMatrixPlacementMap map, int[] coordinates)
    {
        int codewordIndex = map.GetCodewordIndex(coordinates[0], coordinates[1]);
        int bitIndex = 0;
        while (bitIndex < 8)
        {
            int column = coordinates[bitIndex * 2];
            int row = coordinates[(bitIndex * 2) + 1];
            Assert.Multiple(() =>
            {
                Assert.That(map.GetCodewordIndex(column, row), Is.EqualTo(codewordIndex));
                Assert.That(map.GetBitIndex(column, row), Is.EqualTo(bitIndex));
            });
            bitIndex++;
        }
    }

    static int CountFixedModules(DataMatrixPlacementMap map)
    {
        int count = 0;
        int row = 0;
        while (row < map.Rows)
        {
            int column = 0;
            while (column < map.Columns)
            {
                if (map.IsFixed(column, row))
                {
                    count++;
                }

                column++;
            }

            row++;
        }

        return count;
    }

    static int[] FindAssignment(DataMatrixPlacementMap map, int assignment)
    {
        int row = 0;
        while (row < map.Rows)
        {
            int column = 0;
            while (column < map.Columns)
            {
                if (map.GetAssignment(column, row) == assignment)
                {
                    return new int[] { column, row };
                }

                column++;
            }

            row++;
        }

        throw new global::System.InvalidOperationException("Assignment not found.");
    }

    static int[] CreateCodewords(int count, int seed)
    {
        int[] codewords = new int[count];
        int index = 0;
        while (index < codewords.Length)
        {
            codewords[index] = ((index * 149) + (seed * 17)) & 0xFF;
            index++;
        }

        return codewords;
    }

    static int[] CopyRange(int[] values, int start)
    {
        int[] copy = new int[values.Length - start];
        global::System.Array.Copy(values, start, copy, 0, copy.Length);
        return copy;
    }
}
