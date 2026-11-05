using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSourceTracker"/>.
/// </summary>
public interface IEmailSourceTrackerContract
{
    /// <summary>
    /// Gets the position.
    /// </summary>
    /// <returns>The current position.</returns>
    EmailSourcePosition GetPosition();
    /// <summary>
    /// Gets the byte offset.
    /// </summary>
    /// <returns>The current byte offset.</returns>
    long GetByteOffset();
    /// <summary>
    /// Gets the line number.
    /// </summary>
    /// <returns>The current line number.</returns>
    long GetLineNumber();
    /// <summary>
    /// Gets the column number.
    /// </summary>
    /// <returns>The current column number.</returns>
    long GetColumnNumber();
    /// <summary>
    /// Advances the supplied value.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    void Advance(ReadOnlySpan<byte> bytes);
    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    void Reset();
}
