using System;
using System.Collections.Generic;

namespace sql;

internal class SqlAttachParser
{
    internal SqlParserContext Context;

    internal SqlAttachParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlAttachStatement ParseAttach()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "ATTACH"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLATTACH001", "Expected ATTACH.", Context.Peek().Span);
            Context.Consume();
        }

        SkipTrivia();
        bool hasDatabaseKeyword = false;
        if (IsKeyword(Context.Peek(), "DATABASE"))
        {
            Context.Consume();
            hasDatabaseKeyword = true;
            SkipTrivia();
        }

        SqlExpression? databaseExpression = null;
        SqlName? schemaName = null;
        int end = first.Span.GetEnd();
        if (Context.Peek().Kind == SqlTokenKind.EndOfFile || IsKeyword(Context.Peek(), "AS"))
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLATTACH002", "Expected ATTACH database expression.", Context.Peek().Span);
        }
        else
        {
            SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
            databaseExpression = expressionParser.ParseExpression();
            end = databaseExpression.Span.GetEnd();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AS"))
        {
            Context.Consume();
            SkipTrivia();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLATTACH003", "Expected AS in ATTACH statement.", Context.Peek().Span);
        }

        if (Context.Peek().Kind == SqlTokenKind.EndOfFile)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLATTACH004", "Expected ATTACH schema name.", Context.Peek().Span);
        }
        else
        {
            schemaName = ParseName("SQLATTACH004", "Expected ATTACH schema name.");
            end = schemaName.Span.GetEnd();
            SkipTrivia();
        }

        if (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlToken unexpected = Context.Peek();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLATTACH005", "Unexpected token in ATTACH statement.", unexpected.Span);
            while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
            {
                unexpected = Context.Consume();
                end = unexpected.Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        SqlAttachStatement statement = new SqlAttachStatement(databaseExpression, schemaName, span);
        statement.HasDatabaseKeyword = hasDatabaseKeyword;
        return statement;
    }

    internal SqlName ParseName(string diagnosticCode, string diagnosticMessage)
    {
        List<SqlNamePart> parts = new List<SqlNamePart>();
        SkipTrivia();
        if (!IsIdentifierLike(Context.Peek()))
        {
            SqlToken token = Context.Consume();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, token.Span);
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

internal class SqlDetachParser
{
    internal SqlParserContext Context;

    internal SqlDetachParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlDetachStatement ParseDetach()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "DETACH"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDETACH001", "Expected DETACH.", Context.Peek().Span);
            Context.Consume();
        }

        SkipTrivia();
        bool hasDatabaseKeyword = false;
        if (IsKeyword(Context.Peek(), "DATABASE"))
        {
            Context.Consume();
            hasDatabaseKeyword = true;
            SkipTrivia();
        }

        SqlName? schemaName = null;
        int end = first.Span.GetEnd();
        if (Context.Peek().Kind == SqlTokenKind.EndOfFile)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDETACH002", "Expected DETACH schema name.", Context.Peek().Span);
        }
        else
        {
            schemaName = ParseName("SQLDETACH002", "Expected DETACH schema name.");
            end = schemaName.Span.GetEnd();
            SkipTrivia();
        }

        if (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlToken unexpected = Context.Peek();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDETACH003", "Unexpected token in DETACH statement.", unexpected.Span);
            while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
            {
                unexpected = Context.Consume();
                end = unexpected.Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        SqlDetachStatement statement = new SqlDetachStatement(schemaName, span);
        statement.HasDatabaseKeyword = hasDatabaseKeyword;
        return statement;
    }

    internal SqlName ParseName(string diagnosticCode, string diagnosticMessage)
    {
        List<SqlNamePart> parts = new List<SqlNamePart>();
        SkipTrivia();
        if (!IsIdentifierLike(Context.Peek()))
        {
            SqlToken token = Context.Consume();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, token.Span);
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
