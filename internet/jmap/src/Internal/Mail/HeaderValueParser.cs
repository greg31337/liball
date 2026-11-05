using System.Collections.Generic;
using System.Text;

namespace jmap;

internal class EncodedWordResult
{
    internal bool IsEncodedWord;
    internal bool IsDecoded;
    internal string? Text;
}

internal class HeaderTextToken
{
    internal bool IsWhitespace;
    internal bool IsEncodedWord;
    internal string? Raw;
    internal string? Decoded;
}

internal class HeaderValueParser
{
    internal Encoding Utf8 = new UTF8Encoding(false, false);

    internal HeaderValueParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    internal string? DecodeText(string? rawValue)
    {
        if (rawValue == null)
        {
            return null;
        }

        string unfolded = UnfoldValue(rawValue);
        int start = 0;
        while (start < unfolded.Length && unfolded[start] == ' ')
        {
            start = start + 1;
        }

        string value = start == 0 ? unfolded : unfolded.Substring(start);
        List<HeaderTextToken> tokens = TokenizeText(value);
        StringBuilder decoded = new StringBuilder();
        int index = 0;
        while (index < tokens.Count)
        {
            HeaderTextToken token = tokens[index];
            if (token.IsWhitespace && index > 0 && index + 1 < tokens.Count &&
                tokens[index - 1].IsEncodedWord && tokens[index + 1].IsEncodedWord)
            {
                index = index + 1;
                continue;
            }

            if (token.IsEncodedWord)
            {
                decoded.Append(token.Decoded);
            }
            else
            {
                decoded.Append(token.Raw);
            }

            index = index + 1;
        }

        return decoded.ToString().Normalize(NormalizationForm.FormC);
    }

    internal string UnfoldValue(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r' && index + 2 < value.Length && value[index + 1] == '\n' &&
                (value[index + 2] == ' ' || value[index + 2] == '\t'))
            {
                index = index + 2;
            }
            else if ((value[index] == '\r' || value[index] == '\n') && index + 1 < value.Length &&
                (value[index + 1] == ' ' || value[index + 1] == '\t'))
            {
                index = index + 1;
            }
            else
            {
                result.Append(value[index]);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal List<HeaderTextToken> TokenizeText(string value)
    {
        List<HeaderTextToken> tokens = new List<HeaderTextToken>();
        int index = 0;
        while (index < value.Length)
        {
            bool whitespace = value[index] == ' ' || value[index] == '\t';
            int end = index + 1;
            while (end < value.Length && ((value[end] == ' ' || value[end] == '\t') == whitespace))
            {
                end = end + 1;
            }

            string raw = value.Substring(index, end - index);
            HeaderTextToken token = new HeaderTextToken();
            token.IsWhitespace = whitespace;
            token.Raw = raw;
            if (!whitespace)
            {
                EncodedWordResult parsed = DecodeEncodedWord(raw);
                token.IsEncodedWord = parsed.IsEncodedWord;
                if (parsed.IsEncodedWord)
                {
                    token.Decoded = parsed.IsDecoded ? parsed.Text : "\ufffd";
                }
            }

            tokens.Add(token);
            index = end;
        }

        return tokens;
    }

    internal EncodedWordResult DecodeEncodedWord(string token)
    {
        EncodedWordResult result = new EncodedWordResult();
        if (token.Length < 7 || token.Length > 75 || !token.StartsWith("=?", global::System.StringComparison.Ordinal) ||
            !token.EndsWith("?=", global::System.StringComparison.Ordinal))
        {
            return result;
        }

        int charsetEnd = FindCharacter(token, '?', 2);
        if (charsetEnd <= 2)
        {
            return result;
        }

        int encodingEnd = FindCharacter(token, '?', charsetEnd + 1);
        if (encodingEnd != charsetEnd + 2)
        {
            return result;
        }

        char encoding = token[charsetEnd + 1];
        if (encoding != 'B' && encoding != 'b' && encoding != 'Q' && encoding != 'q')
        {
            return result;
        }

        int encodedStart = encodingEnd + 1;
        int encodedLength = token.Length - encodedStart - 2;
        if (encodedLength <= 0 || !IsPrintableEncodedText(token, encodedStart, encodedLength))
        {
            return result;
        }

        result.IsEncodedWord = true;
        string encodedText = token.Substring(encodedStart, encodedLength);
        byte[]? bytes = encoding == 'B' || encoding == 'b' ? DecodeBase64(encodedText) : DecodeQ(encodedText);
        if (bytes == null)
        {
            return result;
        }

        string charset = token.Substring(2, charsetEnd - 2);
        string? decoded = DecodeCharset(bytes, charset);
        if (decoded == null)
        {
            return result;
        }

        result.Text = RemoveDecodedControls(decoded);
        result.IsDecoded = true;
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

    internal bool IsPrintableEncodedText(string value, int start, int length)
    {
        int index = 0;
        while (index < length)
        {
            char character = value[start + index];
            if (character < 33 || character > 126 || character == '?')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal byte[]? DecodeQ(string value)
    {
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '_')
            {
                bytes.Add(32);
                index = index + 1;
            }
            else if (character == '=')
            {
                if (index + 2 >= value.Length)
                {
                    return null;
                }

                int high = HexValue(value[index + 1]);
                int low = HexValue(value[index + 2]);
                if (high < 0 || low < 0)
                {
                    return null;
                }

                bytes.Add((byte)((high * 16) + low));
                index = index + 3;
            }
            else
            {
                if (character < 33 || character > 126 || character == '?')
                {
                    return null;
                }

                bytes.Add((byte)character);
                index = index + 1;
            }
        }

        return bytes.ToArray();
    }

    internal byte[]? DecodeBase64(string value)
    {
        if (value.Length == 0 || value.Length % 4 != 0)
        {
            return null;
        }

        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            int first = Base64Value(value[index]);
            int second = Base64Value(value[index + 1]);
            int third = Base64Value(value[index + 2]);
            int fourth = Base64Value(value[index + 3]);
            bool finalGroup = index + 4 == value.Length;
            if (first < 0 || second < 0)
            {
                return null;
            }

            bytes.Add((byte)((first * 4) + (second / 16)));
            if (third == -2)
            {
                if (!finalGroup || fourth != -2 || second % 16 != 0)
                {
                    return null;
                }
            }
            else if (third < 0)
            {
                return null;
            }
            else
            {
                bytes.Add((byte)(((second % 16) * 16) + (third / 4)));
                if (fourth == -2)
                {
                    if (!finalGroup || third % 4 != 0)
                    {
                        return null;
                    }
                }
                else if (fourth < 0)
                {
                    return null;
                }
                else
                {
                    bytes.Add((byte)(((third % 4) * 64) + fourth));
                }
            }

            index = index + 4;
        }

        return bytes.ToArray();
    }

    internal int Base64Value(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            return character - 'A';
        }

        if (character >= 'a' && character <= 'z')
        {
            return character - 'a' + 26;
        }

        if (character >= '0' && character <= '9')
        {
            return character - '0' + 52;
        }

        if (character == '+')
        {
            return 62;
        }

        if (character == '/')
        {
            return 63;
        }

        return character == '=' ? -2 : -1;
    }

    internal int HexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        return -1;
    }

    internal string? DecodeCharset(byte[] bytes, string charset)
    {
        string normalized = charset.ToLowerInvariant();
        if (normalized == "utf-8" || normalized == "utf8")
        {
            return Utf8.GetString(bytes);
        }

        if (normalized == "iso-8859-1" || normalized == "iso8859-1" || normalized == "latin1" || normalized == "latin-1")
        {
            return Encoding.Latin1.GetString(bytes);
        }

        if (normalized == "us-ascii" || normalized == "ascii")
        {
            return DecodeAscii(bytes);
        }

        if (normalized == "windows-1252" || normalized == "cp1252")
        {
            return DecodeWindows1252(bytes);
        }

        if (normalized == "utf-16" || normalized == "utf-16le" || normalized == "unicode")
        {
            return Encoding.Unicode.GetString(bytes);
        }

        if (normalized == "utf-16be")
        {
            return Encoding.BigEndianUnicode.GetString(bytes);
        }

        Encoding? discovered = FindEncoding(normalized);
        if (discovered != null)
        {
            return discovered.GetString(bytes);
        }

        return null;
    }

    internal Encoding? FindEncoding(string charset)
    {
        string comparable = CanonicalCharset(charset);
        EncodingInfo[] encodings = Encoding.GetEncodings();
        foreach (EncodingInfo encoding in encodings)
        {
            string name = CanonicalCharset(encoding.Name);
            if (name == comparable || IsCodePageAlias(comparable, encoding.CodePage))
            {
                return encoding.GetEncoding();
            }
        }

        return null;
    }

    internal string CanonicalCharset(string charset)
    {
        string result = charset.ToLowerInvariant().Replace('_', '-');
        if (result.StartsWith("iso8859-", global::System.StringComparison.Ordinal))
        {
            return "iso-" + result.Substring(3);
        }

        return result;
    }

    internal bool IsCodePageAlias(string charset, int codePage)
    {
        if (!charset.StartsWith("cp", global::System.StringComparison.Ordinal) || charset.Length <= 2)
        {
            return false;
        }

        int parsed = 0;
        int index = 2;
        while (index < charset.Length)
        {
            char character = charset[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            parsed = (parsed * 10) + (character - '0');
            index = index + 1;
        }

        return parsed == codePage;
    }

    internal string DecodeAscii(byte[] bytes)
    {
        StringBuilder result = new StringBuilder();
        foreach (byte value in bytes)
        {
            result.Append(value <= 127 ? (char)value : '\ufffd');
        }

        return result.ToString();
    }

    internal string DecodeWindows1252(byte[] bytes)
    {
        StringBuilder result = new StringBuilder();
        foreach (byte value in bytes)
        {
            if (value < 128 || value >= 160)
            {
                result.Append((char)value);
            }
            else
            {
                result.Append(Windows1252Character(value));
            }
        }

        return result.ToString();
    }

    internal char Windows1252Character(byte value)
    {
        string characters = "€�‚ƒ„…†‡ˆ‰Š‹Œ�Ž��‘’“”•–—˜™š›œ�žŸ";
        return characters[value - 128];
    }

    internal string RemoveDecodedControls(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!char.IsControl(character))
            {
                result.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }
}
