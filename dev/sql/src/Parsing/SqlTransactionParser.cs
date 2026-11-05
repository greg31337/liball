using System;
using System.Collections.Generic;

namespace sql;

internal class SqlTransactionParser
{
    internal SqlParserContext Context;

    internal SqlTransactionParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlTransactionStatement ParseTransaction()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        SqlTransactionActionKind actionKind = GetActionKind(first);
        if (actionKind == SqlTransactionActionKind.Unknown)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRANSACTION001", "Expected transaction statement.", Context.Peek().Span);
        }

        Context.Consume();
        SqlTransactionMode mode = SqlTransactionMode.None;
        bool hasTransactionKeyword = false;
        bool isToSavepoint = false;
        bool hasSavepointKeyword = false;
        int rollbackTargetStart = 0;
        SqlName? savepointName = null;
        int end = first.Span.GetEnd();
        SkipTrivia();

        if (actionKind == SqlTransactionActionKind.Begin)
        {
            SqlToken modeToken = Context.Peek();
            mode = ParseBeginMode();
            if (mode != SqlTransactionMode.None)
            {
                end = modeToken.Span.GetEnd();
            }

            SkipTrivia();
            if (IsKeyword(Context.Peek(), "TRANSACTION"))
            {
                SqlToken transactionToken = Context.Consume();
                hasTransactionKeyword = true;
                end = transactionToken.Span.GetEnd();
                SkipTrivia();
            }
        }
        else
        {
            if (actionKind == SqlTransactionActionKind.Commit || actionKind == SqlTransactionActionKind.End || actionKind == SqlTransactionActionKind.Rollback)
            {
                if (IsKeyword(Context.Peek(), "TRANSACTION"))
                {
                    SqlToken transactionToken = Context.Consume();
                    hasTransactionKeyword = true;
                    end = transactionToken.Span.GetEnd();
                    SkipTrivia();
                }

                if (actionKind == SqlTransactionActionKind.Rollback && IsKeyword(Context.Peek(), "TO"))
                {
                    isToSavepoint = true;
                    SqlToken toToken = Context.Consume();
                    rollbackTargetStart = toToken.Span.Start;
                    end = toToken.Span.GetEnd();
                    SkipTrivia();
                    if (IsKeyword(Context.Peek(), "SAVEPOINT"))
                    {
                        SqlToken savepointToken = Context.Consume();
                        hasSavepointKeyword = true;
                        end = savepointToken.Span.GetEnd();
                        SkipTrivia();
                    }

                    if (Context.Peek().Kind == SqlTokenKind.EndOfFile)
                    {
                        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRANSACTION003", "Expected ROLLBACK savepoint name.", Context.Peek().Span);
                    }
                    else
                    {
                        savepointName = ParseName("SQLTRANSACTION003", "Expected ROLLBACK savepoint name.");
                        end = savepointName.Span.GetEnd();
                        SkipTrivia();
                    }
                }
            }
        }

        if (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlToken unexpected = Context.Peek();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLTRANSACTION002", "Unexpected token in transaction statement.", unexpected.Span);
            while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
            {
                unexpected = Context.Consume();
                end = unexpected.Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        SqlTransactionStatement statement = new SqlTransactionStatement(actionKind, mode, hasTransactionKeyword, isToSavepoint, savepointName, span);
        statement.HasSavepointKeyword = hasSavepointKeyword;
        if (savepointName != null)
        {
            statement.RollbackTarget = new SqlRollbackTarget(hasSavepointKeyword, savepointName, SqlTextSpan.FromBounds(rollbackTargetStart, savepointName.Span.GetEnd()));
        }

        return statement;
    }

    internal SqlTransactionMode ParseBeginMode()
    {
        if (IsKeyword(Context.Peek(), "DEFERRED"))
        {
            Context.Consume();
            return SqlTransactionMode.Deferred;
        }

        if (IsKeyword(Context.Peek(), "IMMEDIATE"))
        {
            Context.Consume();
            return SqlTransactionMode.Immediate;
        }

        if (IsKeyword(Context.Peek(), "EXCLUSIVE"))
        {
            Context.Consume();
            return SqlTransactionMode.Exclusive;
        }

        return SqlTransactionMode.None;
    }

    internal SqlTransactionActionKind GetActionKind(SqlToken token)
    {
        if (IsKeyword(token, "BEGIN"))
        {
            return SqlTransactionActionKind.Begin;
        }

        if (IsKeyword(token, "COMMIT"))
        {
            return SqlTransactionActionKind.Commit;
        }

        if (IsKeyword(token, "END"))
        {
            return SqlTransactionActionKind.End;
        }

        if (IsKeyword(token, "ROLLBACK"))
        {
            return SqlTransactionActionKind.Rollback;
        }

        return SqlTransactionActionKind.Unknown;
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
