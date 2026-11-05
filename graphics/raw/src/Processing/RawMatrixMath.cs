namespace raw;

internal static class RawMatrixMath
{
    internal static double[] Identity()
    {
        return new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };
    }

    internal static double[] Multiply(double[] first, double[] second)
    {
        RequireMatrix(first, "The first matrix");
        RequireMatrix(second, "The second matrix");
        double[] result = new double[9];
        int row = 0;
        while (row < 3)
        {
            int column = 0;
            while (column < 3)
            {
                result[row * 3 + column] = first[row * 3] * second[column] + first[row * 3 + 1] * second[3 + column] + first[row * 3 + 2] * second[6 + column];
                column++;
            }
            row++;
        }
        return result;
    }

    internal static double[] MultiplyVector(double[]? matrix, double[] vector)
    {
        RequireMatrix(matrix, "The matrix");
        RequireVector(vector, "The vector");
        return new double[]
        {
            (matrix ?? throw new System.InvalidOperationException("A required value is unavailable."))[0] * vector[0] + matrix[1] * vector[1] + matrix[2] * vector[2],
            matrix[3] * vector[0] + matrix[4] * vector[1] + matrix[5] * vector[2],
            matrix[6] * vector[0] + matrix[7] * vector[1] + matrix[8] * vector[2]
        };
    }

    internal static double[] Invert(double[]? matrix)
    {
        RequireMatrix(matrix, "The matrix");
        double determinant = (matrix ?? throw new System.InvalidOperationException("A required value is unavailable."))[0] * (matrix[4] * matrix[8] - matrix[5] * matrix[7]) - matrix[1] * (matrix[3] * matrix[8] - matrix[5] * matrix[6]) + matrix[2] * (matrix[3] * matrix[7] - matrix[4] * matrix[6]);
        if (!RawModelValidation.IsFinite(determinant) || System.Math.Abs(determinant) < 0.000000000001)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "A color matrix is singular or numerically unstable.", -1);
        }
        double inverse = 1.0 / determinant;
        double[] result = new double[]
        {
            (matrix[4] * matrix[8] - matrix[5] * matrix[7]) * inverse,
            (matrix[2] * matrix[7] - matrix[1] * matrix[8]) * inverse,
            (matrix[1] * matrix[5] - matrix[2] * matrix[4]) * inverse,
            (matrix[5] * matrix[6] - matrix[3] * matrix[8]) * inverse,
            (matrix[0] * matrix[8] - matrix[2] * matrix[6]) * inverse,
            (matrix[2] * matrix[3] - matrix[0] * matrix[5]) * inverse,
            (matrix[3] * matrix[7] - matrix[4] * matrix[6]) * inverse,
            (matrix[1] * matrix[6] - matrix[0] * matrix[7]) * inverse,
            (matrix[0] * matrix[4] - matrix[1] * matrix[3]) * inverse
        };
        RequireMatrix(result, "The inverse matrix");
        return result;
    }

    internal static double[] Interpolate(double[] first, double[] second, double fraction)
    {
        RequireMatrix(first, "The first matrix");
        RequireMatrix(second, "The second matrix");
        if (!RawModelValidation.IsFinite(fraction) || fraction < 0.0 || fraction > 1.0)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "A matrix interpolation fraction must be between zero and one.", -1);
        }
        double[] result = new double[9];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = first[index] * (1.0 - fraction) + second[index] * fraction;
            index++;
        }
        return result;
    }

    internal static double[] InterpolateVector(double[] first, double[] second, double fraction)
    {
        RequireVector(first, "The first vector");
        RequireVector(second, "The second vector");
        if (!RawModelValidation.IsFinite(fraction) || fraction < 0.0 || fraction > 1.0)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "A vector interpolation fraction must be between zero and one.", -1);
        }
        return new double[]
        {
            first[0] * (1.0 - fraction) + second[0] * fraction,
            first[1] * (1.0 - fraction) + second[1] * fraction,
            first[2] * (1.0 - fraction) + second[2] * fraction
        };
    }

    internal static double[] Diagonal(double first, double second, double third)
    {
        if (!RawModelValidation.IsFinite(first) || !RawModelValidation.IsFinite(second) || !RawModelValidation.IsFinite(third))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "A diagonal color matrix requires finite values.", -1);
        }
        return new double[] { first, 0.0, 0.0, 0.0, second, 0.0, 0.0, 0.0, third };
    }

    internal static void RequireMatrix(double[]? matrix, string context)
    {
        if (matrix == null || matrix.Length != 9 || !RawBlackLevel.AllFinite(matrix))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, context + " must contain nine finite row-major values.", -1);
        }
    }

    internal static void RequireVector(double[] vector, string context)
    {
        if (vector == null || vector.Length != 3 || !RawBlackLevel.AllFinite(vector))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, context + " must contain three finite values.", -1);
        }
    }
}
