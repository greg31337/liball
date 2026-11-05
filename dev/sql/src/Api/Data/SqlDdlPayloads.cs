using System;

namespace sql;

/// <summary>Represents the preserved declared type of a SQLite column.</summary>
public class SqlColumnTypeName
{
    /// <summary>The normalized type text.</summary>
    public string NormalizedText;
    /// <summary>The type text as it appeared in the source.</summary>
    public string SourceText;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlColumnTypeName"/> from the supplied values.</summary>
    /// <param name="normalizedText">The normalized type text.</param>
    /// <param name="sourceText">The source type text.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">normalizedText, sourceText, or span is <c>null</c>.</exception>
    public SqlColumnTypeName(string normalizedText, string sourceText, SqlTextSpan span)
    {
        if (normalizedText == null)
        {
            throw new ArgumentNullException("normalizedText");
        }

        if (sourceText == null)
        {
            throw new ArgumentNullException("sourceText");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        NormalizedText = normalizedText;
        SourceText = sourceText;
        Span = span;
    }
}

/// <summary>Represents an ordered clause in a SQLite foreign-key reference.</summary>
public class SqlForeignKeyClause
{
    /// <summary>The discriminator that identifies this clause.</summary>
    public SqlForeignKeyClauseKind Kind;
    /// <summary>The foreign-key action, when this is an action clause.</summary>
    public SqlForeignKeyActionKind Action;
    /// <summary>The match name, when this is a match clause.</summary>
    public SqlName? MatchName;
    /// <summary>The explicit deferrability, when this is a deferrability clause.</summary>
    public SqlForeignKeyDeferrabilityKind Deferrability;
    /// <summary>The optional initial timing.</summary>
    public SqlForeignKeyInitialTimingKind InitialTiming;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlForeignKeyClause"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the clause.</param>
    /// <param name="action">The action to use.</param>
    /// <param name="matchName">The match name to use.</param>
    /// <param name="deferrability">The deferrability to use.</param>
    /// <param name="initialTiming">The initial timing to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlForeignKeyClause(SqlForeignKeyClauseKind kind, SqlForeignKeyActionKind action, SqlName? matchName, SqlForeignKeyDeferrabilityKind deferrability, SqlForeignKeyInitialTimingKind initialTiming, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Kind = kind;
        Action = action;
        MatchName = matchName;
        Deferrability = deferrability;
        InitialTiming = initialTiming;
        Span = span;
    }
}

/// <summary>Represents one preserved SQLite virtual-table module argument.</summary>
public class SqlVirtualTableArgument
{
    /// <summary>The argument text as it appeared in the source.</summary>
    public string SourceText;
    /// <summary>The tokens that form the argument.</summary>
    public SqlToken[] Tokens;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlVirtualTableArgument"/> from the supplied values.</summary>
    /// <param name="sourceText">The source argument text.</param>
    /// <param name="tokens">The tokens that form the argument.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">sourceText, tokens, an element of tokens, or span is <c>null</c>.</exception>
    public SqlVirtualTableArgument(string sourceText, SqlToken[] tokens, SqlTextSpan span)
    {
        if (sourceText == null)
        {
            throw new ArgumentNullException("sourceText");
        }

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

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        SourceText = sourceText;
        Tokens = tokens;
        Span = span;
    }
}
