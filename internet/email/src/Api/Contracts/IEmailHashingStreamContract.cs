using System;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHashingStream"/>.
/// </summary>
public interface IEmailHashingStreamContract
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
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult Complete();
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
}
