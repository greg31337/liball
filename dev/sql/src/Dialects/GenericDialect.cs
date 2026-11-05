namespace sql;

internal class GenericDialect
{
    internal string Name;
    internal SqlDialectOptions Options;

    internal GenericDialect()
    {
        Name = "Generic";
        Options = CreateOptions();
    }

    internal static SqlDialectOptions CreateOptions()
    {
        SqlDialectOptions options = new SqlDialectOptions();

        options.Kind = SqlDialectKind.Generic;
        options.AllowDoubleQuotedIdentifiers = true;
        options.AllowBracketQuotedIdentifiers = false;
        options.AllowBacktickQuotedIdentifiers = false;
        options.AllowSingleQuotedStrings = true;
        options.AllowDoubleQuotedStrings = false;
        options.AllowBlobLiterals = false;
        options.AllowHexIntegerLiterals = false;
        options.AllowBooleanLiterals = true;
        options.AllowQuestionParameters = true;
        options.AllowColonParameters = true;
        options.AllowAtParameters = true;
        options.AllowDollarParameters = true;
        options.AllowCommonTableExpressions = true;
        options.AllowWindowFunctions = true;
        options.AllowReturningClause = false;
        options.AllowUpsertClause = false;

        return options;
    }

    internal bool IsKeyword(string text)
    {
        return Options.IsKeyword(text);
    }
}
