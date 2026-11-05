using System;

namespace xml;

internal static class XmlMarkupWellFormedness
{
    internal static void ValidateXmlCommentToken(XmlToken token)
    {
        RequireXmlTokenKind(token, XmlTokenKind.Comment, "comment");
        string? optionalValue = token.Value;
        if (optionalValue == null)
        {
            throw CreateXmlMarkupError("An XML comment requires content", token.StartUtf16Offset);
        }

        int doubleHyphen = optionalValue.IndexOf("--", StringComparison.Ordinal);
        if (doubleHyphen >= 0)
        {
            throw CreateXmlMarkupError("An XML comment cannot contain '--'", token.StartUtf16Offset + 4 + doubleHyphen);
        }

        if (optionalValue.Length > 0 && optionalValue[optionalValue.Length - 1] == '-')
        {
            throw CreateXmlMarkupError("An XML comment cannot end with '-'", token.StartUtf16Offset + 4 + optionalValue.Length - 1);
        }
    }

    internal static void ValidateXmlProcessingInstructionToken(XmlToken token)
    {
        RequireXmlTokenKind(token, XmlTokenKind.ProcessingInstruction, "processing instruction");
        string? optionalTarget = token.Name;
        if (optionalTarget == null || optionalTarget.Length == 0)
        {
            throw CreateXmlMarkupError("An XML processing instruction requires a target", token.StartUtf16Offset + 2);
        }

        ValidateXmlProcessingInstructionTarget(optionalTarget, token.StartUtf16Offset + 2);
        if (string.Equals(optionalTarget, "xml", StringComparison.OrdinalIgnoreCase))
        {
            throw CreateXmlMarkupError("A processing-instruction target matching 'xml' is reserved", token.StartUtf16Offset + 2);
        }

        string? optionalValue = token.Value;
        if (optionalValue == null)
        {
            throw CreateXmlMarkupError("An XML processing instruction requires a data value", token.StartUtf16Offset);
        }

        if (optionalValue.Length > 0 && !token.HasNameValueSeparator)
        {
            throw CreateXmlMarkupError("Whitespace is required between a processing-instruction target and its data", token.StartUtf16Offset + 2 + optionalTarget.Length);
        }

        if (optionalValue.IndexOf("?>", StringComparison.Ordinal) >= 0)
        {
            throw CreateXmlMarkupError("Processing-instruction data cannot contain '?>'", token.StartUtf16Offset);
        }
    }

    internal static void ValidateXmlCDataToken(XmlToken token)
    {
        RequireXmlTokenKind(token, XmlTokenKind.CData, "CDATA section");
        string? optionalValue = token.Value;
        if (optionalValue == null)
        {
            throw CreateXmlMarkupError("An XML CDATA section requires content", token.StartUtf16Offset);
        }

        int closing = optionalValue.IndexOf("]]>", StringComparison.Ordinal);
        if (closing >= 0)
        {
            throw CreateXmlMarkupError("CDATA section content cannot contain ']]>'", token.StartUtf16Offset + 9 + closing);
        }
    }

    internal static void ValidateXmlCharacterDataToken(XmlToken token)
    {
        RequireXmlTokenKind(token, XmlTokenKind.Text, "character data");
        string? optionalValue = token.Value;
        if (optionalValue == null)
        {
            throw CreateXmlMarkupError("An XML character-data token requires content", token.StartUtf16Offset);
        }

        int lessThan = optionalValue.IndexOf('<');
        if (lessThan >= 0)
        {
            throw CreateXmlMarkupError("XML character data cannot contain '<'", token.StartUtf16Offset + lessThan);
        }

        int ampersand = optionalValue.IndexOf('&');
        if (ampersand >= 0)
        {
            throw CreateXmlMarkupError("XML character data cannot contain '&'", token.StartUtf16Offset + ampersand);
        }

        int cdataClosing = optionalValue.IndexOf("]]>", StringComparison.Ordinal);
        if (cdataClosing >= 0)
        {
            throw CreateXmlMarkupError("XML character data cannot contain ']]>'", token.StartUtf16Offset + cdataClosing);
        }
    }

    internal static void ValidateXmlProcessingInstructionTarget(string target, int sourceOffset)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(target.AsMemory());
        if (scanner.IsUnicodeScalarEnd())
        {
            throw CreateXmlMarkupError("An XML processing-instruction target cannot be empty", sourceOffset);
        }

        UnicodeScalar first = scanner.ReadUnicodeScalar();
        if (!Xml10CharacterRules.IsXml10NameStartCharacter(first.Value))
        {
            throw CreateXmlMarkupError("An XML processing-instruction target must be a Name", sourceOffset);
        }

        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            if (!Xml10CharacterRules.IsXml10NameCharacter(scalar.Value))
            {
                throw CreateXmlMarkupError("An XML processing-instruction target must be a Name", sourceOffset + scalar.Utf16Offset);
            }
        }
    }

    internal static void RequireXmlTokenKind(XmlToken token, XmlTokenKind expected, string description)
    {
        Guard.NotNull(token, "token");
        if (token.Kind != expected)
        {
            throw new ArgumentException("The token is not an XML " + description + " token.", "token");
        }
    }

    internal static FormatException CreateXmlMarkupError(string message, int offset)
    {
        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
