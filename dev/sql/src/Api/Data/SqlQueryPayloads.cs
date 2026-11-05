using System;

namespace sql;

/// <summary>Associates a compound operator with the query core that follows it.</summary>
public class SqlCompoundQueryPart
{
    /// <summary>The compound operator.</summary>
    public SqlCompoundOperatorKind OperatorKind;
    /// <summary>The query core following the operator.</summary>
    public SqlSelectCore Core;
    /// <summary>The source span covering the operator and core.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a compound query part.</summary>
    /// <param name="operatorKind">The compound operator.</param>
    /// <param name="core">The following query core.</param>
    /// <param name="span">The source span covering the part.</param>
    public SqlCompoundQueryPart(SqlCompoundOperatorKind operatorKind, SqlSelectCore core, SqlTextSpan span)
    {
        Core = core ?? throw new ArgumentNullException("core");
        Span = span ?? throw new ArgumentNullException("span");
        OperatorKind = operatorKind;
    }
}

/// <summary>Represents a SQL Server-style <c>TOP</c> clause.</summary>
public class SqlTopClause
{
    /// <summary>The requested row quantity.</summary>
    public SqlExpression Quantity;
    /// <summary>Indicates whether the quantity is a percentage.</summary>
    public bool IsPercent;
    /// <summary>Indicates whether ties are included.</summary>
    public bool WithTies;
    /// <summary>Indicates whether the quantity was enclosed in parentheses.</summary>
    public bool IsParenthesized;
    /// <summary>The source span covered by the clause.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a <c>TOP</c> clause.</summary>
    /// <param name="quantity">The requested row quantity.</param>
    /// <param name="isPercent">Whether the quantity is a percentage.</param>
    /// <param name="withTies">Whether ties are included.</param>
    /// <param name="isParenthesized">Whether the quantity was enclosed in parentheses.</param>
    /// <param name="span">The source span covered by the clause.</param>
    public SqlTopClause(SqlExpression quantity, bool isPercent, bool withTies, bool isParenthesized, SqlTextSpan span)
    {
        Quantity = quantity ?? throw new ArgumentNullException("quantity");
        Span = span ?? throw new ArgumentNullException("span");
        IsPercent = isPercent;
        WithTies = withTies;
        IsParenthesized = isParenthesized;
    }
}

/// <summary>Represents standard offset and fetch query limits.</summary>
public class SqlOffsetFetchClause
{
    /// <summary>The optional offset quantity.</summary>
    public SqlExpression? Offset;
    /// <summary>The optional fetched row quantity.</summary>
    public SqlExpression? Fetch;
    /// <summary>The fetch direction spelling.</summary>
    public SqlFetchDirectionKind Direction;
    /// <summary>Indicates whether ties are included instead of requiring only the requested count.</summary>
    public bool WithTies;
    /// <summary>The source span covered by the clause.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes an offset/fetch clause.</summary>
    /// <param name="offset">The optional offset quantity.</param>
    /// <param name="fetch">The optional fetched row quantity.</param>
    /// <param name="direction">The fetch direction spelling.</param>
    /// <param name="withTies">Whether ties are included.</param>
    /// <param name="span">The source span covered by the clause.</param>
    public SqlOffsetFetchClause(SqlExpression? offset, SqlExpression? fetch, SqlFetchDirectionKind direction, bool withTies, SqlTextSpan span)
    {
        Span = span ?? throw new ArgumentNullException("span");
        Offset = offset;
        Fetch = fetch;
        Direction = direction;
        WithTies = withTies;
    }
}

/// <summary>Preserves a dialect-specific query clause that has no semantic AST model.</summary>
public class SqlUnsupportedQueryClause
{
    /// <summary>The clause category.</summary>
    public SqlUnsupportedQueryClauseKind Kind;
    /// <summary>The exact source text.</summary>
    public string SourceText;
    /// <summary>The source span covered by the clause.</summary>
    public SqlTextSpan Span;
    /// <summary>The clause tokens in source order.</summary>
    public SqlToken[] Tokens;

    /// <summary>Initializes a preserved unsupported query clause.</summary>
    /// <param name="kind">The clause category.</param>
    /// <param name="sourceText">The exact source text.</param>
    /// <param name="span">The source span covered by the clause.</param>
    /// <param name="tokens">The clause tokens.</param>
    public SqlUnsupportedQueryClause(SqlUnsupportedQueryClauseKind kind, string sourceText, SqlTextSpan span, SqlToken[] tokens)
    {
        SourceText = sourceText ?? throw new ArgumentNullException("sourceText");
        Span = span ?? throw new ArgumentNullException("span");
        Tokens = tokens ?? throw new ArgumentNullException("tokens");
        Kind = kind;
    }
}

/// <summary>Stores typed data for named, function, subquery, and <c>VALUES</c> table sources.</summary>
public class SqlTableSourceData
{
    /// <summary>The source name or function name.</summary>
    public SqlName? Name;
    /// <summary>The table-function arguments.</summary>
    public SqlExpression[] FunctionArguments;
    /// <summary>The rows of a <c>VALUES</c> table source.</summary>
    public SqlExpression[][] Values;
    /// <summary>The subquery source.</summary>
    public SqlQuery? Query;
    /// <summary>Indicates whether <c>LATERAL</c> was written.</summary>
    public bool IsLateral;

    /// <summary>Initializes typed table-source data.</summary>
    /// <param name="name">The optional source or function name.</param>
    /// <param name="functionArguments">The table-function arguments.</param>
    /// <param name="values">The <c>VALUES</c> rows.</param>
    /// <param name="query">The optional subquery.</param>
    /// <param name="isLateral">Whether <c>LATERAL</c> was written.</param>
    public SqlTableSourceData(SqlName? name, SqlExpression[] functionArguments, SqlExpression[][] values, SqlQuery? query, bool isLateral)
    {
        FunctionArguments = functionArguments ?? throw new ArgumentNullException("functionArguments");
        Values = values ?? throw new ArgumentNullException("values");
        Name = name;
        Query = query;
        IsLateral = isLateral;
    }
}
