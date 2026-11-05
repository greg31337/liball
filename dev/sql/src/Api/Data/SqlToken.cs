using System;

namespace sql;

/// <summary>Represents one lexical token, including its original text, source span, and surrounding trivia.</summary>
public class SqlToken
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlTokenKind Kind;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The text associated with this value.</summary>
    public string Text;
    /// <summary>The decoded lexical value, excluding quotes, parameter prefixes, and escape syntax where applicable.</summary>
    public string Value;
    /// <summary>The identifier quote style used by this token.</summary>
    public SqlNameQuoteStyle IdentifierQuoteStyle;
    /// <summary>The string quote style used by this token.</summary>
    public SqlStringQuoteStyle StringQuoteStyle;
    /// <summary>The parameter placeholder style used by this token.</summary>
    public SqlParameterStyle ParameterStyle;
    /// <summary>The exact source delimiter used by a quoted token, or an empty string when no delimiter applies.</summary>
    public string QuoteDelimiter;
    /// <summary>The leading trivia in source or insertion order.</summary>
    public SqlTrivia[] LeadingTrivia;
    /// <summary>The trailing trivia in source or insertion order.</summary>
    public SqlTrivia[] TrailingTrivia;

    /// <summary>Initializes a token without surrounding trivia.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="text">The source text to process.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    public SqlToken(SqlTokenKind kind, SqlTextSpan span, string text)
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
        Value = text;
        IdentifierQuoteStyle = SqlNameQuoteStyle.None;
        StringQuoteStyle = SqlStringQuoteStyle.None;
        ParameterStyle = SqlParameterStyle.None;
        QuoteDelimiter = "";
        LeadingTrivia = new SqlTrivia[0];
        TrailingTrivia = new SqlTrivia[0];
    }

    /// <summary>Creates a token without surrounding trivia.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="text">The source text to process.</param>
    /// <returns>The resulting token.</returns>
    internal static SqlToken Create(SqlTokenKind kind, SqlTextSpan span, string text)
    {
        return new SqlToken(kind, span, text);
    }

    /// <summary>Creates a token with explicit surrounding trivia.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="text">The source text to process.</param>
    /// <param name="leadingTrivia">The leading trivia to use.</param>
    /// <param name="trailingTrivia">The trailing trivia to use.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    /// <returns>The resulting token.</returns>
    internal static SqlToken Create(SqlTokenKind kind, SqlTextSpan span, string text, SqlTrivia[] leadingTrivia, SqlTrivia[] trailingTrivia)
    {
        SqlToken token = new SqlToken(kind, span, text);
        ValidateTriviaArray("leadingTrivia", leadingTrivia);
        ValidateTriviaArray("trailingTrivia", trailingTrivia);

        token.LeadingTrivia = leadingTrivia;
        token.TrailingTrivia = trailingTrivia;
        return token;
    }

    internal void SetLexicalData(string value, SqlNameQuoteStyle identifierQuoteStyle, SqlStringQuoteStyle stringQuoteStyle, SqlParameterStyle parameterStyle, string quoteDelimiter)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (quoteDelimiter == null)
        {
            throw new ArgumentNullException("quoteDelimiter");
        }

        Value = value;
        IdentifierQuoteStyle = identifierQuoteStyle;
        StringQuoteStyle = stringQuoteStyle;
        ParameterStyle = parameterStyle;
        QuoteDelimiter = quoteDelimiter;
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

    /// <summary>Returns whether leading trivia is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasLeadingTrivia()
    {
        return LeadingTrivia.Length > 0;
    }

    /// <summary>Returns whether trailing trivia is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTrailingTrivia()
    {
        return TrailingTrivia.Length > 0;
    }

    /// <summary>Returns the number of leading trivia items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetLeadingTriviaCount()
    {
        return LeadingTrivia.Length;
    }

    /// <summary>Returns the number of trailing trivia items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetTrailingTriviaCount()
    {
        return TrailingTrivia.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting trivia.</returns>
    internal SqlTrivia GetLeadingTriviaAt(int index)
    {
        return LeadingTrivia[index];
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting trivia.</returns>
    internal SqlTrivia GetTrailingTriviaAt(int index)
    {
        return TrailingTrivia[index];
    }

    internal static void ValidateTriviaArray(string parameterName, SqlTrivia[] trivia)
    {
        if (trivia == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < trivia.Length)
        {
            if (trivia[index] == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            index = index + 1;
        }
    }
}
