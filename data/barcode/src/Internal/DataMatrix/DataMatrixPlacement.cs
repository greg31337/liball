namespace barcode;

internal class DataMatrixPlacementMap
{
    internal const int FixedDarkAssignment = -1;
    internal const int FixedLightAssignment = -2;
    internal const int Unassigned = -3;

    internal int Rows;
    internal int Columns;
    internal int CodewordCount;
    internal int[] Assignments = new int[0];

    internal int GetAssignment(int column, int row)
    {
        ValidateCoordinates(column, row);
        return Assignments[(row * Columns) + column];
    }

    internal bool IsFixed(int column, int row)
    {
        int assignment = GetAssignment(column, row);
        return assignment == FixedDarkAssignment || assignment == FixedLightAssignment;
    }

    internal bool GetFixedValue(int column, int row)
    {
        int assignment = GetAssignment(column, row);
        if (assignment == FixedDarkAssignment)
        {
            return true;
        }

        if (assignment == FixedLightAssignment)
        {
            return false;
        }

        throw new BarcodeValidationException("Data Matrix placement module is not fixed.");
    }

    internal int GetCodewordIndex(int column, int row)
    {
        int assignment = GetAssignment(column, row);
        if (assignment < 0)
        {
            throw new BarcodeValidationException("Data Matrix fixed module does not have a codeword index.");
        }

        return assignment / 8;
    }

    internal int GetBitIndex(int column, int row)
    {
        int assignment = GetAssignment(column, row);
        if (assignment < 0)
        {
            throw new BarcodeValidationException("Data Matrix fixed module does not have a bit index.");
        }

        return assignment % 8;
    }

    void ValidateCoordinates(int column, int row)
    {
        if (column < 0 || column >= Columns || row < 0 || row >= Rows)
        {
            throw new BarcodeValidationException("Data Matrix placement coordinate is outside the mapping matrix.");
        }
    }
}

internal static class DataMatrixPlacement
{
    internal static DataMatrixPlacementMap CreatePlacementMap(DataMatrixSymbolInfo symbol)
    {
        ValidateSymbol(symbol);
        DataMatrixPlacementMap map = new DataMatrixPlacementMap();
        map.Rows = symbol.MappingMatrixRows;
        map.Columns = symbol.MappingMatrixColumns;
        map.CodewordCount = symbol.TotalCodewordCount;
        map.Assignments = new int[CheckedMath.Multiply(map.Rows, map.Columns)];
        int index = 0;
        while (index < map.Assignments.Length)
        {
            map.Assignments[index] = DataMatrixPlacementMap.Unassigned;
            index = index + 1;
        }

        int codewordIndex = 0;
        int row = 4;
        int column = 0;
        do
        {
            if (row == map.Rows && column == 0)
            {
                AssignCorner1(map, codewordIndex);
                codewordIndex = codewordIndex + 1;
            }

            if (row == map.Rows - 2
                && column == 0
                && map.Columns % 4 != 0)
            {
                AssignCorner2(map, codewordIndex);
                codewordIndex = codewordIndex + 1;
            }

            if (row == map.Rows - 2
                && column == 0
                && map.Columns % 8 == 4)
            {
                AssignCorner3(map, codewordIndex);
                codewordIndex = codewordIndex + 1;
            }

            if (row == map.Rows + 4
                && column == 2
                && map.Columns % 8 == 0)
            {
                AssignCorner4(map, codewordIndex);
                codewordIndex = codewordIndex + 1;
            }

            do
            {
                if (row < map.Rows
                    && column >= 0
                    && HasNoAssignment(map, column, row))
                {
                    AssignUtah(map, row, column, codewordIndex);
                    codewordIndex = codewordIndex + 1;
                }

                row = row - 2;
                column = column + 2;
            }
            while (row >= 0 && column < map.Columns);

            row = row + 1;
            column = column + 3;
            do
            {
                if (row >= 0
                    && column < map.Columns
                    && HasNoAssignment(map, column, row))
                {
                    AssignUtah(map, row, column, codewordIndex);
                    codewordIndex = codewordIndex + 1;
                }

                row = row + 2;
                column = column - 2;
            }
            while (row < map.Rows && column >= 0);

            row = row + 3;
            column = column + 1;
        }
        while (row < map.Rows || column < map.Columns);

        CompleteFixedPattern(map);
        ValidateCompleteMap(map, codewordIndex);
        return map;
    }

    internal static ModuleMatrix PlaceCodewords(
        int[] codewords,
        DataMatrixSymbolInfo symbol)
    {
        DataMatrixPlacementMap map = CreatePlacementMap(symbol);
        ValidateCodewords(codewords, map.CodewordCount);
        ModuleMatrix matrix = new ModuleMatrix(map.Columns, map.Rows);
        int row = 0;
        while (row < map.Rows)
        {
            int column = 0;
            while (column < map.Columns)
            {
                int assignment = map.Assignments[(row * map.Columns) + column];
                bool value;
                if (assignment == DataMatrixPlacementMap.FixedDarkAssignment)
                {
                    value = true;
                }
                else if (assignment == DataMatrixPlacementMap.FixedLightAssignment)
                {
                    value = false;
                }
                else
                {
                    int codewordIndex = assignment / 8;
                    int bitIndex = assignment % 8;
                    value = (codewords[codewordIndex] & (1 << (7 - bitIndex))) != 0;
                }

                matrix.SetModule(column, row, value);
                column = column + 1;
            }

            row = row + 1;
        }

        return matrix;
    }

    internal static int[] ExtractCodewords(
        ModuleMatrix mappingMatrix,
        DataMatrixSymbolInfo symbol)
    {
        DataMatrixPlacementMap map = CreatePlacementMap(symbol);
        ValidateMappingMatrix(mappingMatrix, map);
        ValidateFixedModulesWithMap(mappingMatrix, map);
        int[] codewords = new int[map.CodewordCount];
        int row = 0;
        while (row < map.Rows)
        {
            int column = 0;
            while (column < map.Columns)
            {
                int assignment = map.Assignments[(row * map.Columns) + column];
                if (assignment >= 0 && mappingMatrix.GetModule(column, row))
                {
                    int codewordIndex = assignment / 8;
                    int bitIndex = assignment % 8;
                    codewords[codewordIndex] = codewords[codewordIndex]
                        | (1 << (7 - bitIndex));
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return codewords;
    }

    internal static void ValidateFixedModules(
        ModuleMatrix mappingMatrix,
        DataMatrixSymbolInfo symbol)
    {
        DataMatrixPlacementMap map = CreatePlacementMap(symbol);
        ValidateMappingMatrix(mappingMatrix, map);
        ValidateFixedModulesWithMap(mappingMatrix, map);
    }

    static void AssignUtah(
        DataMatrixPlacementMap map,
        int row,
        int column,
        int codewordIndex)
    {
        AssignModule(map, row - 2, column - 2, codewordIndex, 0);
        AssignModule(map, row - 2, column - 1, codewordIndex, 1);
        AssignModule(map, row - 1, column - 2, codewordIndex, 2);
        AssignModule(map, row - 1, column - 1, codewordIndex, 3);
        AssignModule(map, row - 1, column, codewordIndex, 4);
        AssignModule(map, row, column - 2, codewordIndex, 5);
        AssignModule(map, row, column - 1, codewordIndex, 6);
        AssignModule(map, row, column, codewordIndex, 7);
    }

    static void AssignCorner1(DataMatrixPlacementMap map, int codewordIndex)
    {
        AssignModule(map, map.Rows - 1, 0, codewordIndex, 0);
        AssignModule(map, map.Rows - 1, 1, codewordIndex, 1);
        AssignModule(map, map.Rows - 1, 2, codewordIndex, 2);
        AssignModule(map, 0, map.Columns - 2, codewordIndex, 3);
        AssignModule(map, 0, map.Columns - 1, codewordIndex, 4);
        AssignModule(map, 1, map.Columns - 1, codewordIndex, 5);
        AssignModule(map, 2, map.Columns - 1, codewordIndex, 6);
        AssignModule(map, 3, map.Columns - 1, codewordIndex, 7);
    }

    static void AssignCorner2(DataMatrixPlacementMap map, int codewordIndex)
    {
        AssignModule(map, map.Rows - 3, 0, codewordIndex, 0);
        AssignModule(map, map.Rows - 2, 0, codewordIndex, 1);
        AssignModule(map, map.Rows - 1, 0, codewordIndex, 2);
        AssignModule(map, 0, map.Columns - 4, codewordIndex, 3);
        AssignModule(map, 0, map.Columns - 3, codewordIndex, 4);
        AssignModule(map, 0, map.Columns - 2, codewordIndex, 5);
        AssignModule(map, 0, map.Columns - 1, codewordIndex, 6);
        AssignModule(map, 1, map.Columns - 1, codewordIndex, 7);
    }

    static void AssignCorner3(DataMatrixPlacementMap map, int codewordIndex)
    {
        AssignModule(map, map.Rows - 3, 0, codewordIndex, 0);
        AssignModule(map, map.Rows - 2, 0, codewordIndex, 1);
        AssignModule(map, map.Rows - 1, 0, codewordIndex, 2);
        AssignModule(map, 0, map.Columns - 2, codewordIndex, 3);
        AssignModule(map, 0, map.Columns - 1, codewordIndex, 4);
        AssignModule(map, 1, map.Columns - 1, codewordIndex, 5);
        AssignModule(map, 2, map.Columns - 1, codewordIndex, 6);
        AssignModule(map, 3, map.Columns - 1, codewordIndex, 7);
    }

    static void AssignCorner4(DataMatrixPlacementMap map, int codewordIndex)
    {
        AssignModule(map, map.Rows - 1, 0, codewordIndex, 0);
        AssignModule(map, map.Rows - 1, map.Columns - 1, codewordIndex, 1);
        AssignModule(map, 0, map.Columns - 3, codewordIndex, 2);
        AssignModule(map, 0, map.Columns - 2, codewordIndex, 3);
        AssignModule(map, 0, map.Columns - 1, codewordIndex, 4);
        AssignModule(map, 1, map.Columns - 3, codewordIndex, 5);
        AssignModule(map, 1, map.Columns - 2, codewordIndex, 6);
        AssignModule(map, 1, map.Columns - 1, codewordIndex, 7);
    }

    static void AssignModule(
        DataMatrixPlacementMap map,
        int row,
        int column,
        int codewordIndex,
        int bitIndex)
    {
        if (row < 0)
        {
            row = row + map.Rows;
            column = column + 4 - ((map.Rows + 4) % 8);
        }

        if (column < 0)
        {
            column = column + map.Columns;
            row = row + 4 - ((map.Columns + 4) % 8);
        }

        if (row < 0 || row >= map.Rows || column < 0 || column >= map.Columns)
        {
            throw new BarcodeValidationException("Data Matrix placement wrapping produced an invalid coordinate.");
        }

        int index = (row * map.Columns) + column;
        if (map.Assignments[index] != DataMatrixPlacementMap.Unassigned)
        {
            throw new BarcodeValidationException("Data Matrix placement assigns a mapping module more than once.");
        }

        map.Assignments[index] = CheckedMath.Add(
            CheckedMath.Multiply(codewordIndex, 8),
            bitIndex);
    }

    static bool HasNoAssignment(
        DataMatrixPlacementMap map,
        int column,
        int row)
    {
        return map.Assignments[(row * map.Columns) + column]
            == DataMatrixPlacementMap.Unassigned;
    }

    static void CompleteFixedPattern(DataMatrixPlacementMap map)
    {
        int bottomRight = ((map.Rows - 1) * map.Columns) + map.Columns - 1;
        if (map.Assignments[bottomRight] != DataMatrixPlacementMap.Unassigned)
        {
            return;
        }

        map.Assignments[bottomRight] = DataMatrixPlacementMap.FixedDarkAssignment;
        int upperLeft = ((map.Rows - 2) * map.Columns) + map.Columns - 2;
        map.Assignments[upperLeft] = DataMatrixPlacementMap.FixedDarkAssignment;
        int index = 0;
        while (index < map.Assignments.Length)
        {
            if (map.Assignments[index] == DataMatrixPlacementMap.Unassigned)
            {
                map.Assignments[index] = DataMatrixPlacementMap.FixedLightAssignment;
            }

            index = index + 1;
        }
    }

    static void ValidateCompleteMap(
        DataMatrixPlacementMap map,
        int assignedCodewordCount)
    {
        if (assignedCodewordCount != map.CodewordCount)
        {
            throw new BarcodeValidationException("Data Matrix placement codeword count does not match the symbol capacity.");
        }

        bool[] assignedBits = new bool[CheckedMath.Multiply(map.CodewordCount, 8)];
        int fixedCount = 0;
        int index = 0;
        while (index < map.Assignments.Length)
        {
            int assignment = map.Assignments[index];
            if (assignment >= 0)
            {
                if (assignment >= assignedBits.Length || assignedBits[assignment])
                {
                    throw new BarcodeValidationException("Data Matrix placement bit assignment is invalid or duplicated.");
                }

                assignedBits[assignment] = true;
            }
            else if (assignment == DataMatrixPlacementMap.FixedDarkAssignment
                || assignment == DataMatrixPlacementMap.FixedLightAssignment)
            {
                fixedCount = fixedCount + 1;
            }
            else
            {
                throw new BarcodeValidationException("Data Matrix placement contains an unassigned module.");
            }

            index = index + 1;
        }

        index = 0;
        while (index < assignedBits.Length)
        {
            if (!assignedBits[index])
            {
                throw new BarcodeValidationException("Data Matrix placement omits a codeword bit.");
            }

            index = index + 1;
        }

        if (fixedCount != 0 && fixedCount != 4)
        {
            throw new BarcodeValidationException("Data Matrix placement fixed-module count is invalid.");
        }
    }

    static void ValidateFixedModulesWithMap(
        ModuleMatrix mappingMatrix,
        DataMatrixPlacementMap map)
    {
        int row = 0;
        while (row < map.Rows)
        {
            int column = 0;
            while (column < map.Columns)
            {
                int assignment = map.Assignments[(row * map.Columns) + column];
                if (assignment == DataMatrixPlacementMap.FixedDarkAssignment
                    && !mappingMatrix.GetModule(column, row))
                {
                    throw new BarcodeDecodingException("Data Matrix fixed dark mapping module is light.");
                }

                if (assignment == DataMatrixPlacementMap.FixedLightAssignment
                    && mappingMatrix.GetModule(column, row))
                {
                    throw new BarcodeDecodingException("Data Matrix fixed light mapping module is dark.");
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    static void ValidateCodewords(int[] codewords, int expectedCount)
    {
        if (codewords == null)
        {
            throw new BarcodeEncodingException("Data Matrix placement codewords must not be null.");
        }

        if (codewords.Length != expectedCount)
        {
            throw new BarcodeEncodingException("Data Matrix placement requires exactly the symbol's total codeword count.");
        }

        int index = 0;
        while (index < codewords.Length)
        {
            if (codewords[index] < 0 || codewords[index] > 255)
            {
                throw new BarcodeEncodingException("Data Matrix placement codeword is outside the byte range.");
            }

            index = index + 1;
        }
    }

    static void ValidateMappingMatrix(
        ModuleMatrix mappingMatrix,
        DataMatrixPlacementMap map)
    {
        if (mappingMatrix == null)
        {
            throw new BarcodeDecodingException("Data Matrix mapping matrix must not be null.");
        }

        if (mappingMatrix.Width != map.Columns || mappingMatrix.Height != map.Rows)
        {
            throw new BarcodeDecodingException("Data Matrix mapping matrix dimensions do not match the symbol.");
        }
    }

    static void ValidateSymbol(DataMatrixSymbolInfo symbol)
    {
        if (symbol == null)
        {
            throw new BarcodeValidationException("Data Matrix placement symbol information must not be null.");
        }

        if (symbol.MappingMatrixRows <= 0
            || symbol.MappingMatrixColumns <= 0
            || symbol.TotalCodewordCount <= 0)
        {
            throw new BarcodeValidationException("Data Matrix placement symbol geometry is invalid.");
        }

        int moduleCount = CheckedMath.Multiply(
            symbol.MappingMatrixRows,
            symbol.MappingMatrixColumns);
        int dataBitCount = CheckedMath.Multiply(symbol.TotalCodewordCount, 8);
        if (moduleCount != dataBitCount && moduleCount != dataBitCount + 4)
        {
            throw new BarcodeValidationException("Data Matrix mapping matrix area does not match its total codeword count.");
        }
    }
}
