using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFeedParser"/>.
/// </summary>
public interface IEmailFeedParserContract
{
    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void Feed(ReadOnlySpan<byte> input);
    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The parse result produced by the operation.</returns>
    EmailParseResult Complete();
    /// <summary>
    /// Determines whether the result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the result is available; otherwise, <c>false</c>.</returns>
    bool HasResult();
    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailParseResult GetResult();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Gets the received byte count.
    /// </summary>
    /// <returns>The current received byte count.</returns>
    long GetReceivedByteCount();
    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    void Reset();
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
}
