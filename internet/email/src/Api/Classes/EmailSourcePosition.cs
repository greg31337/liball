using System;

namespace email;
/// <summary>
/// Represents a source position.
/// </summary>
public class EmailSourcePosition : IEmailSourcePositionContract
{
    long byteOffset;
    long lineNumber;
    long columnNumber;
    /// <summary>
    /// Creates a new source position.
    /// </summary>
    /// <param name = "byteOffset">The zero-based byte offset.</param>
    /// <param name = "lineNumber">The line number used by the operation.</param>
    /// <param name = "columnNumber">The column number used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSourcePosition(long byteOffset, long lineNumber, long columnNumber)
    {
        if (byteOffset < 0)
        {
            throw new ArgumentOutOfRangeException("byteOffset");
        }

        if (lineNumber < 1)
        {
            throw new ArgumentOutOfRangeException("lineNumber");
        }

        if (columnNumber < 1)
        {
            throw new ArgumentOutOfRangeException("columnNumber");
        }

        this.byteOffset = byteOffset;
        this.lineNumber = lineNumber;
        this.columnNumber = columnNumber;
    }

    /// <summary>
    /// Gets the byte offset.
    /// </summary>
    /// <returns>The current byte offset.</returns>
    public long GetByteOffset()
    {
        return byteOffset;
    }

    /// <summary>
    /// Gets the line number.
    /// </summary>
    /// <returns>The current line number.</returns>
    public long GetLineNumber()
    {
        return lineNumber;
    }

    /// <summary>
    /// Gets the column number.
    /// </summary>
    /// <returns>The current column number.</returns>
    public long GetColumnNumber()
    {
        return columnNumber;
    }
}
