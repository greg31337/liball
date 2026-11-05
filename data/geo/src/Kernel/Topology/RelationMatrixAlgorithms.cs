namespace geo;

internal static class RelationMatrixAlgorithms
{
    internal static void ValidateMatrix(RelationMatrix matrix)
    {
        if (matrix == null)
        {
            throw new InvalidGeometryException("Relation matrix is null.");
        }
        if (matrix.Cells == null)
        {
            throw new InvalidGeometryException("Relation matrix cell storage is null.");
        }
        if (matrix.Cells.Length != 9)
        {
            throw new InvalidGeometryException(
                "A relation matrix must contain exactly nine cells.");
        }

        ValidateTopologicalDimension(
            matrix.LeftDimension, "Relation matrix left dimension");
        ValidateTopologicalDimension(
            matrix.RightDimension, "Relation matrix right dimension");

        int index = 0;
        while (index < matrix.Cells.Length)
        {
            ValidateRelationDimension(matrix.Cells[index]);
            index++;
        }
    }

    internal static RelationDimension GetCell(
        RelationMatrix matrix, TopologicalLocation row,
        TopologicalLocation column)
    {
        ValidateMatrix(matrix);
        int index = GetCellIndex(row, column);
        return matrix.Cells[index];
    }

    internal static void SetCell(
        RelationMatrix matrix, TopologicalLocation row,
        TopologicalLocation column, RelationDimension dimension)
    {
        ValidateMatrix(matrix);
        ValidateRelationDimension(dimension);
        int index = GetCellIndex(row, column);
        matrix.Cells[index] = dimension;
    }

    internal static void SetCellAtLeast(
        RelationMatrix matrix, TopologicalLocation row,
        TopologicalLocation column, RelationDimension minimumDimension)
    {
        ValidateMatrix(matrix);
        ValidateRelationDimension(minimumDimension);
        int index = GetCellIndex(row, column);
        if ((int)matrix.Cells[index] < (int)minimumDimension)
        {
            matrix.Cells[index] = minimumDimension;
        }
    }

    internal static string GetCanonicalText(RelationMatrix matrix)
    {
        ValidateMatrix(matrix);
        char[] characters = new char[9];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = GetDimensionSymbol(matrix.Cells[index]);
            index++;
        }
        return new string(characters);
    }

    internal static RelationMatrix Transpose(RelationMatrix matrix)
    {
        ValidateMatrix(matrix);
        RelationDimension[] cells = new RelationDimension[9];
        int row = 0;
        while (row < 3)
        {
            int column = 0;
            while (column < 3)
            {
                cells[(column * 3) + row] = matrix.Cells[(row * 3) + column];
                column++;
            }
            row++;
        }

        return new RelationMatrix
        {
            Cells = cells,
            LeftDimension = matrix.RightDimension,
            RightDimension = matrix.LeftDimension
        };
    }

    internal static bool MatchesPattern(RelationMatrix matrix, string pattern)
    {
        ValidateMatrix(matrix);
        ValidatePattern(pattern);
        int index = 0;
        while (index < matrix.Cells.Length)
        {
            if (!CellMatches(matrix.Cells[index], pattern[index])) return false;
            index++;
        }
        return true;
    }

    internal static int GetCellIndex(
        TopologicalLocation row, TopologicalLocation column)
    {
        int rowIndex = GetLocationIndex(row);
        int columnIndex = GetLocationIndex(column);
        return (rowIndex * 3) + columnIndex;
    }

    internal static int GetLocationIndex(TopologicalLocation location)
    {
        switch (location)
        {
            case TopologicalLocation.Interior:
                return 0;
            case TopologicalLocation.Boundary:
                return 1;
            case TopologicalLocation.Exterior:
                return 2;
            default:
                throw new InvalidGeometryException(
                    "Relation matrix location is not supported.");
        }
    }

    internal static char GetDimensionSymbol(RelationDimension dimension)
    {
        switch (dimension)
        {
            case RelationDimension.False:
                return 'F';
            case RelationDimension.Point:
                return '0';
            case RelationDimension.Curve:
                return '1';
            case RelationDimension.Surface:
                return '2';
            default:
                throw new InvalidGeometryException(
                    "Relation matrix cell dimension is not supported.");
        }
    }

    internal static bool CellMatches(
        RelationDimension actualDimension, char requiredSymbol)
    {
        ValidateRelationDimension(actualDimension);
        switch (requiredSymbol)
        {
            case '*':
                return true;
            case 'T':
                return actualDimension != RelationDimension.False;
            case 'F':
                return actualDimension == RelationDimension.False;
            case '0':
                return actualDimension == RelationDimension.Point;
            case '1':
                return actualDimension == RelationDimension.Curve;
            case '2':
                return actualDimension == RelationDimension.Surface;
            default:
                throw new InvalidGeometryException(
                    "Relation pattern contains an unsupported symbol.");
        }
    }

    internal static void ValidatePattern(string pattern)
    {
        if (pattern == null)
        {
            throw new InvalidGeometryException("Relation pattern is null.");
        }
        if (pattern.Length != 9)
        {
            throw new InvalidGeometryException(
                "A relation pattern must contain exactly nine symbols.");
        }

        int index = 0;
        while (index < pattern.Length)
        {
            char symbol = pattern[index];
            if (symbol != '*' && symbol != 'T' && symbol != 'F' &&
                symbol != '0' && symbol != '1' && symbol != '2')
            {
                throw new InvalidGeometryException(
                    "Relation pattern contains an unsupported symbol.");
            }
            index++;
        }
    }

    internal static void ValidateRelationDimension(RelationDimension dimension)
    {
        switch (dimension)
        {
            case RelationDimension.False:
            case RelationDimension.Point:
            case RelationDimension.Curve:
            case RelationDimension.Surface:
                return;
            default:
                throw new InvalidGeometryException(
                    "Relation matrix cell dimension is not supported.");
        }
    }

    internal static void ValidateTopologicalDimension(
        TopologicalDimension dimension, string description)
    {
        switch (dimension)
        {
            case TopologicalDimension.Empty:
            case TopologicalDimension.Point:
            case TopologicalDimension.Curve:
            case TopologicalDimension.Surface:
                return;
            default:
                throw new InvalidGeometryException(
                    description + " is not supported.");
        }
    }
}
