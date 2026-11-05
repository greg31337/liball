namespace random;

internal class ArrayMatrix
{
    internal static double[] Copy(double[] source, int rows, int columns)
    {
        ValidateShape(source, rows, columns, "source");
        double[] destination = new double[source.Length];
        CopyElements(source, destination);
        return destination;
    }

    internal static void Copy(double[] source, int rows, int columns, double[] destination)
    {
        ValidateShape(source, rows, columns, "source");
        ValidateShape(destination, rows, columns, "destination");
        if (source == destination)
        {
            return;
        }

        CopyElements(source, destination);
    }

    internal static bool IsSymmetric(double[] matrix, int size)
    {
        return IsSymmetric(matrix, size, 0.0);
    }

    internal static bool IsSymmetric(double[] matrix, int size, double tolerance)
    {
        ValidateShape(matrix, size, size, "matrix");
        Guard.Argument(double.IsFinite(tolerance) && tolerance >= 0.0, "tolerance", "The symmetry tolerance must be finite and nonnegative.");
        int row = 0;
        while (row < size)
        {
            int diagonalIndex = row * size + row;
            if (!double.IsFinite(matrix[diagonalIndex]))
            {
                return false;
            }

            int column = 0;
            while (column < row)
            {
                double first = matrix[row * size + column];
                double second = matrix[column * size + row];
                if (!double.IsFinite(first) || !double.IsFinite(second) || Absolute(first - second) > tolerance)
                {
                    return false;
                }

                column++;
            }

            row++;
        }

        return true;
    }

    internal static double[] Cholesky(double[] matrix, int size)
    {
        return Cholesky(matrix, size, "matrix");
    }

    internal static double[] Cholesky(double[] matrix, int size, string argumentName)
    {
        int elementCount = ValidateShape(matrix, size, size, argumentName);
        ValidateFinite(matrix, argumentName);
        if (!IsSymmetric(matrix, size))
        {
            throw RandomArgumentException.Create(argumentName, "The matrix must be symmetric.");
        }

        double[] result = new double[elementCount];
        CholeskyElements(matrix, size, result, argumentName);
        return result;
    }

    internal static void Cholesky(double[] matrix, int size, double[] destination)
    {
        ValidateShape(matrix, size, size, "matrix");
        ValidateShape(destination, size, size, "destination");
        double[] result = Cholesky(matrix, size, "matrix");
        CopyElements(result, destination);
    }

    internal static void CholeskyElements(double[] matrix, int size, double[] result, string argumentName)
    {
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column <= row)
            {
                double sum = matrix[row * size + column];
                int offset = 0;
                while (offset < column)
                {
                    sum -= result[row * size + offset] * result[column * size + offset];
                    offset++;
                }

                if (row == column)
                {
                    if (!(sum > 0.0) || !double.IsFinite(sum))
                    {
                        throw RandomArgumentException.Create(argumentName, "The matrix must be positive definite.");
                    }

                    result[row * size + column] = PortableMath.Sqrt(sum);
                }
                else
                {
                    result[row * size + column] = sum / result[column * size + column];
                }

                column++;
            }

            row++;
        }
    }

    internal static double[] SolveLowerTriangular(double[] lower, int size, double[] rightHandSide)
    {
        ValidateShape(lower, size, size, "lower");
        ValidateVector(rightHandSide, size, "rightHandSide");
        double[] destination = new double[size];
        SolveLowerTriangular(lower, size, rightHandSide, destination);
        return destination;
    }

    internal static void SolveLowerTriangular(double[] lower, int size, double[] rightHandSide, double[] destination)
    {
        ValidateShape(lower, size, size, "lower");
        ValidateVector(rightHandSide, size, "rightHandSide");
        ValidateVector(destination, size, "destination");
        ValidateFinite(lower, "lower");
        ValidateFinite(rightHandSide, "rightHandSide");
        double[] result = new double[size];
        int row = 0;
        while (row < size)
        {
            double sum = rightHandSide[row];
            int column = 0;
            while (column < row)
            {
                sum -= lower[row * size + column] * result[column];
                column++;
            }

            double diagonal = lower[row * size + row];
            if (diagonal == 0.0)
            {
                throw RandomArgumentException.Create("lower", "The triangular matrix must have a nonzero diagonal.");
            }

            result[row] = sum / diagonal;
            row++;
        }

        CopyElements(result, destination);
    }

    internal static double[] SolveUpperTriangular(double[] upper, int size, double[] rightHandSide)
    {
        ValidateShape(upper, size, size, "upper");
        ValidateVector(rightHandSide, size, "rightHandSide");
        double[] destination = new double[size];
        SolveUpperTriangular(upper, size, rightHandSide, destination);
        return destination;
    }

    internal static void SolveUpperTriangular(double[] upper, int size, double[] rightHandSide, double[] destination)
    {
        ValidateShape(upper, size, size, "upper");
        ValidateVector(rightHandSide, size, "rightHandSide");
        ValidateVector(destination, size, "destination");
        ValidateFinite(upper, "upper");
        ValidateFinite(rightHandSide, "rightHandSide");
        double[] result = new double[size];
        int row = size - 1;
        while (row >= 0)
        {
            double sum = rightHandSide[row];
            int column = row + 1;
            while (column < size)
            {
                sum -= upper[row * size + column] * result[column];
                column++;
            }

            double diagonal = upper[row * size + row];
            if (diagonal == 0.0)
            {
                throw RandomArgumentException.Create("upper", "The triangular matrix must have a nonzero diagonal.");
            }

            result[row] = sum / diagonal;
            row--;
        }

        CopyElements(result, destination);
    }

    internal static double[] Multiply(double[] left, int leftRows, int leftColumns, double[] right, int rightRows, int rightColumns)
    {
        ValidateMultiplication(left, leftRows, leftColumns, right, rightRows, rightColumns);
        int resultLength = Guard.MatrixElementCount(leftRows, rightColumns, "leftRows", "rightColumns");
        double[] destination = new double[resultLength];
        Multiply(left, leftRows, leftColumns, right, rightRows, rightColumns, destination);
        return destination;
    }

    internal static void Multiply(double[] left, int leftRows, int leftColumns, double[] right, int rightRows, int rightColumns, double[] destination)
    {
        ValidateMultiplication(left, leftRows, leftColumns, right, rightRows, rightColumns);
        ValidateShape(destination, leftRows, rightColumns, "destination");
        double[] result = new double[destination.Length];
        int row = 0;
        while (row < leftRows)
        {
            int column = 0;
            while (column < rightColumns)
            {
                double sum = 0.0;
                int shared = 0;
                while (shared < leftColumns)
                {
                    sum += left[row * leftColumns + shared] * right[shared * rightColumns + column];
                    shared++;
                }

                result[row * rightColumns + column] = sum;
                column++;
            }

            row++;
        }

        CopyElements(result, destination);
    }

    internal static double[] Invert(double[] matrix, int size)
    {
        int elementCount = ValidateShape(matrix, size, size, "matrix");
        double[] destination = new double[elementCount];
        Invert(matrix, size, destination);
        return destination;
    }

    internal static void Invert(double[] matrix, int size, double[] destination)
    {
        int elementCount = ValidateShape(matrix, size, size, "matrix");
        ValidateShape(destination, size, size, "destination");
        ValidateFinite(matrix, "matrix");
        double[] working = new double[elementCount];
        double[] result = new double[elementCount];
        CopyElements(matrix, working);
        int diagonal = 0;
        while (diagonal < size)
        {
            result[diagonal * size + diagonal] = 1.0;
            diagonal++;
        }

        int pivotColumn = 0;
        while (pivotColumn < size)
        {
            int pivotRow = pivotColumn;
            double pivotMagnitude = Absolute(working[pivotColumn * size + pivotColumn]);
            int candidateRow = pivotColumn + 1;
            while (candidateRow < size)
            {
                double candidateMagnitude = Absolute(working[candidateRow * size + pivotColumn]);
                if (candidateMagnitude > pivotMagnitude)
                {
                    pivotMagnitude = candidateMagnitude;
                    pivotRow = candidateRow;
                }

                candidateRow++;
            }

            if (pivotMagnitude == 0.0)
            {
                throw RandomArgumentException.Create("matrix", "The matrix must be invertible.");
            }

            if (pivotRow != pivotColumn)
            {
                SwapRows(working, size, pivotRow, pivotColumn);
                SwapRows(result, size, pivotRow, pivotColumn);
            }

            double pivot = working[pivotColumn * size + pivotColumn];
            int column = 0;
            while (column < size)
            {
                working[pivotColumn * size + column] /= pivot;
                result[pivotColumn * size + column] /= pivot;
                column++;
            }

            int row = 0;
            while (row < size)
            {
                if (row != pivotColumn)
                {
                    double factor = working[row * size + pivotColumn];
                    column = 0;
                    while (column < size)
                    {
                        working[row * size + column] -= factor * working[pivotColumn * size + column];
                        result[row * size + column] -= factor * result[pivotColumn * size + column];
                        column++;
                    }
                }

                row++;
            }

            pivotColumn++;
        }

        CopyElements(result, destination);
    }

    internal static int ValidateShape(double[] matrix, int rows, int columns, string argumentName)
    {
        Guard.NotNull(matrix, argumentName);
        Guard.MatrixShape(matrix.Length, rows, columns, argumentName, "rows", "columns");
        return matrix.Length;
    }

    internal static void ValidateVector(double[] vector, int size, string argumentName)
    {
        Guard.NotNull(vector, argumentName);
        Guard.PositiveCount(size, "size");
        Guard.ExactSpanLength(vector.Length, size, argumentName);
    }

    internal static void ValidateFinite(double[] values, string argumentName)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!double.IsFinite(values[index]))
            {
                throw RandomArgumentException.Create(argumentName, "Matrix and vector elements must be finite.");
            }

            index++;
        }
    }

    internal static void ValidateMultiplication(double[] left, int leftRows, int leftColumns, double[] right, int rightRows, int rightColumns)
    {
        ValidateShape(left, leftRows, leftColumns, "left");
        ValidateShape(right, rightRows, rightColumns, "right");
        Guard.Argument(leftColumns == rightRows, "rightRows", "The inner matrix dimensions must match.");
        ValidateFinite(left, "left");
        ValidateFinite(right, "right");
    }

    internal static void CopyElements(double[] source, double[] destination)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[index] = source[index];
            index++;
        }
    }

    internal static void SwapRows(double[] matrix, int columns, int firstRow, int secondRow)
    {
        int column = 0;
        while (column < columns)
        {
            int firstIndex = firstRow * columns + column;
            int secondIndex = secondRow * columns + column;
            double temporary = matrix[firstIndex];
            matrix[firstIndex] = matrix[secondIndex];
            matrix[secondIndex] = temporary;
            column++;
        }
    }

    internal static double Absolute(double value)
    {
        return value < 0.0 ? -value : value;
    }
}
