using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class XPathLexer
{
    internal string Source;
    internal string? SourceUri;
    internal int Offset;
    internal int Line;
    internal int Column;
    internal bool PreviousWasCarriageReturn;
    internal XPathToken? PreviousToken;
    internal string ErrorCode;

    internal XPathLexer(string source, string? sourceUri)
    {
        Guard.NotNull(source, "source");
        Source = source;
        SourceUri = sourceUri;
        Offset = 0;
        Line = 1;
        Column = 1;
        PreviousWasCarriageReturn = false;
        PreviousToken = null;
        ErrorCode = XPathErrorCodes.XPST0003;
    }

    internal XPathToken[] TokenizeXPath()
    {
        List<XPathToken> tokens = new List<XPathToken>();
        while (true)
        {
            XPathToken token = ReadXPathToken();
            tokens.Add(token);
            if (token.Kind == XPathTokenKind.EndOfInput)
            {
                break;
            }
        }

        return tokens.ToArray();
    }

    internal XPathToken ReadXPathToken()
    {
        bool separator = SkipXPathSeparators();
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        if (Offset >= Source.Length)
        {
            return CreateXPathToken(XPathTokenKind.EndOfInput, null, null, null, null, '\0', separator, start, startLine, startColumn);
        }

        XPathToken token;
        int current = Source[Offset];
        if (current == '\'' || current == '"')
        {
            token = ScanXPathStringLiteral(separator, start, startLine, startColumn);
        }
        else if (current >= '0' && current <= '9' || current == '.' && IsFollowingXPathDigit(Offset))
        {
            token = ScanXPathNumericLiteral(separator, start, startLine, startColumn);
        }
        else if (current == 'Q' && IsFollowingXPathCharacter(Offset, '{'))
        {
            token = ScanXPathBracedName(separator, start, startLine, startColumn);
        }
        else
        {
            UnicodeScalar scalar = ReadXPathScalarAt(Offset);
            if (IsXPathNcNameStart(scalar.Value))
            {
                token = ScanXPathName(separator, start, startLine, startColumn);
            }
            else
            {
                token = ScanXPathSymbol(separator, start, startLine, startColumn);
            }
        }

        ValidateXPathTerminalSeparation(token);
        PreviousToken = token;
        return token;
    }

    internal bool SkipXPathSeparators()
    {
        bool skipped = false;
        while (Offset < Source.Length)
        {
            int current = Source[Offset];
            if (IsXPathWhitespace(current))
            {
                skipped = true;
                ConsumeXPathScalar(ReadXPathScalarAt(Offset));
                continue;
            }

            if (current == '(' && IsFollowingXPathCharacter(Offset, ':'))
            {
                skipped = true;
                SkipXPathComment();
                continue;
            }

            break;
        }

        return skipped;
    }

    internal void SkipXPathComment()
    {
        int start = Offset;
        int startLine = Line;
        int startColumn = Column;
        ConsumeXPathAsciiPair();
        int depth = 1;
        while (Offset < Source.Length)
        {
            if (Source[Offset] == '(' && IsFollowingXPathCharacter(Offset, ':'))
            {
                ConsumeXPathAsciiPair();
                depth = depth + 1;
            }
            else if (Source[Offset] == ':' && IsFollowingXPathCharacter(Offset, ')'))
            {
                ConsumeXPathAsciiPair();
                depth = depth - 1;
                if (depth == 0)
                {
                    return;
                }
            }
            else
            {
                ConsumeXPathScalar(ReadXPathScalarAt(Offset));
            }
        }

        throw CreateXPathLexicalError("An XPath comment is not terminated.", start, startLine, startColumn);
    }

    internal XPathToken ScanXPathStringLiteral(bool separator, int start, int startLine, int startColumn)
    {
        char quote = Source[Offset];
        ConsumeXPathScalar(ReadXPathScalarAt(Offset));
        StringBuilder value = new StringBuilder();
        while (Offset < Source.Length)
        {
            UnicodeScalar scalar = ReadXPathScalarAt(Offset);
            if (scalar.Value == quote)
            {
                ConsumeXPathScalar(scalar);
                if (Offset < Source.Length && Source[Offset] == quote)
                {
                    value.Append(quote);
                    ConsumeXPathScalar(ReadXPathScalarAt(Offset));
                    continue;
                }

                return CreateXPathToken(XPathTokenKind.StringLiteral, value.ToString(), null, null, null, quote, separator, start, startLine, startColumn);
            }

            value.Append(Source, Offset, scalar.Utf16CodeUnitCount);
            ConsumeXPathScalar(scalar);
        }

        throw CreateXPathLexicalError("An XPath string literal is not terminated.", start, startLine, startColumn);
    }

    internal XPathToken ScanXPathNumericLiteral(bool separator, int start, int startLine, int startColumn)
    {
        bool decimalPoint = false;
        bool exponent = false;
        if (Source[Offset] == '.')
        {
            decimalPoint = true;
            ConsumeXPathScalar(ReadXPathScalarAt(Offset));
            ConsumeXPathDigits();
        }
        else
        {
            ConsumeXPathDigits();
            if (Offset < Source.Length && Source[Offset] == '.')
            {
                decimalPoint = true;
                ConsumeXPathScalar(ReadXPathScalarAt(Offset));
                ConsumeXPathDigits();
            }
        }

        if (Offset < Source.Length && (Source[Offset] == 'e' || Source[Offset] == 'E'))
        {
            exponent = true;
            int exponentStart = Offset;
            int exponentLine = Line;
            int exponentColumn = Column;
            ConsumeXPathScalar(ReadXPathScalarAt(Offset));
            if (Offset < Source.Length && (Source[Offset] == '+' || Source[Offset] == '-'))
            {
                ConsumeXPathScalar(ReadXPathScalarAt(Offset));
            }

            if (Offset >= Source.Length || Source[Offset] < '0' || Source[Offset] > '9')
            {
                throw CreateXPathLexicalError("An XPath double literal exponent requires digits.", exponentStart, exponentLine, exponentColumn);
            }

            ConsumeXPathDigits();
        }

        XPathTokenKind kind = XPathTokenKind.IntegerLiteral;
        if (exponent)
        {
            kind = XPathTokenKind.DoubleLiteral;
        }
        else if (decimalPoint)
        {
            kind = XPathTokenKind.DecimalLiteral;
        }

        string lexical = Source.Substring(start, Offset - start);
        return CreateXPathToken(kind, lexical, null, null, null, '\0', separator, start, startLine, startColumn);
    }

    internal void ConsumeXPathDigits()
    {
        while (Offset < Source.Length && Source[Offset] >= '0' && Source[Offset] <= '9')
        {
            ConsumeXPathScalar(ReadXPathScalarAt(Offset));
        }
    }

    internal XPathToken ScanXPathBracedName(bool separator, int start, int startLine, int startColumn)
    {
        ConsumeXPathAsciiPair();
        StringBuilder uri = new StringBuilder();
        bool closed = false;
        while (Offset < Source.Length)
        {
            UnicodeScalar scalar = ReadXPathScalarAt(Offset);
            if (scalar.Value == '}')
            {
                ConsumeXPathScalar(scalar);
                closed = true;
                break;
            }

            if (scalar.Value == '{')
            {
                throw CreateXPathLexicalError("An XPath braced URI literal cannot contain braces.", Offset, Line, Column);
            }

            uri.Append(Source, Offset, scalar.Utf16CodeUnitCount);
            ConsumeXPathScalar(scalar);
        }

        if (!closed)
        {
            throw CreateXPathLexicalError("An XPath braced URI literal is not terminated.", start, startLine, startColumn);
        }

        string normalizedUri = new XdmStringDatatype().CollapseXdmStringWhitespace(uri.ToString());
        if (Offset < Source.Length)
        {
            UnicodeScalar first = ReadXPathScalarAt(Offset);
            if (IsXPathNcNameStart(first.Value))
            {
                int localStart = Offset;
                ConsumeXPathNcNameCharacters();
                string localName = Source.Substring(localStart, Offset - localStart);
                return CreateXPathToken(XPathTokenKind.UriQualifiedName, Source.Substring(start, Offset - start), null, localName, normalizedUri, '\0', separator, start, startLine, startColumn);
            }
        }

        return CreateXPathToken(XPathTokenKind.BracedUriLiteral, normalizedUri, null, null, normalizedUri, '\0', separator, start, startLine, startColumn);
    }

    internal XPathToken ScanXPathName(bool separator, int start, int startLine, int startColumn)
    {
        ConsumeXPathNcNameCharacters();
        string first = Source.Substring(start, Offset - start);
        if (Offset < Source.Length && Source[Offset] == ':' && Offset + 1 < Source.Length)
        {
            UnicodeScalar localStartScalar = ReadXPathScalarAt(Offset + 1);
            if (IsXPathNcNameStart(localStartScalar.Value))
            {
                ConsumeXPathScalar(ReadXPathScalarAt(Offset));
                int localStart = Offset;
                ConsumeXPathNcNameCharacters();
                string localName = Source.Substring(localStart, Offset - localStart);
                return CreateXPathToken(XPathTokenKind.QName, Source.Substring(start, Offset - start), first, localName, null, '\0', separator, start, startLine, startColumn);
            }
        }

        XPathTokenKind kind = IsXPathKeyword(first) ? XPathTokenKind.Keyword : XPathTokenKind.NcName;
        return CreateXPathToken(kind, first, null, first, null, '\0', separator, start, startLine, startColumn);
    }

    internal void ConsumeXPathNcNameCharacters()
    {
        UnicodeScalar first = ReadXPathScalarAt(Offset);
        if (!IsXPathNcNameStart(first.Value))
        {
            throw CreateXPathLexicalError("An XPath NCName was expected.", Offset, Line, Column);
        }

        ConsumeXPathScalar(first);
        while (Offset < Source.Length)
        {
            UnicodeScalar scalar = ReadXPathScalarAt(Offset);
            if (!IsXPathNcNameCharacter(scalar.Value))
            {
                break;
            }

            ConsumeXPathScalar(scalar);
        }
    }

    internal XPathToken ScanXPathSymbol(bool separator, int start, int startLine, int startColumn)
    {
        XPathTokenKind kind = XPathTokenKind.EndOfInput;
        int length = 1;
        int current = Source[Offset];
        int next = Offset + 1 < Source.Length ? Source[Offset + 1] : -1;
        if (current == '!')
        {
            kind = next == '=' ? XPathTokenKind.NotEqual : XPathTokenKind.Bang;
            length = next == '=' ? 2 : 1;
        }
        else if (current == '#')
        {
            kind = XPathTokenKind.Hash;
        }
        else if (current == '$')
        {
            kind = XPathTokenKind.Dollar;
        }
        else if (current == '(')
        {
            kind = XPathTokenKind.LeftParenthesis;
        }
        else if (current == ')')
        {
            kind = XPathTokenKind.RightParenthesis;
        }
        else if (current == '*')
        {
            kind = next == ':' ? XPathTokenKind.StarColon : XPathTokenKind.Star;
            length = next == ':' ? 2 : 1;
        }
        else if (current == '+')
        {
            kind = XPathTokenKind.Plus;
        }
        else if (current == ',')
        {
            kind = XPathTokenKind.Comma;
        }
        else if (current == '-')
        {
            kind = XPathTokenKind.Minus;
        }
        else if (current == '.')
        {
            kind = next == '.' ? XPathTokenKind.DotDot : XPathTokenKind.Dot;
            length = next == '.' ? 2 : 1;
        }
        else if (current == '/')
        {
            kind = next == '/' ? XPathTokenKind.SlashSlash : XPathTokenKind.Slash;
            length = next == '/' ? 2 : 1;
        }
        else if (current == ':')
        {
            if (next == '*')
            {
                kind = XPathTokenKind.ColonStar;
                length = 2;
            }
            else if (next == ':')
            {
                kind = XPathTokenKind.ColonColon;
                length = 2;
            }
            else if (next == '=')
            {
                kind = XPathTokenKind.ColonEquals;
                length = 2;
            }
            else
            {
                kind = XPathTokenKind.Colon;
            }
        }
        else if (current == '<')
        {
            if (next == '<')
            {
                kind = XPathTokenKind.NodeBefore;
                length = 2;
            }
            else if (next == '=')
            {
                kind = XPathTokenKind.LessThanOrEqual;
                length = 2;
            }
            else
            {
                kind = XPathTokenKind.LessThan;
            }
        }
        else if (current == '=')
        {
            kind = next == '>' ? XPathTokenKind.Arrow : XPathTokenKind.Equals;
            length = next == '>' ? 2 : 1;
        }
        else if (current == '>')
        {
            if (next == '>')
            {
                kind = XPathTokenKind.NodeAfter;
                length = 2;
            }
            else if (next == '=')
            {
                kind = XPathTokenKind.GreaterThanOrEqual;
                length = 2;
            }
            else
            {
                kind = XPathTokenKind.GreaterThan;
            }
        }
        else if (current == '?')
        {
            kind = XPathTokenKind.Question;
        }
        else if (current == '@')
        {
            kind = XPathTokenKind.At;
        }
        else if (current == '[')
        {
            kind = XPathTokenKind.LeftBracket;
        }
        else if (current == ']')
        {
            kind = XPathTokenKind.RightBracket;
        }
        else if (current == '{')
        {
            kind = XPathTokenKind.LeftBrace;
        }
        else if (current == '|')
        {
            kind = next == '|' ? XPathTokenKind.PipePipe : XPathTokenKind.Pipe;
            length = next == '|' ? 2 : 1;
        }
        else if (current == '}')
        {
            kind = XPathTokenKind.RightBrace;
        }
        else
        {
            throw CreateXPathLexicalError("The character is not valid in an XPath expression.", start, startLine, startColumn);
        }

        int index = 0;
        while (index < length)
        {
            ConsumeXPathScalar(ReadXPathScalarAt(Offset));
            index = index + 1;
        }

        string value = Source.Substring(start, Offset - start);
        return CreateXPathToken(kind, value, null, null, null, '\0', separator, start, startLine, startColumn);
    }

    internal XPathToken CreateXPathToken(XPathTokenKind kind, string? value, string? prefix, string? localName, string? namespaceUri, char quote, bool separator, int start, int startLine, int startColumn)
    {
        string lexeme = Source.Substring(start, Offset - start);
        SourceLocation startLocation = CreateXPathSourceLocation(start, startLine, startColumn);
        SourceLocation endLocation = CreateXPathSourceLocation(Offset, Line, Column);
        SourceRange range = new SourceRange();
        range.Start = startLocation;
        range.End = endLocation;
        return new XPathToken(kind, lexeme, value, prefix, localName, namespaceUri, quote, separator, start, Offset - start, range);
    }

    internal SourceLocation CreateXPathSourceLocation(int offset, int line, int column)
    {
        SourceLocation location = new SourceLocation();
        location.SourceUri = SourceUri;
        location.CharacterOffset = offset;
        location.ByteOffset = -1;
        location.Line = line;
        location.Column = column;
        return location;
    }

    internal void ValidateXPathTerminalSeparation(XPathToken token)
    {
        XPathToken? previous = PreviousToken;
        if (previous == null || token.HasLeadingSeparator)
        {
            return;
        }

        if (IsXPathNonDelimiting(previous.Kind) && IsXPathNonDelimiting(token.Kind))
        {
            SourceLocation start = token.Range.Start == null ? CreateXPathSourceLocation(token.StartUtf16Offset, 1, token.StartUtf16Offset + 1) : token.Range.Start;
            throw CreateXPathLexicalError("A separator is required between XPath terminals.", token.StartUtf16Offset, start.Line, start.Column);
        }

        bool previousName = previous.Kind == XPathTokenKind.NcName || previous.Kind == XPathTokenKind.QName || previous.Kind == XPathTokenKind.Keyword;
        if (previousName && (token.Kind == XPathTokenKind.Dot || token.Kind == XPathTokenKind.Minus))
        {
            SourceLocation start = token.Range.Start == null ? CreateXPathSourceLocation(token.StartUtf16Offset, 1, token.StartUtf16Offset + 1) : token.Range.Start;
            throw CreateXPathLexicalError("A separator is required after an XPath name.", token.StartUtf16Offset, start.Line, start.Column);
        }

        bool previousNumeric = IsXPathNumeric(previous.Kind);
        bool currentNumeric = IsXPathNumeric(token.Kind);
        if (previousNumeric && token.Kind == XPathTokenKind.Dot || previous.Kind == XPathTokenKind.Dot && currentNumeric)
        {
            SourceLocation start = token.Range.Start == null ? CreateXPathSourceLocation(token.StartUtf16Offset, 1, token.StartUtf16Offset + 1) : token.Range.Start;
            throw CreateXPathLexicalError("A separator is required between an XPath numeric literal and '.'.", token.StartUtf16Offset, start.Line, start.Column);
        }
    }

    internal bool IsXPathNonDelimiting(XPathTokenKind kind)
    {
        return kind == XPathTokenKind.NcName || kind == XPathTokenKind.QName || kind == XPathTokenKind.UriQualifiedName || kind == XPathTokenKind.Keyword || IsXPathNumeric(kind);
    }

    internal bool IsXPathNumeric(XPathTokenKind kind)
    {
        return kind == XPathTokenKind.IntegerLiteral || kind == XPathTokenKind.DecimalLiteral || kind == XPathTokenKind.DoubleLiteral;
    }

    internal bool IsXPathKeyword(string value)
    {
        switch (value)
        {
            case "ancestor":
            case "ancestor-or-self":
            case "and":
            case "array":
            case "as":
            case "attribute":
            case "cast":
            case "castable":
            case "child":
            case "comment":
            case "descendant":
            case "descendant-or-self":
            case "div":
            case "document-node":
            case "element":
            case "else":
            case "empty-sequence":
            case "eq":
            case "every":
            case "except":
            case "following":
            case "following-sibling":
            case "for":
            case "function":
            case "ge":
            case "gt":
            case "idiv":
            case "if":
            case "in":
            case "instance":
            case "intersect":
            case "is":
            case "item":
            case "le":
            case "let":
            case "lt":
            case "map":
            case "mod":
            case "namespace":
            case "namespace-node":
            case "ne":
            case "node":
            case "of":
            case "or":
            case "parent":
            case "preceding":
            case "preceding-sibling":
            case "processing-instruction":
            case "return":
            case "satisfies":
            case "schema-attribute":
            case "schema-element":
            case "self":
            case "some":
            case "text":
            case "then":
            case "to":
            case "treat":
            case "union":
                return true;
            default:
                return false;
        }
    }

    internal bool IsXPathWhitespace(int scalar)
    {
        return scalar == 0x20 || scalar == 0x9 || scalar == 0xA || scalar == 0xD;
    }

    internal bool IsXPathNcNameStart(int scalar)
    {
        return scalar != ':' && Xml10CharacterRules.IsXml10NameStartCharacter(scalar);
    }

    internal bool IsXPathNcNameCharacter(int scalar)
    {
        return scalar != ':' && Xml10CharacterRules.IsXml10NameCharacter(scalar);
    }

    internal bool IsFollowingXPathDigit(int position)
    {
        return position + 1 < Source.Length && Source[position + 1] >= '0' && Source[position + 1] <= '9';
    }

    internal bool IsFollowingXPathCharacter(int position, char value)
    {
        return position + 1 < Source.Length && Source[position + 1] == value;
    }

    internal UnicodeScalar ReadXPathScalarAt(int position)
    {
        int first = Source[position];
        int errorColumn = Column + position - Offset;
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            if (position + 1 >= Source.Length)
            {
                throw CreateXPathLexicalError("The XPath expression contains an unpaired high surrogate.", position, Line, errorColumn);
            }

            int second = Source[position + 1];
            if (second < 0xDC00 || second > 0xDFFF)
            {
                throw CreateXPathLexicalError("The XPath expression contains an unpaired high surrogate.", position, Line, errorColumn);
            }

            int value = 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
            if (!Xml10CharacterRules.IsLegalXml10Character(value))
            {
                throw CreateXPathLexicalError("The XPath expression contains an illegal XML character.", position, Line, errorColumn);
            }

            return new UnicodeScalar(value, position, 2, false);
        }

        if (first >= 0xDC00 && first <= 0xDFFF)
        {
            throw CreateXPathLexicalError("The XPath expression contains an unpaired low surrogate.", position, Line, errorColumn);
        }

        if (!Xml10CharacterRules.IsLegalXml10Character(first))
        {
            throw CreateXPathLexicalError("The XPath expression contains an illegal XML character.", position, Line, errorColumn);
        }

        return new UnicodeScalar(first, position, 1, false);
    }

    internal void ConsumeXPathScalar(UnicodeScalar scalar)
    {
        Offset = Offset + scalar.Utf16CodeUnitCount;
        if (scalar.Value == 0xD)
        {
            Line = Line + 1;
            Column = 1;
            PreviousWasCarriageReturn = true;
        }
        else if (scalar.Value == 0xA)
        {
            if (!PreviousWasCarriageReturn)
            {
                Line = Line + 1;
                Column = 1;
            }

            PreviousWasCarriageReturn = false;
        }
        else
        {
            Column = Column + 1;
            PreviousWasCarriageReturn = false;
        }
    }

    internal void ConsumeXPathAsciiPair()
    {
        ConsumeXPathScalar(ReadXPathScalarAt(Offset));
        ConsumeXPathScalar(ReadXPathScalarAt(Offset));
    }

    internal XPathStaticException CreateXPathLexicalError(string message, int offset, int line, int column)
    {
        SourceLocation location = CreateXPathSourceLocation(offset, line, column);
        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = ErrorCode;
        diagnostic.Category = XmlErrorCategory.XPathStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = range;
        return new XPathStaticException(message, diagnostic, location);
    }
}
