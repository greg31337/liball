using System;
using System.IO;

namespace markdown;

/// <summary>
/// Writes text and attributes to HTML while enforcing escaping and output-size limits.
/// </summary>
public class HtmlWriter : IHtmlWriter
{
    internal TextWriter Writer;
    internal MarkdownOutputSizeLimiter OutputSizeLimiter;

    /// <summary>
    /// Initializes a writer without an output-size limit.
    /// </summary>
    /// <param name="writer">The destination writer. The caller retains ownership of it.</param>
    public HtmlWriter(TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        Writer = writer;
        OutputSizeLimiter = new MarkdownOutputSizeLimiter(int.MaxValue);
    }

    /// <summary>
    /// Creates a writer with an output-size limit.
    /// </summary>
    /// <param name="writer">The destination writer. The caller retains ownership of it.</param>
    /// <param name="maximumOutputSize">The maximum number of characters the writer may emit.</param>
    internal static HtmlWriter Create(TextWriter writer, int maximumOutputSize)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        HtmlWriter htmlWriter = new HtmlWriter(writer);
        htmlWriter.OutputSizeLimiter = new MarkdownOutputSizeLimiter(maximumOutputSize);
        return htmlWriter;
    }

    /// <summary>
    /// Writes trusted HTML text verbatim while enforcing the output-size limit.
    /// </summary>
    /// <param name="text">The text to process.</param>
    public void Write(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        OutputSizeLimiter.Add(text);
        Writer.Write(text);
    }

    /// <summary>
    /// Writes escaped text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    public void WriteEscapedText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Write(HtmlEscaper.EscapeText(text));
    }

    /// <summary>
    /// Writes escaped attribute.
    /// </summary>
    /// <param name="text">The text to process.</param>
    public void WriteEscapedAttribute(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Write(HtmlEscaper.EscapeAttribute(text));
    }
}
