using System;

namespace fake;

/// <summary>
/// Calculates, appends, and validates a right-anchored weighted modulo-11 check digit.
/// </summary>
public class Modulo11Checksum : IChecksum
{
    internal int[] Weights;
    internal int MaximumCount;
    internal int MaximumOutputSize;

    internal Modulo11Checksum(int[] weights, int maximumCount, int maximumOutputSize)
    {
        Weights = weights;
        MaximumCount = maximumCount;
        MaximumOutputSize = maximumOutputSize;
    }

    /// <summary>Calculates a weighted modulo-11 check value.</summary>
    /// <remarks>The first weight applies to the rightmost payload digit and the weights repeat toward the left. A result of ten is represented by <c>X</c> when appended.</remarks>
    /// <param name="value">The ASCII decimal payload.</param>
    /// <returns>The check value from zero through ten.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the payload contains a non-digit.</exception>
    public int Calculate(string value)
    {
        Guard.NotNull(value, "value");
        return CalculateFromValue(value.AsSpan());
    }

    /// <summary>Calculates a weighted modulo-11 check value for a span.</summary>
    /// <param name="value">The ASCII decimal payload.</param>
    /// <returns>The check value from zero through ten.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the payload contains a non-digit.</exception>
    public int CalculateFromValue(ReadOnlySpan<char> value)
    {
        return ChecksumAlgorithms.Modulo(value, Weights.AsSpan(), 11, MaximumCount);
    }

    /// <summary>Appends a weighted modulo-11 check character.</summary>
    /// <param name="value">The ASCII decimal payload.</param>
    /// <returns>The payload followed by a digit or <c>X</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the payload contains a non-digit.</exception>
    public string Append(string value)
    {
        Guard.NotNull(value, "value");
        int length = Guard.OutputSize((long)value.Length + 1L, MaximumOutputSize, "value");
        char[] result = new char[length];
        AppendFromValueAndDestination(value.AsSpan(), new Span<char>(result));
        return new string(result);
    }

    /// <summary>Writes a payload and weighted modulo-11 check character to a destination.</summary>
    /// <param name="value">The ASCII decimal payload.</param>
    /// <param name="destination">The destination receiving the payload and check character.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when input is malformed or the destination is too small.</exception>
    public int AppendFromValueAndDestination(ReadOnlySpan<char> value, Span<char> destination)
    {
        int check = CalculateFromValue(value);
        return ChecksumAlgorithms.AppendDigit(value, destination, check, MaximumOutputSize);
    }

    /// <summary>Validates a value containing a weighted modulo-11 check character.</summary>
    /// <param name="value">The complete value ending in a digit or <c>X</c>.</param>
    /// <returns>True when the value is checksum-valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public bool Validate(string value)
    {
        Guard.NotNull(value, "value");
        return ValidateFromValue(value.AsSpan());
    }

    /// <summary>Validates a span containing a weighted modulo-11 check character.</summary>
    /// <param name="value">The complete value ending in a digit or <c>X</c>.</param>
    /// <returns>True when the value is checksum-valid.</returns>
    public bool ValidateFromValue(ReadOnlySpan<char> value)
    {
        return ChecksumAlgorithms.IsModulo11(value, Weights.AsSpan(), MaximumCount);
    }
}
