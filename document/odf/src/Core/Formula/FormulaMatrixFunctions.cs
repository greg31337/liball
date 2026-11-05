namespace odf;

internal class FormulaMatrixData
{
    internal double[] Values = [];
    internal int Rows;
    internal int Columns;
    internal OdfFormulaResult? Error;
}

internal static class FormulaMatrixFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.MDETERM)
        {
            return Determinant(arguments[0]);
        }
        if (name == OdfFormulaNames.MINVERSE)
        {
            return Inverse(arguments[0]);
        }
        if (name == OdfFormulaNames.MMULT)
        {
            return Multiply(arguments[0], arguments[1]);
        }
        if (name == OdfFormulaNames.MUNIT)
        {
            return Unit(arguments[0]);
        }
        if (name == OdfFormulaNames.TRANSPOSE)
        {
            return Transpose(arguments[0]);
        }
        return FormulaValueModel.CreateError(
            OdfFormulaError.UnsupportedFunction);
    }

    static OdfFormulaResult Determinant(OdfFormulaResult value)
    {
        FormulaMatrixData matrix = ReadNumericMatrix(value);
        if (matrix.Error != null)
        {
            return matrix.Error;
        }
        if (matrix.Rows != matrix.Columns)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double[] work = CopyNumbers(matrix.Values);
        double determinant = 1;
        int sign = 1;
        int pivotColumn = 0;
        while (pivotColumn < matrix.Rows)
        {
            int pivotRow = FindPivot(work, matrix.Rows, pivotColumn);
            if (global::System.Math.Abs(
                work[pivotRow * matrix.Rows + pivotColumn]) < 0.000000000000001)
            {
                return FormulaValueModel.CreateNumber(0);
            }
            if (pivotRow != pivotColumn)
            {
                SwapRows(work, matrix.Rows, pivotRow, pivotColumn);
                sign = -sign;
            }
            double pivot = work[pivotColumn * matrix.Rows + pivotColumn];
            determinant = determinant * pivot;
            int row = pivotColumn + 1;
            while (row < matrix.Rows)
            {
                double factor = work[row * matrix.Rows + pivotColumn] / pivot;
                int column = pivotColumn + 1;
                while (column < matrix.Rows)
                {
                    int index = row * matrix.Rows + column;
                    work[index] = work[index]
                        - factor * work[pivotColumn * matrix.Rows + column];
                    column = column + 1;
                }
                row = row + 1;
            }
            pivotColumn = pivotColumn + 1;
        }
        return FormulaValueModel.CreateNumber(determinant * sign);
    }

    static OdfFormulaResult Inverse(OdfFormulaResult value)
    {
        FormulaMatrixData matrix = ReadNumericMatrix(value);
        if (matrix.Error != null)
        {
            return matrix.Error;
        }
        if (matrix.Rows != matrix.Columns)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int size = matrix.Rows;
        int width = size * 2;
        double[] work = new double[size * width];
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                work[row * width + column] =
                    matrix.Values[row * size + column];
                work[row * width + size + column] = row == column ? 1 : 0;
                column = column + 1;
            }
            row = row + 1;
        }
        int pivotColumn = 0;
        while (pivotColumn < size)
        {
            int pivotRow = FindPivot(work, width, pivotColumn);
            double pivot = work[pivotRow * width + pivotColumn];
            if (global::System.Math.Abs(pivot) < 0.000000000000001)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            if (pivotRow != pivotColumn)
            {
                SwapRows(work, width, pivotRow, pivotColumn);
            }
            pivot = work[pivotColumn * width + pivotColumn];
            int column = 0;
            while (column < width)
            {
                work[pivotColumn * width + column] =
                    work[pivotColumn * width + column] / pivot;
                column = column + 1;
            }
            row = 0;
            while (row < size)
            {
                if (row != pivotColumn)
                {
                    double factor = work[row * width + pivotColumn];
                    column = 0;
                    while (column < width)
                    {
                        work[row * width + column] =
                            work[row * width + column]
                            - factor * work[pivotColumn * width + column];
                        column = column + 1;
                    }
                }
                row = row + 1;
            }
            pivotColumn = pivotColumn + 1;
        }
        OdfFormulaResult[] result = new OdfFormulaResult[size * size];
        row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                result[row * size + column] = FormulaValueModel.CreateNumber(
                    work[row * width + size + column]);
                column = column + 1;
            }
            row = row + 1;
        }
        return FormulaValueModel.CreateArray(result, size, size);
    }

    static OdfFormulaResult Multiply(
        OdfFormulaResult leftValue,
        OdfFormulaResult rightValue)
    {
        FormulaMatrixData left = ReadNumericMatrix(leftValue);
        FormulaMatrixData right = ReadNumericMatrix(rightValue);
        if (left.Error != null)
        {
            return left.Error;
        }
        if (right.Error != null)
        {
            return right.Error;
        }
        if (left.Columns != right.Rows)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult[] values =
            new OdfFormulaResult[left.Rows * right.Columns];
        int row = 0;
        while (row < left.Rows)
        {
            int column = 0;
            while (column < right.Columns)
            {
                double sum = 0;
                int shared = 0;
                while (shared < left.Columns)
                {
                    sum = sum
                        + left.Values[row * left.Columns + shared]
                        * right.Values[shared * right.Columns + column];
                    shared = shared + 1;
                }
                values[row * right.Columns + column] =
                    FormulaValueModel.CreateNumber(sum);
                column = column + 1;
            }
            row = row + 1;
        }
        return FormulaValueModel.CreateArray(
            values,
            left.Rows,
            right.Columns);
    }

    static OdfFormulaResult Unit(OdfFormulaResult value)
    {
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(value);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        int size = (int)global::System.Math.Truncate(number.NumericValue);
        if (size <= 0 || size != number.NumericValue)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult[] values = new OdfFormulaResult[size * size];
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                values[row * size + column] =
                    FormulaValueModel.CreateNumber(row == column ? 1 : 0);
                column = column + 1;
            }
            row = row + 1;
        }
        return FormulaValueModel.CreateArray(values, size, size);
    }

    static OdfFormulaResult Transpose(OdfFormulaResult value)
    {
        if (!HasMatrixShape(value))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult[] values = new OdfFormulaResult[value.Values.Length];
        int row = 0;
        while (row < value.RowCount)
        {
            int column = 0;
            while (column < value.ColumnCount)
            {
                values[column * value.RowCount + row] =
                    value.Values[row * value.ColumnCount + column];
                column = column + 1;
            }
            row = row + 1;
        }
        return FormulaValueModel.CreateArray(
            values,
            value.ColumnCount,
            value.RowCount);
    }

    static FormulaMatrixData ReadNumericMatrix(OdfFormulaResult value)
    {
        FormulaMatrixData result = new FormulaMatrixData();
        if (value == null)
        {
            result.Error = FormulaValueModel.CreateError(
                OdfFormulaError.Value);
            return result;
        }
        if (value.Error != OdfFormulaError.None)
        {
            result.Error = value;
            return result;
        }
        if (!HasMatrixShape(value))
        {
            result.Error = FormulaValueModel.CreateError(
                OdfFormulaError.Value);
            return result;
        }
        result.Rows = value.RowCount;
        result.Columns = value.ColumnCount;
        result.Values = new double[value.Values.Length];
        int index = 0;
        while (index < value.Values.Length)
        {
            OdfFormulaResult number = FormulaValueModel.ConvertToNumber(
                value.Values[index]);
            if (number.Error != OdfFormulaError.None)
            {
                result.Error = number;
                return result;
            }
            result.Values[index] = number.NumericValue;
            index = index + 1;
        }
        return result;
    }

    static bool HasMatrixShape(OdfFormulaResult value)
    {
        if (value == null)
        {
            return false;
        }
        return (value.Kind == OdfValueKind.Array
            || value.Kind == OdfValueKind.Reference)
            && value.RowCount > 0
            && value.ColumnCount > 0
            && value.Values.Length == value.RowCount * value.ColumnCount;
    }

    static int FindPivot(double[] values, int width, int column)
    {
        int rows = values.Length / width;
        int best = column;
        int row = column + 1;
        while (row < rows)
        {
            if (global::System.Math.Abs(values[row * width + column])
                > global::System.Math.Abs(values[best * width + column]))
            {
                best = row;
            }
            row = row + 1;
        }
        return best;
    }

    static void SwapRows(
        double[] values,
        int width,
        int first,
        int second)
    {
        int column = 0;
        while (column < width)
        {
            double temporary = values[first * width + column];
            values[first * width + column] = values[second * width + column];
            values[second * width + column] = temporary;
            column = column + 1;
        }
    }

    static double[] CopyNumbers(double[] source)
    {
        double[] result = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }
}
