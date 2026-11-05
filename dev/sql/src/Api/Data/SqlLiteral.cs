using System;

namespace sql;

/// <summary>Represents a literal node in the SQL syntax tree.</summary>
public class SqlLiteral
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlLiteralKind Kind;
    /// <summary>The original source text used to produce this value.</summary>
    public string SourceText;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The value associated with this value.</summary>
    public object? Value;
    /// <summary>The has value associated with this value.</summary>
    public bool HasValue;
    /// <summary>The token associated with this value.</summary>
    public SqlToken? Token;

    /// <summary>Initializes a new instance of <see cref="SqlLiteral"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <param name="span">The source span associated with the value.</param>
    public SqlLiteral(SqlLiteralKind kind, string sourceText, SqlTextSpan span)
    {
        ValidateSource(sourceText, span);

        Kind = kind;
        SourceText = sourceText;
        Span = span;
        Value = null;
        HasValue = false;
        Token = null;
    }

    /// <summary>Creates a literal without a decoded value or associated token.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="sourceText">The source text represented by the literal.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting literal.</returns>
    internal static SqlLiteral Create(SqlLiteralKind kind, string sourceText, SqlTextSpan span)
    {
        return new SqlLiteral(kind, sourceText, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlLiteral"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting value.</returns>
    internal static SqlLiteral Create(SqlLiteralKind kind, string sourceText, SqlTextSpan span, object? value)
    {
        SqlLiteral literal = new SqlLiteral(kind, sourceText, span);
        literal.Value = value;
        literal.HasValue = true;
        return literal;
    }

    /// <summary>Initializes a new instance of <see cref="SqlLiteral"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="sourceText">The source text represented by the result.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="token">The token to use.</param>
    /// <returns>The resulting value.</returns>
    internal static SqlLiteral Create(SqlLiteralKind kind, string sourceText, SqlTextSpan span, object? value, SqlToken token)
    {
        SqlLiteral literal = new SqlLiteral(kind, sourceText, span);
        ValidateToken(sourceText, span, token);

        literal.Value = value;
        literal.HasValue = true;
        literal.Token = token;
        return literal;
    }

    /// <summary>Returns whether the requested kind matches this value's discriminator.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsKind(SqlLiteralKind kind)
    {
        return Kind == kind;
    }

    /// <summary>Returns whether token is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasToken()
    {
        return Token != null;
    }

    /// <summary>Returns whether null literal.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsNullLiteral()
    {
        return Kind == SqlLiteralKind.Null;
    }

    /// <summary>Returns whether numeric literal.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsNumericLiteral()
    {
        return Kind == SqlLiteralKind.Integer || Kind == SqlLiteralKind.Decimal;
    }

    internal static void ValidateSource(string sourceText, SqlTextSpan span)
    {
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
    }

    internal static void ValidateToken(string sourceText, SqlTextSpan span, SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (token.Span.Start != span.Start || token.Span.Length != span.Length)
        {
            throw new ArgumentException("token span must match literal span");
        }

        if (token.Text != sourceText)
        {
            throw new ArgumentException("token text must match source text");
        }
    }
}
