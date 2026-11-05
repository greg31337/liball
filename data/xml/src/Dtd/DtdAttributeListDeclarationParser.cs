using System;
using System.Collections.Generic;

namespace xml;

internal class DtdAttributeListDeclarationParser
{
    internal DtdDeclarationReader Reader;
    internal XmlVersion Version;
    internal int DeclarationOffset;

    internal DtdAttributeListDeclarationParser(DtdDeclarationReader reader, XmlVersion version, int declarationOffset)
    {
        Guard.NotNull(reader, "reader");
        Reader = reader;
        Version = version;
        DeclarationOffset = declarationOffset;
    }

    internal XmlAttributeDeclaration[] ParseDtdAttributeListDeclaration()
    {
        RequireDtdAttributeListWhitespace();
        string elementName = ReadRequiredDtdAttributeName("An attribute-list declaration requires an element name");
        List<XmlAttributeDeclaration> declarations = new List<XmlAttributeDeclaration>();
        while (!Reader.IsDtdDeclarationEnd())
        {
            RequireDtdAttributeListWhitespace();
            if (Reader.IsDtdDeclarationEnd())
            {
                return declarations.ToArray();
            }

            string attributeName = ReadRequiredDtdAttributeName("An attribute definition requires an attribute name");
            RequireDtdAttributeListWhitespace();
            XmlAttributeDeclaration declaration = new XmlAttributeDeclaration();
            declaration.ElementName = CreateDtdAttributeName(elementName);
            declaration.Name = CreateDtdAttributeName(attributeName);
            declaration.AllowedValues = ParseDtdAttributeType(declaration);
            RequireDtdAttributeListWhitespace();
            ParseDtdAttributeDefault(declaration);
            declaration.IsId = declaration.TypeName == "ID";
            declaration.IsNamespaceDeclaration = attributeName == "xmlns" || attributeName.StartsWith("xmlns:", StringComparison.Ordinal);
            declaration.Range = null;
            declarations.Add(declaration);
        }

        return declarations.ToArray();
    }

    internal string[]? ParseDtdAttributeType(XmlAttributeDeclaration declaration)
    {
        DtdTokenEntry type = ReadRequiredDtdAttributeEntry("An attribute definition requires an attribute type");
        if (type.Token.Kind == DtdTokenKind.Symbol && type.Token.Value == "(")
        {
            declaration.TypeName = "ENUMERATION";
            return ParseDtdAttributeAllowedValues(false);
        }

        if (type.Token.Kind != DtdTokenKind.Name || type.Token.Name == null)
        {
            throw CreateDtdAttributeListError("An attribute definition requires a recognized attribute type", type.Token.StartUtf16Offset);
        }

        string typeName = type.Token.Name;
        if (typeName == "CDATA" || typeName == "ID" || typeName == "IDREF" || typeName == "IDREFS" || typeName == "ENTITY" || typeName == "ENTITIES" || typeName == "NMTOKEN" || typeName == "NMTOKENS")
        {
            declaration.TypeName = typeName;
            return null;
        }

        if (typeName == "NOTATION")
        {
            declaration.TypeName = typeName;
            RequireDtdAttributeListWhitespace();
            DtdTokenEntry opening = ReadRequiredDtdAttributeEntry("A NOTATION attribute type requires a parenthesized name list");
            if (opening.Token.Kind != DtdTokenKind.Symbol || opening.Token.Value != "(")
            {
                throw CreateDtdAttributeListError("A NOTATION attribute type requires a parenthesized name list", opening.Token.StartUtf16Offset);
            }

            return ParseDtdAttributeAllowedValues(true);
        }

        throw CreateDtdAttributeListError("The attribute type is not recognized", type.Token.StartUtf16Offset);
    }

    internal string[] ParseDtdAttributeAllowedValues(bool namesOnly)
    {
        List<string> values = new List<string>();
        Reader.ConsumeDtdDeclarationWhitespace();
        while (true)
        {
            DtdTokenEntry value = ReadRequiredDtdAttributeEntry("An enumerated attribute type requires at least one token");
            bool valid = value.Token.Kind == DtdTokenKind.Name || !namesOnly && value.Token.Kind == DtdTokenKind.Nmtoken;
            if (!valid || value.Token.Value == null)
            {
                throw CreateDtdAttributeListError(namesOnly ? "A NOTATION attribute type permits only XML Names" : "An enumeration permits only XML Nmtokens", value.Token.StartUtf16Offset);
            }

            if (values.Contains(value.Token.Value))
            {
                throw CreateDtdAttributeListError("An enumerated attribute type cannot contain duplicate tokens", value.Token.StartUtf16Offset);
            }

            values.Add(value.Token.Value);
            Reader.ConsumeDtdDeclarationWhitespace();
            DtdTokenEntry delimiter = ReadRequiredDtdAttributeEntry("An enumerated attribute type requires a closing ')'");
            if (delimiter.Token.Kind == DtdTokenKind.Symbol && delimiter.Token.Value == ")")
            {
                return values.ToArray();
            }

            if (delimiter.Token.Kind != DtdTokenKind.Symbol || delimiter.Token.Value != "|")
            {
                throw CreateDtdAttributeListError("Enumerated attribute tokens must be separated by '|'", delimiter.Token.StartUtf16Offset);
            }

            Reader.ConsumeDtdDeclarationWhitespace();
        }
    }

    internal void ParseDtdAttributeDefault(XmlAttributeDeclaration declaration)
    {
        DtdTokenEntry first = ReadRequiredDtdAttributeEntry("An attribute definition requires a default declaration");
        if (first.Token.Kind == DtdTokenKind.QuotedLiteral)
        {
            declaration.DefaultKind = "DEFAULT";
            declaration.DefaultValue = first.Token.Value == null ? "" : first.Token.Value;
            ValidateDtdAttributeDefaultValue(declaration.DefaultValue, first.Token.StartUtf16Offset + 1);
            return;
        }

        if (first.Token.Kind != DtdTokenKind.Symbol || first.Token.Value != "#")
        {
            throw CreateDtdAttributeListError("An attribute default must be #REQUIRED, #IMPLIED, #FIXED, or a quoted value", first.Token.StartUtf16Offset);
        }

        DtdTokenEntry kind = ReadRequiredDtdAttributeEntry("An attribute default '#' must be followed by REQUIRED, IMPLIED, or FIXED");
        if (kind.Token.Kind != DtdTokenKind.Name || kind.Token.Name == null)
        {
            throw CreateDtdAttributeListError("An attribute default '#' must be followed by REQUIRED, IMPLIED, or FIXED", kind.Token.StartUtf16Offset);
        }

        if (kind.Token.Name == "REQUIRED" || kind.Token.Name == "IMPLIED")
        {
            declaration.DefaultKind = "#" + kind.Token.Name;
            declaration.DefaultValue = null;
            return;
        }

        if (kind.Token.Name != "FIXED")
        {
            throw CreateDtdAttributeListError("An attribute default '#' must be followed by REQUIRED, IMPLIED, or FIXED", kind.Token.StartUtf16Offset);
        }

        declaration.DefaultKind = "#FIXED";
        RequireDtdAttributeListWhitespace();
        DtdTokenEntry value = ReadRequiredDtdAttributeEntry("A #FIXED attribute default requires a quoted value");
        if (value.Token.Kind != DtdTokenKind.QuotedLiteral)
        {
            throw CreateDtdAttributeListError("A #FIXED attribute default requires a quoted value", value.Token.StartUtf16Offset);
        }

        declaration.DefaultValue = value.Token.Value == null ? "" : value.Token.Value;
        ValidateDtdAttributeDefaultValue(declaration.DefaultValue, value.Token.StartUtf16Offset + 1);
    }

    internal void ValidateDtdAttributeDefaultValue(string value, int sourceOffset)
    {
        int position = 0;
        while (position < value.Length)
        {
            if (value[position] == '<')
            {
                throw CreateDtdAttributeListError("An attribute default value cannot contain '<'", sourceOffset + position);
            }

            if (value[position] != '&')
            {
                position = position + 1;
                continue;
            }

            XmlLexicalScanner scanner = new XmlLexicalScanner(value.AsMemory(), Version);
            if (position + 1 < value.Length && value[position + 1] == '#')
            {
                scanner.ResetXmlLexicalOffset(position);
                scanner.ScanXmlCharacterReference();
                position = scanner.CurrentXmlLexicalOffset();
            }
            else
            {
                scanner.ResetXmlLexicalOffset(position + 1);
                scanner.ScanXmlName();
                position = scanner.CurrentXmlLexicalOffset();
                if (position >= value.Length || value[position] != ';')
                {
                    throw CreateDtdAttributeListError("A general entity reference in an attribute default must end with ';'", sourceOffset + position);
                }

                position = position + 1;
            }
        }
    }

    internal XmlName CreateDtdAttributeName(string qualifiedName)
    {
        XmlName name = new XmlName();
        name.QualifiedName = qualifiedName;
        int colon = qualifiedName.IndexOf(':');
        if (colon > 0 && colon < qualifiedName.Length - 1)
        {
            name.Prefix = qualifiedName.Substring(0, colon);
            name.LocalName = qualifiedName.Substring(colon + 1);
        }
        else
        {
            name.Prefix = null;
            name.LocalName = qualifiedName;
        }

        return name;
    }

    internal void RequireDtdAttributeListWhitespace()
    {
        if (!Reader.ConsumeDtdDeclarationWhitespace())
        {
            DtdTokenEntry? entry = Reader.PeekDtdDeclarationEntry();
            throw CreateDtdAttributeListError("Whitespace is required in an attribute-list declaration", entry == null ? DeclarationOffset : entry.Token.StartUtf16Offset);
        }
    }

    internal DtdTokenEntry ReadRequiredDtdAttributeEntry(string message)
    {
        DtdTokenEntry? entry = Reader.ReadDtdDeclarationEntry();
        if (entry == null)
        {
            throw CreateDtdAttributeListError(message, DeclarationOffset);
        }

        return entry;
    }

    internal string ReadRequiredDtdAttributeName(string message)
    {
        DtdTokenEntry entry = ReadRequiredDtdAttributeEntry(message);
        if (entry.Token.Kind != DtdTokenKind.Name || entry.Token.Name == null)
        {
            throw CreateDtdAttributeListError(message, entry.Token.StartUtf16Offset);
        }

        return entry.Token.Name;
    }

    internal FormatException CreateDtdAttributeListError(string message, int offset)
    {
        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
