using System;
using System.Collections.Generic;

namespace sql;

internal class SqlVacuumParser
{
    internal SqlParserContext Context;

    internal SqlVacuumParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlVacuumStatement ParseVacuum()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "VACUUM"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVACUUM001", "Expected VACUUM.", Context.Peek().Span);
            Context.Consume();
        }

        SqlName? databaseName = null;
        SqlExpression? intoExpression = null;
        int end = first.Span.GetEnd();
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (!IsKeyword(Context.Peek(), "INTO"))
            {
                databaseName = ParseDatabaseName();
                end = databaseName.Span.GetEnd();
                SkipTrivia();
            }

            if (IsKeyword(Context.Peek(), "INTO"))
            {
                SqlToken intoToken = Context.Consume();
                end = intoToken.Span.GetEnd();
                SkipTrivia();
                if (Context.Peek().Kind == SqlTokenKind.EndOfFile)
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVACUUM003", "Expected VACUUM INTO expression.", Context.Peek().Span);
                }
                else
                {
                    SqlExpressionParser parser = new SqlExpressionParser(Context);
                    intoExpression = parser.ParseExpression();
                    end = intoExpression.Span.GetEnd();
                    SkipTrivia();
                }
            }

            if (Context.Peek().Kind != SqlTokenKind.EndOfFile)
            {
                SqlToken unexpected = Context.Peek();
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVACUUM004", "Unexpected token in VACUUM statement.", unexpected.Span);
                while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
                {
                    unexpected = Context.Consume();
                    end = unexpected.Span.GetEnd();
                }
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        return new SqlVacuumStatement(databaseName, intoExpression, span);
    }

    internal SqlName ParseDatabaseName()
    {
        List<SqlNamePart> parts = new List<SqlNamePart>();
        SkipTrivia();
        if (!IsIdentifierLike(Context.Peek()))
        {
            SqlToken token = Context.Consume();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVACUUM002", "Expected VACUUM database name.", token.Span);
            parts.Add(SqlNamePart.Create(token.Text, token.Text, token.Span, SqlNameQuoteStyle.None, token));
            return CreateName(parts);
        }

        ConsumeNamePart(parts);
        return CreateName(parts);
    }

    internal void ConsumeNamePart(List<SqlNamePart> parts)
    {
        SqlToken token = Context.Consume();
        parts.Add(SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token));
    }

    internal SqlName CreateName(List<SqlNamePart> parts)
    {
        if (parts.Count == 0)
        {
            return new SqlName(new SqlNamePart[0], new SqlTextSpan(0, 0));
        }

        SqlNamePart first = parts[0];
        SqlNamePart last = parts[parts.Count - 1];
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        return new SqlName(parts.ToArray(), span);
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

    internal bool IsIdentifierLike(SqlToken token)
    {
        return token.Kind == SqlTokenKind.Identifier || token.Kind == SqlTokenKind.Keyword;
    }

    internal string GetIdentifierText(SqlToken token)
    {
        SqlNameQuoteStyle quoteStyle = GetQuoteStyle(token);
        if (quoteStyle == SqlNameQuoteStyle.None)
        {
            return token.Text;
        }

        return StripOuterQuotes(token.Text, quoteStyle);
    }

    internal SqlNameQuoteStyle GetQuoteStyle(SqlToken token)
    {
        if (token.Text.Length == 0)
        {
            return SqlNameQuoteStyle.None;
        }

        if (token.Text[0] == '"')
        {
            return SqlNameQuoteStyle.DoubleQuote;
        }

        if (token.Text[0] == '[')
        {
            return SqlNameQuoteStyle.Bracket;
        }

        if (token.Text[0] == '`')
        {
            return SqlNameQuoteStyle.Backtick;
        }

        if (token.Text[0] == '\'')
        {
            return SqlNameQuoteStyle.SingleQuote;
        }

        return SqlNameQuoteStyle.None;
    }

    internal string StripOuterQuotes(string text, SqlNameQuoteStyle quoteStyle)
    {
        if (quoteStyle == SqlNameQuoteStyle.None || text.Length == 0)
        {
            return text;
        }

        int start = 1;
        int end = text.Length;
        if (text.Length > 1)
        {
            if (quoteStyle == SqlNameQuoteStyle.DoubleQuote && text[text.Length - 1] == '"')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.Bracket && text[text.Length - 1] == ']')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.Backtick && text[text.Length - 1] == '`')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.SingleQuote && text[text.Length - 1] == '\'')
            {
                end = text.Length - 1;
            }
        }

        if (end < start)
        {
            return "";
        }

        return text.Substring(start, end - start);
    }
}
