namespace icc;

internal static class MatrixMath
{
    internal const double MachineEpsilon = 2.2204460492503131e-16;

    internal static double[] MultiplyMatrices(double[] left, int leftRows, int leftColumns, double[] right, int rightColumns)
    {
        ValidateMatrix(left, leftRows, leftColumns, "left matrix");
        ValidateMatrix(right, leftColumns, rightColumns, "right matrix");
        ValidateResultSize(leftRows, rightColumns);
        double[] result = new double[leftRows * rightColumns];
        int row = 0;
        while (row < leftRows)
        {
            int column = 0;
            while (column < rightColumns)
            {
                double sum = 0.0;
                double compensation = 0.0;
                int shared = 0;
                while (shared < leftColumns)
                {
                    double product = left[(row * leftColumns) + shared] * right[(shared * rightColumns) + column];
                    if (!System.Double.IsFinite(product))
                    {
                        throw new IccTransformException("Matrix multiplication produced a component outside the finite double range.");
                    }

                    double adjusted = product - compensation;
                    double next = sum + adjusted;
                    if (!System.Double.IsFinite(next))
                    {
                        throw new IccTransformException("Matrix multiplication produced a component outside the finite double range.");
                    }

                    compensation = (next - sum) - adjusted;
                    sum = next;
                    shared = shared + 1;
                }

                result[(row * rightColumns) + column] = sum;
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    internal static double[] ComposeSquareMatrices(double[] first, double[] second, int size)
    {
        ValidateMatrix(first, size, size, "first matrix");
        ValidateMatrix(second, size, size, "second matrix");
        return MultiplyMatrices(second, size, size, first, size);
    }

    internal static double CalculateDeterminant(double[] matrix, int size)
    {
        ValidateMatrix(matrix, size, size, "matrix");
        double[] work = CopyValues(matrix);
        double[] scales = CalculateRowScales(work, size);
        double determinant = 1.0;
        int sign = 1;
        int column = 0;
        while (column < size)
        {
            int pivotRow = SelectPivotRow(work, scales, size, column);
            if (pivotRow < 0 || work[(pivotRow * size) + column] == 0.0)
            {
                return 0.0;
            }

            if (pivotRow != column)
            {
                SwapRows(work, size, pivotRow, column);
                SwapScaleValues(scales, pivotRow, column);
                sign = -sign;
            }

            double pivot = work[(column * size) + column];
            determinant = determinant * pivot;
            if (!System.Double.IsFinite(determinant))
            {
                throw new IccTransformException("The matrix determinant is outside the finite double range.");
            }

            int row = column + 1;
            while (row < size)
            {
                double factor = work[(row * size) + column] / pivot;
                work[(row * size) + column] = 0.0;
                int trailingColumn = column + 1;
                while (trailingColumn < size)
                {
                    int index = (row * size) + trailingColumn;
                    work[index] = work[index] - (factor * work[(column * size) + trailingColumn]);
                    if (!System.Double.IsFinite(work[index]))
                    {
                        throw new IccTransformException("Matrix determinant evaluation produced a value outside the finite double range.");
                    }

                    trailingColumn = trailingColumn + 1;
                }

                row = row + 1;
            }

            column = column + 1;
        }

        return determinant * sign;
    }

    internal static double[] InvertMatrix(double[] matrix, int size)
    {
        ValidateMatrix(matrix, size, size, "matrix");
        double[] work = CopyValues(matrix);
        double[] inverse = CreateIdentityMatrix(size);
        double[] scales = CalculateRowScales(work, size);
        int column = 0;
        while (column < size)
        {
            int pivotRow = SelectPivotRow(work, scales, size, column);
            if (pivotRow < 0 || IsSingularPivot(work, scales, size, pivotRow, column))
            {
                throw new IccTransformException("The matrix is singular or numerically singular and cannot be inverted.");
            }

            if (pivotRow != column)
            {
                SwapRows(work, size, pivotRow, column);
                SwapRows(inverse, size, pivotRow, column);
                SwapScaleValues(scales, pivotRow, column);
            }

            double pivot = work[(column * size) + column];
            int pivotColumn = 0;
            while (pivotColumn < size)
            {
                int pivotIndex = (column * size) + pivotColumn;
                work[pivotIndex] = work[pivotIndex] / pivot;
                inverse[pivotIndex] = inverse[pivotIndex] / pivot;
                ValidateFiniteCalculation(work[pivotIndex], "Matrix inversion");
                ValidateFiniteCalculation(inverse[pivotIndex], "Matrix inversion");
                pivotColumn = pivotColumn + 1;
            }

            int row = 0;
            while (row < size)
            {
                if (row != column)
                {
                    double factor = work[(row * size) + column];
                    int eliminationColumn = 0;
                    while (eliminationColumn < size)
                    {
                        int index = (row * size) + eliminationColumn;
                        int pivotIndex = (column * size) + eliminationColumn;
                        work[index] = work[index] - (factor * work[pivotIndex]);
                        inverse[index] = inverse[index] - (factor * inverse[pivotIndex]);
                        ValidateFiniteCalculation(work[index], "Matrix inversion");
                        ValidateFiniteCalculation(inverse[index], "Matrix inversion");
                        eliminationColumn = eliminationColumn + 1;
                    }
                }

                row = row + 1;
            }

            column = column + 1;
        }

        return inverse;
    }

    internal static double[] ApplyMatrixToVector(double[] matrix, int rows, int columns, double[] vector)
    {
        ValidateMatrix(matrix, rows, columns, "matrix");
        ValidateVector(vector, columns);
        double[] result = new double[rows];
        int row = 0;
        while (row < rows)
        {
            double sum = 0.0;
            double compensation = 0.0;
            int column = 0;
            while (column < columns)
            {
                double product = matrix[(row * columns) + column] * vector[column];
                if (!System.Double.IsFinite(product))
                {
                    throw new IccTransformException("Matrix-vector application produced a component outside the finite double range.");
                }

                double adjusted = product - compensation;
                double next = sum + adjusted;
                if (!System.Double.IsFinite(next))
                {
                    throw new IccTransformException("Matrix-vector application produced a component outside the finite double range.");
                }

                compensation = (next - sum) - adjusted;
                sum = next;
                column = column + 1;
            }

            result[row] = sum;
            row = row + 1;
        }

        return result;
    }

    internal static double[] MultiplyMatrices3x3(double[] left, double[] right)
    {
        return MultiplyMatrices(left, 3, 3, right, 3);
    }

    internal static double[] ComposeMatrices3x3(double[] first, double[] second)
    {
        return ComposeSquareMatrices(first, second, 3);
    }

    internal static double CalculateDeterminant3x3(double[] matrix)
    {
        return CalculateDeterminant(matrix, 3);
    }

    internal static double[] InvertMatrix3x3(double[] matrix)
    {
        return InvertMatrix(matrix, 3);
    }

    internal static double[] ApplyMatrix3x3(double[] matrix, double[] vector)
    {
        return ApplyMatrixToVector(matrix, 3, 3, vector);
    }

    internal static double[] CopyValues(double[] values)
    {
        double[] copy = new double[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static double[] CreateIdentityMatrix(int size)
    {
        ValidateDimension(size, "Matrix size");
        ValidateResultSize(size, size);
        double[] identity = new double[size * size];
        int index = 0;
        while (index < size)
        {
            identity[(index * size) + index] = 1.0;
            index = index + 1;
        }

        return identity;
    }

    internal static double[] CalculateRowScales(double[] matrix, int size)
    {
        double[] scales = new double[size];
        int row = 0;
        while (row < size)
        {
            double maximum = 0.0;
            int column = 0;
            while (column < size)
            {
                double magnitude = System.Math.Abs(matrix[(row * size) + column]);
                if (magnitude > maximum)
                {
                    maximum = magnitude;
                }

                column = column + 1;
            }

            scales[row] = maximum;
            row = row + 1;
        }

        return scales;
    }

    internal static int SelectPivotRow(double[] matrix, double[] scales, int size, int column)
    {
        int selected = -1;
        double bestRatio = -1.0;
        int row = column;
        while (row < size)
        {
            if (scales[row] > 0.0)
            {
                double ratio = System.Math.Abs(matrix[(row * size) + column]) / scales[row];
                if (ratio > bestRatio)
                {
                    bestRatio = ratio;
                    selected = row;
                }
            }

            row = row + 1;
        }

        return selected;
    }

    internal static bool IsSingularPivot(double[] matrix, double[] scales, int size, int row, int column)
    {
        if (scales[row] == 0.0)
        {
            return true;
        }

        double ratio = System.Math.Abs(matrix[(row * size) + column]) / scales[row];
        return ratio <= size * MachineEpsilon;
    }

    internal static void SwapRows(double[] matrix, int columns, int firstRow, int secondRow)
    {
        int column = 0;
        while (column < columns)
        {
            int firstIndex = (firstRow * columns) + column;
            int secondIndex = (secondRow * columns) + column;
            double temporary = matrix[firstIndex];
            matrix[firstIndex] = matrix[secondIndex];
            matrix[secondIndex] = temporary;
            column = column + 1;
        }
    }

    internal static void SwapScaleValues(double[] scales, int firstRow, int secondRow)
    {
        double temporary = scales[firstRow];
        scales[firstRow] = scales[secondRow];
        scales[secondRow] = temporary;
    }

    internal static void ValidateMatrix(double[] matrix, int rows, int columns, string description)
    {
        ValidateDimension(rows, "Matrix row count");
        ValidateDimension(columns, "Matrix column count");
        long expectedLength = (long)rows * columns;
        if (expectedLength > System.Int32.MaxValue || matrix == null || matrix.Length != expectedLength)
        {
            throw new IccTransformException($"The {description} length does not match its declared dimensions.");
        }

        int index = 0;
        while (index < matrix.Length)
        {
            if (!System.Double.IsFinite(matrix[index]))
            {
                throw new IccTransformException($"The {description} component {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateVector(double[] vector, int expectedLength)
    {
        if (vector == null || vector.Length != expectedLength)
        {
            throw new IccTransformException("The vector length must match the matrix column count.");
        }

        int index = 0;
        while (index < vector.Length)
        {
            if (!System.Double.IsFinite(vector[index]))
            {
                throw new IccTransformException($"Vector component {index} must be finite.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateDimension(int dimension, string description)
    {
        if (dimension <= 0)
        {
            throw new IccTransformException($"{description} must be positive.");
        }
    }

    internal static void ValidateResultSize(int rows, int columns)
    {
        long resultLength = (long)rows * columns;
        if (resultLength > System.Int32.MaxValue)
        {
            throw new IccTransformException("The matrix result is too large for a managed array.");
        }
    }

    internal static void ValidateFiniteCalculation(double value, string description)
    {
        if (!System.Double.IsFinite(value))
        {
            throw new IccTransformException($"{description} produced a value outside the finite double range.");
        }
    }
}
