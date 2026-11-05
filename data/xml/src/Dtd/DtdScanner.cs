using System;

namespace xml;

internal class DtdScanner
{
    internal ReadOnlyMemory<char> Characters;
    internal XmlVersion Version;
    internal int Offset;
    internal DtdLiteralExpectation LiteralExpectation;
    internal bool InMarkupDeclaration;
    internal string? MarkupDeclarationName;
    internal int MarkupDeclarationPart;
    internal bool Ended;
    internal bool MapWellFormednessErrors;
    internal SourceTracker? ErrorSourceTracker;
    internal IXmlErrorReporter? ErrorReporter;

    internal DtdScanner(ReadOnlyMemory<char> characters, XmlVersion version)
    {
        if (version != XmlVersion.Xml10 && version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        Characters = characters;
        Version = version;
        Offset = 0;
        LiteralExpectation = DtdLiteralExpectation.None;
        InMarkupDeclaration = false;
        MarkupDeclarationName = null;
        MarkupDeclarationPart = 0;
        Ended = false;
        MapWellFormednessErrors = false;
        ErrorSourceTracker = null;
        ErrorReporter = null;
    }

    internal DtdToken ScanNextDtdToken()
    {
        if (Ended)
        {
            return CreateSimpleDtdToken(DtdTokenKind.EndOfInput, Offset, 0);
        }

        if (Offset >= Characters.Length)
        {
            Ended = true;
            return CreateSimpleDtdToken(DtdTokenKind.EndOfInput, Offset, 0);
        }

        int start = Offset;
        if (XmlLexicalCharacterRules.IsXmlSpaceCharacter(Characters.Span[start]))
        {
            return ScanDtdWhitespace();
        }

        if (MatchesDtdDelimiter(start, "<!--"))
        {
            LiteralExpectation = DtdLiteralExpectation.None;
            return ScanDtdComment();
        }

        if (MatchesDtdDelimiter(start, "<?"))
        {
            LiteralExpectation = DtdLiteralExpectation.None;
            return ScanDtdProcessingInstruction();
        }

        if (MatchesDtdDelimiter(start, "<!["))
        {
            LiteralExpectation = DtdLiteralExpectation.None;
            InMarkupDeclaration = false;
            MarkupDeclarationName = null;
            MarkupDeclarationPart = 0;
            Offset = start + 3;
            return CreateSimpleDtdToken(DtdTokenKind.ConditionalSectionOpen, start, 3);
        }

        if (MatchesDtdDelimiter(start, "]]>"))
        {
            LiteralExpectation = DtdLiteralExpectation.None;
            Offset = start + 3;
            return CreateSimpleDtdToken(DtdTokenKind.ConditionalSectionClose, start, 3);
        }

        if (MatchesDtdDelimiter(start, "<!"))
        {
            LiteralExpectation = DtdLiteralExpectation.None;
            InMarkupDeclaration = true;
            MarkupDeclarationName = null;
            MarkupDeclarationPart = 0;
            Offset = start + 2;
            return CreateSimpleDtdToken(DtdTokenKind.MarkupDeclarationOpen, start, 2);
        }

        if (Characters.Span[start] == '>')
        {
            LiteralExpectation = DtdLiteralExpectation.None;
            InMarkupDeclaration = false;
            MarkupDeclarationName = null;
            MarkupDeclarationPart = 0;
            Offset = start + 1;
            return CreateSimpleDtdToken(DtdTokenKind.DeclarationClose, start, 1);
        }

        if (Characters.Span[start] == '%')
        {
            LiteralExpectation = DtdLiteralExpectation.None;
            return ScanDtdParameterEntityOrPercent();
        }

        if (Characters.Span[start] == '\'' || Characters.Span[start] == '"')
        {
            return ScanDtdLiteral();
        }

        UnicodeScalar scalar = ReadDtdScalarAt(start);
        XmlLexicalScanner lexicalScanner = CreateDtdLexicalScannerAt(start);
        if (lexicalScanner.IsXmlNameStartCharacter(scalar.Value))
        {
            return ScanDtdName(lexicalScanner);
        }

        if (lexicalScanner.IsXmlNameCharacter(scalar.Value))
        {
            LiteralExpectation = DtdLiteralExpectation.None;
            XmlLexicalToken nmtoken = lexicalScanner.ScanXmlNmtoken();
            Offset = lexicalScanner.CurrentXmlLexicalOffset();
            return CreateValueDtdToken(DtdTokenKind.Nmtoken, null, nmtoken.Value, start, Offset - start, '\0', false);
        }

        LiteralExpectation = DtdLiteralExpectation.None;
        return ScanDtdSymbol(scalar);
    }

    internal int CurrentDtdScannerOffset()
    {
        return Offset;
    }

    internal void ConfigureDtdScannerErrors(SourceTracker? tracker, IXmlErrorReporter? reporter)
    {
        MapWellFormednessErrors = true;
        ErrorSourceTracker = tracker;
        ErrorReporter = reporter;
    }

    internal void ResetDtdScanner()
    {
        Offset = 0;
        LiteralExpectation = DtdLiteralExpectation.None;
        InMarkupDeclaration = false;
        MarkupDeclarationName = null;
        MarkupDeclarationPart = 0;
        Ended = false;
    }

    internal void ResetDtdScannerAt(int offset)
    {
        Guard.NonNegativeInt(offset, "offset");
        if (offset > Characters.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        ReadOnlySpan<char> span = Characters.Span;
        if (offset > 0 && offset < span.Length && char.IsHighSurrogate(span[offset - 1]) && char.IsLowSurrogate(span[offset]))
        {
            throw new ArgumentException("The offset divides a UTF-16 surrogate pair.", "offset");
        }

        Offset = offset;
        LiteralExpectation = DtdLiteralExpectation.None;
        InMarkupDeclaration = false;
        MarkupDeclarationName = null;
        MarkupDeclarationPart = 0;
        Ended = false;
    }

    internal DtdToken ScanDtdWhitespace()
    {
        int start = Offset;
        XmlLexicalScanner scanner = CreateDtdLexicalScannerAt(start);
        XmlLexicalToken whitespace = scanner.ScanXmlSpace();
        Offset = scanner.CurrentXmlLexicalOffset();
        return CreateValueDtdToken(DtdTokenKind.Whitespace, null, whitespace.Value, start, Offset - start, '\0', false);
    }

    internal DtdToken ScanDtdName(XmlLexicalScanner scanner)
    {
        int start = Offset;
        XmlLexicalToken name = scanner.ScanXmlName();
        Offset = scanner.CurrentXmlLexicalOffset();
        LiteralExpectation = DtdLiteralExpectation.None;
        if (!InMarkupDeclaration)
        {
            return CreateValueDtdToken(DtdTokenKind.Name, name.Value, name.Value, start, Offset - start, '\0', false);
        }

        if (MarkupDeclarationName == null)
        {
            MarkupDeclarationName = name.Value;
        }
        else if ((MarkupDeclarationName == "ENTITY" || MarkupDeclarationName == "NOTATION" || MarkupDeclarationName == "DOCTYPE") && MarkupDeclarationPart == 0)
        {
            MarkupDeclarationPart = 1;
        }
        else if ((MarkupDeclarationName == "ENTITY" || MarkupDeclarationName == "NOTATION" || MarkupDeclarationName == "DOCTYPE") && MarkupDeclarationPart == 1 && name.Value == "PUBLIC")
        {
            LiteralExpectation = DtdLiteralExpectation.PublicIdentifier;
        }
        else if ((MarkupDeclarationName == "ENTITY" || MarkupDeclarationName == "NOTATION" || MarkupDeclarationName == "DOCTYPE") && MarkupDeclarationPart == 1 && name.Value == "SYSTEM")
        {
            LiteralExpectation = DtdLiteralExpectation.SystemIdentifier;
        }

        return CreateValueDtdToken(DtdTokenKind.Name, name.Value, name.Value, start, Offset - start, '\0', false);
    }

    internal DtdToken ScanDtdParameterEntityOrPercent()
    {
        int start = Offset;
        int nameStart = start + 1;
        if (nameStart >= Characters.Length)
        {
            Offset = nameStart;
            return CreateValueDtdToken(DtdTokenKind.Symbol, null, "%", start, 1, '\0', false);
        }

        UnicodeScalar first = ReadDtdScalarAt(nameStart);
        XmlLexicalScanner scanner = CreateDtdLexicalScannerAt(nameStart);
        if (!scanner.IsXmlNameStartCharacter(first.Value))
        {
            Offset = nameStart;
            return CreateValueDtdToken(DtdTokenKind.Symbol, null, "%", start, 1, '\0', false);
        }

        XmlLexicalToken name = scanner.ScanXmlName();
        int position = scanner.CurrentXmlLexicalOffset();
        if (position >= Characters.Length || Characters.Span[position] != ';')
        {
            throw CreateDtdScannerError("A parameter entity reference must end with ';'", position);
        }

        Offset = position + 1;
        return CreateValueDtdToken(DtdTokenKind.ParameterEntityReference, name.Value, null, start, Offset - start, '\0', false);
    }

    internal DtdToken ScanDtdLiteral()
    {
        int start = Offset;
        XmlLexicalScanner scanner = CreateDtdLexicalScannerAt(start);
        DtdTokenKind kind = DtdTokenKind.QuotedLiteral;
        XmlQuotedLiteralToken literal;
        if (LiteralExpectation == DtdLiteralExpectation.PublicIdentifier)
        {
            kind = DtdTokenKind.PublicIdentifierLiteral;
            literal = scanner.ScanXmlPublicIdentifierLiteral();
            LiteralExpectation = DtdLiteralExpectation.SystemIdentifier;
        }
        else
        {
            literal = scanner.ScanXmlQuotedLiteral();
            if (LiteralExpectation == DtdLiteralExpectation.SystemIdentifier)
            {
                kind = DtdTokenKind.SystemLiteral;
            }

            LiteralExpectation = DtdLiteralExpectation.None;
        }

        Offset = scanner.CurrentXmlLexicalOffset();
        return CreateValueDtdToken(kind, null, literal.Value, start, Offset - start, literal.Quote, false);
    }

    internal DtdToken ScanDtdComment()
    {
        int start = Offset;
        int valueStart = start + 4;
        int position = valueStart;
        while (position < Characters.Length)
        {
            if (MatchesDtdDelimiter(position, "-->"))
            {
                int end = position + 3;
                string value = Characters.Span.Slice(valueStart, position - valueStart).ToString();
                int doubleHyphen = value.IndexOf("--", StringComparison.Ordinal);
                if (doubleHyphen >= 0)
                {
                    throw CreateDtdScannerError("An XML comment cannot contain '--'", valueStart + doubleHyphen);
                }

                if (value.Length > 0 && value[value.Length - 1] == '-')
                {
                    throw CreateDtdScannerError("An XML comment cannot end with '-'", valueStart + value.Length - 1);
                }

                Offset = end;
                return CreateValueDtdToken(DtdTokenKind.Comment, null, value, start, end - start, '\0', false);
            }

            UnicodeScalar scalar = ReadDtdScalarAt(position);
            ValidateDirectDtdCharacter(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        throw CreateDtdScannerError("An XML comment is not terminated", start);
    }

    internal DtdToken ScanDtdProcessingInstruction()
    {
        int start = Offset;
        int targetStart = start + 2;
        XmlLexicalScanner targetScanner = CreateDtdLexicalScannerAt(targetStart);
        XmlLexicalToken target = targetScanner.ScanXmlName();
        int position = targetScanner.CurrentXmlLexicalOffset();
        bool separator = false;
        if (position < Characters.Length && XmlLexicalCharacterRules.IsXmlSpaceCharacter(Characters.Span[position]))
        {
            separator = true;
            XmlLexicalScanner spaceScanner = CreateDtdLexicalScannerAt(position);
            spaceScanner.ScanXmlSpace();
            position = spaceScanner.CurrentXmlLexicalOffset();
        }

        int valueStart = position;
        while (position < Characters.Length)
        {
            if (MatchesDtdDelimiter(position, "?>"))
            {
                int end = position + 2;
                string value = Characters.Span.Slice(valueStart, position - valueStart).ToString();
                if (value.Length > 0 && !separator)
                {
                    throw CreateDtdScannerError("Whitespace is required between a processing-instruction target and its data", targetStart + target.Value.Length);
                }

                Offset = end;
                return CreateValueDtdToken(DtdTokenKind.ProcessingInstruction, target.Value, value, start, end - start, '\0', separator);
            }

            UnicodeScalar scalar = ReadDtdScalarAt(position);
            ValidateDirectDtdCharacter(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        throw CreateDtdScannerError("An XML processing instruction is not terminated", start);
    }

    internal DtdToken ScanDtdSymbol(UnicodeScalar scalar)
    {
        int start = Offset;
        ValidateDirectDtdCharacter(scalar);
        Offset = start + scalar.Utf16CodeUnitCount;
        string value = Characters.Span.Slice(start, scalar.Utf16CodeUnitCount).ToString();
        return CreateValueDtdToken(DtdTokenKind.Symbol, null, value, start, scalar.Utf16CodeUnitCount, '\0', false);
    }

    internal void SkipDtdIgnoredSection()
    {
        int start = Offset;
        int position = start;
        int depth = 1;
        while (position < Characters.Length)
        {
            if (MatchesDtdDelimiter(position, "<!["))
            {
                depth = depth + 1;
                position = position + 3;
            }
            else if (MatchesDtdDelimiter(position, "]]>"))
            {
                depth = depth - 1;
                position = position + 3;
                if (depth == 0)
                {
                    Offset = position;
                    LiteralExpectation = DtdLiteralExpectation.None;
                    InMarkupDeclaration = false;
                    MarkupDeclarationName = null;
                    MarkupDeclarationPart = 0;
                    Ended = false;
                    return;
                }
            }
            else
            {
                UnicodeScalar scalar = ReadDtdScalarAt(position);
                ValidateDirectDtdCharacter(scalar);
                position = position + scalar.Utf16CodeUnitCount;
            }
        }

        throw CreateDtdScannerError("An ignored conditional section is not terminated", start);
    }

    internal XmlLexicalScanner CreateDtdLexicalScannerAt(int offset)
    {
        XmlLexicalScanner scanner = new XmlLexicalScanner(Characters, Version);
        if (MapWellFormednessErrors)
        {
            scanner.ConfigureXmlLexicalErrors(ErrorSourceTracker, ErrorReporter);
        }

        scanner.ResetXmlLexicalOffset(offset);
        return scanner;
    }

    internal UnicodeScalar ReadDtdScalarAt(int offset)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(Characters);
        scanner.Offset = offset;
        return scanner.ReadUnicodeScalar();
    }

    internal void ValidateDirectDtdCharacter(UnicodeScalar scalar)
    {
        if (Version == XmlVersion.Xml10)
        {
            if (!Xml10CharacterRules.IsLegalXml10Character(scalar.Value))
            {
                throw CreateDtdScannerError("The DTD token contains an illegal XML 1.0 character", scalar.Utf16Offset);
            }

            return;
        }

        if (!Xml11CharacterRules.IsDirectXml11Character(scalar.Value))
        {
            throw CreateDtdScannerError("The DTD token contains an XML 1.1 character that cannot appear directly", scalar.Utf16Offset);
        }
    }

    internal bool MatchesDtdDelimiter(int offset, string delimiter)
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

    internal DtdToken CreateSimpleDtdToken(DtdTokenKind kind, int start, int length)
    {
        string lexeme = Characters.Span.Slice(start, length).ToString();
        return new DtdToken(kind, null, null, lexeme, start, length, '\0', false);
    }

    internal DtdToken CreateValueDtdToken(DtdTokenKind kind, string? name, string? value, int start, int length, char quote, bool separator)
    {
        string lexeme = Characters.Span.Slice(start, length).ToString();
        return new DtdToken(kind, name, value, lexeme, start, length, quote, separator);
    }

    internal Exception CreateDtdScannerError(string message, int offset)
    {
        if (MapWellFormednessErrors)
        {
            string? sourceUri = ErrorSourceTracker == null ? null : ErrorSourceTracker.SourceUri;
            return XmlParserDiagnosticFactory.CreateXmlWellFormednessError(message, offset, sourceUri, ErrorSourceTracker, ErrorReporter);
        }

        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
