using System;

namespace sql;

/// <summary>Provides an injectable implementation of <see cref="ISqlFormatter"/>.</summary>
public class SqlFormatterService : ISqlFormatter
{
    internal ISqlParser Parser;

    /// <summary>Initializes a formatter service with the supplied parsing dependency.</summary>
    /// <param name="parser">The parser used to convert SQL text into a syntax tree.</param>
    /// <exception cref="ArgumentNullException">parser is <c>null</c>.</exception>
    public SqlFormatterService(ISqlParser parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException("parser");
        }

        Parser = parser;
    }

    /// <summary>Creates a formatter service with the standard parser implementation.</summary>
    /// <returns>The resulting formatter service.</returns>
    internal static SqlFormatterService CreateDefault()
    {
        return new SqlFormatterService(new SqlParserService());
    }

    /// <summary>Creates a formatter service with the supplied parsing dependency.</summary>
    /// <param name="parser">The parser used to convert SQL text into a syntax tree.</param>
    /// <returns>The resulting formatter service.</returns>
    internal static SqlFormatterService Create(ISqlParser parser)
    {
        return new SqlFormatterService(parser);
    }

    /// <summary>Parses and formats SQL text using the supplied options.</summary>
    /// <param name="sql">The SQL text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    /// <exception cref="ArgumentNullException">sql is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    /// <exception cref="FormatException">The input contains SQL syntax errors.</exception>
    public string Format(string sql, SqlFormatOptions options)
    {
        if (sql == null)
        {
            throw new ArgumentNullException("sql");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlParseOptions parseOptions = SqlParseOptions.Create(options.ReadDialect);
        SqlParseResult result = Parser.ParseScript(sql, parseOptions);
        if (!result.IsSuccess())
        {
            throw new FormatException("SQL text could not be parsed.");
        }

        return FormatScript(result.Script, options);
    }

    /// <summary>Formats an expression syntax tree using the supplied options.</summary>
    /// <param name="expression">The expression syntax tree to format.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted SQL expression.</returns>
    public string FormatExpression(SqlExpression expression, SqlFormatOptions options)
    {
        SqlGenerateOptions generateOptions = CreateGenerateOptions(options);
        return SqlGenerator.GenerateExpressionWithGenerateOptions(expression, generateOptions);
    }

    /// <summary>Formats a query syntax tree using the supplied options.</summary>
    /// <param name="query">The query syntax tree to format.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted SQL query.</returns>
    public string FormatQuery(SqlQuery query, SqlFormatOptions options)
    {
        SqlGenerateOptions generateOptions = CreateGenerateOptions(options);
        return SqlGenerator.GenerateQueryWithGenerateOptions(query, generateOptions);
    }

    /// <summary>Formats a statement syntax tree using the supplied options.</summary>
    /// <param name="statement">The statement syntax tree to format.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted SQL statement.</returns>
    public string FormatStatement(SqlStatement statement, SqlFormatOptions options)
    {
        SqlGenerateOptions generateOptions = CreateGenerateOptions(options);
        return SqlGenerator.GenerateStatementWithGenerateOptions(statement, generateOptions);
    }

    /// <summary>Formats a parsed script using the supplied options.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    /// <exception cref="ArgumentNullException">script is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    public string FormatScript(SqlScript script, SqlFormatOptions options)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        SqlGenerateOptions generateOptions = CreateGenerateOptions(options);
        if (options.Mode == SqlFormatMode.RoundTrip)
        {
            generateOptions.PreserveSourceText = true;
        }

        return SqlGenerator.GenerateScript(script, generateOptions);
    }

    internal SqlGenerateOptions CreateGenerateOptions(SqlFormatOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlGenerateOptions generateOptions = new SqlGenerateOptions();
        generateOptions.FormatOptions = options.Copy();
        generateOptions.WriteDialect = options.WriteDialect;
        return generateOptions;
    }
}
