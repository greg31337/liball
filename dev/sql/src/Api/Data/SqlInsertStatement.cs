using System;

namespace sql;

/// <summary>Represents an insert statement node in the SQL syntax tree.</summary>
public class SqlInsertStatement
{
    /// <summary>Indicates whether replace keyword.</summary>
    public bool IsReplaceKeyword;
    /// <summary>The conflict action associated with this value.</summary>
    public SqlConflictActionKind ConflictAction;
    /// <summary>The table name targeted or referenced by this node.</summary>
    public SqlName TableName;
    /// <summary>The optional alias, or <c>null</c> when no alias was written.</summary>
    public SqlName? Alias;
    /// <summary>The column names in source or insertion order.</summary>
    public SqlName[] ColumnNames;
    /// <summary>The source kind associated with this value.</summary>
    public SqlInsertSourceKind SourceKind;
    /// <summary>The values in source or insertion order.</summary>
    public SqlExpression[][] Values;
    /// <summary>The source query associated with this value.</summary>
    public SqlQuery? SourceQuery;
    /// <summary>The upsert clauses in source or insertion order.</summary>
    public SqlUpsertClause[] UpsertClauses;
    /// <summary>The returning associated with this value.</summary>
    public SqlReturningClause? Returning;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlInsertStatement"/> from the supplied values.</summary>
    /// <param name="isReplaceKeyword">The is replace keyword to use.</param>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="columnNames">The column names to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlInsertStatement Create(bool isReplaceKeyword, SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName[] columnNames, SqlTextSpan span)
    {
        return new SqlInsertStatement(isReplaceKeyword, conflictAction, tableName, alias, columnNames, SqlInsertSourceKind.Unknown, new SqlExpression[0][], null, new SqlUpsertClause[0], null, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlInsertStatement"/> from the supplied values.</summary>
    /// <param name="isReplaceKeyword">The is replace keyword to use.</param>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="columnNames">The column names to use.</param>
    /// <param name="sourceKind">The source kind to use.</param>
    /// <param name="values">The values to use.</param>
    /// <param name="sourceQuery">The source query to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlInsertStatement Create(bool isReplaceKeyword, SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName[] columnNames, SqlInsertSourceKind sourceKind, SqlExpression[][] values, SqlQuery? sourceQuery, SqlTextSpan span)
    {
        return new SqlInsertStatement(isReplaceKeyword, conflictAction, tableName, alias, columnNames, sourceKind, values, sourceQuery, new SqlUpsertClause[0], null, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlInsertStatement"/> from the supplied values.</summary>
    /// <param name="isReplaceKeyword">The is replace keyword to use.</param>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="columnNames">The column names to use.</param>
    /// <param name="sourceKind">The source kind to use.</param>
    /// <param name="values">The values to use.</param>
    /// <param name="sourceQuery">The source query to use.</param>
    /// <param name="upsertClauses">The upsert clauses to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <returns>The resulting value.</returns>
    internal static SqlInsertStatement Create(bool isReplaceKeyword, SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName[] columnNames, SqlInsertSourceKind sourceKind, SqlExpression[][] values, SqlQuery? sourceQuery, SqlUpsertClause[] upsertClauses, SqlTextSpan span)
    {
        return new SqlInsertStatement(isReplaceKeyword, conflictAction, tableName, alias, columnNames, sourceKind, values, sourceQuery, upsertClauses, null, span);
    }

    /// <summary>Initializes a new instance of <see cref="SqlInsertStatement"/> from the supplied values.</summary>
    /// <param name="isReplaceKeyword">The is replace keyword to use.</param>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="alias">The alias to use.</param>
    /// <param name="columnNames">The column names to use.</param>
    /// <param name="sourceKind">The source kind to use.</param>
    /// <param name="values">The values to use.</param>
    /// <param name="sourceQuery">The source query to use.</param>
    /// <param name="upsertClauses">The upsert clauses to use.</param>
    /// <param name="returning">The returning to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlInsertStatement(bool isReplaceKeyword, SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName[] columnNames, SqlInsertSourceKind sourceKind, SqlExpression[][] values, SqlQuery? sourceQuery, SqlUpsertClause[] upsertClauses, SqlReturningClause? returning, SqlTextSpan span)
    {
        if (tableName == null)
        {
            throw new ArgumentNullException("tableName");
        }

        ValidateColumnNames(columnNames);
        ValidateValues(values);
        ValidateUpsertClauses(upsertClauses);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IsReplaceKeyword = isReplaceKeyword;
        ConflictAction = conflictAction;
        TableName = tableName;
        Alias = alias;
        ColumnNames = columnNames;
        SourceKind = sourceKind;
        Values = values;
        SourceQuery = sourceQuery;
        UpsertClauses = upsertClauses;
        Returning = returning;
        Span = span;
    }

    /// <summary>Creates an insert statement from all supported clauses.</summary>
    /// <param name="isReplaceKeyword">The replace-keyword flag to use.</param>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="tableName">The target table name to use.</param>
    /// <param name="alias">The optional alias to use.</param>
    /// <param name="columnNames">The column names to use.</param>
    /// <param name="sourceKind">The source kind to use.</param>
    /// <param name="values">The values to use.</param>
    /// <param name="sourceQuery">The optional source query to use.</param>
    /// <param name="upsertClauses">The upsert clauses to use.</param>
    /// <param name="returning">The optional returning clause to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting insert statement.</returns>
    internal static SqlInsertStatement Create(bool isReplaceKeyword, SqlConflictActionKind conflictAction, SqlName tableName, SqlName? alias, SqlName[] columnNames, SqlInsertSourceKind sourceKind, SqlExpression[][] values, SqlQuery? sourceQuery, SqlUpsertClause[] upsertClauses, SqlReturningClause? returning, SqlTextSpan span)
    {
        return new SqlInsertStatement(isReplaceKeyword, conflictAction, tableName, alias, columnNames, sourceKind, values, sourceQuery, upsertClauses, returning, span);
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

    /// <summary>Returns whether values is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasValues()
    {
        return Values.Length > 0;
    }

    /// <summary>Returns the number of value row items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetValueRowCount()
    {
        return Values.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting expression.</returns>
    internal SqlExpression[] GetValueRowAt(int index)
    {
        return Values[index];
    }

    /// <summary>Returns whether source query is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSourceQuery()
    {
        return SourceQuery != null;
    }

    /// <summary>Returns whether upsert clauses is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasUpsertClauses()
    {
        return UpsertClauses.Length > 0;
    }

    /// <summary>Returns the number of upsert clause items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetUpsertClauseCount()
    {
        return UpsertClauses.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting upsert clause.</returns>
    internal SqlUpsertClause GetUpsertClauseAt(int index)
    {
        return UpsertClauses[index];
    }

    /// <summary>Returns whether returning is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasReturning()
    {
        return Returning != null;
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

    internal static void ValidateValues(SqlExpression[][] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentNullException("values");
            }

            int valueIndex = 0;
            while (valueIndex < values[index].Length)
            {
                if (values[index][valueIndex] == null)
                {
                    throw new ArgumentNullException("values");
                }

                valueIndex = valueIndex + 1;
            }

            index = index + 1;
        }
    }

    internal static void ValidateUpsertClauses(SqlUpsertClause[] upsertClauses)
    {
        if (upsertClauses == null)
        {
            throw new ArgumentNullException("upsertClauses");
        }

        int index = 0;
        while (index < upsertClauses.Length)
        {
            if (upsertClauses[index] == null)
            {
                throw new ArgumentNullException("upsertClauses");
            }

            index = index + 1;
        }
    }
}

/// <summary>Represents an upsert clause node in the SQL syntax tree.</summary>
public class SqlUpsertClause
{
    /// <summary>The syntax introducing this clause.</summary>
    public SqlUpsertSyntaxKind SyntaxKind;
    /// <summary>The typed conflict target.</summary>
    public SqlConflictTarget? ConflictTarget;
    /// <summary>The conflict targets in source or insertion order.</summary>
    public SqlOrderingTerm[] ConflictTargets;
    /// <summary>The conflict where associated with this value.</summary>
    public SqlExpression? ConflictWhere;
    /// <summary>The action kind associated with this value.</summary>
    public SqlUpsertActionKind ActionKind;
    /// <summary>The update columns in source or insertion order.</summary>
    public SqlName[] UpdateColumns;
    /// <summary>The update expressions in source or insertion order.</summary>
    public SqlExpression[] UpdateExpressions;
    /// <summary>The typed single- and multi-column update assignments.</summary>
    public SqlAssignment[] Assignments;
    /// <summary>The update where associated with this value.</summary>
    public SqlExpression? UpdateWhere;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlUpsertClause"/> from the supplied values.</summary>
    /// <param name="conflictTargets">The conflict targets to use.</param>
    /// <param name="conflictWhere">The conflict where to use.</param>
    /// <param name="actionKind">The action kind to use.</param>
    /// <param name="updateColumns">The update columns to use.</param>
    /// <param name="updateExpressions">The update expressions to use.</param>
    /// <param name="updateWhere">The update where to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlUpsertClause(SqlOrderingTerm[] conflictTargets, SqlExpression? conflictWhere, SqlUpsertActionKind actionKind, SqlName[] updateColumns, SqlExpression[] updateExpressions, SqlExpression? updateWhere, SqlTextSpan span)
    {
        ValidateOrderingTerms(conflictTargets);
        ValidateNames(updateColumns);
        ValidateExpressions(updateExpressions);
        if (updateColumns.Length != updateExpressions.Length)
        {
            throw new ArgumentException("update column count must match update expression count");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ConflictTargets = conflictTargets;
        ConflictWhere = conflictWhere;
        SyntaxKind = SqlUpsertSyntaxKind.OnConflict;
        ConflictTarget = null;
        if (conflictTargets.Length > 0 || conflictWhere != null)
        {
            ConflictTarget = new SqlConflictTarget(SqlConflictTargetKind.IndexedColumns, conflictTargets, null, conflictWhere, span);
        }

        ActionKind = actionKind;
        UpdateColumns = updateColumns;
        UpdateExpressions = updateExpressions;
        Assignments = CreateTypedAssignments(updateColumns, updateExpressions);
        UpdateWhere = updateWhere;
        Span = span;
    }

    /// <summary>Returns whether conflict targets is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasConflictTargets()
    {
        return ConflictTargets.Length > 0;
    }

    /// <summary>Returns the number of conflict target items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetConflictTargetCount()
    {
        return ConflictTargets.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting ordering term.</returns>
    internal SqlOrderingTerm GetConflictTargetAt(int index)
    {
        return ConflictTargets[index];
    }

    /// <summary>Returns whether conflict where is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasConflictWhere()
    {
        return ConflictWhere != null;
    }

    /// <summary>Returns whether update assignments is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasUpdateAssignments()
    {
        return UpdateColumns.Length > 0;
    }

    /// <summary>Returns the number of update assignment items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetUpdateAssignmentCount()
    {
        return UpdateColumns.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetUpdateColumnAt(int index)
    {
        return UpdateColumns[index];
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting expression.</returns>
    internal SqlExpression GetUpdateExpressionAt(int index)
    {
        return UpdateExpressions[index];
    }

    /// <summary>Returns whether update where is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasUpdateWhere()
    {
        return UpdateWhere != null;
    }

    internal static void ValidateOrderingTerms(SqlOrderingTerm[] terms)
    {
        if (terms == null)
        {
            throw new ArgumentNullException("conflictTargets");
        }

        int index = 0;
        while (index < terms.Length)
        {
            if (terms[index] == null)
            {
                throw new ArgumentNullException("conflictTargets");
            }

            index = index + 1;
        }
    }

    internal static void ValidateNames(SqlName[] names)
    {
        if (names == null)
        {
            throw new ArgumentNullException("updateColumns");
        }

        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == null)
            {
                throw new ArgumentNullException("updateColumns");
            }

            index = index + 1;
        }
    }

    internal static void ValidateExpressions(SqlExpression[] expressions)
    {
        if (expressions == null)
        {
            throw new ArgumentNullException("updateExpressions");
        }

        int index = 0;
        while (index < expressions.Length)
        {
            if (expressions[index] == null)
            {
                throw new ArgumentNullException("updateExpressions");
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
}
