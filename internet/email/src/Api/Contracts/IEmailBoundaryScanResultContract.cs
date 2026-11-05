using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBoundaryScanResult"/>.
/// </summary>
public interface IEmailBoundaryScanResultContract
{
    /// <summary>
    /// Gets the delimiter count.
    /// </summary>
    /// <returns>The current delimiter count.</returns>
    int GetDelimiterCount();
    /// <summary>
    /// Gets the delimiter at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested delimiter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailBoundaryDelimiter GetDelimiterAt(int index);
    /// <summary>
    /// Gets the delimiters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current delimiters.</returns>
    List<EmailBoundaryDelimiter> GetDelimiters();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Determines whether the closing boundary is present or available.
    /// </summary>
    /// <returns><c>true</c> when the closing boundary is available; otherwise, <c>false</c>.</returns>
    bool HasClosingBoundary();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
