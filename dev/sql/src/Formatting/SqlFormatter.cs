using System;

namespace sql;

/// <summary>Parses SQL text or accepts an existing syntax tree and renders consistently formatted SQL.</summary>
/// <remarks>Normal SQL syntax problems are represented as diagnostics where the operation returns a result; argument and configuration errors are reported as exceptions.</remarks>
internal static class SqlFormatter
{
    internal static readonly ISqlFormatter DefaultFormatter = SqlFormatterService.CreateDefault();

    /// <summary>Formats the supplied SQL using the selected layout options.</summary>
    /// <param name="sql">The SQL text to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    /// <exception cref="ArgumentNullException">sql is <c>null</c>.</exception>
    /// <exception cref="FormatException">The input contains SQL syntax errors.</exception>
    internal static string Format(string sql)
    {
        return Format(sql, SqlFormatOptions.CreateDefault());
    }

    /// <summary>Formats the supplied SQL using the selected layout options.</summary>
    /// <param name="sql">The SQL text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    /// <exception cref="ArgumentNullException">sql is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    /// <exception cref="FormatException">The input contains SQL syntax errors.</exception>
    internal static string Format(string sql, SqlFormatOptions options)
    {
        return DefaultFormatter.Format(sql, options);
    }

    /// <summary>Formats the supplied syntax tree without reparsing it.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string FormatScript(SqlScript script)
    {
        return FormatScript(script, SqlFormatOptions.CreateDefault());
    }

    /// <summary>Formats the supplied syntax tree without reparsing it.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    /// <exception cref="ArgumentNullException">script is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    internal static string FormatScript(SqlScript script, SqlFormatOptions options)
    {
        return DefaultFormatter.FormatScript(script, options);
    }

    internal static string FormatExpression(SqlExpression expression, SqlFormatOptions options)
    {
        return DefaultFormatter.FormatExpression(expression, options);
    }

    internal static string FormatQuery(SqlQuery query, SqlFormatOptions options)
    {
        return DefaultFormatter.FormatQuery(query, options);
    }

    internal static string FormatStatement(SqlStatement statement, SqlFormatOptions options)
    {
        return DefaultFormatter.FormatStatement(statement, options);
    }
}
