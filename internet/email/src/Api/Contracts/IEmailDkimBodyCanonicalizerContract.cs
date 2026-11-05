using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimBodyCanonicalizer"/>.
/// </summary>
public interface IEmailDkimBodyCanonicalizerContract
{
    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    byte[] Feed(ReadOnlySpan<byte> input);
    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] Complete();
    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailDkimBodyCanonicalizationResult GetResult();
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
}
