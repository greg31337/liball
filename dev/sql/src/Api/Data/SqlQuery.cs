using System;

namespace sql;

/// <summary>Represents a query, including common table expressions, compound select cores, ordering, and limits.</summary>
public class SqlQuery
{
    /// <summary>The common table expressions in source or insertion order.</summary>
    public SqlCommonTableExpression[] CommonTableExpressions;
    /// <summary>Indicates whether recursive.</summary>
    public bool IsRecursive;
    /// <summary>The select cores in source or insertion order.</summary>
    public SqlSelectCore[] SelectCores;
    /// <summary>The compound operators in source or insertion order.</summary>
    public SqlCompoundOperatorKind[] CompoundOperators;
    /// <summary>The order by in source or insertion order.</summary>
    public SqlOrderingTerm[] OrderBy;
    /// <summary>The limit associated with this value.</summary>
    public SqlLimitClause? Limit;
    /// <summary>Typed compound operator/core pairs in source order.</summary>
    public SqlCompoundQueryPart[] CompoundParts;
    /// <summary>The standard offset/fetch clause.</summary>
    public SqlOffsetFetchClause? OffsetFetch;
    /// <summary>Preserved dialect-specific query clauses.</summary>
    public SqlUnsupportedQueryClause[] UnsupportedClauses;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlQuery"/> from the supplied values.</summary>
    /// <param name="selectCores">The select cores to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlQuery Create(SqlSelectCore[] selectCores, SqlTextSpan span)
    {
        return SqlQuery.Create(new SqlCommonTableExpression[0], false, selectCores, new SqlCompoundOperatorKind[0], new SqlOrderingTerm[0], null, span);
    }

    /// <summary>Creates a query from all query-level clauses.</summary>
    /// <param name="commonTableExpressions">The common table expressions to use.</param>
    /// <param name="isRecursive">The recursive flag to use.</param>
    /// <param name="selectCores">The select cores to use.</param>
    /// <param name="compoundOperators">The compound operators to use.</param>
    /// <param name="orderBy">The ordering terms to use.</param>
    /// <param name="limit">The optional limit to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting query.</returns>
    internal static SqlQuery Create(SqlCommonTableExpression[] commonTableExpressions, bool isRecursive, SqlSelectCore[] selectCores, SqlCompoundOperatorKind[] compoundOperators, SqlOrderingTerm[] orderBy, SqlLimitClause? limit, SqlTextSpan span)
    {
        return new SqlQuery(commonTableExpressions, isRecursive, selectCores, compoundOperators, orderBy, limit, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlQuery"/> from the supplied values.</summary>
    /// <param name="commonTableExpressions">The common table expressions to use.</param>
    /// <param name="isRecursive">The is recursive to use.</param>
    /// <param name="selectCores">The select cores to use.</param>
    /// <param name="compoundOperators">The compound operators to use.</param>
    /// <param name="orderBy">The order by to use.</param>
    /// <param name="limit">The limit to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlQuery(SqlCommonTableExpression[] commonTableExpressions, bool isRecursive, SqlSelectCore[] selectCores, SqlCompoundOperatorKind[] compoundOperators, SqlOrderingTerm[] orderBy, SqlLimitClause? limit, SqlTextSpan span)
    {
        ValidateCommonTableExpressions(commonTableExpressions);
        ValidateSelectCores(selectCores);
        ValidateCompoundOperators(selectCores, compoundOperators);
        ValidateOrderBy(orderBy);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        CommonTableExpressions = commonTableExpressions;
        IsRecursive = isRecursive;
        SelectCores = selectCores;
        CompoundOperators = compoundOperators;
        OrderBy = orderBy;
        Limit = limit;
        CompoundParts = new SqlCompoundQueryPart[0];
        OffsetFetch = null;
        UnsupportedClauses = new SqlUnsupportedQueryClause[0];
        Span = span;
    }

    /// <summary>Returns the number of common table expression items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetCommonTableExpressionCount()
    {
        return CommonTableExpressions.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting common table expression.</returns>
    internal SqlCommonTableExpression GetCommonTableExpressionAt(int index)
    {
        return CommonTableExpressions[index];
    }

    /// <summary>Returns whether common table expressions is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasCommonTableExpressions()
    {
        return CommonTableExpressions.Length > 0;
    }

    /// <summary>Returns the number of select core items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetSelectCoreCount()
    {
        return SelectCores.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting select core.</returns>
    internal SqlSelectCore GetSelectCoreAt(int index)
    {
        return SelectCores[index];
    }

    /// <summary>Returns whether compound.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsCompound()
    {
        return CompoundOperators.Length > 0;
    }

    /// <summary>Returns the number of compound operator items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetCompoundOperatorCount()
    {
        return CompoundOperators.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting compound operator kind.</returns>
    internal SqlCompoundOperatorKind GetCompoundOperatorAt(int index)
    {
        return CompoundOperators[index];
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

    internal static void ValidateCommonTableExpressions(SqlCommonTableExpression[] commonTableExpressions)
    {
        if (commonTableExpressions == null)
        {
            throw new ArgumentNullException("commonTableExpressions");
        }

        int index = 0;
        while (index < commonTableExpressions.Length)
        {
            if (commonTableExpressions[index] == null)
            {
                throw new ArgumentNullException("commonTableExpressions");
            }

            index = index + 1;
        }
    }

    internal static void ValidateSelectCores(SqlSelectCore[] selectCores)
    {
        if (selectCores == null)
        {
            throw new ArgumentNullException("selectCores");
        }

        int index = 0;
        while (index < selectCores.Length)
        {
            if (selectCores[index] == null)
            {
                throw new ArgumentNullException("selectCores");
            }

            index = index + 1;
        }
    }

    internal static void ValidateCompoundOperators(SqlSelectCore[] selectCores, SqlCompoundOperatorKind[] compoundOperators)
    {
        if (compoundOperators == null)
        {
            throw new ArgumentNullException("compoundOperators");
        }

        int expected = 0;
        if (selectCores.Length > 0)
        {
            expected = selectCores.Length - 1;
        }

        if (compoundOperators.Length != expected)
        {
            throw new ArgumentException("compound operator count must be one less than select core count");
        }
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

/// <summary>Provides common table expression functionality for the SQL model.</summary>
public class SqlCommonTableExpression
{
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The column names in source or insertion order.</summary>
    public SqlName[] ColumnNames;
    /// <summary>The materialization associated with this value.</summary>
    public SqlCteMaterializationKind Materialization;
    /// <summary>The nested query associated with this common table expression.</summary>
    public SqlQuery Query;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlCommonTableExpression"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="columnNames">The column names to use.</param>
    /// <param name="materialization">The materialization to use.</param>
    /// <param name="query">The query syntax tree to process.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">query is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlCommonTableExpression(SqlName name, SqlName[] columnNames, SqlCteMaterializationKind materialization, SqlQuery query, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        ValidateColumnNames(columnNames);
        if (query == null)
        {
            throw new ArgumentNullException("query");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Name = name;
        ColumnNames = columnNames;
        Materialization = materialization;
        Query = query;
        Span = span;
    }

    /// <summary>Returns whether column names is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasColumnNames()
    {
        return ColumnNames.Length > 0;
    }

    /// <summary>Returns the number of column name items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetColumnNameCount()
    {
        return ColumnNames.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetColumnNameAt(int index)
    {
        return ColumnNames[index];
    }

    /// <summary>Returns whether materialized.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsMaterialized()
    {
        return Materialization == SqlCteMaterializationKind.Materialized;
    }

    /// <summary>Returns whether not materialized.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsNotMaterialized()
    {
        return Materialization == SqlCteMaterializationKind.NotMaterialized;
    }

    internal static void ValidateColumnNames(SqlName[] columnNames)
    {
        if (columnNames == null)
        {
            throw new ArgumentNullException("columnNames");
        }

        int index = 0;
        while (index < columnNames.Length)
        {
            if (columnNames[index] == null)
            {
                throw new ArgumentNullException("columnNames");
            }

            index = index + 1;
        }
    }
}
