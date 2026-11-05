using System.Collections.Generic;

namespace cbor;

internal class CddlLexer
{
    internal string Text;
    internal CddlParseOptions Options;
    internal int Offset;
    internal int Line = 1;
    internal int Column = 1;
    internal int TokenCount;
    internal List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();

    internal CddlLexer(string text, CddlParseOptions options)
    {
        Text = text;
        Options = options;
    }

    internal CddlToken ReadToken()
    {
        List<CddlTrivia> trivia = new List<CddlTrivia>();
        CddlToken? triviaFailure = ReadTrivia(trivia);
        if (triviaFailure != null)
        {
            return triviaFailure;
        }
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        if (Offset >= Text.Length)
        {
            return CreateToken(
                CddlTokenKind.EndOfFile,
                start,
                startLine,
                startColumn,
                trivia
            );
        }
        TokenCount = TokenCount + 1;
        char current = Text[Offset];
        if (IsIdentifierStart(current))
        {
            if (IsByteStringPrefix())
            {
                return ReadQuotedToken(trivia, true);
            }
            return ReadIdentifierToken(trivia);
        }
        if (current >= '0' && current <= '9')
        {
            if (Offset > 0 && Text[Offset - 1] == '#')
            {
                return ReadMajorTypeToken(trivia);
            }
            return ReadNumberToken(trivia);
        }
        if (current == '-' && Offset + 1 < Text.Length
            && Text[Offset + 1] >= '0' && Text[Offset + 1] <= '9')
        {
            return ReadNumberToken(trivia);
        }
        if (current == '"')
        {
            return ReadQuotedToken(trivia, false);
        }
        if (current == '\'')
        {
            return ReadQuotedToken(trivia, true);
        }
        if (StartsWithText("//="))
        {
            AdvanceAsciiCharacters(3);
            return CreateToken(
                CddlTokenKind.GroupExtensionAssign,
                start,
                startLine,
                startColumn,
                trivia
            );
        }
        if (StartsWithText("..."))
        {
            AdvanceAsciiCharacters(3);
            return CreateToken(
                CddlTokenKind.RangeExclusive,
                start,
                startLine,
                startColumn,
                trivia
            );
        }
        if (StartsWithText("/="))
        {
            AdvanceAsciiCharacters(2);
            return CreateToken(
                CddlTokenKind.TypeExtensionAssign,
                start,
                startLine,
                startColumn,
                trivia
            );
        }
        if (StartsWithText("//"))
        {
            AdvanceAsciiCharacters(2);
            return CreateToken(
                CddlTokenKind.GroupChoice,
                start,
                startLine,
                startColumn,
                trivia
            );
        }
        if (StartsWithText(".."))
        {
            AdvanceAsciiCharacters(2);
            return CreateToken(
                CddlTokenKind.RangeInclusive,
                start,
                startLine,
                startColumn,
                trivia
            );
        }
        if (StartsWithText("=>"))
        {
            AdvanceAsciiCharacters(2);
            return CreateToken(
                CddlTokenKind.Arrow,
                start,
                startLine,
                startColumn,
                trivia
            );
        }
        CddlTokenKind simpleKind = SimpleTokenKind(current);
        if (simpleKind != CddlTokenKind.None)
        {
            AdvanceAsciiCharacters(1);
            return CreateToken(
                simpleKind,
                start,
                startLine,
                startColumn,
                trivia
            );
        }
        int invalidLength = InvalidCharacterLength();
        AdvanceRawCharacters(invalidLength);
        string code = "CDDL_LEX_CHARACTER_INVALID";
        string message = "The source character cannot begin a CDDL token.";
        if (invalidLength == 1 && char.IsSurrogate(current))
        {
            code = "CDDL_LEX_SURROGATE_INVALID";
            message = "An unpaired surrogate is not valid CDDL source text.";
        }
        return CreateInvalidToken(
            code,
            message,
            start,
            startLine,
            startColumn,
            trivia
        );
    }

    internal CddlToken? ReadTrivia(List<CddlTrivia> trivia)
    {
        bool scanning = true;
        while (scanning && Offset < Text.Length)
        {
            char current = Text[Offset];
            if (current == ' ' || current == '\n'
                || (current == '\r' && Offset + 1 < Text.Length
                    && Text[Offset + 1] == '\n'))
            {
                ReadWhitespaceTrivia(trivia);
            }
            else if (current == ';')
            {
                CddlToken? commentFailure = ReadCommentTrivia(trivia);
                if (commentFailure != null)
                {
                    return commentFailure;
                }
            }
            else if (current == '\t' || current == '\r'
                || (current < 0x20 && current != '\n'))
            {
                int start = Offset;
                int startLine = Line;
                int startColumn = Column;
                AdvanceRawCharacters(1);
                return CreateInvalidToken(
                    "CDDL_LEX_WHITESPACE_INVALID",
                    "CDDL whitespace permits spaces, line feeds, and CRLF sequences only.",
                    start,
                    startLine,
                    startColumn,
                    trivia
                );
            }
            else
            {
                scanning = false;
            }
        }
        return null;
    }

    internal void ReadWhitespaceTrivia(List<CddlTrivia> trivia)
    {
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        bool scanning = true;
        while (scanning && Offset < Text.Length)
        {
            char current = Text[Offset];
            if (current == ' ')
            {
                AdvanceAsciiCharacters(1);
            }
            else if (current == '\n')
            {
                ConsumeNewLine(false);
            }
            else if (current == '\r' && Offset + 1 < Text.Length
                && Text[Offset + 1] == '\n')
            {
                ConsumeNewLine(true);
            }
            else
            {
                scanning = false;
            }
        }
        if (Options.RetainTrivia)
        {
            trivia.Add(CreateTrivia(
                CddlTriviaKind.Whitespace,
                start,
                startLine,
                startColumn
            ));
        }
    }

    internal CddlToken? ReadCommentTrivia(List<CddlTrivia> trivia)
    {
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        AdvanceAsciiCharacters(1);
        while (Offset < Text.Length)
        {
            char current = Text[Offset];
            if (current == '\n')
            {
                ConsumeNewLine(false);
                if (Options.RetainTrivia)
                {
                    trivia.Add(CreateTrivia(
                        CddlTriviaKind.Comment,
                        start,
                        startLine,
                        startColumn
                    ));
                }
                return null;
            }
            if (current == '\r')
            {
                if (Offset + 1 < Text.Length && Text[Offset + 1] == '\n')
                {
                    ConsumeNewLine(true);
                    if (Options.RetainTrivia)
                    {
                        trivia.Add(CreateTrivia(
                            CddlTriviaKind.Comment,
                            start,
                            startLine,
                            startColumn
                        ));
                    }
                    return null;
                }
                AdvanceRawCharacters(1);
                return CreateInvalidToken(
                    "CDDL_LEX_COMMENT_CHARACTER_INVALID",
                    "A carriage return in a CDDL comment must be followed by a line feed.",
                    start,
                    startLine,
                    startColumn,
                    trivia
                );
            }
            int scalarLength = CommentScalarLength();
            if (scalarLength == 0)
            {
                AdvanceRawCharacters(InvalidCharacterLength());
                return CreateInvalidToken(
                    "CDDL_LEX_COMMENT_CHARACTER_INVALID",
                    "The CDDL comment contains a character outside PCHAR.",
                    start,
                    startLine,
                    startColumn,
                    trivia
                );
            }
            AdvanceRawCharacters(scalarLength);
        }
        return CreateInvalidToken(
            "CDDL_LEX_COMMENT_UNTERMINATED",
            "A CDDL comment must end with LF or CRLF.",
            start,
            startLine,
            startColumn,
            trivia
        );
    }

    internal CddlToken ReadIdentifierToken(List<CddlTrivia> trivia)
    {
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        AdvanceAsciiCharacters(1);
        bool scanning = true;
        while (scanning && Offset < Text.Length)
        {
            char current = Text[Offset];
            if (IsIdentifierStart(current) || IsAsciiDigit(current))
            {
                AdvanceAsciiCharacters(1);
            }
            else if (current == '-' || current == '.')
            {
                int punctuationEnd = Offset;
                while (punctuationEnd < Text.Length
                    && (Text[punctuationEnd] == '-' || Text[punctuationEnd] == '.'))
                {
                    punctuationEnd = punctuationEnd + 1;
                }
                if (punctuationEnd < Text.Length
                    && (IsIdentifierStart(Text[punctuationEnd])
                        || IsAsciiDigit(Text[punctuationEnd])))
                {
                    AdvanceAsciiCharacters(punctuationEnd - Offset + 1);
                }
                else
                {
                    scanning = false;
                }
            }
            else
            {
                scanning = false;
            }
        }
        return CreateToken(
            CddlTokenKind.Identifier,
            start,
            startLine,
            startColumn,
            trivia
        );
    }

    internal CddlToken ReadNumberToken(List<CddlTrivia> trivia)
    {
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        if (Text[Offset] == '-')
        {
            AdvanceAsciiCharacters(1);
        }
        char previous = '\0';
        bool scanning = true;
        while (scanning && Offset < Text.Length)
        {
            char current = Text[Offset];
            if (IsAsciiDigit(current)
                || (current >= 'A' && current <= 'Z')
                || (current >= 'a' && current <= 'z'))
            {
                previous = current;
                AdvanceAsciiCharacters(1);
            }
            else if (current == '.'
                && !(Offset + 1 < Text.Length && Text[Offset + 1] == '.'))
            {
                previous = current;
                AdvanceAsciiCharacters(1);
            }
            else if ((current == '+' || current == '-')
                && (previous == 'e' || previous == 'E'
                    || previous == 'p' || previous == 'P'))
            {
                previous = current;
                AdvanceAsciiCharacters(1);
            }
            else
            {
                scanning = false;
            }
        }
        CddlToken token = CreateToken(
            CddlTokenKind.Number,
            start,
            startLine,
            startColumn,
            trivia
        );
        return DecodeNumberToken(token);
    }

    internal CddlToken ReadMajorTypeToken(List<CddlTrivia> trivia)
    {
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        AdvanceAsciiCharacters(1);
        CddlToken token = CreateToken(
            CddlTokenKind.Number,
            start,
            startLine,
            startColumn,
            trivia
        );
        return DecodeNumberToken(token);
    }

    internal CddlToken ReadQuotedToken(List<CddlTrivia> trivia, bool byteString)
    {
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        char delimiter = byteString ? '\'' : '"';
        if (byteString && Text[Offset] != '\'')
        {
            if (Text[Offset] == 'h' || Text[Offset] == 'H')
            {
                AdvanceAsciiCharacters(1);
            }
            else
            {
                AdvanceAsciiCharacters(3);
            }
        }
        AdvanceAsciiCharacters(1);
        bool escaped = false;
        while (Offset < Text.Length)
        {
            char current = Text[Offset];
            if (!escaped && current == delimiter)
            {
                AdvanceAsciiCharacters(1);
                CddlToken token = CreateToken(
                    byteString ? CddlTokenKind.ByteString : CddlTokenKind.TextString,
                    start,
                    startLine,
                    startColumn,
                    trivia
                );
                return DecodeStringToken(token);
            }
            if (!escaped && current == '\\')
            {
                escaped = true;
                AdvanceAsciiCharacters(1);
            }
            else
            {
                escaped = false;
                ConsumeSourceCharacter();
            }
        }
        return CreateInvalidToken(
            "CDDL_LEX_LITERAL_UNTERMINATED",
            "The CDDL string literal has no closing delimiter.",
            start,
            startLine,
            startColumn,
            trivia
        );
    }

    internal CddlToken DecodeStringToken(CddlToken token)
    {
        CddlStringParser parser = new CddlStringParser(token);
        CddlStringResult result = parser.ParseLiteral();
        if (result.IsValid)
        {
            token.LiteralKind = result.LiteralKind;
            token.StringValue = result.StringValue;
            token.ByteStringValue = result.ByteStringValue;
            return token;
        }
        token.Kind = CddlTokenKind.Invalid;
        token.ErrorCode = result.ErrorCode;
        token.ErrorMessage = result.ErrorMessage;
        if (Diagnostics.Count < Options.MaxDiagnostics)
        {
            CddlDiagnostic diagnostic = new CddlDiagnostic();
            diagnostic.Severity = CddlDiagnosticSeverity.Error;
            diagnostic.Code = result.ErrorCode ?? "CDDL_LITERAL_INVALID";
            diagnostic.Message = result.ErrorMessage ?? "The CDDL literal is invalid.";
            diagnostic.Span = result.ErrorSpan;
            Diagnostics.Add(diagnostic);
        }
        return token;
    }

    internal CddlToken DecodeNumberToken(CddlToken token)
    {
        CddlNumberParser parser = new CddlNumberParser(token);
        CddlNumberResult result = parser.ParseNumber();
        if (result.IsValid)
        {
            token.LiteralKind = result.LiteralKind;
            token.IntegerValue = result.IntegerValue;
            token.HasIntegerValue = result.HasIntegerValue;
            token.FloatValue = result.FloatValue;
            token.HasFloatValue = result.HasFloatValue;
            return token;
        }
        token.Kind = CddlTokenKind.Invalid;
        token.ErrorCode = result.ErrorCode;
        token.ErrorMessage = result.ErrorMessage;
        if (Diagnostics.Count < Options.MaxDiagnostics)
        {
            CddlDiagnostic diagnostic = new CddlDiagnostic();
            diagnostic.Severity = CddlDiagnosticSeverity.Error;
            diagnostic.Code = result.ErrorCode ?? "CDDL_NUMBER_INVALID";
            diagnostic.Message = result.ErrorMessage ?? "The CDDL number is invalid.";
            diagnostic.Span = result.ErrorSpan;
            Diagnostics.Add(diagnostic);
        }
        return token;
    }

    internal bool IsByteStringPrefix()
    {
        if (Offset + 1 < Text.Length
            && (Text[Offset] == 'h' || Text[Offset] == 'H')
            && Text[Offset + 1] == '\'')
        {
            return true;
        }
        return Offset + 3 < Text.Length
            && (Text[Offset] == 'b' || Text[Offset] == 'B')
            && Text[Offset + 1] == '6'
            && Text[Offset + 2] == '4'
            && Text[Offset + 3] == '\'';
    }

    internal bool StartsWithText(string expected)
    {
        if (Offset > Text.Length - expected.Length)
        {
            return false;
        }
        int index = 0;
        while (index < expected.Length)
        {
            if (Text[Offset + index] != expected[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal CddlTokenKind SimpleTokenKind(char value)
    {
        switch (value)
        {
            case '=':
                return CddlTokenKind.Equal;
            case '/':
                return CddlTokenKind.Slash;
            case '.':
                return CddlTokenKind.Dot;
            case '(':
                return CddlTokenKind.LeftParenthesis;
            case ')':
                return CddlTokenKind.RightParenthesis;
            case '{':
                return CddlTokenKind.LeftBrace;
            case '}':
                return CddlTokenKind.RightBrace;
            case '[':
                return CddlTokenKind.LeftBracket;
            case ']':
                return CddlTokenKind.RightBracket;
            case '<':
                return CddlTokenKind.LessThan;
            case '>':
                return CddlTokenKind.GreaterThan;
            case ',':
                return CddlTokenKind.Comma;
            case ':':
                return CddlTokenKind.Colon;
            case '^':
                return CddlTokenKind.Cut;
            case '?':
                return CddlTokenKind.Optional;
            case '*':
                return CddlTokenKind.Asterisk;
            case '+':
                return CddlTokenKind.Plus;
            case '~':
                return CddlTokenKind.Unwrap;
            case '&':
                return CddlTokenKind.GroupEnumeration;
            case '#':
                return CddlTokenKind.Hash;
            default:
                return CddlTokenKind.None;
        }
    }

    internal bool IsIdentifierStart(char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z')
            || value == '@' || value == '_' || value == '$';
    }

    internal bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal int CommentScalarLength()
    {
        char current = Text[Offset];
        if (current >= 0x20 && current <= 0x7e)
        {
            return 1;
        }
        if (current >= 0x00a0 && current <= 0xd7ff)
        {
            return 1;
        }
        if (current >= 0xe000)
        {
            return 1;
        }
        if (char.IsHighSurrogate(current)
            && Offset + 1 < Text.Length
            && char.IsLowSurrogate(Text[Offset + 1])
            && char.ConvertToUtf32(current, Text[Offset + 1]) <= 0x10fffd)
        {
            return 2;
        }
        return 0;
    }

    internal int InvalidCharacterLength()
    {
        if (Offset < Text.Length
            && char.IsHighSurrogate(Text[Offset])
            && Offset + 1 < Text.Length
            && char.IsLowSurrogate(Text[Offset + 1]))
        {
            return 2;
        }
        return 1;
    }

    internal void ConsumeSourceCharacter()
    {
        if (Text[Offset] == '\r'
            && Offset + 1 < Text.Length
            && Text[Offset + 1] == '\n')
        {
            ConsumeNewLine(true);
            return;
        }
        if (Text[Offset] == '\n')
        {
            ConsumeNewLine(false);
            return;
        }
        int length = InvalidCharacterLength();
        AdvanceRawCharacters(length);
    }

    internal void ConsumeNewLine(bool hasCarriageReturn)
    {
        Offset = Offset + (hasCarriageReturn ? 2 : 1);
        Line = Line + 1;
        Column = 1;
    }

    internal void AdvanceAsciiCharacters(int count)
    {
        Offset = Offset + count;
        Column = Column + count;
    }

    internal void AdvanceRawCharacters(int count)
    {
        Offset = Offset + count;
        Column = Column + count;
    }

    internal CddlTrivia CreateTrivia(
        CddlTriviaKind kind,
        int start,
        int startLine,
        int startColumn)
    {
        CddlTrivia trivia = new CddlTrivia();
        trivia.Kind = kind;
        trivia.Text = Text.Substring(start, Offset - start);
        trivia.Span = CreateSpan(start, startLine, startColumn);
        return trivia;
    }

    internal CddlToken CreateToken(
        CddlTokenKind kind,
        int start,
        int startLine,
        int startColumn,
        List<CddlTrivia> trivia)
    {
        CddlToken token = new CddlToken();
        token.Kind = kind;
        token.Text = Text.Substring(start, Offset - start);
        token.Span = CreateSpan(start, startLine, startColumn);
        token.LeadingTrivia = trivia;
        return token;
    }

    internal CddlToken CreateInvalidToken(
        string code,
        string message,
        int start,
        int startLine,
        int startColumn,
        List<CddlTrivia> trivia)
    {
        CddlToken token = CreateToken(
            CddlTokenKind.Invalid,
            start,
            startLine,
            startColumn,
            trivia
        );
        token.ErrorCode = code;
        token.ErrorMessage = message;
        if (Diagnostics.Count < Options.MaxDiagnostics)
        {
            CddlDiagnostic diagnostic = new CddlDiagnostic();
            diagnostic.Severity = CddlDiagnosticSeverity.Error;
            diagnostic.Code = code;
            diagnostic.Message = message;
            diagnostic.Span = token.Span;
            Diagnostics.Add(diagnostic);
        }
        return token;
    }

    internal CddlSourceSpan CreateSpan(int start, int startLine, int startColumn)
    {
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = Options.SourceName;
        span.Start = start;
        span.Length = Offset - start;
        span.StartLine = startLine;
        span.StartColumn = startColumn;
        span.EndLine = Line;
        span.EndColumn = Column;
        return span;
    }
}
