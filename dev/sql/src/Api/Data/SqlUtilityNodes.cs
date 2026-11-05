using System;

namespace sql;

/// <summary>Represents a pragma statement node in the SQL syntax tree.</summary>
public class SqlPragmaStatement
{
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The value associated with this value.</summary>
    public SqlExpression? Value;
    /// <summary>The value kind associated with this value.</summary>
    public SqlPragmaValueKind ValueKind;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlPragmaStatement"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="valueKind">The value kind to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlPragmaStatement(SqlName name, SqlExpression? value, SqlPragmaValueKind valueKind, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Name = name;
        Value = value;
        ValueKind = valueKind;
        Span = span;
    }

    /// <summary>Returns whether value is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasValue()
    {
        return Value != null;
    }
}

/// <summary>Represents an analyze statement node in the SQL syntax tree.</summary>
public class SqlAnalyzeStatement
{
    /// <summary>The target name associated with this value.</summary>
    public SqlName? TargetName;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlAnalyzeStatement"/> from the supplied values.</summary>
    /// <param name="targetName">The target name to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlAnalyzeStatement(SqlName? targetName, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        TargetName = targetName;
        Span = span;
    }

    /// <summary>Returns whether target name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTargetName()
    {
        return TargetName != null;
    }
}

/// <summary>Represents a vacuum statement node in the SQL syntax tree.</summary>
public class SqlVacuumStatement
{
    /// <summary>The database name associated with this value.</summary>
    public SqlName? DatabaseName;
    /// <summary>The into expression associated with this value.</summary>
    public SqlExpression? IntoExpression;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlVacuumStatement"/> from the supplied values.</summary>
    /// <param name="databaseName">The database name to use.</param>
    /// <param name="intoExpression">The into expression to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlVacuumStatement(SqlName? databaseName, SqlExpression? intoExpression, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        DatabaseName = databaseName;
        IntoExpression = intoExpression;
        Span = span;
    }

    /// <summary>Returns whether database name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDatabaseName()
    {
        return DatabaseName != null;
    }

    /// <summary>Returns whether into expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasIntoExpression()
    {
        return IntoExpression != null;
    }
}

/// <summary>Represents an attach statement node in the SQL syntax tree.</summary>
public class SqlAttachStatement
{
    /// <summary>Indicates whether the optional <c>DATABASE</c> keyword was present.</summary>
    public bool HasDatabaseKeyword;
    /// <summary>The database expression associated with this value.</summary>
    public SqlExpression? DatabaseExpression;
    /// <summary>The schema name associated with this value.</summary>
    public SqlName? SchemaName;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlAttachStatement"/> from the supplied values.</summary>
    /// <param name="databaseExpression">The database expression to use.</param>
    /// <param name="schemaName">The schema name to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlAttachStatement(SqlExpression? databaseExpression, SqlName? schemaName, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        HasDatabaseKeyword = true;
        DatabaseExpression = databaseExpression;
        SchemaName = schemaName;
        Span = span;
    }

    /// <summary>Returns whether database expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDatabaseExpression()
    {
        return DatabaseExpression != null;
    }

    /// <summary>Returns whether schema name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSchemaName()
    {
        return SchemaName != null;
    }
}

/// <summary>Represents a detach statement node in the SQL syntax tree.</summary>
public class SqlDetachStatement
{
    /// <summary>Indicates whether the optional <c>DATABASE</c> keyword was present.</summary>
    public bool HasDatabaseKeyword;
    /// <summary>The schema name associated with this value.</summary>
    public SqlName? SchemaName;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlDetachStatement"/> from the supplied values.</summary>
    /// <param name="schemaName">The schema name to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlDetachStatement(SqlName? schemaName, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        HasDatabaseKeyword = true;
        SchemaName = schemaName;
        Span = span;
    }

    /// <summary>Returns whether schema name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSchemaName()
    {
        return SchemaName != null;
    }
}

/// <summary>Represents a reindex statement node in the SQL syntax tree.</summary>
public class SqlReindexStatement
{
    /// <summary>The target name associated with this value.</summary>
    public SqlName? TargetName;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlReindexStatement"/> from the supplied values.</summary>
    /// <param name="targetName">The target name to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlReindexStatement(SqlName? targetName, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        TargetName = targetName;
        Span = span;
    }

    /// <summary>Returns whether target name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTargetName()
    {
        return TargetName != null;
    }
}

/// <summary>Represents a transaction statement node in the SQL syntax tree.</summary>
public class SqlTransactionStatement
{
    /// <summary>The action kind associated with this value.</summary>
    public SqlTransactionActionKind ActionKind;
    /// <summary>The mode associated with this value.</summary>
    public SqlTransactionMode Mode;
    /// <summary>The has transaction keyword associated with this value.</summary>
    public bool HasTransactionKeyword;
    /// <summary>Indicates whether to savepoint.</summary>
    public bool IsToSavepoint;
    /// <summary>The savepoint name associated with this value.</summary>
    public SqlName? SavepointName;
    /// <summary>Indicates whether the optional <c>SAVEPOINT</c> keyword was present after <c>TO</c>.</summary>
    public bool HasSavepointKeyword;
    /// <summary>The typed rollback target, or <c>null</c> for other transaction forms.</summary>
    public SqlRollbackTarget? RollbackTarget;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlTransactionStatement"/> from the supplied values.</summary>
    /// <param name="actionKind">The action kind to use.</param>
    /// <param name="mode">The mode to use.</param>
    /// <param name="hasTransactionKeyword">The has transaction keyword to use.</param>
    /// <param name="isToSavepoint">The is to savepoint to use.</param>
    /// <param name="savepointName">The savepoint name to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlTransactionStatement(SqlTransactionActionKind actionKind, SqlTransactionMode mode, bool hasTransactionKeyword, bool isToSavepoint, SqlName? savepointName, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ActionKind = actionKind;
        Mode = mode;
        HasTransactionKeyword = hasTransactionKeyword;
        IsToSavepoint = isToSavepoint;
        SavepointName = savepointName;
        HasSavepointKeyword = isToSavepoint;
        if (isToSavepoint && savepointName != null)
        {
            RollbackTarget = new SqlRollbackTarget(true, savepointName, span);
        }
        else
        {
            RollbackTarget = null;
        }

        Span = span;
    }

    /// <summary>Returns whether savepoint name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSavepointName()
    {
        return SavepointName != null;
    }
}

/// <summary>Represents a savepoint statement node in the SQL syntax tree.</summary>
public class SqlSavepointStatement
{
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName? Name;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlSavepointStatement"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlSavepointStatement(SqlName? name, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Name = name;
        Span = span;
    }

    /// <summary>Returns whether name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasName()
    {
        return Name != null;
    }
}

/// <summary>Represents a release statement node in the SQL syntax tree.</summary>
public class SqlReleaseStatement
{
    /// <summary>The has savepoint keyword associated with this value.</summary>
    public bool HasSavepointKeyword;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName? Name;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlReleaseStatement"/> from the supplied values.</summary>
    /// <param name="hasSavepointKeyword">The has savepoint keyword to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlReleaseStatement(bool hasSavepointKeyword, SqlName? name, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        HasSavepointKeyword = hasSavepointKeyword;
        Name = name;
        Span = span;
    }

    /// <summary>Returns whether name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasName()
    {
        return Name != null;
    }
}

/// <summary>Represents an explain statement node in the SQL syntax tree.</summary>
public class SqlExplainStatement
{
    /// <summary>The explicit explain form.</summary>
    public SqlExplainKind Kind;
    /// <summary>Indicates whether query plan.</summary>
    public bool IsQueryPlan;
    /// <summary>The statement associated with this value.</summary>
    public SqlStatement? Statement;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlExplainStatement"/> from the supplied values.</summary>
    /// <param name="isQueryPlan">The is query plan to use.</param>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlExplainStatement(bool isQueryPlan, SqlStatement? statement, SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IsQueryPlan = isQueryPlan;
        if (isQueryPlan)
        {
            Kind = SqlExplainKind.QueryPlan;
        }
        else
        {
            Kind = SqlExplainKind.Explain;
        }

        Statement = statement;
        Span = span;
    }

    /// <summary>Returns whether statement is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasStatement()
    {
        return Statement != null;
    }
}
