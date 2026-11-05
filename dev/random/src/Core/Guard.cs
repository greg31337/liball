namespace random;

internal class Guard
{
    internal static void Argument(bool condition, string argumentName, string message)
    {
        if (!condition)
        {
            throw RandomArgumentException.Create(argumentName, message);
        }
    }

    internal static void NotNull(object? value, string argumentName)
    {
        if (value == null)
        {
            throw RandomArgumentException.Create(argumentName, "The value must not be null.");
        }
    }

    internal static void NotEmpty(string? value, string argumentName)
    {
        if (value == null)
        {
            throw RandomArgumentException.Create(argumentName, "The value must not be null.");
        }

        if (value.Length == 0)
        {
            throw RandomArgumentException.Create(argumentName, "The value must not be empty.");
        }
    }

    internal static void Positive(int value, string argumentName)
    {
        if (value <= 0)
        {
            throw RandomArgumentException.Create(argumentName, "The value must be positive.");
        }
    }

    internal static void NonNegative(int value, string argumentName)
    {
        if (value < 0)
        {
            throw RandomArgumentException.Create(argumentName, "The value must not be negative.");
        }
    }

    internal static void SpanLength(int actualLength, int requiredLength, string argumentName)
    {
        if (requiredLength < 0)
        {
            throw RandomArgumentException.Create(argumentName, "The required span length must not be negative.");
        }

        if (actualLength < requiredLength)
        {
            throw RandomArgumentException.Create(argumentName, "The destination span is too short.");
        }
    }

    internal static void ExactSpanLength(int actualLength, int requiredLength, string argumentName)
    {
        if (requiredLength < 0)
        {
            throw RandomArgumentException.Create(argumentName, "The required span length must not be negative.");
        }

        if (actualLength != requiredLength)
        {
            throw RandomArgumentException.Create(argumentName, "The destination span has an invalid length.");
        }
    }

    internal static void Finite(double value, string argumentName)
    {
        if (!double.IsFinite(value))
        {
            throw RandomArgumentException.Create(argumentName, "The value must be finite.");
        }
    }

    internal static void Finite(float value, string argumentName)
    {
        if (!float.IsFinite(value))
        {
            throw RandomArgumentException.Create(argumentName, "The value must be finite.");
        }
    }

    internal static void Probability(double value, string argumentName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw RandomArgumentException.Create(argumentName, "The probability must be finite and between zero and one inclusive.");
        }
    }

    internal static void Probability(float value, string argumentName)
    {
        if (!float.IsFinite(value) || value < 0.0f || value > 1.0f)
        {
            throw RandomArgumentException.Create(argumentName, "The probability must be finite and between zero and one inclusive.");
        }
    }

    internal static void Count(int value, string argumentName)
    {
        if (value < 0)
        {
            throw RandomArgumentException.Create(argumentName, "The count must not be negative.");
        }
    }

    internal static void PositiveCount(int value, string argumentName)
    {
        if (value <= 0)
        {
            throw RandomArgumentException.Create(argumentName, "The count must be positive.");
        }
    }

    internal static int MatrixElementCount(int rows, int columns, string rowsArgumentName, string columnsArgumentName)
    {
        PositiveCount(rows, rowsArgumentName);
        PositiveCount(columns, columnsArgumentName);

        if (columns > int.MaxValue / rows)
        {
            throw RandomArgumentException.Create(rowsArgumentName, "The matrix shape contains too many elements.");
        }

        return rows * columns;
    }

    internal static void MatrixShape(int actualLength, int rows, int columns, string argumentName, string rowsArgumentName, string columnsArgumentName)
    {
        int requiredLength = MatrixElementCount(rows, columns, rowsArgumentName, columnsArgumentName);
        if (actualLength != requiredLength)
        {
            throw RandomArgumentException.Create(argumentName, "The matrix storage length does not match its shape.");
        }
    }
}
