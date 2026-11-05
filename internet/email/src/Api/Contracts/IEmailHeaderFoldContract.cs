using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderFold"/>.
/// </summary>
public interface IEmailHeaderFoldContract
{
    /// <summary>
    /// Gets the raw value offset.
    /// </summary>
    /// <returns>The current raw value offset.</returns>
    int GetRawValueOffset();
    /// <summary>
    /// Gets the line ending.
    /// </summary>
    /// <returns>The current line ending.</returns>
    EmailLineEnding GetLineEnding();
    /// <summary>
    /// Gets the whitespace byte count.
    /// </summary>
    /// <returns>The current whitespace byte count.</returns>
    int GetWhitespaceByteCount();
    /// <summary>
    /// Determines whether the current value or state is fold.
    /// </summary>
    /// <returns><c>true</c> when the value or state is fold; otherwise, <c>false</c>.</returns>
    bool IsFold();
    /// <summary>
    /// Determines whether the current value or state is canonical.
    /// </summary>
    /// <returns><c>true</c> when the value or state is canonical; otherwise, <c>false</c>.</returns>
    bool IsCanonical();
}
