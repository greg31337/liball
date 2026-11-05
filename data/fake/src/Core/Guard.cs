using System;

namespace fake;

internal static class FakeLimits
{
    internal const int DefaultMaximumCount = 10000;
    internal const int DefaultMaximumOutputSize = 1000000;
}

internal static class Guard
{
    internal static void NotNull(object? value, string parameterName)
    {
        ValidateParameterName(parameterName);
        if (value == null)
        {
            throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null.");
        }
    }

    internal static string NotNullOrEmpty(string? value, string parameterName)
    {
        ValidateParameterName(parameterName);
        if (value == null)
        {
            throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null.");
        }
        if (value.Length == 0)
        {
            throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
        }
        return value;
    }

    internal static string NotNullOrWhiteSpace(string? value, string parameterName)
    {
        ValidateParameterName(parameterName);
        if (value == null)
        {
            throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} cannot be empty or whitespace.", parameterName);
        }
        return value;
    }

    internal static void NonNegative(int value, string parameterName)
    {
        ValidateParameterName(parameterName);
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be zero or greater.");
        }
    }

    internal static void Positive(int value, string parameterName)
    {
        ValidateParameterName(parameterName);
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be greater than zero.");
        }
    }

    internal static void Count(int value, int maximum, string parameterName)
    {
        ValidateParameterName(parameterName);
        NonNegative(maximum, "maximum");
        if (value < 0 || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be between 0 and {maximum}, inclusive.");
        }
    }

    internal static int OutputSize(long requestedSize, int maximumOutputSize, string parameterName)
    {
        ValidateParameterName(parameterName);
        NonNegative(maximumOutputSize, "maximumOutputSize");
        if (requestedSize < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, requestedSize, $"{parameterName} must be zero or greater.");
        }
        if (requestedSize > maximumOutputSize)
        {
            throw new ArgumentOutOfRangeException(parameterName, requestedSize, $"{parameterName} would produce {requestedSize} output units, exceeding the configured maximum of {maximumOutputSize}.");
        }
        return (int)requestedSize;
    }

    internal static int OutputSize(int count, int itemSize, int maximumOutputSize, string countParameterName, string itemSizeParameterName)
    {
        NonNegative(count, countParameterName);
        NonNegative(itemSize, itemSizeParameterName);
        long requestedSize = (long)count * (long)itemSize;
        return OutputSize(requestedSize, maximumOutputSize, countParameterName);
    }

    internal static void Exhausted(string operation, int attempts)
    {
        NotNullOrWhiteSpace(operation, "operation");
        Positive(attempts, "attempts");
        throw FakeException.Create($"Generation operation '{operation}' exhausted its retry limit after {attempts} attempts.");
    }

    static void ValidateParameterName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            throw new ArgumentException("Parameter names cannot be null, empty, or whitespace.", "parameterName");
        }
    }
}
