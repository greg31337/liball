using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCountingStream"/>.
/// </summary>
public interface IEmailCountingStreamContract
{
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    void Write(ReadOnlySpan<byte> value);
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <param name = "count">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void WriteSegment(byte[] value, int offset, int count);
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
    /// <summary>
    /// Determines whether the current output ends with CRLF.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool EndsWithCrlf();
    /// <summary>
    /// Determines whether the current output ends with carriage return.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool EndsWithCarriageReturn();
}
