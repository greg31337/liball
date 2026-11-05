using System;

namespace markdown;

/// <summary>
/// Preserves original source text for round-trip rendering when trivia tracking is enabled.
/// </summary>
public class MarkdownTrivia
{
    /// <summary>
    /// Stores the span value used by this instance.
    /// </summary>
    public MarkdownSourceSpan Span;
    /// <summary>
    /// Stores the text value used by this instance.
    /// </summary>
    public string Text;
    /// <summary>
    /// The first original line-ending sequence in this trivia, or an empty string when none occurs.
    /// </summary>
    public string LineEnding;
    /// <summary>
    /// The original opening delimiter associated with the node, or an empty string when none is known.
    /// </summary>
    public string OpeningDelimiter;
    /// <summary>
    /// The original closing delimiter associated with the node, or an empty string when none is known.
    /// </summary>
    public string ClosingDelimiter;
    /// <summary>
    /// Whether a supported AST edit invalidated direct rendering of the captured text.
    /// </summary>
    public bool IsModified;

    /// <summary>
    /// Initializes a new <see cref="MarkdownTrivia"/> instance.
    /// </summary>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="text">The text to process.</param>
    public MarkdownTrivia(MarkdownSourceSpan span, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Span = span.Copy();
        Text = text;
        LineEnding = "";
        OpeningDelimiter = "";
        ClosingDelimiter = "";
        IsModified = false;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownTrivia"/> value.</returns>
    internal MarkdownTrivia Copy()
    {
        MarkdownTrivia copy = new MarkdownTrivia(Span, Text);
        copy.LineEnding = LineEnding;
        copy.OpeningDelimiter = OpeningDelimiter;
        copy.ClosingDelimiter = ClosingDelimiter;
        copy.IsModified = IsModified;
        return copy;
    }
}
