using System;
using System.Collections.Generic;

namespace sql;

internal class SqlCreateIndexParser
{
    internal SqlParserContext Context;

    internal SqlCreateIndexParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlIndexDefinition ParseCreateIndex()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "CREATE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINDEX001", "Expected CREATE.", Context.Peek().Span);
            Context.Consume();
        }

        bool isUnique = false;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "UNIQUE"))
        {
            Context.Consume();
            isUnique = true;
            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "INDEX"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINDEX002", "Expected INDEX in CREATE INDEX statement.", Context.Peek().Span);
        }

        bool ifNotExists = ParseOptionalIfNotExists();
        SqlName indexName = ParseName("SQLINDEX006", "Expected index name in CREATE INDEX statement.");
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ON"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINDEX007", "Expected ON in CREATE INDEX statement.", Context.Peek().Span);
        }

        SqlName tableName = ParseName("SQLINDEX008", "Expected table name in CREATE INDEX statement.");
        SqlOrderingTerm[] keyExpressions = ParseKeyExpressionList();
        SqlExpression? whereExpression = null;
        int end = tableName.Span.GetEnd();
        if (keyExpressions.Length > 0)
        {
            end = keyExpressions[keyExpressions.Length - 1].Span.GetEnd();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "WHERE"))
        {
            Context.Consume();
            SkipTrivia();
            SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
            whereExpression = expressionParser.ParseExpression();
            end = whereExpression.Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        return new SqlIndexDefinition(isUnique, ifNotExists, indexName, tableName, keyExpressions, whereExpression, span);
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINDEX003", "Expected NOT after IF.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "EXISTS"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINDEX004", "Expected EXISTS after IF NOT.", Context.Peek().Span);
        }

        return true;
    }

    internal SqlOrderingTerm[] ParseKeyExpressionList()
    {
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINDEX009", "Expected index key expression list.", Context.Peek().Span);
            return new SqlOrderingTerm[0];
        }

        List<SqlOrderingTerm> terms = new List<SqlOrderingTerm>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlQueryParser parser = new SqlQueryParser(Context);
            SqlOrderingTerm term = parser.ParseOrderingTerm();
            terms.Add(term);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (terms.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINDEX010", "Expected index key expression.", Context.Peek().Span);
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINDEX011", "Expected closing parenthesis in CREATE INDEX key list.", Context.Peek().Span);
        }

        return terms.ToArray();
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
