using System;

namespace sql;

/// <summary>Identifies a zero-based half-open range within a token array.</summary>
public class SqlTokenRange
{
    /// <summary>The zero-based first token index.</summary>
    public int Start;
    /// <summary>The number of tokens in the range.</summary>
    public int Length;

    /// <summary>Initializes a new instance of <see cref="SqlTokenRange"/>.</summary>
    /// <param name="start">The zero-based first token index.</param>
    /// <param name="length">The number of tokens in the range.</param>
    /// <exception cref="ArgumentOutOfRangeException">start or length is negative, or their sum overflows.</exception>
    public SqlTokenRange(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException("start");
        }

        if (length < 0 || start > int.MaxValue - length)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        Start = start;
        Length = length;
    }

    /// <summary>Returns the exclusive token index after this range.</summary>
    /// <returns>The exclusive end token index.</returns>
    internal int GetEnd()
    {
        return Start + Length;
    }
}

/// <summary>Preserves SQL syntax that has no supported semantic representation.</summary>
public class SqlUnsupportedSyntax
{
    /// <summary>The category of unsupported syntax.</summary>
    public SqlUnsupportedSyntaxKind Kind;
    /// <summary>A human-readable description of the unsupported syntax.</summary>
    public string Description;
    /// <summary>The exact source text.</summary>
    public string SourceText;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The preserved tokens in source order.</summary>
    public SqlToken[] Tokens;

    /// <summary>Initializes a new instance of <see cref="SqlUnsupportedSyntax"/>.</summary>
    /// <param name="kind">The unsupported syntax category.</param>
    /// <param name="description">The human-readable description.</param>
    /// <param name="sourceText">The exact source text.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="tokens">The preserved tokens.</param>
    /// <exception cref="ArgumentNullException">description, sourceText, span, tokens, or a token is <c>null</c>.</exception>
    public SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind kind, string description, string sourceText, SqlTextSpan span, SqlToken[] tokens)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        if (sourceText == null)
        {
            throw new ArgumentNullException("sourceText");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        SqlStatement.ValidateTokens(tokens);
        Kind = kind;
        Description = description;
        SourceText = sourceText;
        Span = span;
        Tokens = tokens;
    }
}
