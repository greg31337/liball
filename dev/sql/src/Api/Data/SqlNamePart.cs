using System;

namespace sql;

/// <summary>Represents a name part node in the SQL syntax tree.</summary>
public class SqlNamePart
{
    /// <summary>The text associated with this value.</summary>
    public string Text;
    /// <summary>The original source text used to produce this value.</summary>
    public string SourceText;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The quote style associated with this value.</summary>
    public SqlNameQuoteStyle QuoteStyle;
    /// <summary>The token associated with this value.</summary>
    public SqlToken? Token;

    /// <summary>Initializes a new instance of <see cref="SqlNamePart"/> from the supplied values.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="quoteStyle">The quote style to use.</param>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">sourceText is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlNamePart(string text, string sourceText, SqlTextSpan span, SqlNameQuoteStyle quoteStyle)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (sourceText == null)
        {
            throw new ArgumentNullException("sourceText");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (span.Length != sourceText.Length)
        {
            throw new ArgumentException("span length must match source text length");
        }

        Text = text;
        SourceText = sourceText;
        Span = span;
        QuoteStyle = quoteStyle;
        Token = null;
    }

    /// <summary>Creates a name part without an associated token.</summary>
    /// <param name="text">The normalized name text.</param>
    /// <param name="sourceText">The original source text.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="quoteStyle">The quote style to use.</param>
    /// <returns>The resulting name part.</returns>
    internal static SqlNamePart Create(string text, string sourceText, SqlTextSpan span, SqlNameQuoteStyle quoteStyle)
    {
        return new SqlNamePart(text, sourceText, span, quoteStyle);
    }

    /// <summary>Initializes a new instance of <see cref="SqlNamePart"/> from the supplied values.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="quoteStyle">The quote style to use.</param>
    /// <param name="token">The token to use.</param>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">sourceText is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">token is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlNamePart Create(string text, string sourceText, SqlTextSpan span, SqlNameQuoteStyle quoteStyle, SqlToken token)
    {
        SqlNamePart part = new SqlNamePart(text, sourceText, span, quoteStyle);
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        part.Token = token;
        return part;
    }

    /// <summary>Returns whether quoted.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsQuoted()
    {
        return QuoteStyle != SqlNameQuoteStyle.None;
    }

    /// <summary>Returns whether token is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasToken()
    {
        return Token != null;
    }
}
