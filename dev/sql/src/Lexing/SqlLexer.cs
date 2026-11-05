using System;
using System.Collections.Generic;
using System.Text;

namespace sql;

internal class SqlLexer
{
    internal SqlSourceText SourceText;
    internal SqlDialectOptions Options;
    internal SqlDiagnosticBag Diagnostics;
    internal int Position;

    internal SqlLexer(SqlSourceText sourceText, SqlDialectOptions options)
    {
        if (sourceText == null)
        {
            throw new ArgumentNullException("sourceText");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();

        SourceText = sourceText;
        Options = options;
        Diagnostics = new SqlDiagnosticBag();
        Position = 0;
    }

    internal static SqlLexer Create(string text)
    {
        return Create(text, GenericDialect.CreateOptions());
    }

    internal static SqlLexer Create(string text, SqlDialectOptions options)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return new SqlLexer(new SqlSourceText(text), options);
    }

    internal static SqlLexer Create(SqlSourceText sourceText)
    {
        return new SqlLexer(sourceText, GenericDialect.CreateOptions());
    }

    internal static SqlLexer Create(SqlSourceText sourceText, SqlDialectOptions options)
    {
        return new SqlLexer(sourceText, options);
    }

    internal static SqlToken[] Tokenize(string text)
    {
        SqlLexer lexer = Create(text);
        return lexer.LexTokens();
    }

    internal static SqlToken[] Tokenize(string text, SqlDialectOptions options)
    {
        SqlLexer lexer = Create(text, options);
        return lexer.LexTokens();
    }

    internal SqlToken[] LexTokens()
    {
        List<SqlToken> tokens = new List<SqlToken>();

        while (Position < SourceText.GetLength())
        {
            tokens.Add(LexToken());
        }

        tokens.Add(new SqlToken(SqlTokenKind.EndOfFile, new SqlTextSpan(Position, 0), ""));
        SqlToken[] result = tokens.ToArray();
        BindTrivia(result);
        return result;
    }

    internal void BindTrivia(SqlToken[] tokens)
    {
        List<SqlTrivia> pending = new List<SqlTrivia>();
        SqlToken? previous = null;
        int index = 0;
        while (index < tokens.Length)
        {
            SqlToken token = tokens[index];
            if (IsTriviaKind(token.Kind))
            {
                pending.Add(CreateTrivia(token));
            }
            else
            {
                SqlTrivia[] trivia = pending.ToArray();
                token.LeadingTrivia = trivia;
                if (previous != null)
                {
                    previous.TrailingTrivia = trivia;
                }

                pending.Clear();
                previous = token;
            }

            index = index + 1;
        }
    }

    internal SqlTrivia CreateTrivia(SqlToken token)
    {
        SqlTriviaKind kind = SqlTriviaKind.Whitespace;
        if (token.Kind == SqlTokenKind.LineComment)
        {
            kind = SqlTriviaKind.LineComment;
        }
        else if (token.Kind == SqlTokenKind.BlockComment)
        {
            kind = SqlTriviaKind.BlockComment;
        }

        return new SqlTrivia(kind, token.Span, token.Text);
    }

    internal bool IsTriviaKind(SqlTokenKind kind)
    {
        return kind == SqlTokenKind.Whitespace || kind == SqlTokenKind.LineComment || kind == SqlTokenKind.BlockComment;
    }

    internal SqlToken LexToken()
    {
        char current = Peek(0);

        if (IsWhitespace(current))
        {
            return LexWhitespace();
        }

        if (current == '-' && Peek(1) == '-')
        {
            return LexLineComment();
        }

        if (current == '/' && Peek(1) == '*')
        {
            return LexBlockComment();
        }

        if (current == '"')
        {
            return LexDoubleQuotedToken();
        }

        if (current == '[' && Options.AllowBracketQuotedIdentifiers)
        {
            return LexBracketQuotedIdentifier();
        }

        if (current == '`' && Options.AllowBacktickQuotedIdentifiers)
        {
            return LexBacktickQuotedIdentifier();
        }

        if (IsEscapedStringStart(current))
        {
            return LexEscapedString();
        }

        if (IsUnicodeStringStart(current))
        {
            return LexUnicodeString();
        }

        if (current == '$' && GetDollarQuoteDelimiterLength() > 0)
        {
            return LexDollarQuotedString();
        }

        if (IsBlobLiteralStart(current))
        {
            return LexBlobLiteral();
        }

        if (current == '\'')
        {
            return LexSingleQuotedString();
        }

        SqlToken? customOperator = LexCustomOperator();
        if (customOperator != null)
        {
            return customOperator;
        }

        if (IsParameterStart(current))
        {
            return LexParameter();
        }

        if (IsNumericLiteralStart(current))
        {
            return LexNumericLiteral();
        }

        if (IsIdentifierStart(current))
        {
            return LexBareIdentifierOrKeyword();
        }

        SqlToken? punctuationOrOperator = LexPunctuationOrOperator();
        if (punctuationOrOperator != null)
        {
            return punctuationOrOperator;
        }

        return LexBadToken();
    }

    internal SqlToken LexWhitespace()
    {
        int start = Position;

        while (Position < SourceText.GetLength() && IsWhitespace(Peek(0)))
        {
            Position = Position + 1;
        }

        return CreateToken(SqlTokenKind.Whitespace, start, Position);
    }

    internal SqlToken LexLineComment()
    {
        int start = Position;

        Position = Position + 2;
        while (Position < SourceText.GetLength() && !IsLineBreak(Peek(0)))
        {
            Position = Position + 1;
        }

        return CreateToken(SqlTokenKind.LineComment, start, Position);
    }

    internal SqlToken LexBlockComment()
    {
        int start = Position;
        bool done = false;

        Position = Position + 2;
        while (Position < SourceText.GetLength() && !done)
        {
            if (Peek(0) == '*' && Peek(1) == '/')
            {
                Position = Position + 2;
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        if (!done)
        {
            AddDiagnostic("SQLLEX002", "Unterminated block comment.", start, Position);
        }

        return CreateToken(SqlTokenKind.BlockComment, start, Position);
    }

    internal SqlToken LexDoubleQuotedToken()
    {
        int start = Position;
        SqlTokenKind kind = SqlTokenKind.BadToken;
        bool done = false;

        Position = Position + 1;
        while (Position < SourceText.GetLength() && !done)
        {
            if (Peek(0) == '"' && Peek(1) == '"')
            {
                Position = Position + 2;
            }
            else if (Peek(0) == '"')
            {
                Position = Position + 1;
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        if (!done)
        {
            AddDiagnostic("SQLLEX003", "Unterminated double-quoted token.", start, Position);
        }

        if (Options.AllowDoubleQuotedIdentifiers)
        {
            kind = SqlTokenKind.Identifier;
        }
        else if (Options.AllowDoubleQuotedStrings)
        {
            kind = SqlTokenKind.StringLiteral;
        }
        else if (done)
        {
            AddDiagnostic("SQLLEX004", "Double-quoted tokens are not allowed by this dialect.", start, Position);
        }

        SqlToken token = CreateToken(kind, start, Position);
        if (kind == SqlTokenKind.Identifier)
        {
            token.SetLexicalData(DecodeDoubledQuoteValue(start + 1, GetQuotedContentEnd(start, Position, done), '"'), SqlNameQuoteStyle.DoubleQuote, SqlStringQuoteStyle.None, SqlParameterStyle.None, "\"");
        }
        else if (kind == SqlTokenKind.StringLiteral)
        {
            token.SetLexicalData(DecodeDoubledQuoteValue(start + 1, GetQuotedContentEnd(start, Position, done), '"'), SqlNameQuoteStyle.None, SqlStringQuoteStyle.DoubleQuote, SqlParameterStyle.None, "\"");
        }

        return token;
    }

    internal SqlToken LexBracketQuotedIdentifier()
    {
        int start = Position;
        bool done = false;

        Position = Position + 1;
        while (Position < SourceText.GetLength() && !done)
        {
            if (Peek(0) == ']' && Peek(1) == ']')
            {
                Position = Position + 2;
            }
            else if (Peek(0) == ']')
            {
                Position = Position + 1;
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        if (!done)
        {
            AddDiagnostic("SQLLEX005", "Unterminated bracket identifier.", start, Position);
        }

        SqlToken token = CreateToken(SqlTokenKind.Identifier, start, Position);
        token.SetLexicalData(DecodeDoubledQuoteValue(start + 1, GetQuotedContentEnd(start, Position, done), ']'), SqlNameQuoteStyle.Bracket, SqlStringQuoteStyle.None, SqlParameterStyle.None, "[]");
        return token;
    }

    internal SqlToken LexBacktickQuotedIdentifier()
    {
        int start = Position;
        bool done = false;

        Position = Position + 1;
        while (Position < SourceText.GetLength() && !done)
        {
            if (Peek(0) == '`' && Peek(1) == '`')
            {
                Position = Position + 2;
            }
            else if (Peek(0) == '`')
            {
                Position = Position + 1;
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        if (!done)
        {
            AddDiagnostic("SQLLEX006", "Unterminated backtick identifier.", start, Position);
        }

        SqlToken token = CreateToken(SqlTokenKind.Identifier, start, Position);
        token.SetLexicalData(DecodeDoubledQuoteValue(start + 1, GetQuotedContentEnd(start, Position, done), '`'), SqlNameQuoteStyle.Backtick, SqlStringQuoteStyle.None, SqlParameterStyle.None, "`");
        return token;
    }

    internal SqlToken LexSingleQuotedString()
    {
        int start = Position;
        SqlTokenKind kind = SqlTokenKind.BadToken;
        bool done = false;

        Position = Position + 1;
        while (Position < SourceText.GetLength() && !done)
        {
            if (Peek(0) == '\'' && Peek(1) == '\'')
            {
                Position = Position + 2;
            }
            else if (Peek(0) == '\'')
            {
                Position = Position + 1;
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        if (!done)
        {
            AddDiagnostic("SQLLEX007", "Unterminated string literal.", start, Position);
        }

        if (Options.AllowSingleQuotedStrings)
        {
            kind = SqlTokenKind.StringLiteral;
        }
        else if (done)
        {
            AddDiagnostic("SQLLEX008", "String literals are not allowed by this dialect.", start, Position);
        }

        SqlToken token = CreateToken(kind, start, Position);
        if (kind == SqlTokenKind.StringLiteral)
        {
            token.SetLexicalData(DecodeDoubledQuoteValue(start + 1, GetQuotedContentEnd(start, Position, done), '\''), SqlNameQuoteStyle.None, SqlStringQuoteStyle.SingleQuote, SqlParameterStyle.None, "'");
        }

        return token;
    }

    internal SqlToken LexEscapedString()
    {
        int start = Position;
        bool done = false;
        Position = Position + 2;
        int contentStart = Position;
        while (Position < SourceText.GetLength() && !done)
        {
            if (Peek(0) == '\\' && Peek(1) != '\0')
            {
                Position = Position + 2;
            }
            else if (Peek(0) == '\'' && Peek(1) == '\'')
            {
                Position = Position + 2;
            }
            else if (Peek(0) == '\'')
            {
                Position = Position + 1;
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        int contentEnd = GetQuotedContentEnd(start, Position, done);
        if (!done)
        {
            AddDiagnostic("SQLLEX013", "Unterminated escaped string literal.", start, Position);
        }

        SqlToken token = CreateToken(SqlTokenKind.StringLiteral, start, Position);
        token.SetLexicalData(DecodeEscapedValue(contentStart, contentEnd), SqlNameQuoteStyle.None, SqlStringQuoteStyle.EscapedSingleQuote, SqlParameterStyle.None, SourceText.Text.Substring(start, 2));
        return token;
    }

    internal SqlToken LexUnicodeString()
    {
        int start = Position;
        bool done = false;
        Position = Position + 3;
        int contentStart = Position;
        while (Position < SourceText.GetLength() && !done)
        {
            if (Peek(0) == '\'' && Peek(1) == '\'')
            {
                Position = Position + 2;
            }
            else if (Peek(0) == '\'')
            {
                Position = Position + 1;
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        int contentEnd = GetQuotedContentEnd(start, Position, done);
        if (!done)
        {
            AddDiagnostic("SQLLEX014", "Unterminated Unicode string literal.", start, Position);
        }

        SqlToken token = CreateToken(SqlTokenKind.StringLiteral, start, Position);
        token.SetLexicalData(DecodeUnicodeValue(contentStart, contentEnd), SqlNameQuoteStyle.None, SqlStringQuoteStyle.UnicodeSingleQuote, SqlParameterStyle.None, SourceText.Text.Substring(start, 3));
        return token;
    }

    internal SqlToken LexDollarQuotedString()
    {
        int start = Position;
        int delimiterLength = GetDollarQuoteDelimiterLength();
        string delimiter = SourceText.Text.Substring(Position, delimiterLength);
        Position = Position + delimiterLength;
        int contentStart = Position;
        bool done = false;
        while (Position < SourceText.GetLength() && !done)
        {
            if (MatchesAtCurrentPosition(delimiter))
            {
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        int contentEnd = Position;
        if (done)
        {
            Position = Position + delimiterLength;
        }
        else
        {
            AddDiagnostic("SQLLEX015", "Unterminated dollar-quoted string literal.", start, Position);
        }

        SqlToken token = CreateToken(SqlTokenKind.StringLiteral, start, Position);
        token.SetLexicalData(SourceText.Text.Substring(contentStart, contentEnd - contentStart), SqlNameQuoteStyle.None, SqlStringQuoteStyle.DollarQuote, SqlParameterStyle.None, delimiter);
        return token;
    }

    internal SqlToken LexBlobLiteral()
    {
        int start = Position;
        int contentStart = Position + 2;
        int contentEnd = 0;
        bool done = false;

        Position = Position + 2;
        while (Position < SourceText.GetLength() && !done)
        {
            if (Peek(0) == '\'' && Peek(1) == '\'')
            {
                Position = Position + 2;
            }
            else if (Peek(0) == '\'')
            {
                Position = Position + 1;
                done = true;
            }
            else
            {
                Position = Position + 1;
            }
        }

        contentEnd = Position;
        if (done)
        {
            contentEnd = Position - 1;
        }
        else
        {
            AddDiagnostic("SQLLEX009", "Unterminated blob literal.", start, Position);
        }

        if (!IsValidBlobContent(contentStart, contentEnd))
        {
            AddDiagnostic("SQLLEX010", "Malformed blob literal.", start, Position);
        }

        SqlToken token = CreateToken(SqlTokenKind.BlobLiteral, start, Position);
        token.SetLexicalData(SourceText.Text.Substring(contentStart, contentEnd - contentStart), SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, SqlParameterStyle.None, "'");
        return token;
    }

    internal SqlToken LexNumericLiteral()
    {
        int start = Position;

        if (IsHexIntegerLiteralStart(Peek(0)))
        {
            Position = Position + 2;
            int digitStart = Position;
            ConsumeHexadecimalDigits();

            if (Position == digitStart)
            {
                AddDiagnostic("SQLLEX011", "Malformed hexadecimal integer literal.", start, Position);
            }

            return CreateToken(SqlTokenKind.NumericLiteral, start, Position);
        }

        if (Peek(0) == '.')
        {
            Position = Position + 1;
            ConsumeDecimalDigits();
        }
        else
        {
            ConsumeDecimalDigits();

            if (Peek(0) == '.' && Peek(1) != '.')
            {
                Position = Position + 1;
                ConsumeDecimalDigits();
            }
        }

        if (IsExponentStart())
        {
            Position = Position + 1;
            if (Peek(0) == '+' || Peek(0) == '-')
            {
                Position = Position + 1;
            }

            ConsumeDecimalDigits();
        }
        else if (Peek(0) == 'e' || Peek(0) == 'E')
        {
            Position = Position + 1;
            if (Peek(0) == '+' || Peek(0) == '-')
            {
                Position = Position + 1;
            }

            AddDiagnostic("SQLLEX012", "Malformed exponent in numeric literal.", start, Position);
        }

        return CreateToken(SqlTokenKind.NumericLiteral, start, Position);
    }

    internal SqlToken LexParameter()
    {
        int start = Position;

        if (Peek(0) == '?')
        {
            Position = Position + 1;
            while (IsDigit(Peek(0)))
            {
                Position = Position + 1;
            }

            SqlToken questionToken = CreateToken(SqlTokenKind.Parameter, start, Position);
            SqlParameterStyle questionStyle = SqlParameterStyle.Question;
            if (Position > start + 1)
            {
                questionStyle = SqlParameterStyle.NumberedQuestion;
            }

            questionToken.SetLexicalData(SourceText.Text.Substring(start + 1, Position - start - 1), SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, questionStyle, "");
            return questionToken;
        }

        Position = Position + 1;
        while (IsParameterNamePart(Peek(0)))
        {
            Position = Position + 1;
        }

        if (SourceText.Text[start] == '$' && Options.Kind == SqlDialectKind.SQLite)
        {
            ConsumeSqliteTclParameterSuffix();
        }

        SqlToken token = CreateToken(SqlTokenKind.Parameter, start, Position);
        SqlParameterStyle style = SqlParameterStyle.ColonName;
        if (SourceText.Text[start] == '@')
        {
            style = SqlParameterStyle.AtName;
        }
        else if (SourceText.Text[start] == '$')
        {
            if (IsDigit(SourceText.Text[start + 1]))
            {
                style = SqlParameterStyle.DollarNumber;
            }
            else
            {
                style = SqlParameterStyle.DollarName;
            }
        }

        token.SetLexicalData(SourceText.Text.Substring(start + 1, Position - start - 1), SqlNameQuoteStyle.None, SqlStringQuoteStyle.None, style, "");
        return token;
    }

    internal void ConsumeDecimalDigits()
    {
        bool done = false;
        while (!done)
        {
            if (IsDigit(Peek(0)))
            {
                Position = Position + 1;
            }
            else if (Options.Kind == SqlDialectKind.SQLite && Peek(0) == '_' && IsDigit(Peek(-1)) && IsDigit(Peek(1)))
            {
                Position = Position + 1;
            }
            else
            {
                done = true;
            }
        }
    }

    internal void ConsumeHexadecimalDigits()
    {
        bool done = false;
        while (!done)
        {
            if (IsHexDigit(Peek(0)))
            {
                Position = Position + 1;
            }
            else if (Options.Kind == SqlDialectKind.SQLite && Peek(0) == '_' && IsHexDigit(Peek(-1)) && IsHexDigit(Peek(1)))
            {
                Position = Position + 1;
            }
            else
            {
                done = true;
            }
        }
    }

    internal void ConsumeSqliteTclParameterSuffix()
    {
        while (Peek(0) == ':' && Peek(1) == ':' && IsParameterNameStart(Peek(2)))
        {
            Position = Position + 2;
            while (IsParameterNamePart(Peek(0)))
            {
                Position = Position + 1;
            }
        }

        if (Peek(0) != '(')
        {
            return;
        }

        int offset = 1;
        while (Peek(offset) != '\0' && Peek(offset) != ')' && !char.IsWhiteSpace(Peek(offset)))
        {
            offset = offset + 1;
        }

        if (Peek(offset) == ')')
        {
            Position = Position + offset + 1;
        }
    }

    internal SqlToken LexBareIdentifierOrKeyword()
    {
        int start = Position;

        Position = Position + 1;
        while (Position < SourceText.GetLength() && IsIdentifierPart(Peek(0)))
        {
            Position = Position + 1;
        }

        return CreateIdentifierOrKeywordToken(start, Position);
    }

    internal SqlToken LexBadToken()
    {
        int start = Position;

        Position = Position + 1;
        AddDiagnostic("SQLLEX001", "Invalid character.", start, Position);
        return CreateToken(SqlTokenKind.BadToken, start, Position);
    }

    internal SqlToken? LexCustomOperator()
    {
        int index = 0;
        int bestLength = 0;
        while (index < Options.CustomOperators.Length)
        {
            string text = Options.CustomOperators[index].Text;
            if (text.Length > bestLength && MatchesAtCurrentPosition(text))
            {
                bestLength = text.Length;
            }

            index = index + 1;
        }

        if (bestLength == 0)
        {
            return null;
        }

        return LexFixed(SqlTokenKind.CustomOperator, Position, bestLength);
    }

    internal bool MatchesAtCurrentPosition(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (Peek(index) != text[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal SqlToken? LexPunctuationOrOperator()
    {
        int start = Position;
        char current = Peek(0);
        char next = Peek(1);

        if (current == ':' && next == ':')
        {
            return LexFixed(SqlTokenKind.DoubleColon, start, 2);
        }

        if (current == '|' && next == '|')
        {
            return LexFixed(SqlTokenKind.DoublePipe, start, 2);
        }

        if (current == '=' && next == '=')
        {
            return LexFixed(SqlTokenKind.DoubleEqual, start, 2);
        }

        if (current == '!' && next == '=')
        {
            return LexFixed(SqlTokenKind.BangEqual, start, 2);
        }

        if (current == '<' && next == '=')
        {
            return LexFixed(SqlTokenKind.LessEqual, start, 2);
        }

        if (current == '>' && next == '=')
        {
            return LexFixed(SqlTokenKind.GreaterEqual, start, 2);
        }

        if (current == '<' && next == '>')
        {
            return LexFixed(SqlTokenKind.LessGreater, start, 2);
        }

        if (current == '<' && next == '<')
        {
            return LexFixed(SqlTokenKind.ShiftLeft, start, 2);
        }

        if (current == '>' && next == '>')
        {
            return LexFixed(SqlTokenKind.ShiftRight, start, 2);
        }

        if (current == '-' && next == '>' && Peek(2) == '>')
        {
            return LexFixed(SqlTokenKind.DoubleArrow, start, 3);
        }

        if (current == '-' && next == '>')
        {
            return LexFixed(SqlTokenKind.Arrow, start, 2);
        }

        if (current == '(')
        {
            return LexFixed(SqlTokenKind.OpenParen, start, 1);
        }

        if (current == ')')
        {
            return LexFixed(SqlTokenKind.CloseParen, start, 1);
        }

        if (current == '[')
        {
            return LexFixed(SqlTokenKind.OpenBracket, start, 1);
        }

        if (current == ']')
        {
            return LexFixed(SqlTokenKind.CloseBracket, start, 1);
        }

        if (current == ',')
        {
            return LexFixed(SqlTokenKind.Comma, start, 1);
        }

        if (current == '.')
        {
            return LexFixed(SqlTokenKind.Dot, start, 1);
        }

        if (current == ';')
        {
            return LexFixed(SqlTokenKind.Semicolon, start, 1);
        }

        if (current == ':')
        {
            return LexFixed(SqlTokenKind.Colon, start, 1);
        }

        if (current == '?')
        {
            return LexFixed(SqlTokenKind.Question, start, 1);
        }

        if (current == '@')
        {
            return LexFixed(SqlTokenKind.At, start, 1);
        }

        if (current == '$')
        {
            return LexFixed(SqlTokenKind.Dollar, start, 1);
        }

        if (current == '+')
        {
            return LexFixed(SqlTokenKind.Plus, start, 1);
        }

        if (current == '-')
        {
            return LexFixed(SqlTokenKind.Minus, start, 1);
        }

        if (current == '*')
        {
            return LexFixed(SqlTokenKind.Star, start, 1);
        }

        if (current == '/')
        {
            return LexFixed(SqlTokenKind.Slash, start, 1);
        }

        if (current == '%')
        {
            return LexFixed(SqlTokenKind.Percent, start, 1);
        }

        if (current == '|')
        {
            return LexFixed(SqlTokenKind.Pipe, start, 1);
        }

        if (current == '&')
        {
            return LexFixed(SqlTokenKind.Ampersand, start, 1);
        }

        if (current == '~')
        {
            return LexFixed(SqlTokenKind.Tilde, start, 1);
        }

        if (current == '^')
        {
            return LexFixed(SqlTokenKind.Caret, start, 1);
        }

        if (current == '=')
        {
            return LexFixed(SqlTokenKind.Equal, start, 1);
        }

        if (current == '<')
        {
            return LexFixed(SqlTokenKind.Less, start, 1);
        }

        if (current == '>')
        {
            return LexFixed(SqlTokenKind.Greater, start, 1);
        }

        return null;
    }

    internal SqlToken LexFixed(SqlTokenKind kind, int start, int length)
    {
        Position = Position + length;
        return CreateToken(kind, start, Position);
    }

    internal SqlToken CreateToken(SqlTokenKind kind, int start, int end)
    {
        SqlTextSpan span = SqlTextSpan.FromBounds(start, end);
        string text = SourceText.GetText(span);

        return new SqlToken(kind, span, text);
    }

    internal SqlToken CreateIdentifierOrKeywordToken(int start, int end)
    {
        SqlTextSpan span = SqlTextSpan.FromBounds(start, end);
        string text = SourceText.GetText(span);
        SqlTokenKind kind = SqlTokenKind.Identifier;

        if (Options.IsKeyword(text))
        {
            kind = SqlTokenKind.Keyword;
        }

        return new SqlToken(kind, span, text);
    }

    internal void AddDiagnostic(string code, string message, int start, int end)
    {
        Diagnostics.AddDiagnosticWithDetails(
            SqlDiagnosticSeverity.Error,
            code,
            message,
            SqlTextSpan.FromBounds(start, end),
            SourceText);
    }

    internal int GetQuotedContentEnd(int start, int end, bool terminated)
    {
        if (terminated && end > start)
        {
            return end - 1;
        }

        return end;
    }

    internal string DecodeDoubledQuoteValue(int start, int end, char quote)
    {
        StringBuilder builder = new StringBuilder();
        int index = start;
        while (index < end)
        {
            char value = SourceText.Text[index];
            if (value == quote && index + 1 < end && SourceText.Text[index + 1] == quote)
            {
                builder.Append(quote);
                index = index + 2;
            }
            else
            {
                builder.Append(value);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal string DecodeEscapedValue(int start, int end)
    {
        StringBuilder builder = new StringBuilder();
        int index = start;
        while (index < end)
        {
            char value = SourceText.Text[index];
            if (value == '\'' && index + 1 < end && SourceText.Text[index + 1] == '\'')
            {
                builder.Append('\'');
                index = index + 2;
            }
            else if (value == '\\' && index + 1 < end)
            {
                char escaped = SourceText.Text[index + 1];
                if (escaped == 'b')
                {
                    builder.Append('\b');
                    index = index + 2;
                }
                else if (escaped == 'f')
                {
                    builder.Append('\f');
                    index = index + 2;
                }
                else if (escaped == 'n')
                {
                    builder.Append('\n');
                    index = index + 2;
                }
                else if (escaped == 'r')
                {
                    builder.Append('\r');
                    index = index + 2;
                }
                else if (escaped == 't')
                {
                    builder.Append('\t');
                    index = index + 2;
                }
                else if (escaped == '\n')
                {
                    index = index + 2;
                }
                else if (escaped == '\r')
                {
                    index = index + 2;
                    if (index < end && SourceText.Text[index] == '\n')
                    {
                        index = index + 1;
                    }
                }
                else if (escaped == 'x')
                {
                    int digitCount = GetAvailableHexDigitCount(index + 2, 2, end);
                    if (digitCount > 0)
                    {
                        AppendDecodedCodePoint(builder, DecodeUnicodeCodePoint(index + 2, digitCount, end));
                        index = index + 2 + digitCount;
                    }
                    else
                    {
                        builder.Append(escaped);
                        index = index + 2;
                    }
                }
                else if (escaped == 'u' || escaped == 'U')
                {
                    int digitCount = 4;
                    if (escaped == 'U')
                    {
                        digitCount = 8;
                    }

                    int codePoint = DecodeUnicodeCodePoint(index + 2, digitCount, end);
                    if (codePoint >= 0)
                    {
                        AppendDecodedCodePoint(builder, codePoint);
                        index = index + 2 + digitCount;
                    }
                    else
                    {
                        builder.Append(escaped);
                        index = index + 2;
                    }
                }
                else if (escaped >= '0' && escaped <= '7')
                {
                    int digitCount = GetAvailableOctalDigitCount(index + 1, 3, end);
                    int codePoint = DecodeOctalCodePoint(index + 1, digitCount);
                    AppendDecodedCodePoint(builder, codePoint);
                    index = index + 1 + digitCount;
                }
                else
                {
                    builder.Append(escaped);
                    index = index + 2;
                }
            }
            else
            {
                builder.Append(value);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal string DecodeUnicodeValue(int start, int end)
    {
        StringBuilder builder = new StringBuilder();
        int index = start;
        while (index < end)
        {
            char value = SourceText.Text[index];
            if (value == '\'' && index + 1 < end && SourceText.Text[index + 1] == '\'')
            {
                builder.Append('\'');
                index = index + 2;
            }
            else if (value == '\\' && index + 1 < end && SourceText.Text[index + 1] == '\\')
            {
                builder.Append('\\');
                index = index + 2;
            }
            else if (value == '\\')
            {
                int digitStart = index + 1;
                int digitCount = 4;
                if (digitStart < end && SourceText.Text[digitStart] == '+')
                {
                    digitStart = digitStart + 1;
                    digitCount = 6;
                }

                int codePoint = DecodeUnicodeCodePoint(digitStart, digitCount, end);
                if (codePoint >= 0)
                {
                    AppendDecodedCodePoint(builder, codePoint);
                    index = digitStart + digitCount;
                }
                else
                {
                    builder.Append(value);
                    index = index + 1;
                }
            }
            else
            {
                builder.Append(value);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal int DecodeUnicodeCodePoint(int start, int count, int end)
    {
        if (start + count > end)
        {
            return -1;
        }

        long value = 0;
        int index = 0;
        while (index < count)
        {
            int digit = GetHexDigitValue(SourceText.Text[start + index]);
            if (digit < 0)
            {
                return -1;
            }

            value = (value * 16) + digit;
            index = index + 1;
        }

        if (value > 1114111)
        {
            return -1;
        }

        return (int)value;
    }

    internal int GetAvailableHexDigitCount(int start, int maximum, int end)
    {
        int count = 0;
        while (count < maximum && start + count < end && GetHexDigitValue(SourceText.Text[start + count]) >= 0)
        {
            count = count + 1;
        }

        return count;
    }

    internal int GetAvailableOctalDigitCount(int start, int maximum, int end)
    {
        int count = 0;
        while (count < maximum && start + count < end)
        {
            char value = SourceText.Text[start + count];
            if (value < '0' || value > '7')
            {
                break;
            }

            count = count + 1;
        }

        return count;
    }

    internal int DecodeOctalCodePoint(int start, int count)
    {
        int value = 0;
        int index = 0;
        while (index < count)
        {
            value = (value * 8) + SourceText.Text[start + index] - '0';
            index = index + 1;
        }

        return value;
    }

    internal void AppendDecodedCodePoint(StringBuilder builder, int codePoint)
    {
        if (codePoint <= 65535)
        {
            builder.Append((char)codePoint);
        }
        else
        {
            builder.Append(char.ConvertFromUtf32(codePoint));
        }
    }

    internal static int GetHexDigitValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }

    internal char Peek(int offset)
    {
        long index = (long)Position + offset;

        if (index < 0 || index >= SourceText.GetLength())
        {
            return '\0';
        }

        return SourceText.Text[(int)index];
    }

    internal static bool IsWhitespace(char value)
    {
        return char.IsWhiteSpace(value);
    }

    internal static bool IsLineBreak(char value)
    {
        return value == '\r' || value == '\n';
    }

    internal bool IsEscapedStringStart(char value)
    {
        if (!Options.AllowEscapedStrings)
        {
            return false;
        }

        return (value == 'e' || value == 'E') && Peek(1) == '\'';
    }

    internal bool IsUnicodeStringStart(char value)
    {
        if (!Options.AllowUnicodeStrings)
        {
            return false;
        }

        return (value == 'u' || value == 'U') && Peek(1) == '&' && Peek(2) == '\'';
    }

    internal int GetDollarQuoteDelimiterLength()
    {
        if (!Options.AllowDollarQuotedStrings || Peek(0) != '$')
        {
            return 0;
        }

        if (Peek(1) == '$')
        {
            return 2;
        }

        if (!IsParameterNameStart(Peek(1)))
        {
            return 0;
        }

        int offset = 2;
        while (IsParameterNamePart(Peek(offset)))
        {
            offset = offset + 1;
        }

        if (Peek(offset) == '$')
        {
            return offset + 1;
        }

        return 0;
    }

    internal bool IsIdentifierStart(char value)
    {
        if (char.IsLetter(value))
        {
            return true;
        }

        if (value == '_')
        {
            return true;
        }

        if (value == '$' && Options.AllowDollarInBareIdentifiers)
        {
            return true;
        }

        return false;
    }

    internal bool IsBlobLiteralStart(char value)
    {
        if (!Options.AllowBlobLiterals)
        {
            return false;
        }

        if (value != 'x' && value != 'X')
        {
            return false;
        }

        return Peek(1) == '\'';
    }

    internal bool IsNumericLiteralStart(char value)
    {
        if (IsDigit(value))
        {
            return true;
        }

        return value == '.' && IsDigit(Peek(1));
    }

    internal bool IsParameterStart(char value)
    {
        if (value == '?' && Options.AllowQuestionParameters)
        {
            return true;
        }

        if (value == ':' && Options.AllowColonParameters && IsParameterNameStart(Peek(1)))
        {
            return true;
        }

        if (value == '@' && Options.AllowAtParameters && IsParameterNameStart(Peek(1)))
        {
            return true;
        }

        if (value == '$' && Options.AllowDollarParameters && IsParameterNameStart(Peek(1)))
        {
            return true;
        }

        if (value == '$' && Options.AllowDollarNumberParameters && IsDigit(Peek(1)))
        {
            return true;
        }

        return false;
    }

    internal bool IsHexIntegerLiteralStart(char value)
    {
        if (!Options.AllowHexIntegerLiterals)
        {
            return false;
        }

        if (value != '0')
        {
            return false;
        }

        return Peek(1) == 'x' || Peek(1) == 'X';
    }

    internal bool IsExponentStart()
    {
        if (Peek(0) != 'e' && Peek(0) != 'E')
        {
            return false;
        }

        if (IsDigit(Peek(1)))
        {
            return true;
        }

        if ((Peek(1) == '+' || Peek(1) == '-') && IsDigit(Peek(2)))
        {
            return true;
        }

        return false;
    }

    internal bool IsIdentifierPart(char value)
    {
        if (char.IsLetterOrDigit(value))
        {
            return true;
        }

        if (value == '_')
        {
            return true;
        }

        if (value == '$' && Options.AllowDollarInBareIdentifiers)
        {
            return true;
        }

        return false;
    }

    internal bool IsValidBlobContent(int start, int end)
    {
        int length = end - start;
        int index = start;

        if ((length % 2) != 0)
        {
            return false;
        }

        while (index < end)
        {
            if (!IsHexDigit(SourceText.Text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsParameterNameStart(char value)
    {
        if (char.IsLetter(value))
        {
            return true;
        }

        return value == '_';
    }

    internal static bool IsParameterNamePart(char value)
    {
        if (char.IsLetterOrDigit(value))
        {
            return true;
        }

        return value == '_';
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return true;
        }

        if (value >= 'a' && value <= 'f')
        {
            return true;
        }

        if (value >= 'A' && value <= 'F')
        {
            return true;
        }

        return false;
    }
}
