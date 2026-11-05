namespace sql;

/// <summary>Defines substitutable SQL tokenization and parsing operations.</summary>
/// <remarks>Use <see cref="SqlParserService"/> as the built-in implementation, or provide a custom implementation for injection, decoration, or testing.</remarks>
public interface ISqlParser
{
    /// <summary>Parses a SQL script using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    SqlParseResult ParseScript(string text, SqlParseOptions options);

    /// <summary>Parses one SQL statement using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    SqlStatementParseResult ParseStatement(string text, SqlParseOptions options);

    /// <summary>Parses one SQL query using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    SqlQueryParseResult ParseQuery(string text, SqlParseOptions options);

    /// <summary>Parses one SQL expression using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    SqlExpressionParseResult ParseExpression(string text, SqlParseOptions options);

    /// <summary>Tokenizes SQL text using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    SqlTokenizeResult Tokenize(string text, SqlParseOptions options);
}
