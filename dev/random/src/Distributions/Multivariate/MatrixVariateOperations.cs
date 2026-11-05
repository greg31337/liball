using System;

namespace random;

internal class MatrixVariateOperations
{
    internal static double[] ValidateCholeskyFactor(double[] factor, int dimension, string argumentName, string dimensionArgumentName)
    {
        Guard.NotNull(factor, argumentName);
        int elementCount = Guard.MatrixElementCount(dimension, dimension, dimensionArgumentName, dimensionArgumentName);
        Guard.ExactSpanLength(factor.Length, elementCount, argumentName);
        int row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column < dimension)
            {
                double value = factor[row * dimension + column];
                if (!double.IsFinite(value))
                {
                    throw RandomArgumentException.Create(argumentName, "Every factor element must be finite.");
                }

                if (column > row && value != 0.0)
                {
                    throw RandomArgumentException.Create(argumentName, "The factor must be lower triangular.");
                }

                if (column == row && !(value > 0.0))
                {
                    throw RandomArgumentException.Create(argumentName, "The factor must have a positive diagonal.");
                }

                column++;
            }

            row++;
        }

        return (double[])factor.Clone();
    }

    internal static double[] ValidateAndFactorScale(IRandomSource generator, double degreesOfFreedom, double[] scale, int dimension)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(degreesOfFreedom, "degreesOfFreedom");
        Guard.PositiveCount(dimension, "dimension");
        if (degreesOfFreedom <= (double)(dimension - 1))
        {
            throw RandomArgumentException.Create("degreesOfFreedom", "The degrees of freedom must be greater than dimension minus one.");
        }

        Guard.MatrixElementCount(dimension, dimension, "dimension", "dimension");
        return ArrayMatrix.Cholesky(scale, dimension, "scale");
    }

    internal static void MultiplyLower(double[] left, double[] right, int dimension, double[] destination)
    {
        int row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column < dimension)
            {
                double sum = 0.0;
                if (column <= row)
                {
                    int offset = column;
                    while (offset <= row)
                    {
                        sum += left[row * dimension + offset] * right[offset * dimension + column];
                        offset++;
                    }
                }

                destination[row * dimension + column] = sum;
                column++;
            }

            row++;
        }
    }

    internal static void SolveRightTransposeLower(double[] rightHandSide, double[] lower, int dimension, double[] destination)
    {
        int row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column < dimension)
            {
                double sum = rightHandSide[row * dimension + column];
                int offset = 0;
                while (offset < column)
                {
                    sum -= lower[column * dimension + offset] * destination[row * dimension + offset];
                    offset++;
                }

                destination[row * dimension + column] = sum / lower[column * dimension + column];
                column++;
            }

            row++;
        }
    }

    internal static void SymmetricProductLower(double[] lower, int dimension, double[] destination)
    {
        int row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column <= row)
            {
                double sum = 0.0;
                int offset = 0;
                while (offset <= column)
                {
                    sum += lower[row * dimension + offset] * lower[column * dimension + offset];
                    offset++;
                }

                destination[row * dimension + column] = sum;
                destination[column * dimension + row] = sum;
                column++;
            }

            row++;
        }
    }

    internal static void SymmetricProduct(double[] matrix, int dimension, double[] destination)
    {
        int row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column <= row)
            {
                double sum = 0.0;
                int offset = 0;
                while (offset < dimension)
                {
                    sum += matrix[row * dimension + offset] * matrix[column * dimension + offset];
                    offset++;
                }

                destination[row * dimension + column] = sum;
                destination[column * dimension + row] = sum;
                column++;
            }

            row++;
        }
    }

    internal static void TransformMatrixNormal(double[] rowFactor, double[] columnFactor, double[] standard, int rows, int columns, double[] intermediate, double[] destination)
    {
        int row = 0;
        while (row < rows)
        {
            int column = 0;
            while (column < columns)
            {
                double sum = 0.0;
                int offset = 0;
                while (offset <= row)
                {
                    sum += rowFactor[row * rows + offset] * standard[offset * columns + column];
                    offset++;
                }

                intermediate[row * columns + column] = sum;
                column++;
            }

            row++;
        }

        row = 0;
        while (row < rows)
        {
            int column = 0;
            while (column < columns)
            {
                double sum = 0.0;
                int offset = 0;
                while (offset <= column)
                {
                    sum += intermediate[row * columns + offset] * columnFactor[column * columns + offset];
                    offset++;
                }

                destination[row * columns + column] = sum;
                column++;
            }

            row++;
        }
    }

    internal static void Copy(double[] source, Span<double> destination)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[index] = source[index];
            index++;
        }
    }
}
