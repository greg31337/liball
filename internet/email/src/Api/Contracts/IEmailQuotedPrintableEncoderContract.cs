using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailQuotedPrintableEncoder"/>.
/// </summary>
public interface IEmailQuotedPrintableEncoderContract
{
    /// <summary>
    /// Gets the line length.
    /// </summary>
    /// <returns>The current line length.</returns>
    int GetLineLength();
    /// <summary>
    /// Gets the newline policy.
    /// </summary>
    /// <returns>The current newline policy.</returns>
    EmailQuotedPrintableNewlinePolicy GetNewlinePolicy();
    /// <summary>
    /// Gets the protect from lines.
    /// </summary>
    /// <returns><c>true</c> when the protect from lines setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetProtectFromLines();
    /// <summary>
    /// Gets the line column.
    /// </summary>
    /// <returns>The current line column.</returns>
    int GetLineColumn();
    /// <summary>
    /// Gets the input byte count.
    /// </summary>
    /// <returns>The current input byte count.</returns>
    long GetInputByteCount();
    /// <summary>
    /// Gets the output byte count.
    /// </summary>
    /// <returns>The current output byte count.</returns>
    long GetOutputByteCount();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    byte[] Feed(ReadOnlySpan<byte> input);
    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    byte[] Complete();
}
