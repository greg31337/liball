using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class XmlEscapingScalar
{
    internal int Value;
    internal int CharacterCount;
    internal bool IsValid;

    internal XmlEscapingScalar(int value, int characterCount, bool isValid)
    {
        Value = value;
        CharacterCount = characterCount;
        IsValid = isValid;
    }
}

internal class XmlExtendedEscaper
{
    internal XmlEscaper Owner;

    internal XmlExtendedEscaper(XmlEscaper owner)
    {
        Owner = owner;
    }

    internal string EscapeExtendedXmlValue(string value, XmlVersion version, XmlCharacterEscapeContext context, XmlInvalidCharacterPolicy policy, char quote)
    {
        if (context == XmlCharacterEscapeContext.Uri)
        {
            return EscapeXmlUriComponent(value, policy);
        }

        if (context == XmlCharacterEscapeContext.Json)
        {
            return EscapeXmlJsonString(value, policy);
        }

        return EscapeXmlConstructContent(value, version, context, policy, quote);
    }

    internal string UnescapeExtendedXmlValue(string value, XmlVersion version, XmlCharacterEscapeContext context)
    {
        if (context == XmlCharacterEscapeContext.Uri)
        {
            return UnescapeXmlUriComponent(value);
        }

        if (context == XmlCharacterEscapeContext.Json)
        {
            return UnescapeXmlJsonString(value);
        }

        if (context == XmlCharacterEscapeContext.XPathStringDoubleQuoted || context == XmlCharacterEscapeContext.XPathStringSingleQuoted)
        {
            return UnescapeXmlXPathString(value, version, context);
        }

        return UnescapeXmlConstructContent(value, version, context);
    }

    internal string EscapeXmlConstructContent(string value, XmlVersion version, XmlCharacterEscapeContext context, XmlInvalidCharacterPolicy policy, char quote)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        while (position < value.Length)
        {
            XmlEscapingScalar scalar = ReadXmlEscapingScalar(value, position);
            bool direct = scalar.IsValid && (version == XmlVersion.Xml10 ? Xml10CharacterRules.IsLegalXml10Character(scalar.Value) : Xml11CharacterRules.IsDirectXml11Character(scalar.Value));
            if (!direct)
            {
                AppendXmlConstructReplacement(result, scalar.Value, position, context, policy);
                position = position + scalar.CharacterCount;
                continue;
            }

            if (context == XmlCharacterEscapeContext.PublicIdentifier && !XmlLexicalCharacterRules.IsXmlPublicIdentifierCharacter(scalar.Value))
            {
                AppendXmlConstructReplacement(result, scalar.Value, position, context, policy);
            }
            else if ((context == XmlCharacterEscapeContext.SystemLiteralDoubleQuoted || context == XmlCharacterEscapeContext.SystemLiteralSingleQuoted) && scalar.Value == quote)
            {
                throw Owner.CreateXmlEscapeError("A system literal contains its active quote delimiter at UTF-16 offset " + position + ".", XmlErrorCodes.MalformedMarkup);
            }
            else if (context == XmlCharacterEscapeContext.PublicIdentifier && scalar.Value == quote)
            {
                throw Owner.CreateXmlEscapeError("A public identifier contains its active quote delimiter at UTF-16 offset " + position + ".", XmlErrorCodes.MalformedMarkup);
            }
            else if ((context == XmlCharacterEscapeContext.XPathStringDoubleQuoted || context == XmlCharacterEscapeContext.XPathStringSingleQuoted) && scalar.Value == quote)
            {
                result.Append(quote);
                result.Append(quote);
            }
            else
            {
                result.Append(value, position, scalar.CharacterCount);
            }

            position = position + scalar.CharacterCount;
        }

        string escaped = result.ToString();
        if (context == XmlCharacterEscapeContext.Comment && (escaped.Contains("--", StringComparison.Ordinal) || escaped.EndsWith("-", StringComparison.Ordinal)))
        {
            throw Owner.CreateXmlEscapeError("XML comment content cannot contain '--' or end with '-'.", XmlErrorCodes.MalformedMarkup);
        }

        if (context == XmlCharacterEscapeContext.ProcessingInstruction && escaped.Contains("?>", StringComparison.Ordinal))
        {
            throw Owner.CreateXmlEscapeError("XML processing-instruction content cannot contain '?>'.", XmlErrorCodes.MalformedMarkup);
        }

        if ((context == XmlCharacterEscapeContext.SystemLiteralDoubleQuoted || context == XmlCharacterEscapeContext.SystemLiteralSingleQuoted) && escaped.Contains("#", StringComparison.Ordinal))
        {
            throw Owner.CreateXmlEscapeError("An XML system identifier cannot contain a fragment identifier.", XmlErrorCodes.MalformedMarkup);
        }

        if (context == XmlCharacterEscapeContext.CData)
        {
            return escaped.Replace("]]>", "]]]]><![CDATA[>", StringComparison.Ordinal);
        }

        return escaped;
    }

    internal string UnescapeXmlConstructContent(string value, XmlVersion version, XmlCharacterEscapeContext context)
    {
        string result = value;
        if (context == XmlCharacterEscapeContext.CData)
        {
            string split = "]]]]><![CDATA[>";
            string remaining = value.Replace(split, "", StringComparison.Ordinal);
            if (remaining.Contains("]]>", StringComparison.Ordinal))
            {
                throw Owner.CreateXmlEscapeError("CDATA content contains an unsplit ']]>' delimiter.", XmlErrorCodes.MalformedMarkup);
            }

            result = value.Replace(split, "]]>", StringComparison.Ordinal);
        }
        else if (context == XmlCharacterEscapeContext.Comment && (value.Contains("--", StringComparison.Ordinal) || value.EndsWith("-", StringComparison.Ordinal)))
        {
            throw Owner.CreateXmlEscapeError("XML comment content cannot contain '--' or end with '-'.", XmlErrorCodes.MalformedMarkup);
        }
        else if (context == XmlCharacterEscapeContext.ProcessingInstruction && value.Contains("?>", StringComparison.Ordinal))
        {
            throw Owner.CreateXmlEscapeError("XML processing-instruction content cannot contain '?>'.", XmlErrorCodes.MalformedMarkup);
        }
        else if ((context == XmlCharacterEscapeContext.SystemLiteralDoubleQuoted && value.Contains("\"", StringComparison.Ordinal)) || (context == XmlCharacterEscapeContext.SystemLiteralSingleQuoted && value.Contains("'", StringComparison.Ordinal)))
        {
            throw Owner.CreateXmlEscapeError("A system literal contains its quote delimiter.", XmlErrorCodes.MalformedMarkup);
        }

        ValidateXmlConstructCharacters(result, version, context);
        if ((context == XmlCharacterEscapeContext.SystemLiteralDoubleQuoted || context == XmlCharacterEscapeContext.SystemLiteralSingleQuoted) && result.Contains("#", StringComparison.Ordinal))
        {
            throw Owner.CreateXmlEscapeError("An XML system identifier cannot contain a fragment identifier.", XmlErrorCodes.MalformedMarkup);
        }

        return result;
    }

    internal string UnescapeXmlXPathString(string value, XmlVersion version, XmlCharacterEscapeContext context)
    {
        char quote = context == XmlCharacterEscapeContext.XPathStringDoubleQuoted ? '"' : '\'';
        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        while (position < value.Length)
        {
            if (value[position] == quote)
            {
                if (position + 1 >= value.Length || value[position + 1] != quote)
                {
                    throw Owner.CreateXmlEscapeError("An XPath string contains an unmatched quote delimiter at UTF-16 offset " + position + ".", XmlErrorCodes.MalformedMarkup);
                }

                result.Append(quote);
                position = position + 2;
                continue;
            }

            XmlEscapingScalar scalar = ReadXmlEscapingScalar(value, position);
            result.Append(value, position, scalar.CharacterCount);
            position = position + scalar.CharacterCount;
        }

        string unescaped = result.ToString();
        ValidateXmlConstructCharacters(unescaped, version, context);
        return unescaped;
    }

    internal void ValidateXmlConstructCharacters(string value, XmlVersion version, XmlCharacterEscapeContext context)
    {
        int position = 0;
        while (position < value.Length)
        {
            XmlEscapingScalar scalar = ReadXmlEscapingScalar(value, position);
            bool direct = scalar.IsValid && (version == XmlVersion.Xml10 ? Xml10CharacterRules.IsLegalXml10Character(scalar.Value) : Xml11CharacterRules.IsDirectXml11Character(scalar.Value));
            if (!direct)
            {
                throw Owner.CreateXmlEscapeError("The value contains a character that cannot appear directly in the selected XML construct at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacter);
            }

            if (context == XmlCharacterEscapeContext.PublicIdentifier && !XmlLexicalCharacterRules.IsXmlPublicIdentifierCharacter(scalar.Value))
            {
                throw Owner.CreateXmlEscapeError("A public identifier contains a character outside PubidChar at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacter);
            }

            position = position + scalar.CharacterCount;
        }
    }

    internal void AppendXmlConstructReplacement(StringBuilder result, int scalar, int position, XmlCharacterEscapeContext context, XmlInvalidCharacterPolicy policy)
    {
        if (policy == XmlInvalidCharacterPolicy.Replace)
        {
            result.Append(context == XmlCharacterEscapeContext.PublicIdentifier && Owner.InvalidCharacterReplacement == "\uFFFD" ? "?" : Owner.InvalidCharacterReplacement);
            return;
        }

        throw Owner.CreateXmlEscapeError("The value contains a character that cannot appear in the selected XML construct at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacter);
    }

    internal string EscapeXmlUriComponent(string value, XmlInvalidCharacterPolicy policy)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        while (position < value.Length)
        {
            XmlEscapingScalar scalar = ReadXmlEscapingScalar(value, position);
            int scalarValue = scalar.Value;
            if (!scalar.IsValid)
            {
                if (policy != XmlInvalidCharacterPolicy.Replace)
                {
                    throw Owner.CreateXmlEscapeError("A URI component contains malformed UTF-16 at offset " + position + ".", XmlErrorCodes.InvalidCharacter);
                }

                scalarValue = 0xFFFD;
            }

            if (IsXmlUriUnreservedCharacter(scalarValue))
            {
                result.Append((char)scalarValue);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(char.ConvertFromUtf32(scalarValue));
                int byteIndex = 0;
                while (byteIndex < bytes.Length)
                {
                    result.Append('%');
                    result.Append(bytes[byteIndex].ToString("X2"));
                    byteIndex = byteIndex + 1;
                }
            }

            position = position + scalar.CharacterCount;
        }

        return result.ToString();
    }

    internal string UnescapeXmlUriComponent(string value)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        while (position < value.Length)
        {
            if (value[position] == '%')
            {
                List<byte> octets = new List<byte>();
                while (position < value.Length && value[position] == '%')
                {
                    if (position + 2 >= value.Length)
                    {
                        throw Owner.CreateXmlEscapeError("A URI percent escape is incomplete at offset " + position + ".", XmlErrorCodes.InvalidCharacterReference);
                    }

                    int high = Xml10CharacterReferenceParser.CharacterReferenceDigit(value[position + 1], 16);
                    int low = Xml10CharacterReferenceParser.CharacterReferenceDigit(value[position + 2], 16);
                    if (high < 0 || low < 0)
                    {
                        throw Owner.CreateXmlEscapeError("A URI percent escape contains a non-hexadecimal digit at offset " + position + ".", XmlErrorCodes.InvalidCharacterReference);
                    }

                    octets.Add((byte)(high * 16 + low));
                    position = position + 3;
                }

                AppendDecodedXmlUriOctets(result, octets);
                continue;
            }

            XmlEscapingScalar scalar = ReadXmlEscapingScalar(value, position);
            if (!scalar.IsValid)
            {
                throw Owner.CreateXmlEscapeError("A URI component contains malformed UTF-16 at offset " + position + ".", XmlErrorCodes.InvalidCharacter);
            }

            result.Append(value, position, scalar.CharacterCount);
            position = position + scalar.CharacterCount;
        }

        return result.ToString();
    }

    internal void AppendDecodedXmlUriOctets(StringBuilder result, List<byte> octets)
    {
        int position = 0;
        while (position < octets.Count)
        {
            int first = octets[position];
            int scalar;
            int count;
            int minimum;
            if (first <= 0x7F)
            {
                scalar = first;
                count = 1;
                minimum = 0;
            }
            else if (first >= 0xC2 && first <= 0xDF)
            {
                scalar = first - 0xC0;
                count = 2;
                minimum = 0x80;
            }
            else if (first >= 0xE0 && first <= 0xEF)
            {
                scalar = first - 0xE0;
                count = 3;
                minimum = 0x800;
            }
            else if (first >= 0xF0 && first <= 0xF4)
            {
                scalar = first - 0xF0;
                count = 4;
                minimum = 0x10000;
            }
            else
            {
                throw Owner.CreateXmlEscapeError("A URI percent escape does not contain valid UTF-8.", XmlErrorCodes.InvalidCharacterReference);
            }

            if (position + count > octets.Count)
            {
                throw Owner.CreateXmlEscapeError("A URI percent escape ends inside a UTF-8 sequence.", XmlErrorCodes.InvalidCharacterReference);
            }

            int continuation = 1;
            while (continuation < count)
            {
                int next = octets[position + continuation];
                if (next < 0x80 || next > 0xBF)
                {
                    throw Owner.CreateXmlEscapeError("A URI percent escape does not contain valid UTF-8.", XmlErrorCodes.InvalidCharacterReference);
                }

                scalar = scalar * 64 + next - 0x80;
                continuation = continuation + 1;
            }

            if (scalar < minimum || scalar > 0x10FFFF || scalar >= 0xD800 && scalar <= 0xDFFF)
            {
                throw Owner.CreateXmlEscapeError("A URI percent escape does not identify a Unicode scalar.", XmlErrorCodes.InvalidCharacterReference);
            }

            result.Append(char.ConvertFromUtf32(scalar));
            position = position + count;
        }
    }

    internal bool IsXmlUriUnreservedCharacter(int scalar)
    {
        if (scalar >= 'A' && scalar <= 'Z' || scalar >= 'a' && scalar <= 'z' || scalar >= '0' && scalar <= '9')
        {
            return true;
        }

        return scalar == '-' || scalar == '.' || scalar == '_' || scalar == '~';
    }

    internal string EscapeXmlJsonString(string value, XmlInvalidCharacterPolicy policy)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        while (position < value.Length)
        {
            XmlEscapingScalar scalar = ReadXmlEscapingScalar(value, position);
            if (!scalar.IsValid)
            {
                if (policy != XmlInvalidCharacterPolicy.Replace)
                {
                    throw Owner.CreateXmlEscapeError("A JSON string contains malformed UTF-16 at offset " + position + ".", XmlErrorCodes.InvalidCharacter);
                }

                result.Append("\\uFFFD");
                position = position + scalar.CharacterCount;
                continue;
            }

            if (scalar.Value == '"')
            {
                result.Append("\\\"");
            }
            else if (scalar.Value == '\\')
            {
                result.Append("\\\\");
            }
            else if (scalar.Value == 0x8)
            {
                result.Append("\\b");
            }
            else if (scalar.Value == 0xC)
            {
                result.Append("\\f");
            }
            else if (scalar.Value == 0xA)
            {
                result.Append("\\n");
            }
            else if (scalar.Value == 0xD)
            {
                result.Append("\\r");
            }
            else if (scalar.Value == 0x9)
            {
                result.Append("\\t");
            }
            else if (scalar.Value < 0x20 || scalar.Value == 0x2028 || scalar.Value == 0x2029)
            {
                AppendXmlJsonUnicodeEscape(result, scalar.Value);
            }
            else
            {
                result.Append(value, position, scalar.CharacterCount);
            }

            position = position + scalar.CharacterCount;
        }

        return result.ToString();
    }

    internal string UnescapeXmlJsonString(string value)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int position = 0;
        while (position < value.Length)
        {
            int character = value[position];
            if (character == '\\')
            {
                if (position + 1 >= value.Length)
                {
                    throw Owner.CreateXmlEscapeError("A JSON escape is incomplete at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacterReference);
                }

                char escaped = value[position + 1];
                if (escaped == '"' || escaped == '\\' || escaped == '/')
                {
                    result.Append(escaped);
                    position = position + 2;
                    continue;
                }

                if (escaped == 'b' || escaped == 'f' || escaped == 'n' || escaped == 'r' || escaped == 't')
                {
                    if (escaped == 'b')
                    {
                        result.Append('\b');
                    }
                    else if (escaped == 'f')
                    {
                        result.Append('\f');
                    }
                    else if (escaped == 'n')
                    {
                        result.Append('\n');
                    }
                    else if (escaped == 'r')
                    {
                        result.Append('\r');
                    }
                    else
                    {
                        result.Append('\t');
                    }

                    position = position + 2;
                    continue;
                }

                if (escaped != 'u')
                {
                    throw Owner.CreateXmlEscapeError("A JSON string contains an unknown escape at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacterReference);
                }

                int first = ParseXmlJsonHexCodeUnit(value, position + 2);
                position = position + 6;
                if (first >= 0xD800 && first <= 0xDBFF)
                {
                    if (position + 5 >= value.Length || value[position] != '\\' || value[position + 1] != 'u')
                    {
                        throw Owner.CreateXmlEscapeError("A JSON high surrogate escape is not followed by a low surrogate escape.", XmlErrorCodes.InvalidCharacterReference);
                    }

                    int second = ParseXmlJsonHexCodeUnit(value, position + 2);
                    if (second < 0xDC00 || second > 0xDFFF)
                    {
                        throw Owner.CreateXmlEscapeError("A JSON high surrogate escape is not followed by a low surrogate escape.", XmlErrorCodes.InvalidCharacterReference);
                    }

                    int combinedScalar = 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
                    result.Append(char.ConvertFromUtf32(combinedScalar));
                    position = position + 6;
                    continue;
                }

                if (first >= 0xDC00 && first <= 0xDFFF)
                {
                    throw Owner.CreateXmlEscapeError("A JSON string contains an unpaired low surrogate escape.", XmlErrorCodes.InvalidCharacterReference);
                }

                result.Append((char)first);
                continue;
            }

            XmlEscapingScalar scalar = ReadXmlEscapingScalar(value, position);
            if (!scalar.IsValid || scalar.Value < 0x20 || scalar.Value == '"')
            {
                throw Owner.CreateXmlEscapeError("A JSON string contains an unescaped character at UTF-16 offset " + position + ".", XmlErrorCodes.InvalidCharacter);
            }

            result.Append(value, position, scalar.CharacterCount);
            position = position + scalar.CharacterCount;
        }

        return result.ToString();
    }

    internal void AppendXmlJsonUnicodeEscape(StringBuilder result, int scalar)
    {
        result.Append("\\u");
        result.Append(scalar.ToString("X4"));
    }

    internal int ParseXmlJsonHexCodeUnit(string value, int position)
    {
        if (position + 4 > value.Length)
        {
            throw Owner.CreateXmlEscapeError("A JSON Unicode escape requires four hexadecimal digits.", XmlErrorCodes.InvalidCharacterReference);
        }

        int result = 0;
        int index = 0;
        while (index < 4)
        {
            int digit = Xml10CharacterReferenceParser.CharacterReferenceDigit(value[position + index], 16);
            if (digit < 0)
            {
                throw Owner.CreateXmlEscapeError("A JSON Unicode escape contains a non-hexadecimal digit.", XmlErrorCodes.InvalidCharacterReference);
            }

            result = result * 16 + digit;
            index = index + 1;
        }

        return result;
    }

    internal XmlEscapingScalar ReadXmlEscapingScalar(string value, int position)
    {
        int first = value[position];
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            if (position + 1 < value.Length)
            {
                int second = value[position + 1];
                if (second >= 0xDC00 && second <= 0xDFFF)
                {
                    int scalar = 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
                    return new XmlEscapingScalar(scalar, 2, true);
                }
            }

            return new XmlEscapingScalar(first, 1, false);
        }

        if (first >= 0xDC00 && first <= 0xDFFF)
        {
            return new XmlEscapingScalar(first, 1, false);
        }

        return new XmlEscapingScalar(first, 1, true);
    }
}
