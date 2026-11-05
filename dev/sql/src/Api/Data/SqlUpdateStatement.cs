using System;

namespace sql;

/// <summary>Represents an update statement node in the SQL syntax tree.</summary>
public class SqlUpdateStatement
{
    /// <summary>The conflict action associated with this value.</summary>
    public SqlConflictActionKind ConflictAction;
    /// <summary>The table name targeted or referenced by this node.</summary>
    public SqlName TableName;
    /// <summary>The optional alias, or <c>null</c> when no alias was written.</summary>
    public SqlName? Alias;
    /// <summary>The indexed by associated with this value.</summary>
    public SqlName? IndexedBy;
    /// <summary>Indicates whether not indexed.</summary>
    public bool IsNotIndexed;
    /// <summary>The assignment columns in source or insertion order.</summary>
    public SqlName[] AssignmentColumns;
    /// <summary>The assignment expressions in source or insertion order.</summary>
    public SqlExpression[] AssignmentExpressions;
    /// <summary>The typed single- and multi-column assignments.</summary>
    public SqlAssignment[] Assignments;
    /// <summary>The from in source or insertion order.</summary>
    public SqlTableReference[] From;
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

    /// <summary>Initializes a new instance of <see cref="SqlUpdateStatement"/> from the supplied values.</summary>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="assignmentColumns">The assignment columns to use.</param>
    /// <param name="assignmentExpressions">The assignment expressions to use.</param>
    /// <param name="where">The where to use.</param>
    /// <param name="returning">The returning to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlUpdateStatement Create(SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName[] assignmentColumns, SqlExpression[] assignmentExpressions, SqlExpression? where, SqlReturningClause? returning, SqlTextSpan span)
    {
        return new SqlUpdateStatement(conflictAction, tableName, alias, null, false, assignmentColumns, assignmentExpressions, new SqlTableReference[0], where, returning, new SqlOrderingTerm[0], null, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlUpdateStatement"/> from the supplied values.</summary>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="assignmentColumns">The assignment columns to use.</param>
    /// <param name="assignmentExpressions">The assignment expressions to use.</param>
    /// <param name="from">The from to use.</param>
    /// <param name="where">The where to use.</param>
    /// <param name="returning">The returning to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlUpdateStatement Create(SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName[] assignmentColumns, SqlExpression[] assignmentExpressions, SqlTableReference[] from, SqlExpression? where, SqlReturningClause? returning, SqlTextSpan span)
    {
        return new SqlUpdateStatement(conflictAction, tableName, alias, null, false, assignmentColumns, assignmentExpressions, from, where, returning, new SqlOrderingTerm[0], null, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlUpdateStatement"/> from the supplied values.</summary>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="indexedBy">The indexed by to use.</param>
    /// <param name="isNotIndexed">The is not indexed to use.</param>
    /// <param name="assignmentColumns">The assignment columns to use.</param>
    /// <param name="assignmentExpressions">The assignment expressions to use.</param>
    /// <param name="from">The from to use.</param>
    /// <param name="where">The where to use.</param>
    /// <param name="returning">The returning to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlUpdateStatement Create(SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName? indexedBy, bool isNotIndexed, SqlName[] assignmentColumns, SqlExpression[] assignmentExpressions, SqlTableReference[] from, SqlExpression? where, SqlReturningClause? returning, SqlTextSpan span)
    {
        return new SqlUpdateStatement(conflictAction, tableName, alias, indexedBy, isNotIndexed, assignmentColumns, assignmentExpressions, from, where, returning, new SqlOrderingTerm[0], null, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlUpdateStatement"/> from the supplied values.</summary>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="indexedBy">The indexed by to use.</param>
    /// <param name="isNotIndexed">The is not indexed to use.</param>
    /// <param name="assignmentColumns">The assignment columns to use.</param>
    /// <param name="assignmentExpressions">The assignment expressions to use.</param>
    /// <param name="from">The from to use.</param>
    /// <param name="where">The where to use.</param>
    /// <param name="returning">The returning to use.</param>
    /// <param name="orderBy">The order by to use.</param>
    /// <param name="limit">The limit to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlUpdateStatement(SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName? indexedBy, bool isNotIndexed, SqlName[] assignmentColumns, SqlExpression[] assignmentExpressions, SqlTableReference[] from, SqlExpression? where, SqlReturningClause? returning, SqlOrderingTerm[] orderBy, SqlLimitClause? limit, SqlTextSpan span)
    {
        if (tableName == null)
        {
            throw new ArgumentNullException("tableName");
        }

        ValidateNames(assignmentColumns);
        ValidateExpressions(assignmentExpressions);
        ValidateTableReferences(from);
        ValidateOrderBy(orderBy);
        if (assignmentColumns.Length != assignmentExpressions.Length)
        {
            throw new ArgumentException("assignment column count must match assignment expression count");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ConflictAction = conflictAction;
        TableName = tableName;
        Alias = alias;
        IndexedBy = indexedBy;
        IsNotIndexed = isNotIndexed;
        AssignmentColumns = assignmentColumns;
        AssignmentExpressions = assignmentExpressions;
        Assignments = CreateTypedAssignments(assignmentColumns, assignmentExpressions);
        From = from;
        Where = where;
        Returning = returning;
        OrderBy = orderBy;
        Limit = limit;
        Span = span;
    }

    /// <summary>Creates an update statement from all supported clauses.</summary>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The target table name to use.</param>
    /// <param name="alias">The optional alias to use.</param>
    /// <param name="indexedBy">The optional index name to use.</param>
    /// <param name="isNotIndexed">The not-indexed flag to use.</param>
    /// <param name="assignmentColumns">The assignment columns to use.</param>
    /// <param name="assignmentExpressions">The assignment expressions to use.</param>
    /// <param name="from">The table references to use.</param>
    /// <param name="where">The optional predicate to use.</param>
    /// <param name="returning">The optional returning clause to use.</param>
    /// <param name="orderBy">The ordering terms to use.</param>
    /// <param name="limit">The optional limit to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting update statement.</returns>
    internal static SqlUpdateStatement Create(SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName? indexedBy, bool isNotIndexed, SqlName[] assignmentColumns, SqlExpression[] assignmentExpressions, SqlTableReference[] from, SqlExpression? where, SqlReturningClause? returning, SqlOrderingTerm[] orderBy, SqlLimitClause? limit, SqlTextSpan span)
    {
        return new SqlUpdateStatement(conflictAction, tableName, alias, indexedBy, isNotIndexed, assignmentColumns, assignmentExpressions, from, where, returning, orderBy, limit, span);
    }

    /// <summary>Returns whether conflict action is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasConflictAction()
    {
        return ConflictAction != SqlConflictActionKind.None;
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

    /// <summary>Returns whether assignments is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasAssignments()
    {
        return AssignmentColumns.Length > 0;
    }

    /// <summary>Returns the number of assignment items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetAssignmentCount()
    {
        return AssignmentColumns.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetAssignmentColumnAt(int index)
    {
        return AssignmentColumns[index];
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting expression.</returns>
    internal SqlExpression GetAssignmentExpressionAt(int index)
    {
        return AssignmentExpressions[index];
    }

    /// <summary>Returns whether from is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasFrom()
    {
        return From.Length > 0;
    }

    /// <summary>Returns the number of from items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetFromCount()
    {
        return From.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting table reference.</returns>
    internal SqlTableReference GetFromAt(int index)
    {
        return From[index];
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

    internal static void ValidateNames(SqlName[] names)
    {
        if (names == null)
        {
            throw new ArgumentNullException("assignmentColumns");
        }

        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == null)
            {
                throw new ArgumentNullException("assignmentColumns");
            }

            index = index + 1;
        }
    }

    internal static SqlAssignment[] CreateTypedAssignments(SqlName[] columns, SqlExpression[] expressions)
    {
        SqlAssignment[] assignments = new SqlAssignment[columns.Length];
        int index = 0;
        while (index < columns.Length)
        {
            SqlTextSpan span = SqlTextSpan.FromBounds(columns[index].Span.Start, expressions[index].Span.GetEnd());
            assignments[index] = new SqlAssignment(new SqlName[] { columns[index] }, expressions[index], span);
            index = index + 1;
        }

        return assignments;
    }

    internal static void ValidateExpressions(SqlExpression[] expressions)
    {
        if (expressions == null)
        {
            throw new ArgumentNullException("assignmentExpressions");
        }

        int index = 0;
        while (index < expressions.Length)
        {
            if (expressions[index] == null)
            {
                throw new ArgumentNullException("assignmentExpressions");
            }

            index = index + 1;
        }
    }

    internal static void ValidateTableReferences(SqlTableReference[] tableReferences)
    {
        if (tableReferences == null)
        {
            throw new ArgumentNullException("from");
        }

        int index = 0;
        while (index < tableReferences.Length)
        {
            if (tableReferences[index] == null)
            {
                throw new ArgumentNullException("from");
            }

            index = index + 1;
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
