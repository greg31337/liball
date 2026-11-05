using System;

namespace sql;

/// <summary>Provides an injectable implementation of <see cref="ISqlParser"/>.</summary>
public class SqlParserService : ISqlParser
{
    /// <summary>Initializes a new parser service.</summary>
    public SqlParserService()
    {
    }

    /// <summary>Parses a SQL script using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    public SqlParseResult ParseScript(string text, SqlParseOptions options)
    {
        return SqlStatementParser.ParseScript(text, options);
    }

    /// <summary>Parses one SQL statement using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    public SqlStatementParseResult ParseStatement(string text, SqlParseOptions options)
    {
        SqlParserContext context = CreateContext(text, options);
        SqlStatementParser parser = new SqlStatementParser(context);
        SqlStatement statement = parser.ParseStatement();
        SkipTrivia(context);
        if (!context.IsAtEnd())
        {
            context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLPARSE003", "Unexpected token after SQL statement.", context.Peek().Span);
        }

        return new SqlStatementParseResult(statement, context.Stream.Tokens, context.Diagnostics, GetSourceText(context), context.Options);
    }

    /// <summary>Parses one SQL query using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    public SqlQueryParseResult ParseQuery(string text, SqlParseOptions options)
    {
        SqlParserContext context = CreateContext(text, options);
        SqlQueryParser parser = new SqlQueryParser(context);
        SqlQuery query = parser.ParseQuery();
        SkipTrivia(context);
        if (!context.IsAtEnd())
        {
            context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY100", "Unexpected token after SQL query.", context.Peek().Span);
        }

        return new SqlQueryParseResult(query, context.Stream.Tokens, context.Diagnostics, GetSourceText(context), context.Options);
    }

    /// <summary>Parses one SQL expression using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    public SqlExpressionParseResult ParseExpression(string text, SqlParseOptions options)
    {
        SqlParserContext context = CreateContext(text, options);
        SqlExpressionParser parser = new SqlExpressionParser(context);
        SqlExpression expression = parser.ParseExpression();
        SkipTrivia(context);
        if (!context.IsAtEnd())
        {
            context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPR100", "Unexpected token after SQL expression.", context.Peek().Span);
        }

        return new SqlExpressionParseResult(expression, context.Stream.Tokens, context.Diagnostics, GetSourceText(context), context.Options);
    }

    /// <summary>Tokenizes SQL text using the supplied options.</summary>
    /// <param name="text">The source text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new result containing the output and diagnostics.</returns>
    public SqlTokenizeResult Tokenize(string text, SqlParseOptions options)
    {
        SqlParserContext context = CreateContext(text, options);
        return new SqlTokenizeResult(context.Stream.Tokens, context.Diagnostics, GetSourceText(context), context.Options);
    }

    internal SqlParserContext CreateContext(string text, SqlParseOptions options)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlSourceText sourceText = new SqlSourceText(text);
        SqlLexer lexer = new SqlLexer(sourceText, options.Dialect);
        SqlToken[] tokens = lexer.LexTokens();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        diagnostics.Merge(lexer.Diagnostics);
        SqlTokenStream stream = SqlTokenStream.Create(tokens, diagnostics);
        return SqlParserContext.Create(stream, options, sourceText);
    }

    internal void SkipTrivia(SqlParserContext context)
    {
        while (!context.IsAtEnd() && IsTrivia(context.Peek()))
        {
            context.Consume();
        }
    }

    internal bool IsTrivia(SqlToken token)
    {
        return token.Kind == SqlTokenKind.Whitespace || token.Kind == SqlTokenKind.LineComment || token.Kind == SqlTokenKind.BlockComment;
    }

    internal SqlSourceText GetSourceText(SqlParserContext context)
    {
        if (context.SourceText == null)
        {
            throw new ArgumentNullException("SourceText");
        }

        return context.SourceText;
    }
}
