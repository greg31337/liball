using System;

namespace xml;

internal class XmlScanner
{
    internal ReadOnlyMemory<char> Characters;
    internal XmlVersion InitialVersion;
    internal XmlVersion Version;
    internal XmlEncodingEntityKind EntityKind;
    internal XmlScannerState State;
    internal int Offset;
    internal int DeclarationBracketDepth;
    internal bool MapWellFormednessErrors;
    internal SourceTracker? ErrorSourceTracker;
    internal IXmlErrorReporter? ErrorReporter;
    internal ParserWorkLimiter? WorkLimiter;

    internal XmlScanner(ReadOnlyMemory<char> characters, XmlVersion version, XmlEncodingEntityKind entityKind)
    {
        if (version != XmlVersion.Xml10 && version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        if (entityKind != XmlEncodingEntityKind.Document && entityKind != XmlEncodingEntityKind.ExternalParsedEntity)
        {
            throw new ArgumentOutOfRangeException("entityKind");
        }

        Characters = characters;
        InitialVersion = version;
        Version = version;
        EntityKind = entityKind;
        State = XmlScannerState.Content;
        Offset = 0;
        DeclarationBracketDepth = 0;
        MapWellFormednessErrors = false;
        ErrorSourceTracker = null;
        ErrorReporter = null;
        WorkLimiter = null;
    }

    internal XmlToken ScanNextXmlToken()
    {
        XmlToken token;
        if (State == XmlScannerState.EndOfInput)
        {
            token = CreateSimpleXmlToken(XmlTokenKind.EndOfInput, Offset, 0);
        }
        else if (Offset >= Characters.Length)
        {
            if (State != XmlScannerState.Content)
            {
                throw CreateXmlScannerError("The input ended inside XML markup", Offset);
            }

            State = XmlScannerState.EndOfInput;
            token = CreateSimpleXmlToken(XmlTokenKind.EndOfInput, Offset, 0);
        }
        else if (State == XmlScannerState.Content)
        {
            token = ScanContentXmlToken();
        }
        else
        {
            token = ScanMarkupXmlToken();
        }

        ParserWorkLimiter? limiter = WorkLimiter;
        if (limiter != null)
        {
            limiter.ObserveXmlToken(token);
        }

        return token;
    }

    internal int CurrentXmlScannerOffset()
    {
        return Offset;
    }

    internal XmlScannerState CurrentXmlScannerState()
    {
        return State;
    }

    internal XmlVersion CurrentXmlScannerVersion()
    {
        return Version;
    }

    internal void ConfigureXmlScannerErrors(SourceTracker? tracker, IXmlErrorReporter? reporter)
    {
        MapWellFormednessErrors = true;
        ErrorSourceTracker = tracker;
        ErrorReporter = reporter;
    }

    internal void ConfigureXmlScannerWork(ParserWorkLimiter workLimiter)
    {
        Guard.NotNull(workLimiter, "workLimiter");
        WorkLimiter = workLimiter;
    }

    internal void ResetXmlScanner()
    {
        Version = InitialVersion;
        State = XmlScannerState.Content;
        Offset = 0;
        DeclarationBracketDepth = 0;
    }

    internal XmlToken ScanContentXmlToken()
    {
        int start = Offset;
        if (Characters.Span[start] == '&')
        {
            return ScanXmlReferenceToken();
        }

        if (Characters.Span[start] != '<')
        {
            return ScanXmlTextToken();
        }

        if (MatchesXmlScannerDelimiter(start, "<!--"))
        {
            return ScanXmlCommentToken();
        }

        if (MatchesXmlScannerDelimiter(start, "<![CDATA["))
        {
            return ScanXmlCDataToken();
        }

        if (MatchesXmlScannerDelimiter(start, "</"))
        {
            Offset = start + 2;
            State = XmlScannerState.EndTag;
            return CreateSimpleXmlToken(XmlTokenKind.EndTagOpen, start, 2);
        }

        if (MatchesXmlScannerDelimiter(start, "<?"))
        {
            return ScanXmlProcessingInstructionToken();
        }

        if (MatchesXmlScannerDelimiter(start, "<!"))
        {
            Offset = start + 2;
            State = XmlScannerState.MarkupDeclaration;
            DeclarationBracketDepth = 0;
            return CreateSimpleXmlToken(XmlTokenKind.MarkupDeclarationOpen, start, 2);
        }

        Offset = start + 1;
        State = XmlScannerState.StartTag;
        return CreateSimpleXmlToken(XmlTokenKind.StartTagOpen, start, 1);
    }

    internal XmlToken ScanMarkupXmlToken()
    {
        int start = Offset;
        if (XmlLexicalCharacterRules.IsXmlSpaceCharacter(Characters.Span[start]))
        {
            XmlLexicalScanner whitespaceScanner = CreateXmlLexicalScannerAt(start);
            XmlLexicalToken whitespace = whitespaceScanner.ScanXmlSpace();
            Offset = whitespaceScanner.CurrentXmlLexicalOffset();
            return CreateValueXmlToken(XmlTokenKind.Whitespace, null, whitespace.Value, start, Offset - start, '\0', false);
        }

        if (State == XmlScannerState.StartTag && MatchesXmlScannerDelimiter(start, "/>"))
        {
            Offset = start + 2;
            State = XmlScannerState.Content;
            return CreateSimpleXmlToken(XmlTokenKind.EmptyElementClose, start, 2);
        }

        if (Characters.Span[start] == '>')
        {
            if (State == XmlScannerState.MarkupDeclaration && DeclarationBracketDepth > 0)
            {
                return ScanXmlMarkupSymbolToken();
            }

            Offset = start + 1;
            State = XmlScannerState.Content;
            return CreateSimpleXmlToken(XmlTokenKind.TagClose, start, 1);
        }

        if (Characters.Span[start] == '\'' || Characters.Span[start] == '"')
        {
            XmlLexicalScanner literalScanner = CreateXmlLexicalScannerAt(start);
            XmlQuotedLiteralToken literal = literalScanner.ScanXmlQuotedLiteral();
            Offset = literalScanner.CurrentXmlLexicalOffset();
            return CreateValueXmlToken(XmlTokenKind.QuotedValue, null, literal.Value, start, Offset - start, literal.Quote, false);
        }

        if (Characters.Span[start] == '=')
        {
            Offset = start + 1;
            return CreateSimpleXmlToken(XmlTokenKind.Equals, start, 1);
        }

        if (State == XmlScannerState.MarkupDeclaration)
        {
            if (MatchesXmlScannerDelimiter(start, "<!--"))
            {
                return ScanXmlCommentToken();
            }

            if (MatchesXmlScannerDelimiter(start, "<?"))
            {
                return ScanXmlProcessingInstructionToken();
            }

            if (Characters.Span[start] == '&')
            {
                return ScanXmlReferenceToken();
            }
        }

        UnicodeScalar first = ReadXmlScannerScalarAt(start);
        if (IsXmlScannerNameStart(first.Value))
        {
            XmlLexicalScanner nameScanner = CreateXmlLexicalScannerAt(start);
            XmlLexicalToken name = nameScanner.ScanXmlName();
            Offset = nameScanner.CurrentXmlLexicalOffset();
            return CreateValueXmlToken(XmlTokenKind.Name, name.Value, name.Value, start, Offset - start, '\0', false);
        }

        if (State == XmlScannerState.MarkupDeclaration)
        {
            return ScanXmlMarkupSymbolToken();
        }

        throw CreateXmlScannerError("An unexpected character appears inside an XML tag", start);
    }

    internal XmlToken ScanXmlTextToken()
    {
        int start = Offset;
        int position = start;
        while (position < Characters.Length && Characters.Span[position] != '<' && Characters.Span[position] != '&')
        {
            UnicodeScalar scalar = ReadXmlScannerScalarAt(position);
            ValidateDirectXmlScannerCharacter(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        string value = Characters.Span.Slice(start, position - start).ToString();
        XmlToken token = CreateValueXmlToken(XmlTokenKind.Text, null, value, start, position - start, '\0', false);
        int cdataClosing = value.IndexOf("]]>", StringComparison.Ordinal);
        if (cdataClosing >= 0)
        {
            throw CreateXmlScannerError("XML character data cannot contain ']]>'", start + cdataClosing);
        }

        XmlMarkupWellFormedness.ValidateXmlCharacterDataToken(token);
        Offset = position;
        return token;
    }

    internal XmlToken ScanXmlReferenceToken()
    {
        int start = Offset;
        if (start + 1 < Characters.Length && Characters.Span[start + 1] == '#')
        {
            XmlLexicalScanner referenceScanner = CreateXmlLexicalScannerAt(start);
            XmlCharacterReference reference = referenceScanner.ScanXmlCharacterReference();
            Offset = referenceScanner.CurrentXmlLexicalOffset();
            string lexeme = Characters.Span.Slice(start, Offset - start).ToString();
            return new XmlToken(XmlTokenKind.CharacterReference, null, reference.ReplacementText, lexeme, start, Offset - start, reference.ScalarValue, reference.RestrictedCharacter, '\0', false, null);
        }

        if (start + 1 >= Characters.Length)
        {
            throw CreateXmlScannerError("An entity reference is incomplete", start);
        }

        XmlLexicalScanner entityScanner = CreateXmlLexicalScannerAt(start + 1);
        XmlLexicalToken name = entityScanner.ScanXmlName();
        int position = entityScanner.CurrentXmlLexicalOffset();
        if (position >= Characters.Length || Characters.Span[position] != ';')
        {
            throw CreateXmlScannerError("An entity reference must end with ';'", position);
        }

        Offset = position + 1;
        string entityLexeme = Characters.Span.Slice(start, Offset - start).ToString();
        return new XmlToken(XmlTokenKind.EntityReference, name.Value, null, entityLexeme, start, Offset - start, 0, false, '\0', false, null);
    }

    internal XmlToken ScanXmlCommentToken()
    {
        int start = Offset;
        int valueStart = start + 4;
        int position = valueStart;
        while (position < Characters.Length)
        {
            if (MatchesXmlScannerDelimiter(position, "-->"))
            {
                int end = position + 3;
                string value = Characters.Span.Slice(valueStart, position - valueStart).ToString();
                XmlToken token = CreateValueXmlToken(XmlTokenKind.Comment, null, value, start, end - start, '\0', false);
                int doubleHyphen = value.IndexOf("--", StringComparison.Ordinal);
                if (doubleHyphen >= 0)
                {
                    throw CreateXmlScannerError("An XML comment cannot contain '--'", valueStart + doubleHyphen);
                }

                if (value.Length > 0 && value[value.Length - 1] == '-')
                {
                    throw CreateXmlScannerError("An XML comment cannot end with '-'", valueStart + value.Length - 1);
                }

                XmlMarkupWellFormedness.ValidateXmlCommentToken(token);
                Offset = end;
                return token;
            }

            UnicodeScalar scalar = ReadXmlScannerScalarAt(position);
            ValidateDirectXmlScannerCharacter(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        throw CreateXmlScannerError("An XML comment is not terminated", start);
    }

    internal XmlToken ScanXmlCDataToken()
    {
        int start = Offset;
        int valueStart = start + 9;
        int position = valueStart;
        while (position < Characters.Length)
        {
            if (MatchesXmlScannerDelimiter(position, "]]>"))
            {
                int end = position + 3;
                string value = Characters.Span.Slice(valueStart, position - valueStart).ToString();
                XmlToken token = CreateValueXmlToken(XmlTokenKind.CData, null, value, start, end - start, '\0', false);
                XmlMarkupWellFormedness.ValidateXmlCDataToken(token);
                Offset = end;
                return token;
            }

            UnicodeScalar scalar = ReadXmlScannerScalarAt(position);
            ValidateDirectXmlScannerCharacter(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        throw CreateXmlScannerError("An XML CDATA section is not terminated", start);
    }

    internal XmlToken ScanXmlProcessingInstructionToken()
    {
        int start = Offset;
        XmlDeclarationParser declarationParser = new XmlDeclarationParser(Characters, EntityKind, start);
        if (MapWellFormednessErrors)
        {
            declarationParser.ConfigureXmlDeclarationErrors(ErrorSourceTracker == null ? null : ErrorSourceTracker.SourceUri, ErrorReporter);
        }

        XmlDeclarationInfo declaration = declarationParser.ParseXmlOrTextDeclaration();
        if (declaration.HasDeclaration)
        {
            Offset = start + declaration.CharacterCount;
            if (declaration.HasVersion)
            {
                Version = declaration.Version;
            }

            XmlTokenKind declarationKind = declaration.IsTextDeclaration ? XmlTokenKind.TextDeclaration : XmlTokenKind.XmlDeclaration;
            string declarationLexeme = Characters.Span.Slice(start, declaration.CharacterCount).ToString();
            return new XmlToken(declarationKind, null, declaration.VersionNumber, declarationLexeme, start, declaration.CharacterCount, 0, false, '\0', false, declaration);
        }

        int targetStart = start + 2;
        XmlLexicalScanner targetScanner = CreateXmlLexicalScannerAt(targetStart);
        XmlLexicalToken target = targetScanner.ScanXmlName();
        int position = targetScanner.CurrentXmlLexicalOffset();
        bool separator = false;
        if (position < Characters.Length && XmlLexicalCharacterRules.IsXmlSpaceCharacter(Characters.Span[position]))
        {
            separator = true;
            while (position < Characters.Length && XmlLexicalCharacterRules.IsXmlSpaceCharacter(Characters.Span[position]))
            {
                position = position + 1;
            }
        }

        int valueStart = position;
        while (position < Characters.Length)
        {
            if (MatchesXmlScannerDelimiter(position, "?>"))
            {
                int end = position + 2;
                string value = Characters.Span.Slice(valueStart, position - valueStart).ToString();
                string lexeme = Characters.Span.Slice(start, end - start).ToString();
                XmlToken token = new XmlToken(XmlTokenKind.ProcessingInstruction, target.Value, value, lexeme, start, end - start, 0, false, '\0', separator, null);
                if (value.Length > 0 && !separator)
                {
                    throw CreateXmlScannerError("Whitespace is required between a processing-instruction target and its data", targetStart + target.Value.Length);
                }

                XmlMarkupWellFormedness.ValidateXmlProcessingInstructionToken(token);
                Offset = end;
                return token;
            }

            UnicodeScalar scalar = ReadXmlScannerScalarAt(position);
            ValidateDirectXmlScannerCharacter(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        throw CreateXmlScannerError("An XML processing instruction is not terminated", start);
    }

    internal XmlToken ScanXmlMarkupSymbolToken()
    {
        int start = Offset;
        UnicodeScalar scalar = ReadXmlScannerScalarAt(start);
        ValidateDirectXmlScannerCharacter(scalar);
        Offset = start + scalar.Utf16CodeUnitCount;
        if (scalar.Value == '[')
        {
            DeclarationBracketDepth = DeclarationBracketDepth + 1;
        }
        else if (scalar.Value == ']' && DeclarationBracketDepth > 0)
        {
            DeclarationBracketDepth = DeclarationBracketDepth - 1;
        }

        string value = Characters.Span.Slice(start, scalar.Utf16CodeUnitCount).ToString();
        return CreateValueXmlToken(XmlTokenKind.MarkupSymbol, null, value, start, scalar.Utf16CodeUnitCount, '\0', false);
    }

    internal XmlLexicalScanner CreateXmlLexicalScannerAt(int offset)
    {
        XmlLexicalScanner scanner = new XmlLexicalScanner(Characters, Version);
        if (MapWellFormednessErrors)
        {
            scanner.ConfigureXmlLexicalErrors(ErrorSourceTracker, ErrorReporter);
        }

        scanner.ResetXmlLexicalOffset(offset);
        return scanner;
    }

    internal UnicodeScalar ReadXmlScannerScalarAt(int offset)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(Characters);
        scanner.Offset = offset;
        return scanner.ReadUnicodeScalar();
    }

    internal bool IsXmlScannerNameStart(int scalar)
    {
        if (Version == XmlVersion.Xml10)
        {
            return Xml10CharacterRules.IsXml10NameStartCharacter(scalar);
        }

        return Xml11CharacterRules.IsXml11NameStartCharacter(scalar);
    }

    internal void ValidateDirectXmlScannerCharacter(UnicodeScalar scalar)
    {
        if (Version == XmlVersion.Xml10)
        {
            if (!Xml10CharacterRules.IsLegalXml10Character(scalar.Value))
            {
                throw CreateXmlScannerError("The token contains an illegal XML 1.0 character", scalar.Utf16Offset);
            }

            return;
        }

        if (!Xml11CharacterRules.IsDirectXml11Character(scalar.Value))
        {
            throw CreateXmlScannerError("The token contains an XML 1.1 character that cannot appear directly", scalar.Utf16Offset);
        }
    }

    internal bool MatchesXmlScannerDelimiter(int offset, string delimiter)
    {
        if (delimiter.Length > Characters.Length - offset)
        {
            return false;
        }

        int index = 0;
        while (index < delimiter.Length)
        {
            if (Characters.Span[offset + index] != delimiter[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XmlToken CreateSimpleXmlToken(XmlTokenKind kind, int start, int length)
    {
        string lexeme = Characters.Span.Slice(start, length).ToString();
        return new XmlToken(kind, null, null, lexeme, start, length, 0, false, '\0', false, null);
    }

    internal XmlToken CreateValueXmlToken(XmlTokenKind kind, string? name, string? value, int start, int length, char quote, bool separator)
    {
        string lexeme = Characters.Span.Slice(start, length).ToString();
        return new XmlToken(kind, name, value, lexeme, start, length, 0, false, quote, separator, null);
    }

    internal Exception CreateXmlScannerError(string message, int offset)
    {
        if (MapWellFormednessErrors)
        {
            string? sourceUri = ErrorSourceTracker == null ? null : ErrorSourceTracker.SourceUri;
            return XmlParserDiagnosticFactory.CreateXmlWellFormednessError(message, offset, sourceUri, ErrorSourceTracker, ErrorReporter);
        }

        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
