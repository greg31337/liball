using System;
using System.Text;

namespace xml;

/// <summary>Escapes and unescapes values for XML output contexts.</summary>
public class XmlEscaper : IXmlEscaper
{
    internal string? SerializationInvalidCharacterCode;
    internal string? SerializationMalformedMarkupCode;
    internal string InvalidCharacterReplacement;

    /// <summary>Creates an XML escaper.</summary>
    public XmlEscaper()
    {
        SerializationInvalidCharacterCode = null;
        SerializationMalformedMarkupCode = null;
        InvalidCharacterReplacement = "\uFFFD";
    }

    internal void ConfigureXmlSerializationErrorCodes(string invalidCharacterCode, string malformedMarkupCode)
    {
        SerializationInvalidCharacterCode = invalidCharacterCode;
        SerializationMalformedMarkupCode = malformedMarkupCode;
    }

    internal void ConfigureXmlSerializationInvalidCharacterReplacement(string? replacement)
    {
        InvalidCharacterReplacement = replacement == null ? "\uFFFD" : replacement;
    }

    /// <summary>Escapes an XML value.</summary>
    public string EscapeXmlValue(string value, XmlVersion version, XmlCharacterEscapeContext context, XmlInvalidCharacterPolicy policy, char quote)
    {
        Guard.NotNull(value, "value");
        ValidateXmlEscapeArguments(version, context, policy, quote);
        if (context != XmlCharacterEscapeContext.Text && !IsXmlAttributeContext(context))
        {
            return new XmlExtendedEscaper(this).EscapeExtendedXmlValue(value, version, context, policy, quote);
        }

        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        int previous = -1;
        int beforePrevious = -1;
        while (position < value.Length)
        {
            int scalar;
            int characterCount;
            int first = value[position];
            if (first >= 0xD800 && first <= 0xDBFF)
            {
                if (position + 1 < value.Length && value[position + 1] >= 0xDC00 && value[position + 1] <= 0xDFFF)
                {
                    int second = value[position + 1];
                    scalar = 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
                    characterCount = 2;
                }
                else
                {
                    AppendInvalidXmlScalar(result, first, position, version, policy);
                    position = position + 1;
                    beforePrevious = previous;
                    previous = 0xFFFD;
                    continue;
                }
            }
            else if (first >= 0xDC00 && first <= 0xDFFF)
            {
                AppendInvalidXmlScalar(result, first, position, version, policy);
                position = position + 1;
                beforePrevious = previous;
                previous = 0xFFFD;
                continue;
            }
            else
            {
                scalar = first;
                characterCount = 1;
            }

            bool legal = version == XmlVersion.Xml10 ? Xml10CharacterRules.IsLegalXml10Character(scalar) : Xml11CharacterRules.IsLegalXml11Character(scalar);
            bool restricted = version == XmlVersion.Xml11 && Xml11CharacterRules.IsRestrictedXml11Character(scalar);
            if (!legal || restricted)
            {
                AppendInvalidXmlScalar(result, scalar, position, version, policy);
            }
            else if (scalar == '&')
            {
                result.Append("&amp;");
            }
            else if (scalar == '<')
            {
                result.Append("&lt;");
            }
            else if (IsXmlAttributeContext(context) && scalar == quote)
            {
                result.Append(quote == '"' ? "&quot;" : "&apos;");
            }
            else if (MustPreserveXmlCharacterWithReference(scalar, version, context))
            {
                AppendXmlCharacterReference(result, scalar);
            }
            else if (context == XmlCharacterEscapeContext.Text && scalar == '>' && previous == ']' && beforePrevious == ']')
            {
                result.Append("&gt;");
            }
            else
            {
                result.Append(value, position, characterCount);
            }

            position = position + characterCount;
            beforePrevious = previous;
            previous = scalar;
        }

        return result.ToString();
    }

    /// <summary>Unescapes an XML value.</summary>
    public string UnescapeXmlValue(string value, XmlVersion version, XmlCharacterEscapeContext context)
    {
        Guard.NotNull(value, "value");
        ValidateXmlUnescapeArguments(version, context);
        if (context != XmlCharacterEscapeContext.Text && !IsXmlAttributeContext(context))
        {
            return new XmlExtendedEscaper(this).UnescapeExtendedXmlValue(value, version, context);
        }

        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        while (position < value.Length)
        {
            if (value[position] == '&')
            {
                XmlCharacterReference reference = ParseXmlEscapeReference(value, position, version);
                result.Append(reference.ReplacementText);
                position = position + reference.SourceCharacterCount;
                continue;
            }

            int scalar;
            int characterCount;
            int first = value[position];
            if (first >= 0xD800 && first <= 0xDBFF && position + 1 < value.Length && value[position + 1] >= 0xDC00 && value[position + 1] <= 0xDFFF)
            {
                int second = value[position + 1];
                scalar = 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
                characterCount = 2;
            }
            else
            {
                scalar = first;
                characterCount = 1;
            }

            bool direct = version == XmlVersion.Xml10 ? Xml10CharacterRules.IsLegalXml10Character(scalar) : Xml11CharacterRules.IsDirectXml11Character(scalar);
            if (!direct)
            {
                throw CreateXmlEscapeError("The escaped value contains an illegal direct XML character at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacter);
            }

            if (scalar == '<' || IsXmlAttributeContext(context) && scalar == XmlEscapeContextQuote(context))
            {
                throw CreateXmlEscapeError("The escaped value contains an unescaped XML delimiter at UTF-16 offset " + position + ".", XmlErrorCodes.MalformedMarkup);
            }

            if (context == XmlCharacterEscapeContext.Text && scalar == '>' && position >= 2 && value[position - 1] == ']' && value[position - 2] == ']')
            {
                throw CreateXmlEscapeError("The escaped text contains the forbidden ']]>' delimiter.", XmlErrorCodes.MalformedMarkup);
            }

            result.Append(value, position, characterCount);
            position = position + characterCount;
        }

        return result.ToString();
    }

    internal void ValidateXmlEscapeArguments(XmlVersion version, XmlCharacterEscapeContext context, XmlInvalidCharacterPolicy policy, char quote)
    {
        ValidateXmlUnescapeArguments(version, context);
        if (policy != XmlInvalidCharacterPolicy.Error && policy != XmlInvalidCharacterPolicy.CharacterReference && policy != XmlInvalidCharacterPolicy.Replace)
        {
            throw new ArgumentOutOfRangeException("policy");
        }

        if (context == XmlCharacterEscapeContext.PublicIdentifier)
        {
            if (quote != '"' && quote != '\'')
            {
                throw new ArgumentException("A public identifier requires a single or double quote delimiter.", "quote");
            }
        }
        else
        {
            char requiredQuote = XmlEscapeContextQuote(context);
            if (requiredQuote == '\0')
            {
                if (quote != '\0')
                {
                    throw new ArgumentException("The selected escape context does not use a quote delimiter.", "quote");
                }
            }
            else if (quote != requiredQuote)
            {
                throw new ArgumentException("The quote delimiter does not match the escape context.", "quote");
            }
        }
    }

    internal void ValidateXmlUnescapeArguments(XmlVersion version, XmlCharacterEscapeContext context)
    {
        if (version != XmlVersion.Xml10 && version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        if (context < XmlCharacterEscapeContext.Text || context > XmlCharacterEscapeContext.Json)
        {
            throw new ArgumentOutOfRangeException("context");
        }
    }

    internal bool IsXmlAttributeContext(XmlCharacterEscapeContext context)
    {
        return context == XmlCharacterEscapeContext.AttributeDoubleQuoted || context == XmlCharacterEscapeContext.AttributeSingleQuoted;
    }

    internal char XmlEscapeContextQuote(XmlCharacterEscapeContext context)
    {
        if (context == XmlCharacterEscapeContext.AttributeDoubleQuoted || context == XmlCharacterEscapeContext.SystemLiteralDoubleQuoted || context == XmlCharacterEscapeContext.XPathStringDoubleQuoted || context == XmlCharacterEscapeContext.Json)
        {
            return '"';
        }

        if (context == XmlCharacterEscapeContext.AttributeSingleQuoted || context == XmlCharacterEscapeContext.SystemLiteralSingleQuoted || context == XmlCharacterEscapeContext.XPathStringSingleQuoted)
        {
            return '\'';
        }

        return '\0';
    }

    internal bool MustPreserveXmlCharacterWithReference(int scalar, XmlVersion version, XmlCharacterEscapeContext context)
    {
        if (scalar == 0xD)
        {
            return true;
        }

        if (IsXmlAttributeContext(context) && (scalar == 0x9 || scalar == 0xA))
        {
            return true;
        }

        return version == XmlVersion.Xml11 && (scalar == 0x85 || scalar == 0x2028);
    }

    internal void AppendInvalidXmlScalar(StringBuilder result, int scalar, int offset, XmlVersion version, XmlInvalidCharacterPolicy policy)
    {
        if (policy == XmlInvalidCharacterPolicy.Replace)
        {
            result.Append(InvalidCharacterReplacement);
            return;
        }

        bool referenceLegal = version == XmlVersion.Xml10 ? Xml10CharacterRules.IsLegalXml10Character(scalar) : Xml11CharacterRules.IsLegalXml11Character(scalar);
        if (policy == XmlInvalidCharacterPolicy.CharacterReference && referenceLegal)
        {
            AppendXmlCharacterReference(result, scalar);
            return;
        }

        throw CreateXmlEscapeError("The value contains a character that cannot be written directly in XML " + (version == XmlVersion.Xml10 ? "1.0" : "1.1") + " at UTF-16 offset " + offset + ".", XmlErrorCodes.InvalidCharacter);
    }

    internal void AppendXmlCharacterReference(StringBuilder result, int scalar)
    {
        result.Append("&#x");
        result.Append(scalar.ToString("X"));
        result.Append(';');
    }

    internal XmlCharacterReference ParseXmlEscapeReference(string value, int position, XmlVersion version)
    {
        if (position + 1 < value.Length && value[position + 1] == '#')
        {
            return ParseXmlNumericEscapeReference(value, position, version);
        }

        int end = value.IndexOf(';', position + 1);
        if (end < 0)
        {
            throw CreateXmlEscapeError("An XML entity reference must end with ';' at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacterReference);
        }

        string name = value.Substring(position + 1, end - position - 1);
        string replacement;
        int scalar;
        if (string.Equals(name, "amp", StringComparison.Ordinal))
        {
            replacement = "&";
            scalar = '&';
        }
        else if (string.Equals(name, "lt", StringComparison.Ordinal))
        {
            replacement = "<";
            scalar = '<';
        }
        else if (string.Equals(name, "gt", StringComparison.Ordinal))
        {
            replacement = ">";
            scalar = '>';
        }
        else if (string.Equals(name, "apos", StringComparison.Ordinal))
        {
            replacement = "'";
            scalar = '\'';
        }
        else if (string.Equals(name, "quot", StringComparison.Ordinal))
        {
            replacement = "\"";
            scalar = '"';
        }
        else
        {
            throw CreateXmlEscapeError("The entity reference '&" + name + ";' is not a predefined XML entity.", XmlErrorCodes.InvalidCharacterReference);
        }

        return new XmlCharacterReference(scalar, end - position + 1, replacement, false);
    }

    internal XmlCharacterReference ParseXmlNumericEscapeReference(string value, int position, XmlVersion version)
    {
        int current = position + 2;
        int numberBase = 10;
        if (current < value.Length && value[current] == 'x')
        {
            numberBase = 16;
            current = current + 1;
        }

        int digitCount = 0;
        int scalar = 0;
        while (current < value.Length && value[current] != ';')
        {
            int digit = Xml10CharacterReferenceParser.CharacterReferenceDigit(value[current], numberBase);
            if (digit < 0)
            {
                throw CreateXmlEscapeError("A character reference contains an invalid digit at UTF-16 offset " + current + ".", XmlErrorCodes.InvalidCharacterReference);
            }

            if (scalar > (0x10FFFF - digit) / numberBase)
            {
                throw CreateXmlEscapeError("A character reference exceeds the Unicode scalar range at UTF-16 offset " + current + ".", XmlErrorCodes.InvalidCharacterReference);
            }

            scalar = scalar * numberBase + digit;
            digitCount = digitCount + 1;
            current = current + 1;
        }

        if (digitCount == 0)
        {
            throw CreateXmlEscapeError("A character reference requires at least one digit at UTF-16 offset " + current + ".", XmlErrorCodes.InvalidCharacterReference);
        }

        if (current >= value.Length || value[current] != ';')
        {
            throw CreateXmlEscapeError("A character reference must end with ';' at UTF-16 offset " + current + ".", XmlErrorCodes.InvalidCharacterReference);
        }

        bool legal = version == XmlVersion.Xml10 ? Xml10CharacterRules.IsLegalXml10Character(scalar) : Xml11CharacterRules.IsLegalXml11Character(scalar);
        if (!legal)
        {
            throw CreateXmlEscapeError("A character reference does not identify a legal XML character at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacterReference);
        }

        string replacement = char.ConvertFromUtf32(scalar);
        bool restricted = version == XmlVersion.Xml11 && Xml11CharacterRules.IsRestrictedXml11Character(scalar);
        return new XmlCharacterReference(scalar, current - position + 1, replacement, restricted);
    }

    internal XmlSerializationException CreateXmlEscapeError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        if (SerializationInvalidCharacterCode != null && string.Equals(code, XmlErrorCodes.InvalidCharacter, StringComparison.Ordinal))
        {
            diagnostic.Code = SerializationInvalidCharacterCode;
        }
        else if (SerializationMalformedMarkupCode != null && string.Equals(code, XmlErrorCodes.MalformedMarkup, StringComparison.Ordinal))
        {
            diagnostic.Code = SerializationMalformedMarkupCode;
        }
        else
        {
            diagnostic.Code = code;
        }
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
