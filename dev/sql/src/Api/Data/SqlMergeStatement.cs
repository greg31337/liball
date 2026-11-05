using System;

namespace sql;

/// <summary>Represents a merge statement node in the SQL syntax tree.</summary>
public class SqlMergeStatement
{
    /// <summary>The target table name.</summary>
    public SqlName TableName;
    /// <summary>The optional target alias.</summary>
    public SqlName? Alias;
    /// <summary>The source table reference.</summary>
    public SqlTableReference Source;
    /// <summary>The target-source match condition.</summary>
    public SqlExpression Condition;
    /// <summary>The conditional actions in source order.</summary>
    public SqlMergeWhenClause[] Clauses;
    /// <summary>The optional returning clause.</summary>
    public SqlReturningClause? Returning;
    /// <summary>The half-open source span covered by the statement.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a merge statement.</summary>
    /// <param name="tableName">The target table name.</param>
    /// <param name="alias">The optional target alias.</param>
    /// <param name="source">The source table reference.</param>
    /// <param name="condition">The match condition.</param>
    /// <param name="clauses">The conditional actions.</param>
    /// <param name="returning">The optional returning clause.</param>
    /// <param name="span">The source span covered by the statement.</param>
    public SqlMergeStatement(SqlName tableName, SqlName? alias, SqlTableReference source, SqlExpression condition, SqlMergeWhenClause[] clauses, SqlReturningClause? returning, SqlTextSpan span)
    {
        TableName = tableName ?? throw new ArgumentNullException("tableName");
        Source = source ?? throw new ArgumentNullException("source");
        Condition = condition ?? throw new ArgumentNullException("condition");
        ValidateClauses(clauses);
        Span = span ?? throw new ArgumentNullException("span");
        Alias = alias;
        Clauses = clauses;
        Returning = returning;
    }

    internal static void ValidateClauses(SqlMergeWhenClause[] clauses)
    {
        if (clauses == null)
        {
            throw new ArgumentNullException("clauses");
        }

        int index = 0;
        while (index < clauses.Length)
        {
            if (clauses[index] == null)
            {
                throw new ArgumentNullException("clauses");
            }

            index = index + 1;
        }
    }
}

/// <summary>Represents one conditional action in a merge statement.</summary>
public class SqlMergeWhenClause
{
    /// <summary>The source-target relationship that activates the clause.</summary>
    public SqlMergeMatchKind MatchKind;
    /// <summary>The optional additional condition.</summary>
    public SqlExpression? Condition;
    /// <summary>The action performed by the clause.</summary>
    public SqlMergeActionKind ActionKind;
    /// <summary>The update assignments.</summary>
    public SqlAssignment[] Assignments;
    /// <summary>The columns used by an insert action.</summary>
    public SqlName[] InsertColumns;
    /// <summary>The values used by an insert action.</summary>
    public SqlExpression[] InsertValues;
    /// <summary>The half-open source span covered by the clause.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a merge conditional action.</summary>
    /// <param name="matchKind">The source-target relationship.</param>
    /// <param name="condition">The optional additional condition.</param>
    /// <param name="actionKind">The action kind.</param>
    /// <param name="assignments">The update assignments.</param>
    /// <param name="insertColumns">The insert columns.</param>
    /// <param name="insertValues">The insert values.</param>
    /// <param name="span">The source span covered by the clause.</param>
    public SqlMergeWhenClause(SqlMergeMatchKind matchKind, SqlExpression? condition, SqlMergeActionKind actionKind, SqlAssignment[] assignments, SqlName[] insertColumns, SqlExpression[] insertValues, SqlTextSpan span)
    {
        ValidateAssignments(assignments);
        ValidateNames(insertColumns);
        ValidateExpressions(insertValues);
        Span = span ?? throw new ArgumentNullException("span");
        MatchKind = matchKind;
        Condition = condition;
        ActionKind = actionKind;
        Assignments = assignments;
        InsertColumns = insertColumns;
        InsertValues = insertValues;
    }

    internal static void ValidateAssignments(SqlAssignment[] assignments)
    {
        if (assignments == null)
        {
            throw new ArgumentNullException("assignments");
        }

        int index = 0;
        while (index < assignments.Length)
        {
            if (assignments[index] == null)
            {
                throw new ArgumentNullException("assignments");
            }

            index = index + 1;
        }
    }

    internal static void ValidateNames(SqlName[] names)
    {
        if (names == null)
        {
            throw new ArgumentNullException("insertColumns");
        }

        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == null)
            {
                throw new ArgumentNullException("insertColumns");
            }

            index = index + 1;
        }
    }

    internal static void ValidateExpressions(SqlExpression[] expressions)
    {
        if (expressions == null)
        {
            throw new ArgumentNullException("insertValues");
        }

        int index = 0;
        while (index < expressions.Length)
        {
            if (expressions[index] == null)
            {
                throw new ArgumentNullException("insertValues");
            }

            index = index + 1;
        }
    }
}
