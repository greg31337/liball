using System;

namespace xml;

internal class XmlLexicalToken
{
    internal string Value;
    internal int StartUtf16Offset;
    internal int Utf16Length;

    internal XmlLexicalToken(string value, int startUtf16Offset, int utf16Length)
    {
        Value = value;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
    }
}

internal class XmlQualifiedNameToken
{
    internal string Value;
    internal string? Prefix;
    internal string LocalName;
    internal int StartUtf16Offset;
    internal int Utf16Length;

    internal XmlQualifiedNameToken(string value, string? prefix, string localName, int startUtf16Offset, int utf16Length)
    {
        Value = value;
        Prefix = prefix;
        LocalName = localName;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
    }
}

internal class XmlQuotedLiteralToken
{
    internal string Value;
    internal char Quote;
    internal int StartUtf16Offset;
    internal int Utf16Length;

    internal XmlQuotedLiteralToken(string value, char quote, int startUtf16Offset, int utf16Length)
    {
        Value = value;
        Quote = quote;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
    }
}

internal static class XmlLexicalCharacterRules
{
    internal static bool IsXmlSpaceCharacter(int scalar)
    {
        return scalar == 0x20 || scalar == 0x9 || scalar == 0xD || scalar == 0xA;
    }

    internal static bool IsXmlPublicIdentifierCharacter(int scalar)
    {
        if (scalar == 0x20 || scalar == 0xD || scalar == 0xA)
        {
            return true;
        }

        if (scalar >= 'a' && scalar <= 'z' || scalar >= 'A' && scalar <= 'Z' || scalar >= '0' && scalar <= '9')
        {
            return true;
        }

        if (scalar == '-' || scalar == '\'' || scalar == '(' || scalar == ')' || scalar == '+' || scalar == ',' || scalar == '.' || scalar == '/')
        {
            return true;
        }

        if (scalar == ':' || scalar == '=' || scalar == '?' || scalar == ';' || scalar == '!' || scalar == '*' || scalar == '#' || scalar == '@')
        {
            return true;
        }

        return scalar == '$' || scalar == '_' || scalar == '%';
    }
}

internal class XmlLexicalScanner
{
    internal ReadOnlyMemory<char> Characters;
    internal XmlVersion Version;
    internal int Offset;
    internal XmlTokenBuffer TokenBuffer;
    internal bool MapWellFormednessErrors;
    internal SourceTracker? ErrorSourceTracker;
    internal IXmlErrorReporter? ErrorReporter;

    internal XmlLexicalScanner(ReadOnlyMemory<char> characters, XmlVersion version)
    {
        if (version != XmlVersion.Xml10 && version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        Characters = characters;
        Version = version;
        Offset = 0;
        TokenBuffer = new XmlTokenBuffer(32);
        MapWellFormednessErrors = false;
        ErrorSourceTracker = null;
        ErrorReporter = null;
    }

    internal void ConfigureXmlLexicalErrors(SourceTracker? tracker, IXmlErrorReporter? reporter)
    {
        MapWellFormednessErrors = true;
        ErrorSourceTracker = tracker;
        ErrorReporter = reporter;
    }

    internal int CurrentXmlLexicalOffset()
    {
        return Offset;
    }

    internal bool IsXmlLexicalEnd()
    {
        return Offset >= Characters.Length;
    }

    internal void ResetXmlLexicalOffset(int offset)
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
    }

    internal XmlLexicalToken ScanXmlSpace()
    {
        int start = Offset;
        int position = start;
        TokenBuffer.ClearToken();
        while (position < Characters.Length)
        {
            UnicodeScalar scalar = ReadXmlScalarAt(position);
            if (!XmlLexicalCharacterRules.IsXmlSpaceCharacter(scalar.Value))
            {
                break;
            }

            AppendXmlScalarSource(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        if (position == start)
        {
            throw CreateXmlLexicalError("XML whitespace was expected", start);
        }

        Offset = position;
        return new XmlLexicalToken(TokenBuffer.TokenText(), start, position - start);
    }

    internal XmlLexicalToken ScanXmlName()
    {
        int start = Offset;
        int position = start;
        TokenBuffer.ClearToken();
        if (position >= Characters.Length)
        {
            throw CreateXmlLexicalError("An XML Name was expected", start);
        }

        UnicodeScalar first = ReadXmlScalarAt(position);
        if (!IsXmlNameStartCharacter(first.Value))
        {
            throw CreateXmlLexicalError("An XML Name must begin with a NameStartChar", start);
        }

        AppendXmlScalarSource(first);
        position = position + first.Utf16CodeUnitCount;
        while (position < Characters.Length)
        {
            UnicodeScalar scalar = ReadXmlScalarAt(position);
            if (!IsXmlNameCharacter(scalar.Value))
            {
                break;
            }

            AppendXmlScalarSource(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        Offset = position;
        return new XmlLexicalToken(TokenBuffer.TokenText(), start, position - start);
    }

    internal XmlLexicalToken ScanXmlNmtoken()
    {
        int start = Offset;
        int position = start;
        TokenBuffer.ClearToken();
        while (position < Characters.Length)
        {
            UnicodeScalar scalar = ReadXmlScalarAt(position);
            if (!IsXmlNameCharacter(scalar.Value))
            {
                break;
            }

            AppendXmlScalarSource(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        if (position == start)
        {
            throw CreateXmlLexicalError("An XML Nmtoken was expected", start);
        }

        Offset = position;
        return new XmlLexicalToken(TokenBuffer.TokenText(), start, position - start);
    }

    internal XmlLexicalToken ScanXmlNcName()
    {
        int start = Offset;
        int position = start;
        TokenBuffer.ClearToken();
        if (position >= Characters.Length)
        {
            throw CreateXmlLexicalError("An XML NCName was expected", start);
        }

        UnicodeScalar first = ReadXmlScalarAt(position);
        if (!IsXmlNcNameStartCharacter(first.Value))
        {
            throw CreateXmlLexicalError("An XML NCName must begin with an NCNameStartChar", start);
        }

        AppendXmlScalarSource(first);
        position = position + first.Utf16CodeUnitCount;
        while (position < Characters.Length)
        {
            UnicodeScalar scalar = ReadXmlScalarAt(position);
            if (!IsXmlNcNameCharacter(scalar.Value))
            {
                break;
            }

            AppendXmlScalarSource(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        Offset = position;
        return new XmlLexicalToken(TokenBuffer.TokenText(), start, position - start);
    }

    internal XmlQualifiedNameToken ScanXmlQualifiedName()
    {
        int start = Offset;
        int position = start;
        TokenBuffer.ClearToken();
        if (position >= Characters.Length)
        {
            throw CreateXmlLexicalError("An XML QName was expected", start);
        }

        UnicodeScalar first = ReadXmlScalarAt(position);
        if (!IsXmlNcNameStartCharacter(first.Value))
        {
            throw CreateXmlLexicalError("An XML QName must begin with an NCNameStartChar", start);
        }

        AppendXmlScalarSource(first);
        position = position + first.Utf16CodeUnitCount;
        while (position < Characters.Length)
        {
            UnicodeScalar scalar = ReadXmlScalarAt(position);
            if (!IsXmlNcNameCharacter(scalar.Value))
            {
                break;
            }

            AppendXmlScalarSource(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        string? prefix = null;
        int localStart = start;
        if (position < Characters.Length && Characters.Span[position] == ':')
        {
            prefix = TokenBuffer.TokenText();
            TokenBuffer.AppendTokenCharacter(':');
            position = position + 1;
            localStart = position;
            if (position >= Characters.Length)
            {
                throw CreateXmlLexicalError("An XML QName requires a local name after ':'", position);
            }

            UnicodeScalar localFirst = ReadXmlScalarAt(position);
            if (!IsXmlNcNameStartCharacter(localFirst.Value))
            {
                throw CreateXmlLexicalError("An XML QName local name must begin with an NCNameStartChar", position);
            }

            AppendXmlScalarSource(localFirst);
            position = position + localFirst.Utf16CodeUnitCount;
            while (position < Characters.Length)
            {
                UnicodeScalar scalar = ReadXmlScalarAt(position);
                if (!IsXmlNcNameCharacter(scalar.Value))
                {
                    break;
                }

                AppendXmlScalarSource(scalar);
                position = position + scalar.Utf16CodeUnitCount;
            }

            if (position < Characters.Length && Characters.Span[position] == ':')
            {
                throw CreateXmlLexicalError("An XML QName cannot contain more than one ':'", position);
            }
        }

        string localName = Characters.Span.Slice(localStart, position - localStart).ToString();
        Offset = position;
        return new XmlQualifiedNameToken(TokenBuffer.TokenText(), prefix, localName, start, position - start);
    }

    internal XmlQuotedLiteralToken ScanXmlQuotedLiteral()
    {
        int start = Offset;
        if (start >= Characters.Length || Characters.Span[start] != '\'' && Characters.Span[start] != '"')
        {
            throw CreateXmlLexicalError("A quoted XML literal was expected", start);
        }

        char quote = Characters.Span[start];
        int position = start + 1;
        TokenBuffer.ClearToken();
        while (position < Characters.Length)
        {
            if (Characters.Span[position] == quote)
            {
                Offset = position + 1;
                return new XmlQuotedLiteralToken(TokenBuffer.TokenText(), quote, start, Offset - start);
            }

            UnicodeScalar scalar = ReadXmlScalarAt(position);
            ValidateDirectXmlLiteralCharacter(scalar);
            AppendXmlScalarSource(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        throw CreateXmlLexicalError("A quoted XML literal is not terminated", start);
    }

    internal XmlQuotedLiteralToken ScanXmlPublicIdentifierLiteral()
    {
        int start = Offset;
        if (start >= Characters.Length || Characters.Span[start] != '\'' && Characters.Span[start] != '"')
        {
            throw CreateXmlLexicalError("An XML public identifier literal was expected", start);
        }

        char quote = Characters.Span[start];
        int position = start + 1;
        TokenBuffer.ClearToken();
        while (position < Characters.Length)
        {
            if (Characters.Span[position] == quote)
            {
                Offset = position + 1;
                return new XmlQuotedLiteralToken(TokenBuffer.TokenText(), quote, start, Offset - start);
            }

            UnicodeScalar scalar = ReadXmlScalarAt(position);
            if (!XmlLexicalCharacterRules.IsXmlPublicIdentifierCharacter(scalar.Value))
            {
                throw CreateXmlLexicalError("An XML public identifier contains a character outside PubidChar", position);
            }

            AppendXmlScalarSource(scalar);
            position = position + scalar.Utf16CodeUnitCount;
        }

        throw CreateXmlLexicalError("An XML public identifier literal is not terminated", start);
    }

    internal XmlCharacterReference ScanXmlCharacterReference()
    {
        XmlCharacterReference reference;
        if (Version == XmlVersion.Xml10)
        {
            reference = Xml10CharacterReferenceParser.ParseXml10CharacterReference(Characters.Span, Offset);
        }
        else
        {
            reference = Xml11CharacterReferenceParser.ParseXml11CharacterReference(Characters.Span, Offset);
        }

        Offset = Offset + reference.SourceCharacterCount;
        return reference;
    }

    internal bool IsXmlDelimiter(ReadOnlySpan<char> delimiter)
    {
        ValidateXmlDelimiter(delimiter);
        if (delimiter.Length > Characters.Length - Offset)
        {
            return false;
        }

        ReadOnlySpan<char> input = Characters.Span;
        int index = 0;
        while (index < delimiter.Length)
        {
            if (input[Offset + index] != delimiter[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ConsumeOptionalXmlDelimiter(ReadOnlySpan<char> delimiter)
    {
        if (!IsXmlDelimiter(delimiter))
        {
            return false;
        }

        Offset = Offset + delimiter.Length;
        return true;
    }

    internal void ConsumeRequiredXmlDelimiter(ReadOnlySpan<char> delimiter)
    {
        if (!IsXmlDelimiter(delimiter))
        {
            throw CreateXmlLexicalError("The required XML delimiter was not found", Offset);
        }

        Offset = Offset + delimiter.Length;
    }

    internal UnicodeScalar ReadXmlScalarAt(int position)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(Characters);
        scanner.Offset = position;
        return scanner.ReadUnicodeScalar();
    }

    internal void AppendXmlScalarSource(UnicodeScalar scalar)
    {
        TokenBuffer.AppendTokenCharacters(Characters.Span.Slice(scalar.Utf16Offset, scalar.Utf16CodeUnitCount));
    }

    internal bool IsXmlNameStartCharacter(int scalar)
    {
        if (Version == XmlVersion.Xml10)
        {
            return Xml10CharacterRules.IsXml10NameStartCharacter(scalar);
        }

        return Xml11CharacterRules.IsXml11NameStartCharacter(scalar);
    }

    internal bool IsXmlNameCharacter(int scalar)
    {
        if (Version == XmlVersion.Xml10)
        {
            return Xml10CharacterRules.IsXml10NameCharacter(scalar);
        }

        return Xml11CharacterRules.IsXml11NameCharacter(scalar);
    }

    internal bool IsXmlNcNameStartCharacter(int scalar)
    {
        return scalar != ':' && IsXmlNameStartCharacter(scalar);
    }

    internal bool IsXmlNcNameCharacter(int scalar)
    {
        return scalar != ':' && IsXmlNameCharacter(scalar);
    }

    internal void ValidateDirectXmlLiteralCharacter(UnicodeScalar scalar)
    {
        if (Version == XmlVersion.Xml10)
        {
            if (!Xml10CharacterRules.IsLegalXml10Character(scalar.Value))
            {
                throw CreateXmlLexicalError("A quoted literal contains an illegal XML 1.0 character", scalar.Utf16Offset);
            }

            return;
        }

        if (!Xml11CharacterRules.IsDirectXml11Character(scalar.Value))
        {
            throw CreateXmlLexicalError("A quoted literal contains an XML 1.1 character that cannot appear directly", scalar.Utf16Offset);
        }
    }

    internal void ValidateXmlDelimiter(ReadOnlySpan<char> delimiter)
    {
        if (delimiter.Length == 0)
        {
            throw new ArgumentException("An XML delimiter cannot be empty.", "delimiter");
        }
    }

    internal Exception CreateXmlLexicalError(string message, int offset)
    {
        if (MapWellFormednessErrors)
        {
            string? sourceUri = ErrorSourceTracker == null ? null : ErrorSourceTracker.SourceUri;
            return XmlParserDiagnosticFactory.CreateXmlWellFormednessError(message, offset, sourceUri, ErrorSourceTracker, ErrorReporter);
        }

        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
