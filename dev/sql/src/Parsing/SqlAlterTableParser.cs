using System;
using System.Collections.Generic;

namespace sql;

internal class SqlAlterTableParser
{
    internal SqlParserContext Context;

    internal SqlAlterTableParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlAlterTableDefinition ParseAlterTable()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "ALTER"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLALTER001", "Expected ALTER.", Context.Peek().Span);
            Context.Consume();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "TABLE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLALTER002", "Expected TABLE in ALTER TABLE statement.", Context.Peek().Span);
        }

        SqlName tableName = ParseName("SQLALTER003", "Expected table name in ALTER TABLE statement.");
        SqlAlterTableActionKind actionKind = SqlAlterTableActionKind.Unknown;
        SqlName? newTableName = null;
        SqlName? columnName = null;
        SqlName? newColumnName = null;
        SqlColumnDefinition? columnDefinition = null;
        int end = tableName.Span.GetEnd();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "RENAME"))
        {
            SqlAlterTableAction action = ParseRenameAction();
            actionKind = action.Kind;
            newTableName = action.NewTableName;
            columnName = action.ColumnName;
            newColumnName = action.NewColumnName;
            end = action.End;
        }
        else
        {
            if (IsKeyword(Context.Peek(), "ADD"))
            {
                SqlAlterTableAction action = ParseAddColumnAction();
                actionKind = action.Kind;
                columnDefinition = action.ColumnDefinition;
                end = action.End;
            }
            else
            {
                if (IsKeyword(Context.Peek(), "DROP"))
                {
                    SqlAlterTableAction action = ParseDropColumnAction();
                    actionKind = action.Kind;
                    columnName = action.ColumnName;
                    end = action.End;
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLALTER004", "Expected ALTER TABLE action.", Context.Peek().Span);
                }
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        return new SqlAlterTableDefinition(tableName, actionKind, newTableName, columnName, newColumnName, columnDefinition, span);
    }

    internal SqlAlterTableAction ParseRenameAction()
    {
        SqlToken rename = Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "TO"))
        {
            Context.Consume();
            SqlName newTableName = ParseName("SQLALTER005", "Expected new table name in ALTER TABLE rename.");
            return new SqlAlterTableAction(SqlAlterTableActionKind.RenameTable, newTableName, null, null, null, newTableName.Span.GetEnd());
        }

        if (IsKeyword(Context.Peek(), "COLUMN"))
        {
            Context.Consume();
            SkipTrivia();
        }

        SqlName columnName = ParseName("SQLALTER006", "Expected column name in ALTER TABLE rename.");
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "TO"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLALTER007", "Expected TO in ALTER TABLE rename column.", Context.Peek().Span);
        }

        SqlName newColumnName = ParseName("SQLALTER008", "Expected new column name in ALTER TABLE rename column.");
        return new SqlAlterTableAction(SqlAlterTableActionKind.RenameColumn, null, columnName, newColumnName, null, newColumnName.Span.GetEnd());
    }

    internal SqlAlterTableAction ParseAddColumnAction()
    {
        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "COLUMN"))
        {
            Context.Consume();
            SkipTrivia();
        }

        SqlCreateTableParser parser = new SqlCreateTableParser(Context);
        SqlColumnDefinition columnDefinition = parser.ParseColumnDefinition();
        return new SqlAlterTableAction(SqlAlterTableActionKind.AddColumn, null, null, null, columnDefinition, columnDefinition.Span.GetEnd());
    }

    internal SqlAlterTableAction ParseDropColumnAction()
    {
        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "COLUMN"))
        {
            Context.Consume();
            SkipTrivia();
        }

        SqlName columnName = ParseName("SQLALTER009", "Expected column name in ALTER TABLE drop column.");
        return new SqlAlterTableAction(SqlAlterTableActionKind.DropColumn, null, columnName, null, null, columnName.Span.GetEnd());
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

internal class SqlAlterTableAction
{
    internal SqlAlterTableActionKind Kind;
    internal SqlName? NewTableName;
    internal SqlName? ColumnName;
    internal SqlName? NewColumnName;
    internal SqlColumnDefinition? ColumnDefinition;
    internal int End;

    internal SqlAlterTableAction(SqlAlterTableActionKind kind, SqlName? newTableName, SqlName? columnName, SqlName? newColumnName, SqlColumnDefinition? columnDefinition, int end)
    {
        Kind = kind;
        NewTableName = newTableName;
        ColumnName = columnName;
        NewColumnName = newColumnName;
        ColumnDefinition = columnDefinition;
        End = end;
    }
}
