using System;

namespace sql;

internal class SqlExplainParser
{
    internal SqlParserContext Context;

    internal SqlExplainParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlExplainStatement ParseExplain()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "EXPLAIN"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPLAIN001", "Expected EXPLAIN.", Context.Peek().Span);
            Context.Consume();
        }

        bool isQueryPlan = false;
        SqlStatement? statement = null;
        int end = first.Span.GetEnd();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "QUERY"))
        {
            SqlToken queryToken = Context.Consume();
            end = queryToken.Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "PLAN"))
            {
                SqlToken planToken = Context.Consume();
                isQueryPlan = true;
                end = planToken.Span.GetEnd();
                SkipTrivia();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPLAIN002", "Expected PLAN in EXPLAIN QUERY PLAN.", Context.Peek().Span);
            }
        }

        if (Context.Peek().Kind == SqlTokenKind.EndOfFile)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLEXPLAIN003", "Expected statement after EXPLAIN.", Context.Peek().Span);
        }
        else
        {
            SqlStatementParser parser = new SqlStatementParser(Context);
            statement = parser.ParseStatement();
            end = statement.Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        return new SqlExplainStatement(isQueryPlan, statement, span);
    }

    internal void SkipTrivia()
    {
        while (Context.Peek().Kind == SqlTokenKind.Whitespace || Context.Peek().Kind == SqlTokenKind.LineComment || Context.Peek().Kind == SqlTokenKind.BlockComment)
        {
            Context.Consume();
        }
    }

    internal bool IsKeyword(SqlToken token, string text)
    {
        string? canonical = Context.Options.Dialect.GetCanonicalKeyword(token.Text);
        if (canonical == null)
        {
            canonical = token.Text.ToUpperInvariant();
        }

        return canonical == text;
    }
}
