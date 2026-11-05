using System;

namespace sql;

/// <summary>Represents a delete statement node in the SQL syntax tree.</summary>
public class SqlDeleteStatement
{
    /// <summary>The table name targeted or referenced by this node.</summary>
    public SqlName TableName;
    /// <summary>The optional alias, or <c>null</c> when no alias was written.</summary>
    public SqlName? Alias;
    /// <summary>The indexed by associated with this value.</summary>
    public SqlName? IndexedBy;
    /// <summary>Indicates whether not indexed.</summary>
    public bool IsNotIndexed;
    /// <summary>The where associated with this value.</summary>
    public SqlExpression? Where;
    /// <summary>The returning associated with this value.</summary>
    public SqlReturningClause? Returning;
    /// <summary>The order by in source or insertion order.</summary>
    public SqlOrderingTerm[] OrderBy;
    /// <summary>The limit associated with this value.</summary>
    public SqlLimitClause? Limit;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlDeleteStatement"/> from the supplied values.</summary>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="where">The where to use.</param>
    /// <param name="returning">The returning to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlDeleteStatement Create(SqlName tableName, SqlName? alias, SqlExpression? where, SqlReturningClause? returning, SqlTextSpan span)
    {
        return new SqlDeleteStatement(tableName, alias, null, false, where, returning, new SqlOrderingTerm[0], null, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlDeleteStatement"/> from the supplied values.</summary>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="indexedBy">The indexed by to use.</param>
    /// <param name="isNotIndexed">The is not indexed to use.</param>
    /// <param name="where">The where to use.</param>
    /// <param name="returning">The returning to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlDeleteStatement Create(SqlName tableName, SqlName? alias, SqlName? indexedBy, bool isNotIndexed, SqlExpression? where, SqlReturningClause? returning, SqlTextSpan span)
    {
        return new SqlDeleteStatement(tableName, alias, indexedBy, isNotIndexed, where, returning, new SqlOrderingTerm[0], null, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlDeleteStatement"/> from the supplied values.</summary>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="indexedBy">The indexed by to use.</param>
    /// <param name="isNotIndexed">The is not indexed to use.</param>
    /// <param name="where">The where to use.</param>
    /// <param name="returning">The returning to use.</param>
    /// <param name="orderBy">The order by to use.</param>
    /// <param name="limit">The limit to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlDeleteStatement(SqlName tableName, SqlName? alias, SqlName? indexedBy, bool isNotIndexed, SqlExpression? where, SqlReturningClause? returning, SqlOrderingTerm[] orderBy, SqlLimitClause? limit, SqlTextSpan span)
    {
        if (tableName == null)
        {
            throw new ArgumentNullException("tableName");
        }

        ValidateOrderBy(orderBy);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        TableName = tableName;
        Alias = alias;
        IndexedBy = indexedBy;
        IsNotIndexed = isNotIndexed;
        Where = where;
        Returning = returning;
        OrderBy = orderBy;
        Limit = limit;
        Span = span;
    }

    /// <summary>Creates a delete statement from all supported clauses.</summary>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The optional alias to use.</param>
    /// <param name="indexedBy">The optional index name to use.</param>
    /// <param name="isNotIndexed">The not-indexed flag to use.</param>
    /// <param name="where">The optional predicate to use.</param>
    /// <param name="returning">The optional returning clause to use.</param>
    /// <param name="orderBy">The ordering terms to use.</param>
    /// <param name="limit">The optional limit to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting delete statement.</returns>
    internal static SqlDeleteStatement Create(SqlName tableName, SqlName? alias, SqlName? indexedBy, bool isNotIndexed, SqlExpression? where, SqlReturningClause? returning, SqlOrderingTerm[] orderBy, SqlLimitClause? limit, SqlTextSpan span)
    {
        return new SqlDeleteStatement(tableName, alias, indexedBy, isNotIndexed, where, returning, orderBy, limit, span);
    }

    /// <summary>Returns whether alias is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasAlias()
    {
        return Alias != null;
    }

    /// <summary>Returns whether indexed by is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasIndexedBy()
    {
        return IndexedBy != null;
    }

    /// <summary>Returns whether where is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasWhere()
    {
        return Where != null;
    }

    /// <summary>Returns whether returning is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasReturning()
    {
        return Returning != null;
    }

    /// <summary>Returns whether order by is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasOrderBy()
    {
        return OrderBy.Length > 0;
    }

    /// <summary>Returns the number of order by items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetOrderByCount()
    {
        return OrderBy.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting ordering term.</returns>
    internal SqlOrderingTerm GetOrderByAt(int index)
    {
        return OrderBy[index];
    }

    /// <summary>Returns whether limit is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasLimit()
    {
        return Limit != null;
    }

    internal static void ValidateOrderBy(SqlOrderingTerm[] orderBy)
    {
        if (orderBy == null)
        {
            throw new ArgumentNullException("orderBy");
        }

        int index = 0;
        while (index < orderBy.Length)
        {
            if (orderBy[index] == null)
            {
                throw new ArgumentNullException("orderBy");
            }

            index = index + 1;
        }
    }
}
