using System;

namespace email;
/// <summary>
/// Represents a flowed text line.
/// </summary>
public class EmailFlowedTextLine : IEmailFlowedTextLineContract
{
    string content;
    string ending;
    int quoteDepth;
    /// <summary>
    /// Creates a new flowed text line.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "ending">The ending used by the operation.</param>
    /// <param name = "quoteDepth">The quote depth used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailFlowedTextLine(string content, string ending, int quoteDepth)
    {
        if (content == null)
            throw new ArgumentNullException("content");
        if (ending == null)
            throw new ArgumentNullException("ending");
        if (ending.Length > 0 && !string.Equals(ending, "\r\n", StringComparison.Ordinal) && !string.Equals(ending, "\r", StringComparison.Ordinal) && !string.Equals(ending, "\n", StringComparison.Ordinal))
            throw new ArgumentException("A line ending must be CRLF, CR, LF, or empty.", "ending");
        if (quoteDepth < 0)
            throw new ArgumentOutOfRangeException("quoteDepth");
        this.content = content;
        this.ending = ending;
        this.quoteDepth = quoteDepth;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>The current content.</returns>
    public string GetContent()
    {
        return content;
    }

    /// <summary>
    /// Gets the ending.
    /// </summary>
    /// <returns>The current ending.</returns>
    public string GetEnding()
    {
        return ending;
    }

    /// <summary>
    /// Gets the quote depth.
    /// </summary>
    /// <returns>The current quote depth.</returns>
    public int GetQuoteDepth()
    {
        return quoteDepth;
    }
}
