using System;

namespace fake;
/// <summary>
/// Calculates, applies, and validates the check digit in position nine of a 17-character VIN shape.
/// </summary>
/// <remarks>Structural validity does not establish WMI assignment, manufacture, registration, ownership, or vehicle existence.</remarks>
public class VinChecksum : IVinChecksum
{
    internal int MaximumCount;
    internal int MaximumOutputSize;
    internal VinChecksum(int maximumCount, int maximumOutputSize)
    {
        MaximumCount = maximumCount;
        MaximumOutputSize = maximumOutputSize;
    }

    /// <summary>
    /// Calculates the VIN check value while ignoring the current character in position nine.
    /// </summary>
    /// <param name="value">Exactly 17 permitted uppercase VIN characters.</param>
    /// <returns>A value from zero through ten; ten is represented by <c>X</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value has invalid syntax.</exception>
    public int Calculate(string value)
    {
        Guard.NotNull(value, "value");
        return CalculateFromValue(value.AsSpan());
    }

    /// <summary>
    /// Calculates the VIN check value while ignoring the current character in position nine.
    /// </summary>
    /// <param name="value">Exactly 17 permitted uppercase VIN characters.</param>
    /// <returns>A value from zero through ten.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value has invalid syntax.</exception>
    public int CalculateFromValue(ReadOnlySpan<char> value)
    {
        return VinAlgorithms.Calculate(value, MaximumCount);
    }

    /// <summary>
    /// Replaces position nine with the calculated check digit.
    /// </summary>
    /// <param name="value">Exactly 17 permitted uppercase VIN characters.</param>
    /// <returns>A checksum-valid VIN shape.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value has invalid syntax.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    public string Apply(string value)
    {
        Guard.NotNull(value, "value");
        return VinAlgorithms.Apply(value.AsSpan(), MaximumCount, MaximumOutputSize);
    }

    /// <summary>
    /// Writes a VIN with position nine replaced by its calculated check digit.
    /// </summary>
    /// <param name="value">Exactly 17 permitted uppercase VIN characters.</param>
    /// <param name="destination">A destination containing at least 17 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value or destination is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    public int ApplyFromValueAndDestination(ReadOnlySpan<char> value, Span<char> destination)
    {
        return VinAlgorithms.Apply(value, destination, MaximumCount, MaximumOutputSize);
    }

    /// <summary>
    /// Reports whether a VIN shape has permitted characters and the correct position-nine check digit.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when structure and checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    public bool Validate(string value)
    {
        Guard.NotNull(value, "value");
        return ValidateFromValue(value.AsSpan());
    }

    /// <summary>
    /// Reports whether a VIN shape has permitted characters and the correct position-nine check digit.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when structure and checksum are valid.</returns>
    public bool ValidateFromValue(ReadOnlySpan<char> value)
    {
        return VinAlgorithms.Validate(value, MaximumCount);
    }
}
