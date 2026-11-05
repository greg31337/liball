using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides database-shaped generation operations.
/// </summary>
public class DatabaseProvider : IDatabaseProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal DatabaseProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("database");
    }

    /// <summary>Generates a project-authored portable database identifier.</summary>
    /// <returns>A lowercase, non-reserved database name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string DatabaseName()
    {
        return DatabaseGenerator.DatabaseName(Context, RandomValue);
    }

    /// <summary>Generates a project-authored portable schema identifier.</summary>
    /// <returns>A lowercase, non-reserved schema name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SchemaName()
    {
        return DatabaseGenerator.SchemaName(Context, RandomValue);
    }

    /// <summary>Generates a project-authored portable table identifier.</summary>
    /// <returns>A lowercase, non-reserved table name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string TableName()
    {
        return DatabaseGenerator.TableName(Context, RandomValue);
    }

    /// <summary>Generates a project-authored portable column identifier.</summary>
    /// <returns>A lowercase, non-reserved column name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string ColumnName()
    {
        return DatabaseGenerator.ColumnName(Context, RandomValue);
    }

    /// <summary>Generates an index identifier using an <c>ix_table_column</c> convention.</summary>
    /// <returns>A lowercase, non-reserved index name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string IndexName()
    {
        return DatabaseGenerator.IndexName(Context, RandomValue);
    }

    /// <summary>Generates a conventional database constraint identifier.</summary>
    /// <returns>A primary-key, foreign-key, unique, or check constraint name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string ConstraintName()
    {
        return DatabaseGenerator.ConstraintName(Context, RandomValue);
    }

    /// <summary>Generates a database constraint identifier for a requested category.</summary>
    /// <param name="kind">The requested constraint naming convention.</param>
    /// <returns>A lowercase constraint name with a category-specific prefix.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string ConstraintNameFromKind(DatabaseConstraintKind kind)
    {
        return DatabaseGenerator.ConstraintName(Context, RandomValue, kind);
    }

    /// <summary>Selects a conventional SQL type declaration.</summary>
    /// <remarks>Support for a selected type depends on the target database engine.</remarks>
    /// <returns>A reviewed SQL type string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Type()
    {
        return DatabaseGenerator.Type(Context, RandomValue);
    }

    /// <summary>Selects a conventional database collation label.</summary>
    /// <remarks>Collation availability and semantics depend on the target database engine and installation.</remarks>
    /// <returns>A reviewed collation name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Collation()
    {
        return DatabaseGenerator.Collation(Context, RandomValue);
    }

    /// <summary>Selects a conventional database engine name.</summary>
    /// <returns>A reviewed database product label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Engine()
    {
        return DatabaseGenerator.Engine(Context, RandomValue);
    }

    /// <summary>Generates a portable unquoted SQL identifier.</summary>
    /// <remarks>The result excludes common SQL reserved words and uses only ASCII letters, digits, and underscores.</remarks>
    /// <returns>A project-authored database, schema, table, or column identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SqlIdentifier()
    {
        return DatabaseGenerator.SqlIdentifier(Context, RandomValue);
    }

    /// <summary>Reports whether text has the portable unquoted SQL identifier shape.</summary>
    /// <remarks>Common SQL reserved words are rejected case-insensitively; individual engines may impose additional restrictions.</remarks>
    /// <param name="value">The identifier text to inspect.</param>
    /// <returns><c>true</c> for a non-reserved ASCII identifier; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured count limit is insufficient.</exception>
    public bool IsSqlIdentifier(string value)
    {
        return DatabaseGenerator.IsSqlIdentifier(Context, value);
    }

    /// <summary>Generates inert connection-string-shaped text for a selected database family.</summary>
    /// <remarks>The network-oriented fixtures use the reserved <c>example.invalid</c> host and explicitly fake credentials. This method never loads a database client or opens a connection.</remarks>
    /// <returns>A PostgreSQL, MySQL, SQL Server, or in-memory SQLite connection-string shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string ConnectionString()
    {
        return SqlTextGenerator.ConnectionString(Context, RandomValue);
    }

    /// <summary>Returns inert connection-string-shaped text for a requested database family.</summary>
    /// <remarks>The network-oriented fixtures use the reserved <c>example.invalid</c> host and explicitly fake credentials. This method never loads a database client or opens a connection.</remarks>
    /// <param name="kind">The requested connection-string key convention.</param>
    /// <returns>A project-authored connection-string-shaped value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string ConnectionStringFromKind(DatabaseConnectionKind kind)
    {
        return SqlTextGenerator.ConnectionString(Context, kind);
    }

    /// <summary>Quotes a caller value as an ANSI delimited SQL identifier.</summary>
    /// <remarks>Embedded double quotes are doubled. Individual database engines may apply additional identifier rules.</remarks>
    /// <param name="value">The nonempty single-line identifier text.</param>
    /// <returns>The value surrounded by double quotes with embedded double quotes escaped.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when value is empty, malformed UTF-16, or contains a null or newline character.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SqlIdentifierFromValue(string value)
    {
        return SqlTextGenerator.QuoteIdentifier(Context, value);
    }

    /// <summary>Quotes a caller value as an ANSI SQL string literal.</summary>
    /// <remarks>Embedded apostrophes are doubled. The result is returned as text only and is not sent to a database.</remarks>
    /// <param name="value">The text to quote.</param>
    /// <returns>The value surrounded by apostrophes with embedded apostrophes escaped.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when value contains malformed UTF-16 or a null character.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SqlStringLiteral(string value)
    {
        return SqlTextGenerator.StringLiteral(Context, value);
    }

    /// <summary>Generates an inert ANSI SQL insert statement containing one quoted string value.</summary>
    /// <remarks>Identifiers and values are escaped as data. This method never opens a connection or executes the returned statement.</remarks>
    /// <returns>A project-authored <c>INSERT</c> statement string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Insert()
    {
        return SqlTextGenerator.Insert(Context, RandomValue);
    }

    /// <summary>Formats an inert ANSI SQL insert statement containing one quoted string value.</summary>
    /// <remarks>The table and column are delimited identifiers and the value is a string literal. This method never opens a connection or executes the returned statement.</remarks>
    /// <param name="table">The nonempty single-line table identifier.</param>
    /// <param name="column">The nonempty single-line column identifier.</param>
    /// <param name="value">The string value.</param>
    /// <returns>An escaped <c>INSERT</c> statement string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an identifier or value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string InsertFromTableAndColumnAndValue(string table, string column, string value)
    {
        return SqlTextGenerator.Insert(Context, table, column, value);
    }

    /// <summary>Formats an inert ANSI SQL insert statement containing quoted string values.</summary>
    /// <remarks>Every identifier is delimited and every value is escaped as a string literal. This method never opens a connection or executes the returned statement.</remarks>
    /// <param name="table">The nonempty single-line table identifier.</param>
    /// <param name="columns">One or more nonempty single-line column identifiers.</param>
    /// <param name="values">String values corresponding positionally to columns.</param>
    /// <returns>An escaped multi-column <c>INSERT</c> statement string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an array, identifier, value, or array item is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when arrays differ in length, contain no items, or contain malformed text.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string InsertFromTableAndColumnsAndValues(string table, string[] columns, string[] values)
    {
        return SqlTextGenerator.Insert(Context, table, columns, values);
    }
}
