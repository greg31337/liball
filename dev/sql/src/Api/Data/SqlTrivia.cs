using System;

namespace sql;

/// <summary>Represents whitespace, a comment, or skipped source text attached to a token.</summary>
public class SqlTrivia
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlTriviaKind Kind;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The text associated with this value.</summary>
    public string Text;

    /// <summary>Initializes a new instance of <see cref="SqlTrivia"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="text">The source text to process.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    public SqlTrivia(SqlTriviaKind kind, SqlTextSpan span, string text)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (span.Length != text.Length)
        {
            throw new ArgumentException("span length must match text length");
        }

        Kind = kind;
        Span = span;
        Text = text;
    }

    /// <summary>Returns the length associated with this value.</summary>
    /// <returns>The requested zero-based position.</returns>
    internal int GetLength()
    {
        return Text.Length;
    }

    /// <summary>Returns whether this value contains no elements or source characters.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return Text.Length == 0;
    }
}
