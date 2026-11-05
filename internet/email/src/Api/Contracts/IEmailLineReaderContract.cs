using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailLineReader"/>.
/// </summary>
public interface IEmailLineReaderContract
{
    /// <summary>
    /// Gets the max line bytes.
    /// </summary>
    /// <returns>The current max line bytes.</returns>
    int GetMaxLineBytes();
    /// <summary>
    /// Gets the buffered byte count.
    /// </summary>
    /// <returns>The current buffered byte count.</returns>
    int GetBufferedByteCount();
    /// <summary>
    /// Reads the next value from the input.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "endOfInput">Whether to enable the end of input behavior.</param>
    /// <returns>The line read result produced by the operation.</returns>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailLineReadResult Read(ReadOnlySpan<byte> input, bool endOfInput);
    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    void Reset();
}
