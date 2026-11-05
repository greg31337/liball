using System;

namespace email;
/// <summary>
/// Represents a line.
/// </summary>
public class EmailLine : IEmailLineContract
{
    byte[] content;
    EmailLineEnding lineEnding;
    /// <summary>
    /// Creates a new line.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "lineEnding">The line ending used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailLine(byte[] content, EmailLineEnding lineEnding)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (lineEnding < EmailLineEnding.None || lineEnding > EmailLineEnding.Cr)
        {
            throw new ArgumentOutOfRangeException("lineEnding");
        }

        this.content = new byte[content.Length];
        content.CopyTo(this.content, 0);
        this.lineEnding = lineEnding;
    }

    /// <summary>
    /// Gets the content length.
    /// </summary>
    /// <returns>The current content length.</returns>
    public int GetContentLength()
    {
        return content.Length;
    }

    /// <summary>
    /// Gets the content bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content bytes.</returns>
    public byte[] GetContentBytes()
    {
        byte[] copy = new byte[content.Length];
        content.CopyTo(copy, 0);
        return copy;
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
    /// Determines whether the line ending is present or available.
    /// </summary>
    /// <returns><c>true</c> when the line ending is available; otherwise, <c>false</c>.</returns>
    public bool HasLineEnding()
    {
        return lineEnding != EmailLineEnding.None;
    }

    /// <summary>
    /// Gets the line ending byte count.
    /// </summary>
    /// <returns>The current line ending byte count.</returns>
    public int GetLineEndingByteCount()
    {
        if (lineEnding == EmailLineEnding.CrLf)
        {
            return 2;
        }

        if (lineEnding == EmailLineEnding.Cr || lineEnding == EmailLineEnding.Lf)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Gets the total byte count.
    /// </summary>
    /// <returns>The current total byte count.</returns>
    public long GetTotalByteCount()
    {
        return (long)content.Length + GetLineEndingByteCount();
    }
}
