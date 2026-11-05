using System;
using System.Collections.Generic;

namespace xml;

internal class XmlRawAttribute
{
    internal string Name;
    internal string RawValue;
    internal char Quote;
    internal int StartUtf16Offset;
    internal int Utf16Length;

    internal XmlRawAttribute(string name, string rawValue, char quote, int startUtf16Offset, int utf16Length)
    {
        Name = name;
        RawValue = rawValue;
        Quote = quote;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
    }
}

internal class XmlElementTag
{
    internal string Name;
    internal XmlRawAttribute[] Attributes;
    internal bool IsEndElement;
    internal bool IsEmptyElement;
    internal int Depth;
    internal int StartUtf16Offset;
    internal int Utf16Length;

    internal XmlElementTag(string name, XmlRawAttribute[] attributes, bool isEndElement, bool isEmptyElement, int depth, int startUtf16Offset, int utf16Length)
    {
        Name = name;
        Attributes = attributes;
        IsEndElement = isEndElement;
        IsEmptyElement = isEmptyElement;
        Depth = depth;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
    }
}

internal interface IXmlAttributeDuplicateDetector
{
    bool IsDuplicateXmlAttribute(XmlRawAttribute[] precedingAttributes, XmlRawAttribute candidate);
}

internal class XmlElementTagParser
{
    internal XmlScanner Scanner;
    internal bool PreserveAttributeQuotes;
    internal IXmlAttributeDuplicateDetector? DuplicateDetector;
    internal ParserWorkLimiter? WorkLimiter;
    internal List<string> OpenElementNames;

    internal XmlElementTagParser(XmlScanner scanner, bool preserveAttributeQuotes, IXmlAttributeDuplicateDetector? duplicateDetector)
    {
        Guard.NotNull(scanner, "scanner");
        Scanner = scanner;
        PreserveAttributeQuotes = preserveAttributeQuotes;
        DuplicateDetector = duplicateDetector;
        WorkLimiter = null;
        OpenElementNames = new List<string>();
    }

    internal void ConfigureXmlElementWork(ParserWorkLimiter workLimiter)
    {
        Guard.NotNull(workLimiter, "workLimiter");
        WorkLimiter = workLimiter;
    }

    internal XmlElementTag ParseNextXmlElementTag()
    {
        XmlToken opening = Scanner.ScanNextXmlToken();
        if (opening.Kind == XmlTokenKind.StartTagOpen)
        {
            return ParseXmlStartOrEmptyElementTag(opening);
        }

        if (opening.Kind == XmlTokenKind.EndTagOpen)
        {
            return ParseXmlEndElementTag(opening);
        }

        throw CreateXmlElementTagError("An XML start or end tag was expected", opening.StartUtf16Offset);
    }

    internal int CurrentXmlElementDepth()
    {
        return OpenElementNames.Count;
    }

    internal XmlElementTag ParseXmlStartOrEmptyElementTag(XmlToken opening)
    {
        XmlToken nameToken = Scanner.ScanNextXmlToken();
        if (nameToken.Kind != XmlTokenKind.Name || nameToken.Name == null)
        {
            throw CreateXmlElementTagError("An XML start tag requires an element name", nameToken.StartUtf16Offset);
        }

        string elementName = nameToken.Name;
        List<XmlRawAttribute> attributes = new List<XmlRawAttribute>();
        XmlToken token = Scanner.ScanNextXmlToken();
        while (true)
        {
            if (token.Kind == XmlTokenKind.TagClose || token.Kind == XmlTokenKind.EmptyElementClose)
            {
                bool empty = token.Kind == XmlTokenKind.EmptyElementClose;
                int depth = OpenElementNames.Count;
                int end = token.StartUtf16Offset + token.Utf16Length;
                XmlElementTag tag = new XmlElementTag(elementName, attributes.ToArray(), false, empty, depth, opening.StartUtf16Offset, end - opening.StartUtf16Offset);
                ParserWorkLimiter? limiter = WorkLimiter;
                if (limiter != null)
                {
                    limiter.ObserveXmlElementTag(tag);
                }

                if (!empty)
                {
                    OpenElementNames.Add(elementName);
                }

                return tag;
            }

            if (token.Kind != XmlTokenKind.Whitespace)
            {
                throw CreateXmlElementTagError("Whitespace or the end of an XML start tag was expected", token.StartUtf16Offset);
            }

            token = Scanner.ScanNextXmlToken();
            if (token.Kind == XmlTokenKind.TagClose || token.Kind == XmlTokenKind.EmptyElementClose)
            {
                continue;
            }

            if (token.Kind != XmlTokenKind.Name || token.Name == null)
            {
                throw CreateXmlElementTagError("An XML attribute name was expected", token.StartUtf16Offset);
            }

            XmlRawAttribute attribute = ParseXmlRawAttribute(token);
            ValidateXmlAttributeDuplicate(attributes, attribute);
            attributes.Add(attribute);
            token = Scanner.ScanNextXmlToken();
        }
    }

    internal XmlRawAttribute ParseXmlRawAttribute(XmlToken nameToken)
    {
        string attributeName = nameToken.Name == null ? "" : nameToken.Name;
        XmlToken token = Scanner.ScanNextXmlToken();
        if (token.Kind == XmlTokenKind.Whitespace)
        {
            token = Scanner.ScanNextXmlToken();
        }

        if (token.Kind != XmlTokenKind.Equals)
        {
            throw CreateXmlElementTagError("An XML attribute name must be followed by '='", token.StartUtf16Offset);
        }

        token = Scanner.ScanNextXmlToken();
        if (token.Kind == XmlTokenKind.Whitespace)
        {
            token = Scanner.ScanNextXmlToken();
        }

        if (token.Kind != XmlTokenKind.QuotedValue || token.Value == null)
        {
            throw CreateXmlElementTagError("An XML attribute value must be quoted", token.StartUtf16Offset);
        }

        int lessThan = token.Value.IndexOf('<');
        if (lessThan >= 0)
        {
            throw CreateXmlElementTagError("An XML attribute value cannot contain '<'", token.StartUtf16Offset + 1 + lessThan);
        }

        ValidateXmlAttributeReferences(token.Value, token.StartUtf16Offset + 1);
        char quote = PreserveAttributeQuotes ? token.Quote : '\0';
        int end = token.StartUtf16Offset + token.Utf16Length;
        return new XmlRawAttribute(attributeName, token.Value, quote, nameToken.StartUtf16Offset, end - nameToken.StartUtf16Offset);
    }

    internal void ValidateXmlAttributeReferences(string value, int sourceOffset)
    {
        int position = 0;
        while (position < value.Length)
        {
            if (value[position] != '&')
            {
                position = position + 1;
                continue;
            }

            int semicolon = value.IndexOf(';', position + 1);
            if (semicolon < 0)
            {
                throw CreateXmlElementTagError("An XML attribute reference must end with ';'", sourceOffset + position);
            }

            if (position + 1 < semicolon && value[position + 1] == '#')
            {
                ValidateXmlAttributeCharacterReference(value, position, semicolon, sourceOffset);
            }
            else
            {
                ValidateXmlAttributeEntityReference(value, position, semicolon, sourceOffset);
            }

            position = semicolon + 1;
        }
    }

    internal void ValidateXmlAttributeCharacterReference(string value, int ampersand, int semicolon, int sourceOffset)
    {
        int position = ampersand + 2;
        int radix = 10;
        if (position < semicolon && value[position] == 'x')
        {
            radix = 16;
            position = position + 1;
        }

        if (position >= semicolon)
        {
            throw CreateXmlElementTagError("An XML character reference requires digits", sourceOffset + ampersand);
        }

        int scalar = 0;
        while (position < semicolon)
        {
            int digit = XmlAttributeReferenceDigit(value[position], radix);
            if (digit < 0)
            {
                throw CreateXmlElementTagError("An XML character reference contains an invalid digit", sourceOffset + position);
            }

            if (scalar > (0x10FFFF - digit) / radix)
            {
                throw CreateXmlElementTagError("An XML character reference exceeds the Unicode scalar range", sourceOffset + ampersand);
            }

            scalar = scalar * radix + digit;
            position = position + 1;
        }

        bool legal = Scanner.CurrentXmlScannerVersion() == XmlVersion.Xml10 ? Xml10CharacterRules.IsLegalXml10Character(scalar) : Xml11CharacterRules.IsLegalXml11Character(scalar);
        if (!legal)
        {
            throw CreateXmlElementTagError("An XML character reference is not legal for the XML version", sourceOffset + ampersand);
        }
    }

    internal int XmlAttributeReferenceDigit(char character, int radix)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (radix == 16 && character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        if (radix == 16 && character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    internal void ValidateXmlAttributeEntityReference(string value, int ampersand, int semicolon, int sourceOffset)
    {
        int position = ampersand + 1;
        if (position >= semicolon)
        {
            throw CreateXmlElementTagError("An XML entity reference requires a name", sourceOffset + ampersand);
        }

        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        scanner.Offset = position;
        UnicodeScalar scalar = scanner.ReadUnicodeScalar();
        bool validStart = Scanner.CurrentXmlScannerVersion() == XmlVersion.Xml10 ? Xml10CharacterRules.IsXml10NameStartCharacter(scalar.Value) : Xml11CharacterRules.IsXml11NameStartCharacter(scalar.Value);
        if (!validStart)
        {
            throw CreateXmlElementTagError("An XML entity reference must begin with a NameStartChar", sourceOffset + position);
        }

        position = scanner.CurrentUtf16Offset();
        while (position < semicolon)
        {
            scalar = scanner.ReadUnicodeScalar();
            bool valid = Scanner.CurrentXmlScannerVersion() == XmlVersion.Xml10 ? Xml10CharacterRules.IsXml10NameCharacter(scalar.Value) : Xml11CharacterRules.IsXml11NameCharacter(scalar.Value);
            if (!valid || scanner.CurrentUtf16Offset() > semicolon)
            {
                throw CreateXmlElementTagError("An XML entity reference must contain only Name characters", sourceOffset + position);
            }

            position = scanner.CurrentUtf16Offset();
        }
    }

    internal void ValidateXmlAttributeDuplicate(List<XmlRawAttribute> attributes, XmlRawAttribute candidate)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, candidate.Name, StringComparison.Ordinal))
            {
                throw CreateXmlElementTagError("An XML start tag cannot contain duplicate attributes", candidate.StartUtf16Offset);
            }

            index = index + 1;
        }

        if (DuplicateDetector != null && DuplicateDetector.IsDuplicateXmlAttribute(attributes.ToArray(), candidate))
        {
            throw CreateXmlElementTagError("The XML attribute duplicate detector rejected an attribute", candidate.StartUtf16Offset);
        }
    }

    internal XmlElementTag ParseXmlEndElementTag(XmlToken opening)
    {
        XmlToken nameToken = Scanner.ScanNextXmlToken();
        if (nameToken.Kind != XmlTokenKind.Name || nameToken.Name == null)
        {
            throw CreateXmlElementTagError("An XML end tag requires an element name", nameToken.StartUtf16Offset);
        }

        string elementName = nameToken.Name;
        XmlToken closing = Scanner.ScanNextXmlToken();
        if (closing.Kind == XmlTokenKind.Whitespace)
        {
            closing = Scanner.ScanNextXmlToken();
        }

        if (closing.Kind != XmlTokenKind.TagClose)
        {
            throw CreateXmlElementTagError("An XML end tag must end with '>'", closing.StartUtf16Offset);
        }

        if (OpenElementNames.Count == 0)
        {
            throw CreateXmlElementTagError("An XML end tag has no matching start tag", opening.StartUtf16Offset);
        }

        int depth = OpenElementNames.Count - 1;
        string expectedName = OpenElementNames[depth];
        if (!string.Equals(expectedName, elementName, StringComparison.Ordinal))
        {
            throw CreateXmlElementTagError("The XML end tag does not match the open start tag", nameToken.StartUtf16Offset);
        }

        int end = closing.StartUtf16Offset + closing.Utf16Length;
        XmlElementTag tag = new XmlElementTag(elementName, Array.Empty<XmlRawAttribute>(), true, false, depth, opening.StartUtf16Offset, end - opening.StartUtf16Offset);
        ParserWorkLimiter? limiter = WorkLimiter;
        if (limiter != null)
        {
            limiter.ObserveXmlElementTag(tag);
        }

        OpenElementNames.RemoveAt(depth);
        return tag;
    }

    internal Exception CreateXmlElementTagError(string message, int offset)
    {
        return Scanner.CreateXmlScannerError(message, offset);
    }
}
