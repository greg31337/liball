namespace sql;

/// <summary>Defines substitutable SQL formatting operations.</summary>
/// <remarks>Use <see cref="SqlFormatterService"/> as the built-in implementation, or provide a custom implementation for injection or testing.</remarks>
public interface ISqlFormatter
{
    /// <summary>Parses and formats SQL text using the supplied options.</summary>
    /// <param name="sql">The SQL text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    string Format(string sql, SqlFormatOptions options);

    /// <summary>Formats an expression syntax tree using the supplied options.</summary>
    /// <param name="expression">The expression syntax tree to format.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted SQL expression.</returns>
    string FormatExpression(SqlExpression expression, SqlFormatOptions options);

    /// <summary>Formats a query syntax tree using the supplied options.</summary>
    /// <param name="query">The query syntax tree to format.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted SQL query.</returns>
    string FormatQuery(SqlQuery query, SqlFormatOptions options);

    /// <summary>Formats a statement syntax tree using the supplied options.</summary>
    /// <param name="statement">The statement syntax tree to format.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted SQL statement.</returns>
    string FormatStatement(SqlStatement statement, SqlFormatOptions options);

    /// <summary>Formats a parsed script using the supplied options.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    string FormatScript(SqlScript script, SqlFormatOptions options);
}
