using System;

namespace fake;

/// <summary>
/// Calculates, appends, and validates ISO 7064 MOD 97-10 decimal check characters.
/// </summary>
public class Mod97Checksum : IChecksum
{
    internal int MaximumCount;
    internal int MaximumOutputSize;

    internal Mod97Checksum(int maximumCount, int maximumOutputSize)
    {
        MaximumCount = maximumCount;
        MaximumOutputSize = maximumOutputSize;
    }

    /// <summary>Calculates the two-digit MOD 97-10 check value for a decimal payload.</summary>
    /// <param name="value">The ASCII decimal payload before its check characters.</param>
    /// <returns>The check value from 02 through 98.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the payload contains a non-digit.</exception>
    public int Calculate(string value)
    {
        Guard.NotNull(value, "value");
        return CalculateFromValue(value.AsSpan());
    }

    /// <summary>Calculates the two-digit MOD 97-10 check value for a decimal span.</summary>
    /// <param name="value">The ASCII decimal payload before its check characters.</param>
    /// <returns>The check value from 02 through 98.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the payload contains a non-digit.</exception>
    public int CalculateFromValue(ReadOnlySpan<char> value)
    {
        return ChecksumAlgorithms.Mod97(value, MaximumCount);
    }

    /// <summary>Appends two MOD 97-10 check characters to a decimal payload.</summary>
    /// <param name="value">The ASCII decimal payload.</param>
    /// <returns>The payload followed by two decimal check characters.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the payload contains a non-digit.</exception>
    public string Append(string value)
    {
        Guard.NotNull(value, "value");
        int length = Guard.OutputSize((long)value.Length + 2L, MaximumOutputSize, "value");
        char[] result = new char[length];
        AppendFromValueAndDestination(value.AsSpan(), new Span<char>(result));
        return new string(result);
    }

    /// <summary>Writes a decimal payload and its two MOD 97-10 check characters to a destination.</summary>
    /// <param name="value">The ASCII decimal payload.</param>
    /// <param name="destination">The destination receiving the payload and check characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when input is malformed or the destination is too small.</exception>
    public int AppendFromValueAndDestination(ReadOnlySpan<char> value, Span<char> destination)
    {
        int check = CalculateFromValue(value);
        return ChecksumAlgorithms.AppendMod97(value, destination, check, MaximumOutputSize);
    }

    /// <summary>Validates a decimal value ending in two MOD 97-10 check characters.</summary>
    /// <param name="value">The complete decimal value.</param>
    /// <returns>True when the value has at least two digits and a remainder of one.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public bool Validate(string value)
    {
        Guard.NotNull(value, "value");
        return ValidateFromValue(value.AsSpan());
    }

    /// <summary>Validates a decimal span ending in two MOD 97-10 check characters.</summary>
    /// <param name="value">The complete decimal value.</param>
    /// <returns>True when the value has at least two digits and a remainder of one.</returns>
    public bool ValidateFromValue(ReadOnlySpan<char> value)
    {
        return ChecksumAlgorithms.IsMod97(value, MaximumCount);
    }
}
