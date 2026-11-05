namespace sql;

internal class SqliteDialect
{
    internal string Name;
    internal SqlDialectOptions Options;

    internal SqliteDialect()
    {
        Name = "SQLite";
        Options = CreateOptions();
    }

    internal static SqlDialectOptions CreateOptions()
    {
        SqlDialectOptions options = new SqlDialectOptions();

        options.Kind = SqlDialectKind.SQLite;
        options.Keywords.AddMany(GetAdditionalKeywords());
        options.AllowDoubleQuotedIdentifiers = true;
        options.AllowBracketQuotedIdentifiers = true;
        options.AllowBacktickQuotedIdentifiers = true;
        options.AllowDollarInBareIdentifiers = false;
        options.AllowSingleQuotedStrings = true;
        options.AllowDoubleQuotedStrings = false;
        options.AllowEscapedStrings = false;
        options.AllowBlobLiterals = true;
        options.AllowHexIntegerLiterals = true;
        options.AllowBooleanLiterals = true;
        options.AllowQuestionParameters = true;
        options.AllowColonParameters = true;
        options.AllowAtParameters = true;
        options.AllowDollarParameters = true;
        options.AllowCommonTableExpressions = true;
        options.AllowWindowFunctions = true;
        options.AllowReturningClause = true;
        options.AllowUpsertClause = true;

        return options;
    }

    internal static string[] GetAdditionalKeywords()
    {
        return new string[]
        {
            "ABORT",
            "ACTION",
            "AFTER",
            "ANALYZE",
            "ATTACH",
            "AUTOINCREMENT",
            "BEFORE",
            "BEGIN",
            "CASCADE",
            "CAST",
            "DATABASE",
            "DEFERRED",
            "DEFERRABLE",
            "DETACH",
            "EACH",
            "EXCLUSIVE",
            "EXPLAIN",
            "FAIL",
            "FILTER",
            "FOLLOWING",
            "GENERATED",
            "GLOB",
            "IGNORE",
            "IMMEDIATE",
            "INDEXED",
            "INITIALLY",
            "INSTEAD",
            "MATERIALIZED",
            "MATCH",
            "NATURAL",
            "NO",
            "NOTHING",
            "NOTNULL",
            "NULLS",
            "OF",
            "OFFSET",
            "PLAN",
            "PRAGMA",
            "PRECEDING",
            "QUERY",
            "RAISE",
            "RECURSIVE",
            "REGEXP",
            "REINDEX",
            "RELEASE",
            "RENAME",
            "REPLACE",
            "RESTRICT",
            "RETURNING",
            "ROW",
            "ROWS",
            "SAVEPOINT",
            "STRICT",
            "TEMP",
            "TEMPORARY",
            "TRIGGER",
            "UNBOUNDED",
            "VACUUM",
            "VIRTUAL",
            "WITHOUT"
        };
    }

    internal bool IsKeyword(string text)
    {
        return Options.IsKeyword(text);
    }
}
