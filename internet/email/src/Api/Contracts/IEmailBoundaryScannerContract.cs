using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBoundaryScanner"/>.
/// </summary>
public interface IEmailBoundaryScannerContract
{
    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailBoundaryDelimiter> Feed(ReadOnlySpan<byte> input);
    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The boundary scan result produced by the operation.</returns>
    EmailBoundaryScanResult Complete();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the closing boundary is present or available.
    /// </summary>
    /// <returns><c>true</c> when the closing boundary is available; otherwise, <c>false</c>.</returns>
    bool HasClosingBoundary();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
}
