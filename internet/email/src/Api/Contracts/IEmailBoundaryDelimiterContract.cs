using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBoundaryDelimiter"/>.
/// </summary>
public interface IEmailBoundaryDelimiterContract
{
    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    EmailRange GetSourceRange();
    /// <summary>
    /// Determines whether the current value or state is closing.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closing; otherwise, <c>false</c>.</returns>
    bool IsClosing();
    /// <summary>
    /// Gets the line ending.
    /// </summary>
    /// <returns>The current line ending.</returns>
    EmailLineEnding GetLineEnding();
    /// <summary>
    /// Gets the preceding line ending byte count.
    /// </summary>
    /// <returns>The current preceding line ending byte count.</returns>
    int GetPrecedingLineEndingByteCount();
    /// <summary>
    /// Gets the transport padding byte count.
    /// </summary>
    /// <returns>The current transport padding byte count.</returns>
    int GetTransportPaddingByteCount();
    /// <summary>
    /// Determines whether the operation or source value was recovered.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasRecovered();
}
