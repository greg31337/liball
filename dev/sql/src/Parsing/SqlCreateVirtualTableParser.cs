using System;
using System.Collections.Generic;

namespace sql;

internal class SqlCreateVirtualTableParser
{
    internal SqlParserContext Context;

    internal SqlCreateVirtualTableParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlVirtualTableDefinition ParseCreateVirtualTable()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "CREATE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIRTUAL001", "Expected CREATE.", Context.Peek().Span);
            Context.Consume();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "VIRTUAL"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIRTUAL002", "Expected VIRTUAL in CREATE VIRTUAL TABLE statement.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "TABLE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIRTUAL003", "Expected TABLE in CREATE VIRTUAL TABLE statement.", Context.Peek().Span);
        }

        bool ifNotExists = ParseOptionalIfNotExists();
        SqlName tableName = ParseName("SQLVIRTUAL007", "Expected virtual table name.");
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "USING"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIRTUAL008", "Expected USING in CREATE VIRTUAL TABLE statement.", Context.Peek().Span);
        }

        SqlName moduleName = ParseName("SQLVIRTUAL009", "Expected virtual table module name.");
        SqlToken[] moduleArguments = ParseModuleArguments();
        int end = moduleName.Span.GetEnd();
        if (Context.PeekAtOffset(-1).Kind == SqlTokenKind.CloseParen)
        {
            end = Context.PeekAtOffset(-1).Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        SqlVirtualTableDefinition definition = new SqlVirtualTableDefinition(ifNotExists, tableName, moduleName, moduleArguments, span);
        definition.Arguments = CreateVirtualTableArguments(moduleArguments);
        return definition;
    }

    internal SqlVirtualTableArgument[] CreateVirtualTableArguments(SqlToken[] tokens)
    {
        List<SqlVirtualTableArgument> arguments = new List<SqlVirtualTableArgument>();
        List<SqlToken> current = new List<SqlToken>();
        int depth = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            SqlToken token = tokens[index];
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
                current.Add(token);
            }
            else if (token.Kind == SqlTokenKind.CloseParen)
            {
                depth = depth - 1;
                current.Add(token);
            }
            else if (token.Kind == SqlTokenKind.Comma && depth == 0)
            {
                AddVirtualTableArgument(arguments, current);
                current.Clear();
            }
            else
            {
                current.Add(token);
            }

            index = index + 1;
        }

        AddVirtualTableArgument(arguments, current);
        return arguments.ToArray();
    }

    internal void AddVirtualTableArgument(List<SqlVirtualTableArgument> arguments, List<SqlToken> tokens)
    {
        int first = 0;
        while (first < tokens.Count && IsTrivia(tokens[first]))
        {
            first = first + 1;
        }

        int last = tokens.Count - 1;
        while (last >= first && IsTrivia(tokens[last]))
        {
            last = last - 1;
        }

        if (last < first)
        {
            return;
        }

        SqlToken[] argumentTokens = new SqlToken[last - first + 1];
        int index = first;
        while (index <= last)
        {
            argumentTokens[index - first] = tokens[index];
            index = index + 1;
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(tokens[first].Span.Start, tokens[last].Span.GetEnd());
        string sourceText = GetModuleArgumentText(argumentTokens, span);
        arguments.Add(new SqlVirtualTableArgument(sourceText, argumentTokens, span));
    }

    internal string GetModuleArgumentText(SqlToken[] tokens, SqlTextSpan span)
    {
        if (Context.SourceText != null)
        {
            return Context.SourceText.GetText(span);
        }

        string text = "";
        int index = 0;
        while (index < tokens.Length)
        {
            text = text + tokens[index].Text;
            index = index + 1;
        }

        return text;
    }

    internal bool IsTrivia(SqlToken token)
    {
        return token.Kind == SqlTokenKind.Whitespace || token.Kind == SqlTokenKind.LineComment || token.Kind == SqlTokenKind.BlockComment;
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIRTUAL004", "Expected NOT after IF.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "EXISTS"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIRTUAL005", "Expected EXISTS after IF NOT.", Context.Peek().Span);
        }

        return true;
    }

    internal SqlToken[] ParseModuleArguments()
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.OpenParen)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIRTUAL010", "Expected module argument list in CREATE VIRTUAL TABLE statement.", Context.Peek().Span);
            return tokens.ToArray();
        }

        Context.Consume();
        int depth = 1;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && depth > 0)
        {
            SqlToken token = Context.Consume();
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
                tokens.Add(token);
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                    if (depth > 0)
                    {
                        tokens.Add(token);
                    }
                }
                else
                {
                    tokens.Add(token);
                }
            }
        }

        if (depth > 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLVIRTUAL011", "Expected closing parenthesis in module argument list.", Context.Peek().Span);
        }

        return tokens.ToArray();
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
