using System;

namespace sql;

/// <summary>Represents a result column node in the SQL syntax tree.</summary>
public class SqlResultColumn
{
    /// <summary>The expression, or <c>null</c> when this syntax form has no expression.</summary>
    public SqlExpression? Expression;
    /// <summary>Indicates whether wildcard.</summary>
    public bool IsWildcard;
    /// <summary>The qualifier associated with this value.</summary>
    public SqlName? Qualifier;
    /// <summary>The optional alias, or <c>null</c> when no alias was written.</summary>
    public SqlName? Alias;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The tokens in source or insertion order.</summary>
    public SqlToken[] Tokens;

    /// <summary>Initializes a new instance of <see cref="SqlResultColumn"/> from the supplied values.</summary>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <param name="isWildcard">The is wildcard to use.</param>
    /// <param name="qualifier">The qualifier to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="tokens">The tokens in source order.</param>
    /// <exception cref="ArgumentNullException">expression is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlResultColumn(SqlExpression? expression, bool isWildcard, SqlName? qualifier, SqlName? alias, SqlTextSpan span, SqlToken[] tokens)
    {
        if (!isWildcard && expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ValidateTokens(tokens);

        Expression = expression;
        IsWildcard = isWildcard;
        Qualifier = qualifier;
        Alias = alias;
        Span = span;
        Tokens = tokens;
    }

    /// <summary>Returns whether expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasExpression()
    {
        return Expression != null;
    }

    /// <summary>Returns whether qualifier is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasQualifier()
    {
        return Qualifier != null;
    }

    /// <summary>Returns whether alias is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasAlias()
    {
        return Alias != null;
    }

    /// <summary>Returns whether tokens is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTokens()
    {
        return Tokens.Length > 0;
    }

    /// <summary>Returns the number of token items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetTokenCount()
    {
        return Tokens.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting token.</returns>
    internal SqlToken GetTokenAt(int index)
    {
        return Tokens[index];
    }

    internal static void ValidateTokens(SqlToken[] tokens)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == null)
            {
                throw new ArgumentNullException("tokens");
            }

            index = index + 1;
        }
    }
}
