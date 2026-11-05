using System;
using System.Collections.Generic;

namespace sql;

internal class SqlDeleteParser
{
    internal SqlParserContext Context;

    internal SqlDeleteParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlDeleteStatement ParseDeleteStatement()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "DELETE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDELETE001", "Expected DELETE.", Context.Peek().Span);
            Context.Consume();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "FROM"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDELETE002", "Expected FROM in DELETE statement.", Context.Peek().Span);
        }

        SkipTrivia();
        SqlName tableName = ParseName("SQLDELETE003", "Expected table name in DELETE statement.");
        SqlName? alias = ParseOptionalAlias();
        SqlName? indexedBy = null;
        bool isNotIndexed = false;
        int end = tableName.Span.GetEnd();
        if (alias != null)
        {
            end = alias.Span.GetEnd();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "INDEXED"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "BY"))
            {
                Context.Consume();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDELETE007", "Expected BY after INDEXED.", Context.Peek().Span);
            }

            indexedBy = ParseName("SQLDELETE008", "Expected index name after INDEXED BY.");
            end = indexedBy.Span.GetEnd();
        }
        else
        {
            if (IsKeyword(Context.Peek(), "NOT"))
            {
                SqlToken notToken = Context.Consume();
                SkipTrivia();
                if (IsKeyword(Context.Peek(), "INDEXED"))
                {
                    SqlToken indexedToken = Context.Consume();
                    isNotIndexed = true;
                    end = indexedToken.Span.GetEnd();
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDELETE009", "Expected INDEXED after NOT.", Context.Peek().Span);
                    end = notToken.Span.GetEnd();
                }
            }
        }

        SqlExpression? where = null;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "WHERE"))
        {
            Context.Consume();
            where = ParseExpressionUntil(new string[] { "RETURNING", "ORDER", "LIMIT" }, "SQLDELETE005", "Expected DELETE WHERE expression.");
            end = where.Span.GetEnd();
        }

        SqlReturningClause? returning = ParseOptionalReturningClause();
        if (returning != null)
        {
            end = returning.Span.GetEnd();
        }

        SqlOrderingTerm[] orderBy = new SqlOrderingTerm[0];
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ORDER"))
        {
            orderBy = ParseOrderByClause();
            if (orderBy.Length > 0)
            {
                end = orderBy[orderBy.Length - 1].Span.GetEnd();
            }
        }

        SqlLimitClause? limit = null;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "LIMIT"))
        {
            limit = ParseLimitClause();
            end = limit.Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        return new SqlDeleteStatement(tableName, alias, indexedBy, isNotIndexed, where, returning, orderBy, limit, span);
    }

    internal SqlOrderingTerm[] ParseOrderByClause()
    {
        SqlQueryParser parser = new SqlQueryParser(Context);
        return parser.ParseOrderByClause();
    }

    internal SqlLimitClause ParseLimitClause()
    {
        SqlQueryParser parser = new SqlQueryParser(Context);
        return parser.ParseLimitClause();
    }

    internal SqlName? ParseOptionalAlias()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "AS"))
        {
            return null;
        }

        Context.Consume();
        SkipTrivia();
        if (!IsIdentifierLike(Context.Peek()))
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDELETE004", "Expected table alias after AS.", Context.Peek().Span);
            return null;
        }

        SqlToken aliasToken = Context.Consume();
        return CreateSinglePartName(aliasToken);
    }

    internal SqlReturningClause? ParseOptionalReturningClause()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "RETURNING"))
        {
            return null;
        }

        SqlToken returningToken = Context.Consume();
        if (!Context.Options.Dialect.AllowReturningClause)
        {
            Context.AddUnsupportedFeature("RETURNING clause", returningToken.Span);
        }

        List<SqlResultColumn> resultColumns = new List<SqlResultColumn>();
        int end = returningToken.Span.GetEnd();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && Context.Peek().Kind != SqlTokenKind.Semicolon)
        {
            SqlQueryParser queryParser = new SqlQueryParser(Context);
            SqlResultColumn resultColumn = queryParser.ParseResultColumn();
            resultColumns.Add(resultColumn);
            end = resultColumn.Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (resultColumns.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDELETE006", "Expected RETURNING expression.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(returningToken.Span.Start, end);
        return new SqlReturningClause(resultColumns.ToArray(), span);
    }

    internal SqlExpression ParseExpressionUntil(string[] terminatorKeywords, string diagnosticCode, string diagnosticMessage)
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SkipTrivia();
        int start = Context.Peek().Span.Start;
        int end = start;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && Context.Peek().Kind != SqlTokenKind.Semicolon)
        {
            if (depth == 0 && IsAnyKeyword(Context.Peek(), terminatorKeywords))
            {
                break;
            }

            if (depth == 0 && Context.Peek().Kind == SqlTokenKind.CloseParen)
            {
                break;
            }

            SqlToken token = Context.Consume();
            tokens.Add(token);
            end = token.Span.GetEnd();
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                }
            }
        }

        if (tokens.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, Context.Peek().Span);
            return SqlExpression.Create(SqlExpressionKind.Unknown, Context.Peek().Span);
        }

        SqlToken[] expressionTokens = CreateParseTokens(tokens, end);
        SqlTokenStream stream = SqlTokenStream.Create(expressionTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlExpressionParser parser = new SqlExpressionParser(context);
        return parser.ParseExpression();
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

    internal SqlName CreateSinglePartName(SqlToken token)
    {
        SqlNamePart part = SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token);
        return new SqlName(new SqlNamePart[] { part }, token.Span);
    }

    internal void SkipTrivia()
    {
        while (Context.Peek().Kind == SqlTokenKind.Whitespace || Context.Peek().Kind == SqlTokenKind.LineComment || Context.Peek().Kind == SqlTokenKind.BlockComment)
        {
            Context.Consume();
        }
    }

    internal bool IsAnyKeyword(SqlToken token, string[] keywords)
    {
        if (keywords == null)
        {
            throw new ArgumentNullException("keywords");
        }

        int index = 0;
        while (index < keywords.Length)
        {
            if (IsKeyword(token, keywords[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
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

    internal SqlToken[] CreateParseTokens(List<SqlToken> tokens, int end)
    {
        SqlToken[] result = new SqlToken[tokens.Count + 1];
        int index = 0;
        while (index < tokens.Count)
        {
            result[index] = tokens[index];
            index = index + 1;
        }

        result[tokens.Count] = new SqlToken(SqlTokenKind.EndOfFile, new SqlTextSpan(end, 0), "");
        return result;
    }

    internal string GetIdentifierText(SqlToken token)
    {
        SqlNameQuoteStyle quoteStyle = GetQuoteStyle(token);
        if (quoteStyle == SqlNameQuoteStyle.None)
        {
            return token.Text;
        }

        string text = StripOuterQuotes(token.Text, quoteStyle);
        if (quoteStyle == SqlNameQuoteStyle.DoubleQuote)
        {
            return text.Replace("\"\"", "\"");
        }

        if (quoteStyle == SqlNameQuoteStyle.Bracket)
        {
            return text.Replace("]]", "]");
        }

        if (quoteStyle == SqlNameQuoteStyle.Backtick)
        {
            return text.Replace("``", "`");
        }

        if (quoteStyle == SqlNameQuoteStyle.SingleQuote)
        {
            return text.Replace("''", "'");
        }

        return text;
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
