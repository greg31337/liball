using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class XPointerPart
{
    internal string SchemeName;
    internal string? Prefix;
    internal string LocalName;
    internal string SchemeData;

    internal XPointerPart(string schemeName, string? prefix, string localName, string schemeData)
    {
        SchemeName = schemeName;
        Prefix = prefix;
        LocalName = localName;
        SchemeData = schemeData;
    }
}

internal class XPointerExpression
{
    internal bool IsValid;
    internal string? ErrorMessage;
    internal string? Shorthand;
    internal XPointerPart[] Parts;

    internal XPointerExpression(bool isValid, string? errorMessage, string? shorthand, XPointerPart[] parts)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        Shorthand = shorthand;
        Parts = parts;
    }
}

internal class XPointerParser
{
    internal XPointerExpression ParseXPointer(string pointer, XmlVersion version)
    {
        Guard.NotNull(pointer, "pointer");
        QualifiedNameParser nameParser = CreateXPointerNameParser(version);
        if (nameParser.IsValidXmlNcName(pointer))
        {
            return new XPointerExpression(true, null, pointer, []);
        }

        if (pointer.Length == 0 || IsXPointerSpace(pointer[0]))
        {
            return CreateInvalidXPointerExpression("An XPointer must begin with a shorthand name or scheme name.");
        }

        List<XPointerPart> parts = new List<XPointerPart>();
        int offset = 0;
        while (offset < pointer.Length)
        {
            int nameStart = offset;
            while (offset < pointer.Length && pointer[offset] != '(' && !IsXPointerSpace(pointer[offset]))
            {
                offset = offset + 1;
            }

            if (offset == nameStart || offset >= pointer.Length || pointer[offset] != '(')
            {
                return CreateInvalidXPointerExpression("An XPointer scheme name must be followed immediately by '('.");
            }

            string schemeName = pointer.Substring(nameStart, offset - nameStart);
            if (!IsValidXPointerQualifiedName(schemeName, nameParser))
            {
                return CreateInvalidXPointerExpression("An XPointer scheme name must be a valid QName.");
            }

            string? prefix = XPointerSchemePrefix(schemeName);
            string localName = XPointerSchemeLocalName(schemeName);
            offset = offset + 1;
            StringBuilder data = new StringBuilder();
            int depth = 1;
            while (offset < pointer.Length && depth > 0)
            {
                char character = pointer[offset];
                if (character == '^')
                {
                    if (offset + 1 >= pointer.Length || pointer[offset + 1] != '^' && pointer[offset + 1] != '(' && pointer[offset + 1] != ')')
                    {
                        return CreateInvalidXPointerExpression("An XPointer circumflex must escape '^', '(', or ')'.");
                    }

                    data.Append(pointer[offset + 1]);
                    offset = offset + 2;
                }
                else if (character == '(')
                {
                    depth = depth + 1;
                    data.Append(character);
                    offset = offset + 1;
                }
                else if (character == ')')
                {
                    depth = depth - 1;
                    if (depth > 0)
                    {
                        data.Append(character);
                    }

                    offset = offset + 1;
                }
                else
                {
                    data.Append(character);
                    offset = offset + 1;
                }
            }

            if (depth != 0)
            {
                return CreateInvalidXPointerExpression("An XPointer scheme-data parenthesis is not balanced.");
            }

            parts.Add(new XPointerPart(schemeName, prefix, localName, data.ToString()));
            if (offset < pointer.Length)
            {
                if (IsXPointerSpace(pointer[offset]))
                {
                    while (offset < pointer.Length && IsXPointerSpace(pointer[offset]))
                    {
                        offset = offset + 1;
                    }

                    if (offset >= pointer.Length)
                    {
                        return CreateInvalidXPointerExpression("An XPointer cannot end with whitespace.");
                    }
                }
            }
        }

        if (parts.Count == 0)
        {
            return CreateInvalidXPointerExpression("An XPointer scheme-based pointer requires at least one pointer part.");
        }

        return new XPointerExpression(true, null, null, parts.ToArray());
    }

    internal XPointerExpression CreateInvalidXPointerExpression(string message)
    {
        return new XPointerExpression(false, message, null, []);
    }

    internal QualifiedNameParser CreateXPointerNameParser(XmlVersion version)
    {
        XmlNamespaceHandling handling = version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10;
        return new QualifiedNameParser(handling, null);
    }

    internal bool IsValidXPointerQualifiedName(string value, QualifiedNameParser nameParser)
    {
        int colon = value.IndexOf(':');
        if (colon < 0)
        {
            return nameParser.IsValidXmlNcName(value);
        }

        if (colon == 0 || colon == value.Length - 1 || colon != value.LastIndexOf(':'))
        {
            return false;
        }

        return nameParser.IsValidXmlNcName(value.Substring(0, colon)) && nameParser.IsValidXmlNcName(value.Substring(colon + 1));
    }

    internal string? XPointerSchemePrefix(string schemeName)
    {
        int colon = schemeName.IndexOf(':');
        return colon < 0 ? null : schemeName.Substring(0, colon);
    }

    internal string XPointerSchemeLocalName(string schemeName)
    {
        int colon = schemeName.IndexOf(':');
        return colon < 0 ? schemeName : schemeName.Substring(colon + 1);
    }

    internal bool IsXPointerSpace(char character)
    {
        return XmlLexicalCharacterRules.IsXmlSpaceCharacter(character);
    }
}
