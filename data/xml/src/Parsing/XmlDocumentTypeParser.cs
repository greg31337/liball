using System;

namespace xml;

internal class XmlDocumentTypeInfo
{
    internal string Name;
    internal string? PublicIdentifier;
    internal string? SystemIdentifier;
    internal string? InternalSubset;
    internal int StartUtf16Offset;
    internal int Utf16Length;

    internal XmlDocumentTypeInfo(string name, string? publicIdentifier, string? systemIdentifier, string? internalSubset, int startUtf16Offset, int utf16Length)
    {
        Name = name;
        PublicIdentifier = publicIdentifier;
        SystemIdentifier = systemIdentifier;
        InternalSubset = internalSubset;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
    }
}

internal class XmlDocumentTypeParser
{
    internal XmlScanner Scanner;
    internal bool Parsed;
    internal string? PublicIdentifier;
    internal string? SystemIdentifier;

    internal XmlDocumentTypeParser(XmlScanner scanner)
    {
        Guard.NotNull(scanner, "scanner");
        Scanner = scanner;
        Parsed = false;
        PublicIdentifier = null;
        SystemIdentifier = null;
    }

    internal XmlDocumentTypeInfo ParseXmlDocumentTypeDeclaration(XmlToken opening)
    {
        Guard.NotNull(opening, "opening");
        if (Parsed)
        {
            throw new InvalidOperationException("The document type parser has already parsed a declaration.");
        }

        if (opening.Kind != XmlTokenKind.MarkupDeclarationOpen)
        {
            throw new ArgumentException("A document type declaration must begin with a markup declaration token.", "opening");
        }

        Parsed = true;
        XmlToken keyword = Scanner.ScanNextXmlToken();
        if (keyword.Kind != XmlTokenKind.Name || !string.Equals(keyword.Name, "DOCTYPE", StringComparison.Ordinal))
        {
            throw CreateXmlDocumentTypeError("The document prolog markup declaration must be a DOCTYPE", keyword.StartUtf16Offset);
        }

        XmlToken separator = Scanner.ScanNextXmlToken();
        if (separator.Kind != XmlTokenKind.Whitespace)
        {
            throw CreateXmlDocumentTypeError("Whitespace is required after DOCTYPE", separator.StartUtf16Offset);
        }

        XmlToken name = Scanner.ScanNextXmlToken();
        if (name.Kind != XmlTokenKind.Name || name.Name == null)
        {
            throw CreateXmlDocumentTypeError("A DOCTYPE requires a root name", name.StartUtf16Offset);
        }

        XmlToken token = Scanner.ScanNextXmlToken();
        bool separated = false;
        if (token.Kind == XmlTokenKind.Whitespace)
        {
            separated = true;
            token = Scanner.ScanNextXmlToken();
        }

        if (token.Kind == XmlTokenKind.Name && (string.Equals(token.Name, "SYSTEM", StringComparison.Ordinal) || string.Equals(token.Name, "PUBLIC", StringComparison.Ordinal)))
        {
            if (!separated)
            {
                throw CreateXmlDocumentTypeError("Whitespace is required before a DOCTYPE external identifier", token.StartUtf16Offset);
            }

            token = ParseXmlDocumentExternalIdentifier(token);
            if (token.Kind == XmlTokenKind.Whitespace)
            {
                token = Scanner.ScanNextXmlToken();
            }
        }

        string? internalSubset = null;
        if (IsXmlDocumentTypeSymbol(token, "["))
        {
            int subsetStart = token.StartUtf16Offset + token.Utf16Length;
            token = Scanner.ScanNextXmlToken();
            while (!IsClosingXmlInternalSubsetToken(token))
            {
                if (token.Kind == XmlTokenKind.EndOfInput)
                {
                    throw CreateXmlDocumentTypeError("The DOCTYPE internal subset is not terminated", subsetStart);
                }

                token = Scanner.ScanNextXmlToken();
            }

            int subsetEnd = token.StartUtf16Offset;
            internalSubset = Scanner.Characters.Span.Slice(subsetStart, subsetEnd - subsetStart).ToString();
            token = Scanner.ScanNextXmlToken();
            if (token.Kind == XmlTokenKind.Whitespace)
            {
                token = Scanner.ScanNextXmlToken();
            }
        }

        if (token.Kind != XmlTokenKind.TagClose)
        {
            throw CreateXmlDocumentTypeError("Only an external identifier or internal subset may follow the DOCTYPE name", token.StartUtf16Offset);
        }

        int end = token.StartUtf16Offset + token.Utf16Length;
        return new XmlDocumentTypeInfo(name.Name, PublicIdentifier, SystemIdentifier, internalSubset, opening.StartUtf16Offset, end - opening.StartUtf16Offset);
    }

    internal XmlToken ParseXmlDocumentExternalIdentifier(XmlToken keyword)
    {
        string keywordName = keyword.Name == null ? "" : keyword.Name;
        XmlToken separator = Scanner.ScanNextXmlToken();
        if (separator.Kind != XmlTokenKind.Whitespace)
        {
            throw CreateXmlDocumentTypeError("Whitespace is required after the DOCTYPE external identifier keyword", separator.StartUtf16Offset);
        }

        XmlToken firstLiteral = Scanner.ScanNextXmlToken();
        if (firstLiteral.Kind != XmlTokenKind.QuotedValue || firstLiteral.Value == null)
        {
            throw CreateXmlDocumentTypeError("A quoted external identifier literal is required", firstLiteral.StartUtf16Offset);
        }

        if (string.Equals(keywordName, "SYSTEM", StringComparison.Ordinal))
        {
            ValidateXmlSystemLiteral(firstLiteral);
            SystemIdentifier = firstLiteral.Value;
            return Scanner.ScanNextXmlToken();
        }

        ValidateXmlPublicIdentifierLiteral(firstLiteral);
        PublicIdentifier = firstLiteral.Value;
        XmlToken publicSeparator = Scanner.ScanNextXmlToken();
        if (publicSeparator.Kind != XmlTokenKind.Whitespace)
        {
            throw CreateXmlDocumentTypeError("Whitespace and a system literal are required after a DOCTYPE public identifier", publicSeparator.StartUtf16Offset);
        }

        XmlToken systemLiteral = Scanner.ScanNextXmlToken();
        if (systemLiteral.Kind != XmlTokenKind.QuotedValue || systemLiteral.Value == null)
        {
            throw CreateXmlDocumentTypeError("A quoted system literal is required after a DOCTYPE public identifier", systemLiteral.StartUtf16Offset);
        }

        ValidateXmlSystemLiteral(systemLiteral);
        SystemIdentifier = systemLiteral.Value;
        return Scanner.ScanNextXmlToken();
    }

    internal void ValidateXmlPublicIdentifierLiteral(XmlToken literal)
    {
        string value = literal.Value == null ? "" : literal.Value;
        int index = 0;
        while (index < value.Length)
        {
            if (!XmlLexicalCharacterRules.IsXmlPublicIdentifierCharacter(value[index]))
            {
                throw CreateXmlDocumentTypeError("The DOCTYPE public identifier contains a character outside PubidChar", literal.StartUtf16Offset + 1 + index);
            }

            index = index + 1;
        }
    }

    internal void ValidateXmlSystemLiteral(XmlToken literal)
    {
        string value = literal.Value == null ? "" : literal.Value;
        int fragment = value.IndexOf('#');
        if (fragment >= 0)
        {
            throw CreateXmlDocumentTypeError("A system identifier cannot contain a fragment identifier", literal.StartUtf16Offset + 1 + fragment);
        }
    }

    internal bool IsClosingXmlInternalSubsetToken(XmlToken token)
    {
        return IsXmlDocumentTypeSymbol(token, "]") && Scanner.DeclarationBracketDepth == 0;
    }

    internal bool IsXmlDocumentTypeSymbol(XmlToken token, string symbol)
    {
        return token.Kind == XmlTokenKind.MarkupSymbol && string.Equals(token.Value, symbol, StringComparison.Ordinal);
    }

    internal Exception CreateXmlDocumentTypeError(string message, int offset)
    {
        return Scanner.CreateXmlScannerError(message, offset);
    }
}
