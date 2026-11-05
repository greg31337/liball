using System;
using System.Collections.Generic;

namespace sql;

internal class SqlCreateViewParser
{
    internal SqlParserContext Context;

    internal SqlCreateViewParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlViewDefinition ParseCreateView()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "CREATE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIEW001", "Expected CREATE.", Context.Peek().Span);
            Context.Consume();
        }

        bool isTemporary = false;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "TEMP") || IsKeyword(Context.Peek(), "TEMPORARY"))
        {
            Context.Consume();
            isTemporary = true;
            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "VIEW"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIEW002", "Expected VIEW in CREATE VIEW statement.", Context.Peek().Span);
        }

        bool ifNotExists = ParseOptionalIfNotExists();
        SqlName viewName = ParseName("SQLVIEW006", "Expected view name in CREATE VIEW statement.");
        SqlName[] columnNames = ParseOptionalColumnList();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AS"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIEW009", "Expected AS in CREATE VIEW statement.", Context.Peek().Span);
        }

        SkipTrivia();
        SqlQueryParser parser = new SqlQueryParser(Context);
        SqlQuery query = parser.ParseQuery();
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, query.Span.GetEnd());
        return new SqlViewDefinition(isTemporary, ifNotExists, viewName, columnNames, query, span);
    }

    internal bool ParseOptionalIfNotExists()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "IF"))
        {
            return false;
        }

        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "NOT"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIEW003", "Expected NOT after IF.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "EXISTS"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIEW004", "Expected EXISTS after IF NOT.", Context.Peek().Span);
        }

        return true;
    }

    internal SqlName[] ParseOptionalColumnList()
    {
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.OpenParen)
        {
            return new SqlName[0];
        }

        Context.Consume();
        List<SqlName> names = new List<SqlName>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlName name = ParseName("SQLVIEW007", "Expected column name in CREATE VIEW column list.");
            names.Add(name);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (names.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIEW008", "Expected column name in CREATE VIEW column list.", Context.Peek().Span);
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIEW005", "Expected closing parenthesis in CREATE VIEW column list.", Context.Peek().Span);
        }

        return names.ToArray();
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
        SkipTrivia();
        while (Context.Peek().Kind == SqlTokenKind.Dot)
        {
            Context.Consume();
            SkipTrivia();
            if (!IsIdentifierLike(Context.Peek()))
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, Context.Peek().Span);
                break;
            }

            ConsumeNamePart(parts);
            SkipTrivia();
        }

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
