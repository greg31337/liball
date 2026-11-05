using System;

namespace sql;

/// <summary>Represents a single- or multi-column DML assignment.</summary>
public class SqlAssignment
{
    /// <summary>The assigned columns in source order.</summary>
    public SqlName[] Columns;
    /// <summary>The value assigned to the columns.</summary>
    public SqlExpression Value;
    /// <summary>The half-open source span covered by the assignment.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a DML assignment.</summary>
    /// <param name="columns">The assigned columns.</param>
    /// <param name="value">The assigned value.</param>
    /// <param name="span">The source span covered by the assignment.</param>
    public SqlAssignment(SqlName[] columns, SqlExpression value, SqlTextSpan span)
    {
        ValidateColumns(columns);
        Value = value ?? throw new ArgumentNullException("value");
        Span = span ?? throw new ArgumentNullException("span");
        Columns = columns;
    }

    internal static void ValidateColumns(SqlName[] columns)
    {
        if (columns == null)
        {
            throw new ArgumentNullException("columns");
        }

        int index = 0;
        while (index < columns.Length)
        {
            if (columns[index] == null)
            {
                throw new ArgumentNullException("columns");
            }

            index = index + 1;
        }

        if (columns.Length == 0)
        {
            throw new ArgumentException("columns must not be empty");
        }
    }
}

/// <summary>Represents an explicit or dialect-defined upsert conflict target.</summary>
public class SqlConflictTarget
{
    /// <summary>The target form.</summary>
    public SqlConflictTargetKind Kind;
    /// <summary>The indexed-column target terms.</summary>
    public SqlOrderingTerm[] IndexedColumns;
    /// <summary>The optional named constraint.</summary>
    public SqlName? ConstraintName;
    /// <summary>The optional target predicate.</summary>
    public SqlExpression? Predicate;
    /// <summary>The half-open source span covered by the target.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes an upsert conflict target.</summary>
    /// <param name="kind">The target form.</param>
    /// <param name="indexedColumns">The indexed-column terms.</param>
    /// <param name="constraintName">The optional constraint name.</param>
    /// <param name="predicate">The optional target predicate.</param>
    /// <param name="span">The source span covered by the target.</param>
    public SqlConflictTarget(SqlConflictTargetKind kind, SqlOrderingTerm[] indexedColumns, SqlName? constraintName, SqlExpression? predicate, SqlTextSpan span)
    {
        ValidateIndexedColumns(indexedColumns);
        Span = span ?? throw new ArgumentNullException("span");
        Kind = kind;
        IndexedColumns = indexedColumns;
        ConstraintName = constraintName;
        Predicate = predicate;
    }

    internal static void ValidateIndexedColumns(SqlOrderingTerm[] columns)
    {
        if (columns == null)
        {
            throw new ArgumentNullException("indexedColumns");
        }

        int index = 0;
        while (index < columns.Length)
        {
            if (columns[index] == null)
            {
                throw new ArgumentNullException("indexedColumns");
            }

            index = index + 1;
        }
    }
}
