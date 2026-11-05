using System;

namespace fake;

/// <summary>
/// Defines the common operations supported by append-style checksum engines.
/// </summary>
/// <remarks>
/// Implementations may impose algorithm-specific syntax and payload-length
/// requirements. A successful validation establishes checksum correctness only;
/// it does not establish allocation, ownership, or authenticity.
/// </remarks>
public interface IChecksum
{
    /// <summary>Calculates the trailing check value for a payload.</summary>
    /// <param name="value">The payload without its check character or characters.</param>
    /// <returns>The algorithm-specific numeric check value.</returns>
    int Calculate(string value);

    /// <summary>Calculates the trailing check value for a payload span.</summary>
    /// <param name="value">The payload without its check character or characters.</param>
    /// <returns>The algorithm-specific numeric check value.</returns>
    int CalculateFromValue(ReadOnlySpan<char> value);

    /// <summary>Appends the check character or characters to a payload.</summary>
    /// <param name="value">The payload without its check character or characters.</param>
    /// <returns>The complete payload and checksum.</returns>
    string Append(string value);

    /// <summary>Writes a payload and its check character or characters to a destination.</summary>
    /// <param name="value">The payload without its check character or characters.</param>
    /// <param name="destination">The destination receiving the complete value.</param>
    /// <returns>The number of characters written.</returns>
    int AppendFromValueAndDestination(ReadOnlySpan<char> value, Span<char> destination);

    /// <summary>Validates a value containing its check character or characters.</summary>
    /// <param name="value">The complete value to validate.</param>
    /// <returns>True when the value satisfies the implementation's checksum contract.</returns>
    bool Validate(string value);

    /// <summary>Validates a span containing its check character or characters.</summary>
    /// <param name="value">The complete value to validate.</param>
    /// <returns>True when the value satisfies the implementation's checksum contract.</returns>
    bool ValidateFromValue(ReadOnlySpan<char> value);
}
