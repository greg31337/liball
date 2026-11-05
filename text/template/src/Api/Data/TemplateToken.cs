using System;

namespace template;

/// <summary>
/// Represents one lexical unit and its source range, text, and whitespace-control flags.
/// </summary>
public class TemplateToken
{
    /// <summary>
    /// Identifies the kind represented by this token.
    /// </summary>
    public TemplateTokenKind Kind;
    /// <summary>
    /// Identifies the source range represented by this value.
    /// </summary>
    public TemplateSourceSpan Span;
    /// <summary>
    /// Stores the source or rendered text represented by this value.
    /// </summary>
    public string Text;
    /// <summary>
    /// Indicates whether the tag removes whitespace immediately before it.
    /// </summary>
    public bool TrimLeft;
    /// <summary>
    /// Indicates whether the tag removes whitespace immediately after it.
    /// </summary>
    public bool TrimRight;
    /// <summary>
    /// Describes the left trim marker carried by this token.
    /// </summary>
    public TemplateTrimKind LeftTrimKind;
    /// <summary>
    /// Describes the right trim marker carried by this token.
    /// </summary>
    public TemplateTrimKind RightTrimKind;
    /// <summary>
    /// Indicates whether the tag occupies a logic-only line eligible for standalone trimming.
    /// </summary>
    public bool StandaloneLine;
    /// <summary>
    /// Indicates whether an escape character caused delimiter text to be treated literally.
    /// </summary>
    public bool Escaped;

    /// <summary>
    /// Initializes a new template token and validates any supplied values.
    /// </summary>
    /// <param name="kind">The kind represented by the new value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateToken(TemplateTokenKind kind, TemplateSourceSpan span, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Kind = kind;
        Span = span;
        Text = text;
        TrimLeft = false;
        TrimRight = false;
        LeftTrimKind = TemplateTrimKind.None;
        RightTrimKind = TemplateTrimKind.None;
        StandaloneLine = false;
        Escaped = false;
    }

    /// <summary>
    /// Determines whether the end of file condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsEndOfFile()
    {
        return Kind == TemplateTokenKind.EndOfFile;
    }

    /// <summary>
    /// Determines whether the text condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsText()
    {
        return Kind == TemplateTokenKind.Text
            || Kind == TemplateTokenKind.CommentText
            || Kind == TemplateTokenKind.RawText;
    }

    /// <summary>
    /// Determines whether the delimiter condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsDelimiter()
    {
        return TemplateTokenKindFacts.IsDelimiter(Kind);
    }

    /// <summary>
    /// Determines whether the trim marker condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsTrimMarker()
    {
        return TemplateTokenKindFacts.IsTrimMarker(Kind);
    }

    /// <summary>
    /// Determines whether the literal condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsLiteral()
    {
        return TemplateTokenKindFacts.IsLiteral(Kind);
    }
}
