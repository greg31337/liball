using System.Collections.Generic;
using System.Text;

namespace jmap;

internal class MimeParameterValue
{
    internal string? Name;
    internal string? Value;
    internal int Sequence = -1;
    internal bool IsExtended;
}

internal class MimeHeaderValue
{
    internal string? MainValue;
    internal List<MimeParameterValue> Parameters = new List<MimeParameterValue>();
    internal bool IsMalformed;
}

internal class MimeDecodedStringResult
{
    internal string? Value;
    internal bool IsMalformed;
}

internal class MimeHeaderValueParser
{
    internal HeaderValueParser Text = new HeaderValueParser();
    internal AddressHeaderParser Addresses = new AddressHeaderParser();

    internal MimeHeaderValue ParseHeaderValue(string? rawValue)
    {
        MimeHeaderValue result = new MimeHeaderValue();
        if (rawValue == null)
        {
            result.IsMalformed = true;
            return result;
        }

        string unfolded = Text.UnfoldValue(rawValue);
        int separator = FindParameterSeparator(unfolded, 0);
        string main = separator < 0 ? unfolded : unfolded.Substring(0, separator);
        result.MainValue = TrimWhitespace(Addresses.RemoveComments(main)).ToLowerInvariant();
        int index = separator < 0 ? unfolded.Length : separator + 1;
        while (index < unfolded.Length)
        {
            index = SkipWhitespaceAndComments(unfolded, index);
            if (index >= unfolded.Length)
            {
                break;
            }

            int nameStart = index;
            while (index < unfolded.Length && unfolded[index] != '=' && unfolded[index] != ';')
            {
                index = index + 1;
            }

            if (index >= unfolded.Length || unfolded[index] != '=')
            {
                result.IsMalformed = true;
                int next = FindParameterSeparator(unfolded, index);
                index = next < 0 ? unfolded.Length : next + 1;
                continue;
            }

            string name = TrimWhitespace(unfolded.Substring(nameStart, index - nameStart)).ToLowerInvariant();
            index = SkipWhitespaceAndComments(unfolded, index + 1);
            string value;
            if (index < unfolded.Length && unfolded[index] == '"')
            {
                MimeDecodedStringResult quoted = ReadQuotedParameter(unfolded, index);
                value = quoted.Value == null ? "" : quoted.Value;
                result.IsMalformed = result.IsMalformed || quoted.IsMalformed;
                index = FindQuotedParameterEnd(unfolded, index);
                index = SkipWhitespaceAndComments(unfolded, index);
            }
            else
            {
                int end = FindParameterSeparator(unfolded, index);
                int valueEnd = end < 0 ? unfolded.Length : end;
                value = TrimWhitespace(Addresses.RemoveComments(unfolded.Substring(index, valueEnd - index)));
                index = valueEnd;
            }

            MimeParameterValue? parameter = ParseParameterIdentity(name, value);
            if (parameter == null)
            {
                result.IsMalformed = true;
            }
            else
            {
                result.Parameters.Add(parameter);
            }

            if (index < unfolded.Length && unfolded[index] == ';')
            {
                index = index + 1;
            }
        }

        if (result.MainValue == null || result.MainValue.Length == 0)
        {
            result.IsMalformed = true;
        }

        return result;
    }

    internal string? GetSimpleParameter(MimeHeaderValue header, string name)
    {
        foreach (MimeParameterValue parameter in header.Parameters)
        {
            if (parameter.Name != null && parameter.Value != null && parameter.Sequence < 0 &&
                !parameter.IsExtended && string.Equals(parameter.Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return parameter.Value;
            }
        }

        return null;
    }

    internal MimeDecodedStringResult GetDecodedParameter(MimeHeaderValue header, string name)
    {
        MimeDecodedStringResult result = new MimeDecodedStringResult();
        StringBuilder combined = new StringBuilder();
        bool extended = false;
        int sequence = 0;
        MimeParameterValue? piece = FindParameterSequence(header, name, sequence);
        if (piece != null)
        {
            while (piece != null)
            {
                if (CountParameterSequence(header, name, sequence) != 1)
                {
                    result.IsMalformed = true;
                }

                combined.Append(piece.Value);
                extended = extended || piece.IsExtended;
                sequence = sequence + 1;
                piece = FindParameterSequence(header, name, sequence);
            }

            if (HasLaterParameterSequence(header, name, sequence))
            {
                result.IsMalformed = true;
            }
        }
        else
        {
            if (HasLaterParameterSequence(header, name, 0))
            {
                result.IsMalformed = true;
            }

            MimeParameterValue? singleExtended = FindSingleExtendedParameter(header, name);
            if (singleExtended != null)
            {
                if (CountSingleExtendedParameter(header, name) != 1)
                {
                    result.IsMalformed = true;
                }

                combined.Append(singleExtended.Value);
                extended = true;
            }
            else
            {
                string? simple = GetSimpleParameter(header, name);
                if (simple == null)
                {
                    return result;
                }

                if (CountSimpleParameter(header, name) != 1)
                {
                    result.IsMalformed = true;
                }

                string? decodedText = Text.DecodeText(simple);
                result.Value = Text.RemoveDecodedControls(decodedText == null ? simple : decodedText);
                return result;
            }
        }

        if (!extended)
        {
            string? decodedText = Text.DecodeText(combined.ToString());
            result.Value = decodedText == null ? combined.ToString() : decodedText;
            return result;
        }

        MimeDecodedStringResult decodedParameter = DecodeExtendedParameter(combined.ToString());
        decodedParameter.IsMalformed = decodedParameter.IsMalformed || result.IsMalformed;
        return decodedParameter;
    }

    internal MimeDecodedStringResult DecodeExtendedParameter(string value)
    {
        MimeDecodedStringResult result = new MimeDecodedStringResult();
        int firstQuote = FindCharacter(value, '\'', 0);
        int secondQuote = firstQuote < 0 ? -1 : FindCharacter(value, '\'', firstQuote + 1);
        string charset = "utf-8";
        string encoded = value;
        if (firstQuote >= 0 && secondQuote > firstQuote)
        {
            charset = value.Substring(0, firstQuote);
            encoded = value.Substring(secondQuote + 1);
            if (charset.Length == 0)
            {
                charset = "utf-8";
                result.IsMalformed = true;
            }
        }
        else
        {
            result.IsMalformed = true;
        }

        byte[] bytes = DecodePercentBytes(encoded, result);
        string? decoded = Text.DecodeCharset(bytes, charset);
        if (decoded == null)
        {
            result.IsMalformed = true;
            decoded = Text.Utf8.GetString(bytes);
        }

        result.Value = Text.RemoveDecodedControls(decoded.Normalize(NormalizationForm.FormC));
        return result;
    }

    internal byte[] DecodePercentBytes(string value, MimeDecodedStringResult result)
    {
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                if (index + 2 < value.Length)
                {
                    int high = Text.HexValue(value[index + 1]);
                    int low = Text.HexValue(value[index + 2]);
                    if (high >= 0 && low >= 0)
                    {
                        bytes.Add((byte)((high * 16) + low));
                        index = index + 3;
                        continue;
                    }
                }

                result.IsMalformed = true;
                bytes.Add(37);
                index = index + 1;
            }
            else if (value[index] <= 255)
            {
                bytes.Add((byte)value[index]);
                index = index + 1;
            }
            else
            {
                byte[] encoded = Text.Utf8.GetBytes(value[index].ToString());
                foreach (byte item in encoded)
                {
                    bytes.Add(item);
                }

                index = index + 1;
            }
        }

        return bytes.ToArray();
    }

    internal MimeParameterValue? ParseParameterIdentity(string name, string value)
    {
        if (name.Length == 0)
        {
            return null;
        }

        MimeParameterValue result = new MimeParameterValue();
        result.Value = value;
        int star = FindCharacter(name, '*', 0);
        if (star < 0)
        {
            if (!IsMimeToken(name))
            {
                return null;
            }

            result.Name = name;
            return result;
        }

        result.Name = name.Substring(0, star);
        string suffix = name.Substring(star + 1);
        if (!IsMimeToken(result.Name))
        {
            return null;
        }

        if (suffix.Length == 0)
        {
            result.IsExtended = true;
            return result;
        }

        if (suffix.EndsWith("*", global::System.StringComparison.Ordinal))
        {
            result.IsExtended = true;
            suffix = suffix.Substring(0, suffix.Length - 1);
        }

        int sequence = ParseDigits(suffix);
        if (sequence < 0)
        {
            return null;
        }

        result.Sequence = sequence;
        return result;
    }

    internal MimeParameterValue? FindParameterSequence(MimeHeaderValue header, string name, int sequence)
    {
        foreach (MimeParameterValue parameter in header.Parameters)
        {
            if (parameter.Name != null && parameter.Sequence == sequence &&
                string.Equals(parameter.Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return null;
    }

    internal MimeParameterValue? FindSingleExtendedParameter(MimeHeaderValue header, string name)
    {
        foreach (MimeParameterValue parameter in header.Parameters)
        {
            if (parameter.Name != null && parameter.Sequence < 0 && parameter.IsExtended &&
                string.Equals(parameter.Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return null;
    }

    internal int CountParameterSequence(MimeHeaderValue header, string name, int sequence)
    {
        int result = 0;
        foreach (MimeParameterValue parameter in header.Parameters)
        {
            if (parameter.Name != null && parameter.Sequence == sequence &&
                string.Equals(parameter.Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                result = result + 1;
            }
        }

        return result;
    }

    internal bool HasLaterParameterSequence(MimeHeaderValue header, string name, int sequence)
    {
        foreach (MimeParameterValue parameter in header.Parameters)
        {
            if (parameter.Name != null && parameter.Sequence >= sequence &&
                string.Equals(parameter.Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal int CountSingleExtendedParameter(MimeHeaderValue header, string name)
    {
        int result = 0;
        foreach (MimeParameterValue parameter in header.Parameters)
        {
            if (parameter.Name != null && parameter.Sequence < 0 && parameter.IsExtended &&
                string.Equals(parameter.Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                result = result + 1;
            }
        }

        return result;
    }

    internal int CountSimpleParameter(MimeHeaderValue header, string name)
    {
        int result = 0;
        foreach (MimeParameterValue parameter in header.Parameters)
        {
            if (parameter.Name != null && parameter.Sequence < 0 && !parameter.IsExtended &&
                string.Equals(parameter.Name, name, global::System.StringComparison.OrdinalIgnoreCase))
            {
                result = result + 1;
            }
        }

        return result;
    }

    internal bool IsMimeToken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < 33 || character > 126 || character == '(' || character == ')' || character == '<' ||
                character == '>' || character == '@' || character == ',' || character == ';' || character == ':' ||
                character == '\\' || character == '"' || character == '/' || character == '[' || character == ']' ||
                character == '?' || character == '=')
            {
                return false;
            }
        }

        return true;
    }

    internal MimeDecodedStringResult ReadQuotedParameter(string value, int start)
    {
        MimeDecodedStringResult result = new MimeDecodedStringResult();
        StringBuilder decoded = new StringBuilder();
        int index = start + 1;
        bool escaped = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                decoded.Append(character);
                escaped = false;
            }
            else if (character == '\\')
            {
                escaped = true;
            }
            else if (character == '"')
            {
                result.Value = decoded.ToString();
                return result;
            }
            else
            {
                decoded.Append(character);
            }

            index = index + 1;
        }

        result.IsMalformed = true;
        result.Value = decoded.ToString();
        return result;
    }

    internal int FindQuotedParameterEnd(string value, int start)
    {
        int index = start + 1;
        bool escaped = false;
        while (index < value.Length)
        {
            if (escaped)
            {
                escaped = false;
            }
            else if (value[index] == '\\')
            {
                escaped = true;
            }
            else if (value[index] == '"')
            {
                return index + 1;
            }

            index = index + 1;
        }

        return value.Length;
    }

    internal int FindParameterSeparator(string value, int start)
    {
        int index = start;
        bool quoted = false;
        bool escaped = false;
        int commentDepth = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\' && (quoted || commentDepth > 0))
            {
                escaped = true;
            }
            else if (commentDepth > 0)
            {
                if (character == '(')
                {
                    commentDepth = commentDepth + 1;
                }
                else if (character == ')')
                {
                    commentDepth = commentDepth - 1;
                }
            }
            else if (character == '(' && !quoted)
            {
                commentDepth = 1;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == ';' && !quoted)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int SkipWhitespaceAndComments(string value, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == ' ' || value[index] == '\t' || value[index] == '\r' || value[index] == '\n')
            {
                index = index + 1;
            }
            else if (value[index] == '(')
            {
                int depth = 1;
                index = index + 1;
                bool escaped = false;
                while (index < value.Length && depth > 0)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (value[index] == '\\')
                    {
                        escaped = true;
                    }
                    else if (value[index] == '(')
                    {
                        depth = depth + 1;
                    }
                    else if (value[index] == ')')
                    {
                        depth = depth - 1;
                    }

                    index = index + 1;
                }
            }
            else
            {
                break;
            }
        }

        return index;
    }

    internal int ParseDigits(string value)
    {
        if (value.Length == 0)
        {
            return -1;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9' || result > 100000)
            {
                return -1;
            }

            result = (result * 10) + (value[index] - '0');
            index = index + 1;
        }

        return result;
    }

    internal int FindCharacter(string value, char character, int start)
    {
        int index = start;
        while (index < value.Length)
        {
            if (value[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string TrimWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == ' ' || value[start] == '\t' || value[start] == '\r' || value[start] == '\n'))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == ' ' || value[end - 1] == '\t' || value[end - 1] == '\r' || value[end - 1] == '\n'))
        {
            end = end - 1;
        }

        return start == 0 && end == value.Length ? value : value.Substring(start, end - start);
    }
}
