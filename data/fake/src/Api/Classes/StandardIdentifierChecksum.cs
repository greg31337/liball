using System;

namespace fake;

/// <summary>
/// Calculates, appends, and validates one configured compact standard identifier checksum.
/// </summary>
/// <remarks>Validation covers compact syntax, supported length, required ISBN prefix where applicable, and the check character. It does not establish allocation, ownership, publication, or commercial usability.</remarks>
public class StandardIdentifierChecksum : IChecksum
{
    internal StandardIdentifierChecksumKind Kind;
    internal int MaximumCount;
    internal int MaximumOutputSize;

    internal StandardIdentifierChecksum(
        StandardIdentifierChecksumKind kind,
        int maximumCount,
        int maximumOutputSize)
    {
        Kind = kind;
        MaximumCount = maximumCount;
        MaximumOutputSize = maximumOutputSize;
    }

    /// <summary>Calculates the trailing check value for a compact payload.</summary>
    /// <param name="value">The compact ASCII decimal payload without its check character.</param>
    /// <returns>A value from zero through ten; ten is appended as <c>X</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when syntax, length, or an ISBN prefix is invalid.</exception>
    public int Calculate(string value)
    {
        Guard.NotNull(value, "value");
        return CalculateFromValue(value.AsSpan());
    }

    /// <summary>Calculates the trailing check value for a compact payload span.</summary>
    /// <param name="value">The compact ASCII decimal payload without its check character.</param>
    /// <returns>A value from zero through ten; ten is appended as <c>X</c>.</returns>
    /// <exception cref="System.ArgumentException">Thrown when syntax, length, or an ISBN prefix is invalid.</exception>
    public int CalculateFromValue(ReadOnlySpan<char> value)
    {
        return StandardIdentifierChecks.Calculate(Kind, value, MaximumCount);
    }

    /// <summary>Appends the configured standard check character to a compact payload.</summary>
    /// <param name="value">The compact ASCII decimal payload.</param>
    /// <returns>The payload followed by its check character.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when syntax, length, or an ISBN prefix is invalid.</exception>
    public string Append(string value)
    {
        Guard.NotNull(value, "value");
        int length = Guard.OutputSize((long)value.Length + 1L, MaximumOutputSize, "value");
        char[] result = new char[length];
        AppendFromValueAndDestination(value.AsSpan(), new Span<char>(result));
        return new string(result);
    }

    /// <summary>Writes a compact payload and its check character to a destination.</summary>
    /// <param name="value">The compact ASCII decimal payload.</param>
    /// <param name="destination">The destination receiving the payload and check character.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when input is malformed or the destination is too small.</exception>
    public int AppendFromValueAndDestination(ReadOnlySpan<char> value, Span<char> destination)
    {
        int check = CalculateFromValue(value);
        return ChecksumAlgorithms.AppendDigit(value, destination, check, MaximumOutputSize);
    }

    /// <summary>Validates a compact identifier containing its check character.</summary>
    /// <param name="value">The complete compact identifier.</param>
    /// <returns>True when syntax, length, prefix, and checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public bool Validate(string value)
    {
        Guard.NotNull(value, "value");
        return ValidateFromValue(value.AsSpan());
    }

    /// <summary>Validates a compact identifier span containing its check character.</summary>
    /// <param name="value">The complete compact identifier.</param>
    /// <returns>True when syntax, length, prefix, and checksum are valid.</returns>
    public bool ValidateFromValue(ReadOnlySpan<char> value)
    {
        return StandardIdentifierChecks.Validate(Kind, value, MaximumCount);
    }
}
