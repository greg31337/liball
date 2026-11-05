namespace sql;

/// <summary>Provides dialect catalog functionality for the SQL model.</summary>
internal static class SqlDialectCatalog
{
    /// <summary>Returns a descriptor for the supplied built-in dialect.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <returns>The resulting dialect descriptor.</returns>
    internal static ISqlDialect GetDialect(SqlDialectKind kind)
    {
        return new SqlDialect(kind);
    }

    /// <summary>Returns the configured display name for the supplied dialect.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <returns>The resulting string.</returns>
    internal static string GetName(SqlDialectKind kind)
    {
        return GetDialect(kind).GetName();
    }

    internal static string GetNameCore(SqlDialectKind kind)
    {
        if (kind == SqlDialectKind.SQLite)
        {
            return "SQLite";
        }

        if (kind == SqlDialectKind.PostgreSql)
        {
            return "PostgreSQL";
        }

        if (kind == SqlDialectKind.MySql)
        {
            return "MySQL";
        }

        if (kind == SqlDialectKind.MariaDb)
        {
            return "MariaDB";
        }

        if (kind == SqlDialectKind.SqlServer)
        {
            return "SQL Server";
        }

        if (kind == SqlDialectKind.Oracle)
        {
            return "Oracle";
        }

        if (kind == SqlDialectKind.DuckDb)
        {
            return "DuckDB";
        }

        if (kind == SqlDialectKind.BigQuery)
        {
            return "BigQuery";
        }

        if (kind == SqlDialectKind.Snowflake)
        {
            return "Snowflake";
        }

        if (kind == SqlDialectKind.Redshift)
        {
            return "Redshift";
        }

        if (kind == SqlDialectKind.Databricks)
        {
            return "Databricks";
        }

        if (kind == SqlDialectKind.Ansi)
        {
            return "ANSI";
        }

        return "Generic";
    }

    /// <summary>Creates, stores, and returns an options.</summary>
    /// <param name="kind">The kind that identifies the value's form.</param>
    /// <returns>The resulting dialect options.</returns>
    internal static SqlDialectOptions CreateOptions(SqlDialectKind kind)
    {
        return GetDialect(kind).CreateOptions();
    }

    internal static SqlDialectOptions CreateOptionsCore(SqlDialectKind kind)
    {
        if (kind == SqlDialectKind.SQLite)
        {
            return SqliteDialect.CreateOptions();
        }

        if (kind == SqlDialectKind.PostgreSql)
        {
            return CreatePostgreSqlOptions();
        }

        if (kind == SqlDialectKind.MySql)
        {
            return CreateMySqlOptions(SqlDialectKind.MySql);
        }

        if (kind == SqlDialectKind.MariaDb)
        {
            return CreateMySqlOptions(SqlDialectKind.MariaDb);
        }

        if (kind == SqlDialectKind.SqlServer)
        {
            return CreateSqlServerOptions();
        }

        if (kind == SqlDialectKind.Oracle)
        {
            return CreateOracleOptions();
        }

        if (kind == SqlDialectKind.DuckDb)
        {
            return CreateDuckDbOptions();
        }

        if (kind == SqlDialectKind.BigQuery)
        {
            return CreateBigQueryOptions();
        }

        if (kind == SqlDialectKind.Snowflake)
        {
            return CreateSnowflakeOptions();
        }

        if (kind == SqlDialectKind.Redshift)
        {
            return CreateRedshiftOptions();
        }

        if (kind == SqlDialectKind.Databricks)
        {
            return CreateDatabricksOptions();
        }

        if (kind == SqlDialectKind.Ansi)
        {
            return CreateAnsiOptions();
        }

        return GenericDialect.CreateOptions();
    }

    internal static SqlDialectKind NormalizeKind(SqlDialectKind kind)
    {
        if (kind == SqlDialectKind.SQLite ||
            kind == SqlDialectKind.PostgreSql ||
            kind == SqlDialectKind.MySql ||
            kind == SqlDialectKind.MariaDb ||
            kind == SqlDialectKind.SqlServer ||
            kind == SqlDialectKind.Oracle ||
            kind == SqlDialectKind.DuckDb ||
            kind == SqlDialectKind.BigQuery ||
            kind == SqlDialectKind.Snowflake ||
            kind == SqlDialectKind.Redshift ||
            kind == SqlDialectKind.Databricks ||
            kind == SqlDialectKind.Ansi)
        {
            return kind;
        }

        return SqlDialectKind.Generic;
    }

    internal static SqlDialectOptions CreatePostgreSqlOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.PostgreSql);
        options.Keywords.AddMany(new string[] { "BIGSERIAL", "ILIKE", "JSONB", "LATERAL", "SERIAL", "SIMILAR" });
        options.AllowDollarInBareIdentifiers = true;
        options.AllowEscapedStrings = true;
        options.AllowUnicodeStrings = true;
        options.AllowDollarQuotedStrings = true;
        options.AllowReturningClause = true;
        options.AllowUpsertClause = true;
        options.AllowQuestionParameters = false;
        options.AllowAtParameters = false;
        options.AllowDollarParameters = true;
        options.AllowDollarNumberParameters = true;
        options.CustomOperators = new SqlOperatorPolicy[]
        {
            new SqlOperatorPolicy("#>>", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 50, false),
            new SqlOperatorPolicy("#>", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 50, false),
            new SqlOperatorPolicy("@>", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("<@", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("?|", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("?&", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 40, false),
            new SqlOperatorPolicy("#-", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 50, false)
        };
        return options;
    }

    internal static SqlDialectOptions CreateMySqlOptions(SqlDialectKind kind)
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(kind);
        options.Keywords.AddMany(new string[] { "AUTO_INCREMENT", "DATABASE", "DUPLICATE", "ENGINE", "LIMIT", "SIGNED", "UNSIGNED" });
        options.AllowDoubleQuotedIdentifiers = false;
        options.AllowBacktickQuotedIdentifiers = true;
        options.AllowBooleanLiterals = false;
        options.AllowReturningClause = false;
        options.AllowUpsertClause = true;
        return options;
    }

    internal static SqlDialectOptions CreateSqlServerOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.SqlServer);
        options.Keywords.AddMany(new string[] { "IDENTITY", "MERGE", "NVARCHAR", "OFFSET", "OUTPUT", "TOP" });
        options.AllowBracketQuotedIdentifiers = true;
        options.AllowColonParameters = false;
        options.AllowDollarParameters = false;
        options.AddSupportedStatement(SqlStatementKind.Merge);
        return options;
    }

    internal static SqlDialectOptions CreateOracleOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.Oracle);
        options.Keywords.AddMany(new string[] { "CONNECT", "MINUS", "NOCYCLE", "NUMBER", "ROWNUM", "START", "VARCHAR2" });
        options.AllowQuestionParameters = false;
        options.AllowAtParameters = false;
        options.AllowDollarParameters = false;
        return options;
    }

    internal static SqlDialectOptions CreateDuckDbOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.DuckDb);
        options.Keywords.AddMany(new string[] { "EXCLUDE", "QUALIFY", "STRUCT", "UNNEST" });
        options.AllowReturningClause = true;
        options.AllowUpsertClause = true;
        return options;
    }

    internal static SqlDialectOptions CreateBigQueryOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.BigQuery);
        options.Keywords.AddMany(new string[] { "ARRAY", "QUALIFY", "STRUCT", "UNNEST" });
        options.AllowDoubleQuotedIdentifiers = false;
        options.AllowBacktickQuotedIdentifiers = true;
        options.AllowQuestionParameters = false;
        options.AllowColonParameters = false;
        options.AllowAtParameters = true;
        options.AllowDollarParameters = false;
        options.AllowReturningClause = false;
        options.AllowUpsertClause = false;
        return options;
    }

    internal static SqlDialectOptions CreateSnowflakeOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.Snowflake);
        options.Keywords.AddMany(new string[] { "IFF", "ILIKE", "QUALIFY", "SAMPLE", "VARIANT" });
        options.AllowQuestionParameters = false;
        options.AllowDollarParameters = true;
        options.AllowReturningClause = false;
        options.AllowUpsertClause = false;
        return options;
    }

    internal static SqlDialectOptions CreateRedshiftOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.Redshift);
        options.Keywords.AddMany(new string[] { "DISTKEY", "ILIKE", "SORTKEY", "SUPER" });
        options.AllowDollarInBareIdentifiers = true;
        options.AllowReturningClause = false;
        options.AllowUpsertClause = false;
        return options;
    }

    internal static SqlDialectOptions CreateDatabricksOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.Databricks);
        options.Keywords.AddMany(new string[] { "DELTA", "DISTRIBUTE", "LATERAL", "QUALIFY", "STRUCT" });
        options.AllowBacktickQuotedIdentifiers = true;
        options.AllowReturningClause = false;
        options.AllowUpsertClause = false;
        return options;
    }

    internal static SqlDialectOptions CreateAnsiOptions()
    {
        SqlDialectOptions options = CreateBaseConcreteOptions(SqlDialectKind.Ansi);
        options.AllowQuestionParameters = false;
        options.AllowColonParameters = false;
        options.AllowAtParameters = false;
        options.AllowDollarParameters = false;
        options.AllowReturningClause = false;
        options.AllowUpsertClause = false;
        return options;
    }

    internal static SqlDialectOptions CreateBaseConcreteOptions(SqlDialectKind kind)
    {
        SqlDialectOptions options = GenericDialect.CreateOptions();
        options.Kind = kind;
        return options;
    }
}
