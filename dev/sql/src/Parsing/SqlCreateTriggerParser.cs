using System;
using System.Collections.Generic;

namespace sql;

internal class SqlCreateTriggerParser
{
    internal SqlParserContext Context;

    internal SqlCreateTriggerParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlTriggerDefinition ParseCreateTrigger()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "CREATE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER001", "Expected CREATE.", Context.Peek().Span);
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

        if (IsKeyword(Context.Peek(), "TRIGGER"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER002", "Expected TRIGGER in CREATE TRIGGER statement.", Context.Peek().Span);
        }

        bool ifNotExists = ParseOptionalIfNotExists();
        SqlName triggerName = ParseName("SQLTRIGGER006", "Expected trigger name in CREATE TRIGGER statement.");
        SqlTriggerTiming timing = ParseOptionalTiming();
        SqlTriggerEvent triggerEvent = ParseTriggerEvent();
        SqlName[] updateColumns = new SqlName[0];
        if (triggerEvent == SqlTriggerEvent.Update)
        {
            updateColumns = ParseOptionalUpdateColumnList();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ON"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER011", "Expected ON in CREATE TRIGGER statement.", Context.Peek().Span);
        }

        SqlName targetName = ParseName("SQLTRIGGER012", "Expected trigger target name.");
        bool forEachRow = ParseOptionalForEachRow();
        SqlExpression? whenExpression = ParseOptionalWhenExpression();
        SqlTriggerBodyParseResult body = ParseTriggerBody();
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, body.End);
        return new SqlTriggerDefinition(isTemporary, ifNotExists, triggerName, timing, triggerEvent, updateColumns, targetName, forEachRow, whenExpression, body.Statements, span);
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER003", "Expected NOT after IF.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "EXISTS"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER004", "Expected EXISTS after IF NOT.", Context.Peek().Span);
        }

        return true;
    }

    internal SqlTriggerTiming ParseOptionalTiming()
    {
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "BEFORE"))
        {
            Context.Consume();
            return SqlTriggerTiming.Before;
        }

        if (IsKeyword(Context.Peek(), "AFTER"))
        {
            Context.Consume();
            return SqlTriggerTiming.After;
        }

        if (IsKeyword(Context.Peek(), "INSTEAD"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "OF"))
            {
                Context.Consume();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER007", "Expected OF after INSTEAD.", Context.Peek().Span);
            }

            return SqlTriggerTiming.InsteadOf;
        }

        return SqlTriggerTiming.Unknown;
    }

    internal SqlTriggerEvent ParseTriggerEvent()
    {
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "INSERT"))
        {
            Context.Consume();
            return SqlTriggerEvent.Insert;
        }

        if (IsKeyword(Context.Peek(), "UPDATE"))
        {
            Context.Consume();
            return SqlTriggerEvent.Update;
        }

        if (IsKeyword(Context.Peek(), "DELETE"))
        {
            Context.Consume();
            return SqlTriggerEvent.Delete;
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER008", "Expected trigger event.", Context.Peek().Span);
        return SqlTriggerEvent.Unknown;
    }

    internal SqlName[] ParseOptionalUpdateColumnList()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "OF"))
        {
            return new SqlName[0];
        }

        Context.Consume();
        List<SqlName> names = new List<SqlName>();
        SkipTrivia();
        while (!Context.IsAtEnd() && !IsKeyword(Context.Peek(), "ON"))
        {
            SqlName name = ParseName("SQLTRIGGER009", "Expected UPDATE OF column name.");
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER010", "Expected UPDATE OF column name.", Context.Peek().Span);
        }

        return names.ToArray();
    }

    internal bool ParseOptionalForEachRow()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "FOR"))
        {
            return false;
        }

        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "EACH"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER013", "Expected EACH after FOR.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ROW"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER014", "Expected ROW after FOR EACH.", Context.Peek().Span);
        }

        return true;
    }

    internal SqlExpression? ParseOptionalWhenExpression()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "WHEN"))
        {
            return null;
        }

        Context.Consume();
        return ParseExpressionUntilBegin();
    }

    internal SqlExpression ParseExpressionUntilBegin()
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SkipTrivia();
        int start = Context.Peek().Span.Start;
        int end = start;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (depth == 0 && IsKeyword(Context.Peek(), "BEGIN"))
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER015", "Expected WHEN expression.", Context.Peek().Span);
            return SqlExpression.Create(SqlExpressionKind.Unknown, Context.Peek().Span);
        }

        SqlToken[] expressionTokens = CreateParseTokens(tokens, end);
        SqlTokenStream stream = SqlTokenStream.Create(expressionTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlExpressionParser parser = new SqlExpressionParser(context);
        return parser.ParseExpression();
    }

    internal SqlTriggerBodyParseResult ParseTriggerBody()
    {
        SkipTrivia();
        SqlToken last = Context.PeekAtOffset(-1);
        if (IsKeyword(Context.Peek(), "BEGIN"))
        {
            last = Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER016", "Expected BEGIN in CREATE TRIGGER statement.", Context.Peek().Span);
            return new SqlTriggerBodyParseResult(new SqlStatement[0], last.Span.GetEnd());
        }

        List<SqlToken> bodyTokens = new List<SqlToken>();
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (IsKeyword(Context.Peek(), "END"))
            {
                last = Context.Consume();
                return new SqlTriggerBodyParseResult(ParseBodyStatements(bodyTokens, last.Span.Start), last.Span.GetEnd());
            }

            SqlToken token = Context.Consume();
            bodyTokens.Add(token);
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRIGGER017", "Expected END in CREATE TRIGGER statement.", Context.Peek().Span);
        return new SqlTriggerBodyParseResult(ParseBodyStatements(bodyTokens, last.Span.GetEnd()), last.Span.GetEnd());
    }

    internal SqlStatement[] ParseBodyStatements(List<SqlToken> bodyTokens, int end)
    {
        if (bodyTokens.Count == 0)
        {
            return new SqlStatement[0];
        }

        SqlToken[] tokens = CreateParseTokens(bodyTokens, end);
        SqlTokenStream stream = SqlTokenStream.Create(tokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlStatementParser parser = new SqlStatementParser(context);
        SqlScript script = parser.ParseScript();
        return script.Statements;
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

internal class SqlTriggerBodyParseResult
{
    internal SqlStatement[] Statements;
    internal int End;

    internal SqlTriggerBodyParseResult(SqlStatement[] statements, int end)
    {
        if (statements == null)
        {
            throw new ArgumentNullException("statements");
        }

        Statements = statements;
        End = end;
    }
}
