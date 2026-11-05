using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSourceInfo"/>.
/// </summary>
public interface IEmailSourceInfoContract
{
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>The current content.</returns>
    EmailContent? GetContent();
    /// <summary>
    /// Gets the message range.
    /// </summary>
    /// <returns>The current message range.</returns>
    EmailRange? GetMessageRange();
    /// <summary>
    /// Determines whether the header block range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the header block range is available; otherwise, <c>false</c>.</returns>
    bool HasHeaderBlockRange();
    /// <summary>
    /// Gets the header block range.
    /// </summary>
    /// <returns>The current header block range.</returns>
    EmailRange? GetHeaderBlockRange();
    /// <summary>
    /// Determines whether the separator range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the separator range is available; otherwise, <c>false</c>.</returns>
    bool HasSeparatorRange();
    /// <summary>
    /// Gets the separator range.
    /// </summary>
    /// <returns>The current separator range.</returns>
    EmailRange? GetSeparatorRange();
    /// <summary>
    /// Determines whether the body range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body range is available; otherwise, <c>false</c>.</returns>
    bool HasBodyRange();
    /// <summary>
    /// Gets the body range.
    /// </summary>
    /// <returns>The current body range.</returns>
    EmailRange? GetBodyRange();
    /// <summary>
    /// Determines whether the source name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source name is available; otherwise, <c>false</c>.</returns>
    bool HasSourceName();
    /// <summary>
    /// Gets the source name.
    /// </summary>
    /// <returns>The current source name.</returns>
    string? GetSourceName();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
}
