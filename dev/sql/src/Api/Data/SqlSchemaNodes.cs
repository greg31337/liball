using System;

namespace sql;

/// <summary>Represents a table definition node in the SQL syntax tree.</summary>
public class SqlTableDefinition
{
    /// <summary>Indicates whether temporary.</summary>
    public bool IsTemporary;
    /// <summary>Indicates whether an <c>IF NOT EXISTS</c> clause was present.</summary>
    public bool IfNotExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The columns in source or insertion order.</summary>
    public SqlColumnDefinition[] Columns;
    /// <summary>The constraints in source or insertion order.</summary>
    public SqlTableConstraint[] Constraints;
    /// <summary>The nested query, or <c>null</c> when this table was not created from a query.</summary>
    public SqlQuery? Query;
    /// <summary>The without row id associated with this value.</summary>
    public bool WithoutRowId;
    /// <summary>Indicates whether strict.</summary>
    public bool IsStrict;
    /// <summary>The table options in source or insertion order.</summary>
    public SqlTableOptionKind[] Options;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlTableDefinition"/> from the supplied values.</summary>
    /// <param name="isTemporary">The is temporary to use.</param>
    /// <param name="ifNotExists">The if not exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="columns">The columns to use.</param>
    /// <param name="constraints">The constraints to use.</param>
    /// <param name="query">The query syntax tree to process.</param>
    /// <param name="withoutRowId">The without row id to use.</param>
    /// <param name="isStrict">The is strict to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlTableDefinition(bool isTemporary, bool ifNotExists, SqlName name, SqlColumnDefinition[] columns, SqlTableConstraint[] constraints, SqlQuery? query, bool withoutRowId, bool isStrict, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        ValidateColumns(columns);
        ValidateTableConstraints(constraints);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IsTemporary = isTemporary;
        IfNotExists = ifNotExists;
        Name = name;
        Columns = columns;
        Constraints = constraints;
        Query = query;
        WithoutRowId = withoutRowId;
        IsStrict = isStrict;
        if (isStrict && withoutRowId)
        {
            Options = new SqlTableOptionKind[] { SqlTableOptionKind.Strict, SqlTableOptionKind.WithoutRowId };
        }
        else if (isStrict)
        {
            Options = new SqlTableOptionKind[] { SqlTableOptionKind.Strict };
        }
        else if (withoutRowId)
        {
            Options = new SqlTableOptionKind[] { SqlTableOptionKind.WithoutRowId };
        }
        else
        {
            Options = new SqlTableOptionKind[0];
        }

        Span = span;
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
    /// <returns>The resulting column definition.</returns>
    internal SqlColumnDefinition GetColumnAt(int index)
    {
        return Columns[index];
    }

    /// <summary>Returns whether constraints is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasConstraints()
    {
        return Constraints.Length > 0;
    }

    /// <summary>Returns the number of constraint items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetConstraintCount()
    {
        return Constraints.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting table constraint.</returns>
    internal SqlTableConstraint GetConstraintAt(int index)
    {
        return Constraints[index];
    }

    /// <summary>Returns whether query is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasQuery()
    {
        return Query != null;
    }

    internal static void ValidateColumns(SqlColumnDefinition[] columns)
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
    }

    internal static void ValidateTableConstraints(SqlTableConstraint[] constraints)
    {
        if (constraints == null)
        {
            throw new ArgumentNullException("constraints");
        }

        int index = 0;
        while (index < constraints.Length)
        {
            if (constraints[index] == null)
            {
                throw new ArgumentNullException("constraints");
            }

            index = index + 1;
        }
    }
}

/// <summary>Represents an alter table definition node in the SQL syntax tree.</summary>
public class SqlAlterTableDefinition
{
    /// <summary>The table name targeted or referenced by this node.</summary>
    public SqlName TableName;
    /// <summary>The action kind associated with this value.</summary>
    public SqlAlterTableActionKind ActionKind;
    /// <summary>The new table name associated with this value.</summary>
    public SqlName? NewTableName;
    /// <summary>The column name associated with this value.</summary>
    public SqlName? ColumnName;
    /// <summary>The new column name associated with this value.</summary>
    public SqlName? NewColumnName;
    /// <summary>The column definition associated with this value.</summary>
    public SqlColumnDefinition? ColumnDefinition;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlAlterTableDefinition"/> from the supplied values.</summary>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="actionKind">The action kind to use.</param>
    /// <param name="newTableName">The new table name to use.</param>
    /// <param name="columnName">The column name to use.</param>
    /// <param name="newColumnName">The new column name to use.</param>
    /// <param name="columnDefinition">The column definition to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlAlterTableDefinition(SqlName tableName, SqlAlterTableActionKind actionKind, SqlName? newTableName, SqlName? columnName, SqlName? newColumnName, SqlColumnDefinition? columnDefinition, SqlTextSpan span)
    {
        if (tableName == null)
        {
            throw new ArgumentNullException("tableName");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        TableName = tableName;
        ActionKind = actionKind;
        NewTableName = newTableName;
        ColumnName = columnName;
        NewColumnName = newColumnName;
        ColumnDefinition = columnDefinition;
        Span = span;
    }

    /// <summary>Returns whether new table name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasNewTableName()
    {
        return NewTableName != null;
    }

    /// <summary>Returns whether column name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasColumnName()
    {
        return ColumnName != null;
    }

    /// <summary>Returns whether new column name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasNewColumnName()
    {
        return NewColumnName != null;
    }

    /// <summary>Returns whether column definition is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasColumnDefinition()
    {
        return ColumnDefinition != null;
    }
}

/// <summary>Represents a drop table definition node in the SQL syntax tree.</summary>
public class SqlDropTableDefinition
{
    /// <summary>Indicates whether an <c>IF EXISTS</c> clause was present.</summary>
    public bool IfExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlDropTableDefinition"/> from the supplied values.</summary>
    /// <param name="ifExists">The if exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlDropTableDefinition(bool ifExists, SqlName name, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IfExists = ifExists;
        Name = name;
        Span = span;
    }
}

/// <summary>Represents a drop index definition node in the SQL syntax tree.</summary>
public class SqlDropIndexDefinition
{
    /// <summary>Indicates whether an <c>IF EXISTS</c> clause was present.</summary>
    public bool IfExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlDropIndexDefinition"/> from the supplied values.</summary>
    /// <param name="ifExists">The if exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlDropIndexDefinition(bool ifExists, SqlName name, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IfExists = ifExists;
        Name = name;
        Span = span;
    }
}

/// <summary>Represents a drop view definition node in the SQL syntax tree.</summary>
public class SqlDropViewDefinition
{
    /// <summary>Indicates whether an <c>IF EXISTS</c> clause was present.</summary>
    public bool IfExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlDropViewDefinition"/> from the supplied values.</summary>
    /// <param name="ifExists">The if exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlDropViewDefinition(bool ifExists, SqlName name, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IfExists = ifExists;
        Name = name;
        Span = span;
    }
}

/// <summary>Represents a drop trigger definition node in the SQL syntax tree.</summary>
public class SqlDropTriggerDefinition
{
    /// <summary>Indicates whether an <c>IF EXISTS</c> clause was present.</summary>
    public bool IfExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlDropTriggerDefinition"/> from the supplied values.</summary>
    /// <param name="ifExists">The if exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlDropTriggerDefinition(bool ifExists, SqlName name, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IfExists = ifExists;
        Name = name;
        Span = span;
    }
}

/// <summary>Represents a column definition node in the SQL syntax tree.</summary>
public class SqlColumnDefinition
{
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The type name associated with this value.</summary>
    public string TypeName;
    /// <summary>The source type name associated with this value.</summary>
    public string SourceTypeName;
    /// <summary>The preserved declared type, or <c>null</c> for a typeless column.</summary>
    public SqlColumnTypeName? DeclaredType;
    /// <summary>The constraints in source or insertion order.</summary>
    public SqlColumnConstraint[] Constraints;
    /// <summary>The default expression associated with this value.</summary>
    public SqlExpression? DefaultExpression;
    /// <summary>The generated expression associated with this value.</summary>
    public SqlExpression? GeneratedExpression;
    /// <summary>The generated kind associated with this value.</summary>
    public SqlGeneratedColumnKind GeneratedKind;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlColumnDefinition"/> from the supplied values.</summary>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="typeName">The type name to use.</param>
    /// <param name="sourceTypeName">The source type name to use.</param>
    /// <param name="constraints">The constraints to use.</param>
    /// <param name="defaultExpression">The default expression to use.</param>
    /// <param name="generatedExpression">The generated expression to use.</param>
    /// <param name="generatedKind">The generated kind to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">typeName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">sourceTypeName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlColumnDefinition(SqlName name, string typeName, string sourceTypeName, SqlColumnConstraint[] constraints, SqlExpression? defaultExpression, SqlExpression? generatedExpression, SqlGeneratedColumnKind generatedKind, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (typeName == null)
        {
            throw new ArgumentNullException("typeName");
        }

        if (sourceTypeName == null)
        {
            throw new ArgumentNullException("sourceTypeName");
        }

        ValidateColumnConstraints(constraints);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        Name = name;
        TypeName = typeName;
        SourceTypeName = sourceTypeName;
        if (sourceTypeName.Length > 0)
        {
            DeclaredType = new SqlColumnTypeName(typeName, sourceTypeName, span);
        }
        else
        {
            DeclaredType = null;
        }

        Constraints = constraints;
        DefaultExpression = defaultExpression;
        GeneratedExpression = generatedExpression;
        GeneratedKind = generatedKind;
        Span = span;
    }

    /// <summary>Returns whether type name is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTypeName()
    {
        return TypeName.Length > 0;
    }

    /// <summary>Returns whether constraints is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasConstraints()
    {
        return Constraints.Length > 0;
    }

    /// <summary>Returns the number of constraint items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetConstraintCount()
    {
        return Constraints.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting column constraint.</returns>
    internal SqlColumnConstraint GetConstraintAt(int index)
    {
        return Constraints[index];
    }

    /// <summary>Returns whether default expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDefaultExpression()
    {
        return DefaultExpression != null;
    }

    /// <summary>Returns whether generated expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasGeneratedExpression()
    {
        return GeneratedExpression != null;
    }

    internal static void ValidateColumnConstraints(SqlColumnConstraint[] constraints)
    {
        if (constraints == null)
        {
            throw new ArgumentNullException("constraints");
        }

        int index = 0;
        while (index < constraints.Length)
        {
            if (constraints[index] == null)
            {
                throw new ArgumentNullException("constraints");
            }

            index = index + 1;
        }
    }
}

/// <summary>Represents an index definition node in the SQL syntax tree.</summary>
public class SqlIndexDefinition
{
    /// <summary>Indicates whether unique.</summary>
    public bool IsUnique;
    /// <summary>Indicates whether an <c>IF NOT EXISTS</c> clause was present.</summary>
    public bool IfNotExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The table name targeted or referenced by this node.</summary>
    public SqlName TableName;
    /// <summary>The key expressions in source or insertion order.</summary>
    public SqlOrderingTerm[] KeyExpressions;
    /// <summary>The indexed columns or expressions in source or insertion order.</summary>
    public SqlOrderingTerm[] IndexedColumns;
    /// <summary>The where expression associated with this value.</summary>
    public SqlExpression? WhereExpression;
    /// <summary>The partial-index predicate, or <c>null</c> for a non-partial index.</summary>
    public SqlExpression? Predicate;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlIndexDefinition"/> from the supplied values.</summary>
    /// <param name="isUnique">The is unique to use.</param>
    /// <param name="ifNotExists">The if not exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="tableName">The table name to use.</param>
    /// <param name="keyExpressions">The key expressions to use.</param>
    /// <param name="whereExpression">The where expression to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">tableName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlIndexDefinition(bool isUnique, bool ifNotExists, SqlName name, SqlName tableName, SqlOrderingTerm[] keyExpressions, SqlExpression? whereExpression, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (tableName == null)
        {
            throw new ArgumentNullException("tableName");
        }

        ValidateKeyExpressions(keyExpressions);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IsUnique = isUnique;
        IfNotExists = ifNotExists;
        Name = name;
        TableName = tableName;
        KeyExpressions = keyExpressions;
        IndexedColumns = keyExpressions;
        WhereExpression = whereExpression;
        Predicate = whereExpression;
        Span = span;
    }

    /// <summary>Returns the number of key expression items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetKeyExpressionCount()
    {
        return KeyExpressions.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting ordering term.</returns>
    internal SqlOrderingTerm GetKeyExpressionAt(int index)
    {
        return KeyExpressions[index];
    }

    /// <summary>Returns whether where expression is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasWhereExpression()
    {
        return WhereExpression != null;
    }

    internal static void ValidateKeyExpressions(SqlOrderingTerm[] keyExpressions)
    {
        if (keyExpressions == null)
        {
            throw new ArgumentNullException("keyExpressions");
        }

        int index = 0;
        while (index < keyExpressions.Length)
        {
            if (keyExpressions[index] == null)
            {
                throw new ArgumentNullException("keyExpressions");
            }

            index = index + 1;
        }
    }
}

/// <summary>Represents a view definition node in the SQL syntax tree.</summary>
public class SqlViewDefinition
{
    /// <summary>Indicates whether temporary.</summary>
    public bool IsTemporary;
    /// <summary>Indicates whether an <c>IF NOT EXISTS</c> clause was present.</summary>
    public bool IfNotExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The column names in source or insertion order.</summary>
    public SqlName[] ColumnNames;
    /// <summary>The nested query associated with this view definition.</summary>
    public SqlQuery Query;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlViewDefinition"/> from the supplied values.</summary>
    /// <param name="isTemporary">The is temporary to use.</param>
    /// <param name="ifNotExists">The if not exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="columnNames">The column names to use.</param>
    /// <param name="query">The query syntax tree to process.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">query is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlViewDefinition(bool isTemporary, bool ifNotExists, SqlName name, SqlName[] columnNames, SqlQuery query, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        ValidateNames("columnNames", columnNames);
        if (query == null)
        {
            throw new ArgumentNullException("query");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IsTemporary = isTemporary;
        IfNotExists = ifNotExists;
        Name = name;
        ColumnNames = columnNames;
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

/// <summary>Represents a virtual table definition node in the SQL syntax tree.</summary>
public class SqlVirtualTableDefinition
{
    /// <summary>Indicates whether an <c>IF NOT EXISTS</c> clause was present.</summary>
    public bool IfNotExists;
    /// <summary>The SQL name stored by this model.</summary>
    public SqlName Name;
    /// <summary>The module name associated with this value.</summary>
    public SqlName ModuleName;
    /// <summary>The module arguments in source or insertion order.</summary>
    public SqlToken[] ModuleArguments;
    /// <summary>The preserved module arguments in source or insertion order.</summary>
    public SqlVirtualTableArgument[] Arguments;
    /// <summary>The half-open source span covered by this value.</summary>
    public SqlTextSpan Span;

    /// <summary>Initializes a new instance of <see cref="SqlVirtualTableDefinition"/> from the supplied values.</summary>
    /// <param name="ifNotExists">The if not exists to use.</param>
    /// <param name="name">The name to use or locate.</param>
    /// <param name="moduleName">The module name to use.</param>
    /// <param name="moduleArguments">The module arguments to use.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <exception cref="ArgumentNullException">name is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">moduleName is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    public SqlVirtualTableDefinition(bool ifNotExists, SqlName name, SqlName moduleName, SqlToken[] moduleArguments, SqlTextSpan span)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (moduleName == null)
        {
            throw new ArgumentNullException("moduleName");
        }

        ValidateTokens(moduleArguments);
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        IfNotExists = ifNotExists;
        Name = name;
        ModuleName = moduleName;
        ModuleArguments = moduleArguments;
        Arguments = new SqlVirtualTableArgument[0];
        Span = span;
    }

    /// <summary>Returns whether module arguments is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasModuleArguments()
    {
        return ModuleArguments.Length > 0;
    }

    /// <summary>Returns the number of module argument items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetModuleArgumentCount()
    {
        return ModuleArguments.Length;
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting token.</returns>
    internal SqlToken GetModuleArgumentAt(int index)
    {
        return ModuleArguments[index];
    }

    internal static void ValidateTokens(SqlToken[] tokens)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("moduleArguments");
        }

        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == null)
            {
                throw new ArgumentNullException("moduleArguments");
            }

            index = index + 1;
        }
    }
}
