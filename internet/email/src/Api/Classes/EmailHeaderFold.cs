using System;

namespace email;
/// <summary>
/// Represents a header fold.
/// </summary>
public class EmailHeaderFold : IEmailHeaderFoldContract
{
    int rawValueOffset;
    EmailLineEnding lineEnding;
    int whitespaceByteCount;
    /// <summary>
    /// Creates a new header fold.
    /// </summary>
    /// <param name = "rawValueOffset">The zero-based raw value offset.</param>
    /// <param name = "lineEnding">The line ending used by the operation.</param>
    /// <param name = "whitespaceByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHeaderFold(int rawValueOffset, EmailLineEnding lineEnding, int whitespaceByteCount)
    {
        if (rawValueOffset < 0)
        {
            throw new ArgumentOutOfRangeException("rawValueOffset");
        }

        if (lineEnding < EmailLineEnding.CrLf || lineEnding > EmailLineEnding.Cr)
        {
            throw new ArgumentOutOfRangeException("lineEnding");
        }

        if (whitespaceByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("whitespaceByteCount");
        }

        this.rawValueOffset = rawValueOffset;
        this.lineEnding = lineEnding;
        this.whitespaceByteCount = whitespaceByteCount;
    }

    /// <summary>
    /// Gets the raw value offset.
    /// </summary>
    /// <returns>The current raw value offset.</returns>
    public int GetRawValueOffset()
    {
        return rawValueOffset;
    }

    /// <summary>
    /// Gets the line ending.
    /// </summary>
    /// <returns>The current line ending.</returns>
    public EmailLineEnding GetLineEnding()
    {
        return lineEnding;
    }

    /// <summary>
    /// Gets the whitespace byte count.
    /// </summary>
    /// <returns>The current whitespace byte count.</returns>
    public int GetWhitespaceByteCount()
    {
        return whitespaceByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is fold.
    /// </summary>
    /// <returns><c>true</c> when the value or state is fold; otherwise, <c>false</c>.</returns>
    public bool IsFold()
    {
        return whitespaceByteCount > 0;
    }

    /// <summary>
    /// Determines whether the current value or state is canonical.
    /// </summary>
    /// <returns><c>true</c> when the value or state is canonical; otherwise, <c>false</c>.</returns>
    public bool IsCanonical()
    {
        return lineEnding == EmailLineEnding.CrLf && whitespaceByteCount > 0;
    }
}
