using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Describes the outcome of a flowed text decode operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailFlowedTextDecodeResult : IEmailFlowedTextDecodeResultContract
{
    List<EmailFlowedTextLine> lines;
    bool deleteSpace;
    /// <summary>
    /// Creates a new flowed text decode result.
    /// </summary>
    /// <param name = "lines">The lines used by the operation.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailFlowedTextDecodeResult(List<EmailFlowedTextLine> lines, bool deleteSpace)
    {
        if (lines == null)
            throw new ArgumentNullException("lines");
        this.lines = new List<EmailFlowedTextLine>(lines);
        this.deleteSpace = deleteSpace;
    }

    /// <summary>
    /// Gets the line count.
    /// </summary>
    /// <returns>The current line count.</returns>
    public int GetLineCount()
    {
        return lines.Count;
    }

    /// <summary>
    /// Gets the line.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current line.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailFlowedTextLine GetLine(int index)
    {
        if (index < 0 || index >= lines.Count)
            throw new ArgumentOutOfRangeException("index");
        EmailFlowedTextLine line = lines[index];
        return new EmailFlowedTextLine(line.GetContent(), line.GetEnding(), line.GetQuoteDepth());
    }

    /// <summary>
    /// Gets the lines.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current lines.</returns>
    public List<EmailFlowedTextLine> GetLines()
    {
        List<EmailFlowedTextLine> copy = new List<EmailFlowedTextLine>();
        int index = 0;
        while (index < lines.Count)
        {
            EmailFlowedTextLine line = lines[index];
            copy.Add(new EmailFlowedTextLine(line.GetContent(), line.GetEnding(), line.GetQuoteDepth()));
            index++;
        }

        return copy;
    }

    /// <summary>
    /// Gets the delete space.
    /// </summary>
    /// <returns><c>true</c> when the delete space setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetDeleteSpace()
    {
        return deleteSpace;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    public string GetText()
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < lines.Count)
        {
            EmailFlowedTextLine line = lines[index];
            int quote = 0;
            while (quote < line.GetQuoteDepth())
            {
                result.Append('>');
                quote++;
            }

            result.Append(line.GetContent());
            result.Append(line.GetEnding());
            index++;
        }

        return result.ToString();
    }
}
