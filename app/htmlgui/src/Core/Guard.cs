using System;

namespace htmlgui;

internal static class Guard
{
    internal static void ArgumentNotNull(object? value, string argumentName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    internal static string ArgumentNotEmpty(string? value, string argumentName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("The value cannot be empty.", argumentName);
        }

        return value;
    }

    internal static string ArgumentNotWhiteSpace(string? value, string argumentName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }

        if (value.Trim().Length == 0)
        {
            throw new ArgumentException("The value cannot consist only of whitespace.", argumentName);
        }

        return value;
    }

    internal static int ArgumentNonNegative(int value, string argumentName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(argumentName, value, "The value cannot be negative.");
        }

        return value;
    }

    internal static int ArgumentPositive(int value, string argumentName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(argumentName, value, "The value must be positive.");
        }

        return value;
    }

    internal static int ArgumentInRange(int value, int minimum, int maximum, string argumentName)
    {
        if (minimum > maximum)
        {
            throw new ArgumentException("The minimum cannot exceed the maximum.", argumentName);
        }

        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(argumentName, value, "The value is outside the allowed range.");
        }

        return value;
    }

    internal static string ArgumentMaximumLength(string? value, int maximumLength, string argumentName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }

        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength", maximumLength, "The maximum length cannot be negative.");
        }

        if (value.Length > maximumLength)
        {
            throw new ArgumentException("The value exceeds the maximum length.", argumentName);
        }

        return value;
    }
}
