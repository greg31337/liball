using System;

namespace sql;

/// <summary>Represents a table reference node in the SQL syntax tree.</summary>
/// <remarks>The <see cref="Kind"/> discriminator determines whether <see cref="Name"/>, <see cref="Query"/>, <see cref="FunctionExpression"/>, or <see cref="Inner"/> contains the primary table source.</remarks>
public class SqlTableReference
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlTableReferenceKind Kind;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName? Name;
    /// <summary>The nested query, or <c>null</c> when this syntax form has no query.</summary>
    public SqlQuery? Query;
    /// <summary>The function expression associated with this value.</summary>
    public SqlExpression? FunctionExpression;
    /// <summary>The inner associated with this value.</summary>
    public SqlTableReference? Inner;
    /// <summary>The joins in source or insertion order.</summary>
    public SqlJoin[] Joins;
    /// <summary>The optional alias, or <c>null</c> when no alias was written.</summary>
    public SqlName? Alias;
    /// <summary>The column aliases in source or insertion order.</summary>
    public SqlName[] ColumnAliases;
    /// <summary>The typed table-source payload.</summary>
    public SqlTableSourceData? SourceData;
    /// <summary>Preserved unsupported modifiers attached to this source.</summary>
    public SqlUnsupportedQueryClause[] UnsupportedClauses;

    /// <summary>Initializes a new instance of <see cref="SqlTableReference"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlTableReference Create(SqlTableReferenceKind kind, SqlTextSpan span)
    {
        return new SqlTableReference(kind, span, null, null, null, null, new SqlJoin[0], null, new SqlName[0]);
    }

    /// <summary>Creates a table reference from all supported table-source fields.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="name">The optional name to use.</param>
    /// <param name="query">The optional query to use.</param>
    /// <param name="functionExpression">The optional function expression to use.</param>
    /// <param name="inner">The optional inner reference to use.</param>
    /// <param name="joins">The joins to use.</param>
    /// <param name="alias">The optional alias to use.</param>
    /// <param name="columnAliases">The column aliases to use.</param>
    /// <returns>The resulting table reference.</returns>
    internal static SqlTableReference Create(SqlTableReferenceKind kind, SqlTextSpan span, SqlName? name, SqlQuery? query, SqlExpression? functionExpression, SqlTableReference? inner, SqlJoin[] joins, SqlName? alias, SqlName[] columnAliases)
    {
        return new SqlTableReference(kind, span, name, query, functionExpression, inner, joins, alias, columnAliases);
    }

    /// <summary>Initializes a new instance of <see cref="SqlTableReference"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="query">The query syntax tree to process.</param>
    /// <param name="functionExpression">The function expression to use.</param>
    /// <param name="inner">The inner to use.</param>
    /// <param name="joins">The joins to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="columnAliases">The column aliases to use.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlTableReference(SqlTableReferenceKind kind, SqlTextSpan span, SqlName? name, SqlQuery? query, SqlExpression? functionExpression, SqlTableReference? inner, SqlJoin[] joins, SqlName? alias, SqlName[] columnAliases)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ValidateJoins(joins);
        ValidateColumnAliases(columnAliases);

        Kind = kind;
        Span = span;
        Name = name;
        Query = query;
        FunctionExpression = functionExpression;
        Inner = inner;
        Joins = joins;
        Alias = alias;
        ColumnAliases = columnAliases;
        SourceData = null;
        UnsupportedClauses = new SqlUnsupportedQueryClause[0];
    }

    /// <summary>Returns whether the requested kind matches this value's discriminator.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsKind(SqlTableReferenceKind kind)
    {
        return Kind == kind;
    }

    /// <summary>Returns whether name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasName()
    {
        return Name != null;
    }

    /// <summary>Returns whether query is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasQuery()
    {
        return Query != null;
    }

    /// <summary>Returns whether function expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasFunctionExpression()
    {
        return FunctionExpression != null;
    }

    /// <summary>Returns whether inner is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasInner()
    {
        return Inner != null;
    }

    /// <summary>Returns whether joins is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasJoins()
    {
        return Joins.Length > 0;
    }

    /// <summary>Returns the number of join items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetJoinCount()
    {
        return Joins.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting join.</returns>
    internal SqlJoin GetJoinAt(int index)
    {
        return Joins[index];
    }

    /// <summary>Returns whether alias is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasAlias()
    {
        return Alias != null;
    }

    /// <summary>Returns whether column aliases is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasColumnAliases()
    {
        return ColumnAliases.Length > 0;
    }

    /// <summary>Returns the number of column alias items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetColumnAliasCount()
    {
        return ColumnAliases.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetColumnAliasAt(int index)
    {
        return ColumnAliases[index];
    }

    internal static void ValidateJoins(SqlJoin[] joins)
    {
        if (joins == null)
        {
            throw new ArgumentNullException("joins");
        }

        int index = 0;
        while (index < joins.Length)
        {
            if (joins[index] == null)
            {
                throw new ArgumentNullException("joins");
            }

            index = index + 1;
        }
    }

    internal static void ValidateColumnAliases(SqlName[] columnAliases)
    {
        if (columnAliases == null)
        {
            throw new ArgumentNullException("columnAliases");
        }

        int index = 0;
        while (index < columnAliases.Length)
        {
            if (columnAliases[index] == null)
            {
                throw new ArgumentNullException("columnAliases");
            }

            index = index + 1;
        }
    }
}
