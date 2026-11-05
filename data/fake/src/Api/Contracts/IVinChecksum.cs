using System;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="VinChecksum"/>.</summary>
public interface IVinChecksum
{
    /// <summary>
    /// Calculates the VIN check value while ignoring the current character in position nine.
    /// </summary>
    /// <param name="value">Exactly 17 permitted uppercase VIN characters.</param>
    /// <returns>A value from zero through ten; ten is represented by <c>X</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value has invalid syntax.</exception>
    int Calculate(string value);
    /// <summary>
    /// Calculates the VIN check value while ignoring the current character in position nine.
    /// </summary>
    /// <param name="value">Exactly 17 permitted uppercase VIN characters.</param>
    /// <returns>A value from zero through ten.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value has invalid syntax.</exception>
    int CalculateFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Replaces position nine with the calculated check digit.
    /// </summary>
    /// <param name="value">Exactly 17 permitted uppercase VIN characters.</param>
    /// <returns>A checksum-valid VIN shape.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value has invalid syntax.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    string Apply(string value);
    /// <summary>
    /// Writes a VIN with position nine replaced by its calculated check digit.
    /// </summary>
    /// <param name="value">Exactly 17 permitted uppercase VIN characters.</param>
    /// <param name="destination">A destination containing at least 17 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value or destination is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    int ApplyFromValueAndDestination(ReadOnlySpan<char> value, Span<char> destination);
    /// <summary>
    /// Reports whether a VIN shape has permitted characters and the correct position-nine check digit.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when structure and checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool Validate(string value);
    /// <summary>
    /// Reports whether a VIN shape has permitted characters and the correct position-nine check digit.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when structure and checksum are valid.</returns>
    bool ValidateFromValue(ReadOnlySpan<char> value);
}
