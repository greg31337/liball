using System;

namespace sql;

/// <summary>Provides the supported entry points for tokenizing SQL and parsing scripts, statements, or expressions.</summary>
/// <remarks>Normal SQL syntax problems are represented as diagnostics where the operation returns a result; argument and configuration errors are reported as exceptions.</remarks>
internal static class SqlParser
{
    internal static readonly ISqlParser DefaultParser = new SqlParserService();

    /// <summary>Parses the SQL input and returns the resulting syntax tree together with diagnostics and preserved source data.</summary>
    /// <param name="text">The source text to process.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    internal static SqlParseResult ParseScript(string text)
    {
        return ParseScript(text, new SqlParseOptions());
    }

    /// <summary>Parses the SQL input and returns the resulting syntax tree together with diagnostics and preserved source data.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    internal static SqlParseResult ParseScript(string text, SqlParseOptions options)
    {
        return DefaultParser.ParseScript(text, options);
    }

    /// <summary>Parses the SQL input and returns the resulting syntax tree together with diagnostics and preserved source data.</summary>
    /// <param name="text">The source text to process.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    internal static SqlStatementParseResult ParseStatement(string text)
    {
        return ParseStatement(text, new SqlParseOptions());
    }

    /// <summary>Parses the SQL input and returns the resulting syntax tree together with diagnostics and preserved source data.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    internal static SqlStatementParseResult ParseStatement(string text, SqlParseOptions options)
    {
        return DefaultParser.ParseStatement(text, options);
    }

    /// <summary>Parses the SQL input and returns the resulting syntax tree together with diagnostics and preserved source data.</summary>
    /// <param name="text">The source text to process.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    internal static SqlExpressionParseResult ParseExpression(string text)
    {
        return ParseExpression(text, new SqlParseOptions());
    }

    /// <summary>Parses the SQL input and returns the resulting syntax tree together with diagnostics and preserved source data.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    internal static SqlExpressionParseResult ParseExpression(string text, SqlParseOptions options)
    {
        return DefaultParser.ParseExpression(text, options);
    }

    /// <summary>Tokenizes the SQL input and returns tokens in source order, including the end-of-file token.</summary>
    /// <param name="text">The source text to process.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    internal static SqlTokenizeResult Tokenize(string text)
    {
        return Tokenize(text, new SqlParseOptions());
    }

    /// <summary>Tokenizes the SQL input and returns tokens in source order, including the end-of-file token.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    internal static SqlTokenizeResult Tokenize(string text, SqlParseOptions options)
    {
        return DefaultParser.Tokenize(text, options);
    }

}
