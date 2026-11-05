using System;
using System.Text;

namespace xml;

internal class XdmStringDatatype
{
    internal XdmTypeSystem TypeSystem;

    internal XdmStringDatatype()
    {
        TypeSystem = new XdmTypeSystem();
    }

    internal XdmAtomicValue ParseXdmStringAtomic(string lexicalValue, XdmAtomicType targetType)
    {
        XdmStringParseResult result = TryParseXdmStringAtomic(lexicalValue, targetType);
        if (!result.Success || result.Value == null)
        {
            string message = result.ErrorMessage == null ? "The lexical value is invalid for the requested string datatype." : result.ErrorMessage;
            throw CreateXdmStringLexicalError(message);
        }

        return result.Value;
    }

    internal XdmStringParseResult TryParseXdmStringAtomic(string lexicalValue, XdmAtomicType targetType)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        RequireXdmStringDatatype(targetType);
        string normalized = NormalizeXdmStringWhitespace(lexicalValue, targetType);
        if (!IsValidXdmStringValue(normalized, targetType))
        {
            string? optionalLocalName = TypeSystem.GetXdmAtomicTypeDefinition(targetType).Name.LocalName;
            string localName = optionalLocalName == null ? targetType.ToString() : optionalLocalName;
            return new XdmStringParseResult(false, null, "The lexical value is not valid for xs:" + localName + ".");
        }

        XdmAtomicValue value = new XdmAtomicValue();
        value.Type = targetType;
        value.LexicalValue = lexicalValue;
        value.StringValue = normalized;
        value.CanonicalValue = normalized;
        value.IsImmutable = true;
        return new XdmStringParseResult(true, value, null);
    }

    internal string NormalizeXdmStringWhitespace(string value, XdmAtomicType targetType)
    {
        Guard.NotNull(value, "value");
        RequireXdmStringDatatype(targetType);
        if (targetType == XdmAtomicType.String)
        {
            return value;
        }

        if (targetType == XdmAtomicType.NormalizedString)
        {
            return ReplaceXdmStringWhitespace(value);
        }

        return CollapseXdmStringWhitespace(value);
    }

    internal bool IsValidXdmStringLexical(string lexicalValue, XdmAtomicType targetType)
    {
        Guard.NotNull(lexicalValue, "lexicalValue");
        RequireXdmStringDatatype(targetType);
        string normalized = NormalizeXdmStringWhitespace(lexicalValue, targetType);
        return IsValidXdmStringValue(normalized, targetType);
    }

    internal string CanonicalizeXdmStringValue(string value, XdmAtomicType targetType)
    {
        Guard.NotNull(value, "value");
        RequireXdmStringDatatype(targetType);
        if (!IsValidXdmStringValue(value, targetType))
        {
            throw CreateXdmStringLexicalError("The value is not in the value space of the requested string datatype.");
        }

        return value;
    }

    internal string ReplaceXdmStringWhitespace(string value)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\t' || character == '\n' || character == '\r')
            {
                result.Append(' ');
            }
            else
            {
                result.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string CollapseXdmStringWhitespace(string value)
    {
        StringBuilder result = new StringBuilder(value.Length);
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == ' ' || character == '\t' || character == '\n' || character == '\r')
            {
                if (result.Length != 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    result.Append(' ');
                    pendingSpace = false;
                }

                result.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal bool IsValidXdmStringValue(string value, XdmAtomicType targetType)
    {
        Guard.NotNull(value, "value");
        RequireXdmStringDatatype(targetType);
        if (!string.Equals(value, NormalizeXdmStringWhitespace(value, targetType), StringComparison.Ordinal))
        {
            return false;
        }

        if (targetType == XdmAtomicType.String || targetType == XdmAtomicType.NormalizedString || targetType == XdmAtomicType.Token)
        {
            return IsWellFormedXdmUtf16(value);
        }

        if (targetType == XdmAtomicType.Language)
        {
            return IsValidXdmLanguage(value);
        }

        if (targetType == XdmAtomicType.Nmtoken)
        {
            return IsValidXdmNmtoken(value);
        }

        if (targetType == XdmAtomicType.Name)
        {
            return IsValidXdmName(value, false);
        }

        return IsValidXdmName(value, true);
    }

    internal bool IsValidXdmLanguage(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int segmentLength = 0;
        bool firstSegment = true;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '-')
            {
                if (segmentLength == 0 || segmentLength > 8)
                {
                    return false;
                }

                segmentLength = 0;
                firstSegment = false;
            }
            else
            {
                bool letter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
                bool digit = character >= '0' && character <= '9';
                if (!letter && (firstSegment || !digit))
                {
                    return false;
                }

                segmentLength = segmentLength + 1;
                if (segmentLength > 8)
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return segmentLength != 0;
    }

    internal bool IsValidXdmName(string value, bool ncName)
    {
        if (value.Length == 0 || !IsWellFormedXdmUtf16(value))
        {
            return false;
        }

        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        UnicodeScalar scalar = scanner.ReadUnicodeScalar();
        if (!Xml10CharacterRules.IsXml10NameStartCharacter(scalar.Value) || ncName && scalar.Value == ':')
        {
            return false;
        }

        while (!scanner.IsUnicodeScalarEnd())
        {
            scalar = scanner.ReadUnicodeScalar();
            if (!Xml10CharacterRules.IsXml10NameCharacter(scalar.Value) || ncName && scalar.Value == ':')
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsValidXdmNmtoken(string value)
    {
        if (value.Length == 0 || !IsWellFormedXdmUtf16(value))
        {
            return false;
        }

        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            if (!Xml10CharacterRules.IsXml10NameCharacter(scalar.Value))
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsWellFormedXdmUtf16(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character >= 0xD800 && character <= 0xDBFF)
            {
                if (index + 1 >= value.Length)
                {
                    return false;
                }

                int low = value[index + 1];
                if (low < 0xDC00 || low > 0xDFFF)
                {
                    return false;
                }

                int scalar = 0x10000 + (character - 0xD800) * 0x400 + low - 0xDC00;
                if (!Xml10CharacterRules.IsLegalXml10Character(scalar))
                {
                    return false;
                }

                index = index + 2;
            }
            else
            {
                if (character >= 0xDC00 && character <= 0xDFFF)
                {
                    return false;
                }

                if (!Xml10CharacterRules.IsLegalXml10Character(character))
                {
                    return false;
                }

                index = index + 1;
            }
        }

        return true;
    }

    internal void RequireXdmStringDatatype(XdmAtomicType targetType)
    {
        if (!TypeSystem.IsXdmStringDerivedAtomicType(targetType))
        {
            throw new ArgumentException("The requested atomic type is not derived from xs:string.", "targetType");
        }
    }

    internal XPathDynamicException CreateXdmStringLexicalError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.FORG0001;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
