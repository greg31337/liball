using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailLineReadResult"/>.
/// </summary>
public interface IEmailLineReadResultContract
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailLineReadStatus GetStatus();
    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    int GetConsumedByteCount();
    /// <summary>
    /// Determines whether the line is present or available.
    /// </summary>
    /// <returns><c>true</c> when the line is available; otherwise, <c>false</c>.</returns>
    bool HasLine();
    /// <summary>
    /// Gets the line.
    /// </summary>
    /// <returns>The current line.</returns>
    EmailLine? GetLine();
}
