using System;

namespace sql;

/// <summary>Represents a column constraint node in the SQL syntax tree.</summary>
public class SqlColumnConstraint
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlConstraintKind Kind;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName? Name;
    /// <summary>The expression, or <c>null</c> when this syntax form has no expression.</summary>
    public SqlExpression? Expression;
    /// <summary>The collation name associated with this value.</summary>
    public SqlName? CollationName;
    /// <summary>The conflict action associated with this value.</summary>
    public SqlConflictActionKind ConflictAction;
    /// <summary>The references associated with this value.</summary>
    public SqlForeignKeyReference? References;
    /// <summary>The generated kind associated with this value.</summary>
    public SqlGeneratedColumnKind GeneratedKind;
    /// <summary>Indicates whether autoincrement.</summary>
    public bool IsAutoincrement;
    /// <summary>The optional ordering direction on a column primary key.</summary>
    public SqlOrderingDirection PrimaryKeyDirection;
    /// <summary>Indicates whether the generated constraint used <c>GENERATED ALWAYS</c>.</summary>
    public bool IsGeneratedAlways;
    /// <summary>Indicates whether the generated storage kind was explicitly written.</summary>
    public bool HasGeneratedStorageKind;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlColumnConstraint"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <param name="collationName">The collation name to use.</param>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="references">The references to use.</param>
    /// <param name="generatedKind">The generated kind to use.</param>
    /// <param name="isAutoincrement">The is autoincrement to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlColumnConstraint(SqlConstraintKind kind, SqlName? name, SqlExpression? expression, SqlName? collationName, SqlConflictActionKind conflictAction, SqlForeignKeyReference? references, SqlGeneratedColumnKind generatedKind, bool isAutoincrement, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Kind = kind;
        Name = name;
        Expression = expression;
        CollationName = collationName;
        ConflictAction = conflictAction;
        References = references;
        GeneratedKind = generatedKind;
        IsAutoincrement = isAutoincrement;
        PrimaryKeyDirection = SqlOrderingDirection.None;
        IsGeneratedAlways = kind == SqlConstraintKind.Generated;
        HasGeneratedStorageKind = generatedKind != SqlGeneratedColumnKind.None;
        Span = span;
    }

    /// <summary>Returns whether name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasName()
    {
        return Name != null;
    }

    /// <summary>Returns whether expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasExpression()
    {
        return Expression != null;
    }

    /// <summary>Returns whether collation name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasCollationName()
    {
        return CollationName != null;
    }

    /// <summary>Returns whether conflict action is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasConflictAction()
    {
        return ConflictAction != SqlConflictActionKind.None;
    }

    /// <summary>Returns whether references is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasReferences()
    {
        return References != null;
    }

    /// <summary>Returns whether generated.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsGenerated()
    {
        return Kind == SqlConstraintKind.Generated;
    }
}

/// <summary>Represents a table constraint node in the SQL syntax tree.</summary>
public class SqlTableConstraint
{
    /// <summary>The discriminator that identifies this value's syntax form.</summary>
    public SqlConstraintKind Kind;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName? Name;
    /// <summary>The columns in source or insertion order.</summary>
    public SqlName[] Columns;
    /// <summary>The indexed columns in source or insertion order.</summary>
    public SqlOrderingTerm[] IndexedColumns;
    /// <summary>The expressions in source or insertion order.</summary>
    public SqlExpression[] Expressions;
    /// <summary>The conflict action associated with this value.</summary>
    public SqlConflictActionKind ConflictAction;
    /// <summary>The references associated with this value.</summary>
    public SqlForeignKeyReference? References;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlTableConstraint"/> from the supplied values.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="columns">The columns to use.</param>
    /// <param name="expressions">The expressions to use.</param>
    /// <param name="conflictAction">The conflict action to use.</param>
    /// <param name="references">The references to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlTableConstraint(SqlConstraintKind kind, SqlName? name, SqlName[] columns, SqlExpression[] expressions, SqlConflictActionKind conflictAction, SqlForeignKeyReference? references, SqlTextSpan span)
    {
        ValidateNames("columns", columns);
        ValidateExpressions(expressions);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Kind = kind;
        Name = name;
        Columns = columns;
        IndexedColumns = new SqlOrderingTerm[0];
        Expressions = expressions;
        ConflictAction = conflictAction;
        References = references;
        Span = span;
    }

    /// <summary>Returns whether name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasName()
    {
        return Name != null;
    }

    /// <summary>Returns whether columns is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasColumns()
    {
        return Columns.Length > 0;
    }

    /// <summary>Returns the number of column items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetColumnCount()
    {
        return Columns.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting name.</returns>
    internal SqlName GetColumnAt(int index)
    {
        return Columns[index];
    }

    /// <summary>Returns whether expressions is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasExpressions()
    {
        return Expressions.Length > 0;
    }

    /// <summary>Returns the number of expression items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetExpressionCount()
    {
        return Expressions.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting expression.</returns>
    internal SqlExpression GetExpressionAt(int index)
    {
        return Expressions[index];
    }

    /// <summary>Returns whether conflict action is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasConflictAction()
    {
        return ConflictAction != SqlConflictActionKind.None;
    }

    /// <summary>Returns whether references is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasReferences()
    {
        return References != null;
    }

    internal static void ValidateNames(string parameterName, SqlName[] names)
    {
        if (names == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            index = index + 1;
        }
    }

    internal static void ValidateExpressions(SqlExpression[] expressions)
    {
        if (expressions == null)
        {
            throw new ArgumentNullException("expressions");
        }

        int index = 0;
        while (index < expressions.Length)
        {
            if (expressions[index] == null)
            {
                throw new ArgumentNullException("expressions");
            }

            index = index + 1;
        }
    }
}

/// <summary>Represents a foreign key reference node in the SQL syntax tree.</summary>
public class SqlForeignKeyReference
{
    /// <summary>The table name targeted or referenced by this node.</summary>
    public SqlName TableName;
    /// <summary>The column names in source or insertion order.</summary>
    public SqlName[] ColumnNames;
    /// <summary>The on delete action associated with this value.</summary>
    public string OnDeleteAction;
    /// <summary>The on update action associated with this value.</summary>
    public string OnUpdateAction;
    /// <summary>The match name associated with this value.</summary>
    public SqlName? MatchName;
    /// <summary>Indicates whether deferrable.</summary>
    public bool IsDeferrable;
    /// <summary>Indicates whether initially deferred.</summary>
    public bool IsInitiallyDeferred;
    /// <summary>The typed <c>ON DELETE</c> action.</summary>
    public SqlForeignKeyActionKind OnDelete;
    /// <summary>The typed <c>ON UPDATE</c> action.</summary>
    public SqlForeignKeyActionKind OnUpdate;
    /// <summary>The explicit deferrability form.</summary>
    public SqlForeignKeyDeferrabilityKind Deferrability;
    /// <summary>The explicit initial timing form.</summary>
    public SqlForeignKeyInitialTimingKind InitialTiming;
    /// <summary>The foreign-key clauses in source or insertion order.</summary>
    public SqlForeignKeyClause[] Clauses;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlForeignKeyReference"/> from the supplied values.</summary>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="columnNames">The column names to use.</param>
    /// <param name="onDeleteAction">The on delete action to use.</param>
    /// <param name="onUpdateAction">The on update action to use.</param>
    /// <param name="matchName">The match name to use.</param>
    /// <param name="isDeferrable">The is deferrable to use.</param>
    /// <param name="isInitiallyDeferred">The is initially deferred to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">onDeleteAction is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">onUpdateAction is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlForeignKeyReference(SqlName tableName, SqlName[] columnNames, string onDeleteAction, string onUpdateAction, SqlName? matchName, bool isDeferrable, bool isInitiallyDeferred, SqlTextSpan span)
    {
        if (tableName == null)
        {
            throw new ArgumentNullException("tableName");
        }

        ValidateNames("columnNames", columnNames);
        if (onDeleteAction == null)
        {
            throw new ArgumentNullException("onDeleteAction");
        }

        if (onUpdateAction == null)
        {
            throw new ArgumentNullException("onUpdateAction");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        TableName = tableName;
        ColumnNames = columnNames;
        OnDeleteAction = onDeleteAction;
        OnUpdateAction = onUpdateAction;
        MatchName = matchName;
        IsDeferrable = isDeferrable;
        IsInitiallyDeferred = isInitiallyDeferred;
        OnDelete = GetActionKind(onDeleteAction);
        OnUpdate = GetActionKind(onUpdateAction);
        if (isDeferrable)
        {
            Deferrability = SqlForeignKeyDeferrabilityKind.Deferrable;
        }
        else
        {
            Deferrability = SqlForeignKeyDeferrabilityKind.None;
        }

        if (isInitiallyDeferred)
        {
            InitialTiming = SqlForeignKeyInitialTimingKind.Deferred;
        }
        else
        {
            InitialTiming = SqlForeignKeyInitialTimingKind.None;
        }

        Clauses = new SqlForeignKeyClause[0];
        Span = span;
    }

    internal static SqlForeignKeyActionKind GetActionKind(string action)
    {
        if (action == "NO ACTION")
        {
            return SqlForeignKeyActionKind.NoAction;
        }

        if (action == "RESTRICT")
        {
            return SqlForeignKeyActionKind.Restrict;
        }

        if (action == "SET NULL")
        {
            return SqlForeignKeyActionKind.SetNull;
        }

        if (action == "SET DEFAULT")
        {
            return SqlForeignKeyActionKind.SetDefault;
        }

        if (action == "CASCADE")
        {
            return SqlForeignKeyActionKind.Cascade;
        }

        return SqlForeignKeyActionKind.None;
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

    /// <summary>Returns whether on delete action is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasOnDeleteAction()
    {
        return OnDeleteAction.Length > 0;
    }

    /// <summary>Returns whether on update action is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasOnUpdateAction()
    {
        return OnUpdateAction.Length > 0;
    }

    /// <summary>Returns whether match name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasMatchName()
    {
        return MatchName != null;
    }

    internal static void ValidateNames(string parameterName, SqlName[] names)
    {
        if (names == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            index = index + 1;
        }
    }
}
