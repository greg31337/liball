using System;

namespace email;

internal static class EmailLimitValidation
{
    internal static int RequirePositiveWithValueAndParameterNameUsingInt32AndString(int value, string parameterName)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }

    internal static long RequirePositiveWithValueAndParameterNameUsingInt64AndString(long value, string parameterName)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }

    internal static long RequireNonNegative(long value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }
}
