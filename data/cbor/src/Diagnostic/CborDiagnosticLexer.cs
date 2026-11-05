using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CborDiagnosticLexer
{
    internal string Text;
    internal bool Extended;
    internal int Offset;
    internal bool TriviaConsumed;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal int ErrorOffset = -1;

    internal CborDiagnosticLexer(string text, bool extended)
    {
        Text = text;
        Extended = extended;
    }

    internal bool SkipTrivia()
    {
        TriviaConsumed = false;
        bool scanning = true;
        while (scanning && Offset < Text.Length)
        {
            char current = Text[Offset];
            if (char.IsWhiteSpace(current))
            {
                TriviaConsumed = true;
                Offset = Offset + 1;
            }
            else if (current == '/' && Extended)
            {
                TriviaConsumed = true;
                if (!SkipComment())
                {
                    return false;
                }
            }
            else
            {
                scanning = false;
            }
        }
        return true;
    }

    internal bool SkipComment()
    {
        int start = Offset;
        Offset = Offset + 1;
        while (Offset < Text.Length && Text[Offset] != '/')
        {
            Offset = Offset + 1;
        }
        if (Offset >= Text.Length)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_COMMENT_UNTERMINATED",
                "The diagnostic comment has no closing slash.",
                start
            );
            return false;
        }
        Offset = Offset + 1;
        return true;
    }

    internal CborDiagnosticLexeme? ReadQuotedText(char delimiter)
    {
        int start = Offset;
        if (Offset >= Text.Length || Text[Offset] != delimiter)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_STRING_EXPECTED",
                "A quoted diagnostic string was expected.",
                Offset
            );
            return null;
        }
        Offset = Offset + 1;
        StringBuilder value = new StringBuilder();
        while (Offset < Text.Length)
        {
            char current = Text[Offset];
            Offset = Offset + 1;
            if (current == delimiter)
            {
                CborDiagnosticLexeme completed = new CborDiagnosticLexeme();
                completed.Start = start;
                completed.Text = value.ToString();
                return completed;
            }
            if (current < 0x20)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_STRING_CONTROL",
                    "An unescaped control character is not allowed in a diagnostic string.",
                    Offset - 1
                );
                return null;
            }
            if (current != '\\')
            {
                if (char.IsLowSurrogate(current))
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_STRING_SURROGATE",
                        "The diagnostic string contains an unpaired surrogate.",
                        Offset - 1
                    );
                    return null;
                }
                if (char.IsHighSurrogate(current))
                {
                    if (Offset >= Text.Length
                        || !char.IsLowSurrogate(Text[Offset]))
                    {
                        SetFailure(
                            "CBOR_DIAGNOSTIC_STRING_SURROGATE",
                            "The diagnostic string contains an unpaired surrogate.",
                            Offset - 1
                        );
                        return null;
                    }
                    value.Append(current);
                    value.Append(Text[Offset]);
                    Offset = Offset + 1;
                }
                else
                {
                    value.Append(current);
                }
                continue;
            }
            if (Offset >= Text.Length)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_ESCAPE_INCOMPLETE",
                    "The diagnostic string ends inside an escape sequence.",
                    Offset
                );
                return null;
            }
            char escape = Text[Offset];
            Offset = Offset + 1;
            switch (escape)
            {
                case '"':
                    value.Append('"');
                    break;
                case '\'':
                    if (delimiter == '\'')
                    {
                        value.Append('\'');
                    }
                    else
                    {
                        SetFailure(
                            "CBOR_DIAGNOSTIC_ESCAPE_INVALID",
                            "A single quote escape is valid only in a text-based byte string.",
                            Offset - 1
                        );
                        return null;
                    }
                    break;
                case '\\':
                    value.Append('\\');
                    break;
                case '/':
                    value.Append('/');
                    break;
                case 'b':
                    value.Append('\b');
                    break;
                case 'f':
                    value.Append('\f');
                    break;
                case 'n':
                    value.Append('\n');
                    break;
                case 'r':
                    value.Append('\r');
                    break;
                case 't':
                    value.Append('\t');
                    break;
                case 'u':
                    if (!AppendUnicodeEscape(value))
                    {
                        return null;
                    }
                    break;
                default:
                    SetFailure(
                        "CBOR_DIAGNOSTIC_ESCAPE_INVALID",
                        "The diagnostic string contains an unsupported escape sequence.",
                        Offset - 1
                    );
                    return null;
            }
        }
        SetFailure(
            "CBOR_DIAGNOSTIC_STRING_UNTERMINATED",
            "The diagnostic string has no closing quote.",
            start
        );
        return null;
    }

    internal bool AppendUnicodeEscape(StringBuilder value)
    {
        int first = ReadHexQuad();
        if (first < 0)
        {
            return false;
        }
        char firstCharacter = (char)first;
        if (char.IsLowSurrogate(firstCharacter))
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_STRING_SURROGATE",
                "A low surrogate must follow an escaped high surrogate.",
                Offset - 4
            );
            return false;
        }
        if (!char.IsHighSurrogate(firstCharacter))
        {
            value.Append(firstCharacter);
            return true;
        }
        if (Offset > Text.Length - 6
            || Text[Offset] != '\\'
            || Text[Offset + 1] != 'u')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_STRING_SURROGATE",
                "An escaped high surrogate must be followed by an escaped low surrogate.",
                Offset - 4
            );
            return false;
        }
        Offset = Offset + 2;
        int second = ReadHexQuad();
        if (second < 0)
        {
            return false;
        }
        char secondCharacter = (char)second;
        if (!char.IsLowSurrogate(secondCharacter))
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_STRING_SURROGATE",
                "An escaped high surrogate must be followed by an escaped low surrogate.",
                Offset - 4
            );
            return false;
        }
        value.Append(firstCharacter);
        value.Append(secondCharacter);
        return true;
    }

    internal int ReadHexQuad()
    {
        if (Offset > Text.Length - 4)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_UNICODE_ESCAPE_INCOMPLETE",
                "A Unicode escape requires exactly four hexadecimal digits.",
                Offset
            );
            return -1;
        }
        int value = 0;
        int index = 0;
        while (index < 4)
        {
            int digit = HexValue(Text[Offset + index]);
            if (digit < 0)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_UNICODE_ESCAPE_INVALID",
                    "A Unicode escape contains a non-hexadecimal digit.",
                    Offset + index
                );
                return -1;
            }
            value = value * 16 + digit;
            index = index + 1;
        }
        Offset = Offset + 4;
        return value;
    }

    internal CborDiagnosticLexeme? ReadByteString()
    {
        int start = Offset;
        if (Offset < Text.Length && Text[Offset] == '\'')
        {
            if (!Extended
                && (Offset + 2 >= Text.Length
                    || Text[Offset + 1] != '\''
                    || Text[Offset + 2] != '_'))
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_EXTENDED_NOTATION_DISABLED",
                    "Text-based byte strings require extended diagnostic notation.",
                    Offset
                );
                return null;
            }
            CborDiagnosticLexeme? textBytes = ReadQuotedText('\'');
            if (textBytes == null)
            {
                return null;
            }
            CborDiagnosticLexeme result = new CborDiagnosticLexeme();
            result.Start = start;
            result.Bytes = Encoding.UTF8.GetBytes(textBytes.Text ?? "");
            return result;
        }
        string prefix = ReadBytePrefix();
        if (prefix.Length == 0
            || Offset >= Text.Length
            || Text[Offset] != '\'')
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_BYTE_STRING_PREFIX_INVALID",
                "A supported byte-string encoding prefix was expected.",
                start
            );
            return null;
        }
        Offset = Offset + 1;
        StringBuilder encoded = new StringBuilder();
        while (Offset < Text.Length && Text[Offset] != '\'')
        {
            char current = Text[Offset];
            if (char.IsWhiteSpace(current))
            {
                if (!Extended)
                {
                    SetFailure(
                        "CBOR_DIAGNOSTIC_BYTE_STRING_WHITESPACE",
                        "Whitespace inside an encoded byte string requires extended notation.",
                        Offset
                    );
                    return null;
                }
                Offset = Offset + 1;
            }
            else if (current == '/' && Extended)
            {
                bool comment = prefix != "b64";
                if (prefix == "b64")
                {
                    int search = Offset + 1;
                    while (search < Text.Length
                        && Text[search] != '\''
                        && Text[search] != '/')
                    {
                        search = search + 1;
                    }
                    comment = search < Text.Length && Text[search] == '/';
                }
                if (comment && !SkipComment())
                {
                    return null;
                }
                if (!comment)
                {
                    encoded.Append(current);
                    Offset = Offset + 1;
                }
            }
            else
            {
                encoded.Append(current);
                Offset = Offset + 1;
            }
        }
        if (Offset >= Text.Length)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_BYTE_STRING_UNTERMINATED",
                "The encoded byte string has no closing quote.",
                start
            );
            return null;
        }
        Offset = Offset + 1;
        byte[]? decoded = DecodeByteContent(prefix, encoded.ToString(), start);
        if (decoded == null)
        {
            return null;
        }
        CborDiagnosticLexeme completed = new CborDiagnosticLexeme();
        completed.Start = start;
        completed.Bytes = decoded;
        return completed;
    }

    internal string ReadBytePrefix()
    {
        if (Offset < Text.Length && Text[Offset] == 'h')
        {
            if (Offset <= Text.Length - 3
                && Text[Offset + 1] == '3'
                && Text[Offset + 2] == '2')
            {
                Offset = Offset + 3;
                return "h32";
            }
            Offset = Offset + 1;
            return "h";
        }
        if (Offset <= Text.Length - 3
            && Text[Offset] == 'b'
            && Text[Offset + 1] == '3'
            && Text[Offset + 2] == '2')
        {
            Offset = Offset + 3;
            return "b32";
        }
        if (Offset <= Text.Length - 3
            && Text[Offset] == 'b'
            && Text[Offset + 1] == '6'
            && Text[Offset + 2] == '4')
        {
            Offset = Offset + 3;
            return "b64";
        }
        return "";
    }

    internal byte[]? DecodeByteContent(string prefix, string content, int start)
    {
        if (prefix == "h")
        {
            return DecodeHex(content, start);
        }
        if (prefix == "b32")
        {
            return DecodeBase32(content, false, start);
        }
        if (prefix == "h32")
        {
            return DecodeBase32(content, true, start);
        }
        return DecodeBase64(content, start);
    }

    internal byte[]? DecodeHex(string content, int start)
    {
        if (content.Length % 2 != 0)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_HEX_LENGTH_INVALID",
                "A hexadecimal byte string requires an even number of digits.",
                start
            );
            return null;
        }
        byte[] bytes = new byte[content.Length / 2];
        int index = 0;
        while (index < bytes.Length)
        {
            int high = HexValue(content[index * 2]);
            int low = HexValue(content[index * 2 + 1]);
            if (high < 0 || low < 0)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_HEX_INVALID",
                    "A hexadecimal byte string contains a non-hexadecimal digit.",
                    start
                );
                return null;
            }
            bytes[index] = (byte)(high * 16 + low);
            index = index + 1;
        }
        return bytes;
    }

    internal byte[]? DecodeBase32(string content, bool hexadecimal, int start)
    {
        int remainder = content.Length % 8;
        bool lengthValid = remainder == 0
            || remainder == 2
            || remainder == 4
            || remainder == 5
            || remainder == 7;
        if (!lengthValid)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_BASE32_LENGTH_INVALID",
                "The unpadded base32 byte string has an invalid length.",
                start
            );
            return null;
        }
        List<byte> bytes = new List<byte>();
        int buffer = 0;
        int bitCount = 0;
        int index = 0;
        while (index < content.Length)
        {
            int digit = Base32Value(content[index], hexadecimal);
            if (digit < 0)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_BASE32_INVALID",
                    "The byte string contains a character outside its base32 alphabet.",
                    start
                );
                return null;
            }
            buffer = (buffer << 5) | digit;
            bitCount = bitCount + 5;
            if (bitCount >= 8)
            {
                bitCount = bitCount - 8;
                bytes.Add((byte)((buffer >> bitCount) & 255));
            }
            index = index + 1;
        }
        if (bitCount > 0 && (buffer & ((1 << bitCount) - 1)) != 0)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_BASE32_PADDING_INVALID",
                "Unused base32 bits must be zero.",
                start
            );
            return null;
        }
        return bytes.ToArray();
    }

    internal byte[]? DecodeBase64(string content, int start)
    {
        if (content.Length % 4 == 1)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_BASE64_LENGTH_INVALID",
                "The unpadded base64 byte string has an invalid length.",
                start
            );
            return null;
        }
        List<byte> bytes = new List<byte>();
        int buffer = 0;
        int bitCount = 0;
        int index = 0;
        bool standardAlphabet = false;
        bool urlAlphabet = false;
        while (index < content.Length)
        {
            if (content[index] == '+' || content[index] == '/')
            {
                standardAlphabet = true;
            }
            if (content[index] == '-' || content[index] == '_')
            {
                urlAlphabet = true;
            }
            if (standardAlphabet && urlAlphabet)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_BASE64_INVALID",
                    "A byte string cannot mix base64 and base64url alphabets.",
                    start
                );
                return null;
            }
            int digit = Base64Value(content[index]);
            if (digit < 0)
            {
                SetFailure(
                    "CBOR_DIAGNOSTIC_BASE64_INVALID",
                    "The byte string contains a character outside the base64 alphabets.",
                    start
                );
                return null;
            }
            buffer = (buffer << 6) | digit;
            bitCount = bitCount + 6;
            if (bitCount >= 8)
            {
                bitCount = bitCount - 8;
                bytes.Add((byte)((buffer >> bitCount) & 255));
            }
            index = index + 1;
        }
        if (bitCount > 0 && (buffer & ((1 << bitCount) - 1)) != 0)
        {
            SetFailure(
                "CBOR_DIAGNOSTIC_BASE64_PADDING_INVALID",
                "Unused base64 bits must be zero.",
                start
            );
            return null;
        }
        return bytes.ToArray();
    }

    internal string ReadAtom()
    {
        int start = Offset;
        while (Offset < Text.Length)
        {
            char current = Text[Offset];
            bool delimiter = char.IsWhiteSpace(current)
                || current == ','
                || current == ']'
                || current == '}'
                || current == ')'
                || current == ':'
                || current == '('
                || current == '<'
                || current == '>'
                || current == '_'
                || (current == '/' && Extended);
            if (delimiter)
            {
                break;
            }
            Offset = Offset + 1;
        }
        return Text.Substring(start, Offset - start);
    }

    internal bool IsByteStringStart()
    {
        if (Offset >= Text.Length)
        {
            return false;
        }
        if (Text[Offset] == '\'')
        {
            return true;
        }
        if (Text[Offset] == 'h')
        {
            return Offset + 1 < Text.Length && Text[Offset + 1] == '\''
                || Offset + 3 < Text.Length
                    && Text.Substring(Offset, 4) == "h32'";
        }
        if (Text[Offset] == 'b')
        {
            return Offset + 3 < Text.Length
                && (Text.Substring(Offset, 4) == "b32'"
                    || Text.Substring(Offset, 4) == "b64'");
        }
        return false;
    }

    internal int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    internal int Base32Value(char value, bool hexadecimal)
    {
        char upper = char.ToUpperInvariant(value);
        if (hexadecimal)
        {
            if (upper >= '0' && upper <= '9')
            {
                return upper - '0';
            }
            if (upper >= 'A' && upper <= 'V')
            {
                return upper - 'A' + 10;
            }
            return -1;
        }
        if (upper >= 'A' && upper <= 'Z')
        {
            return upper - 'A';
        }
        if (upper >= '2' && upper <= '7')
        {
            return upper - '2' + 26;
        }
        return -1;
    }

    internal int Base64Value(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }
        if (value >= 'a' && value <= 'z')
        {
            return value - 'a' + 26;
        }
        if (value >= '0' && value <= '9')
        {
            return value - '0' + 52;
        }
        if (value == '+' || value == '-')
        {
            return 62;
        }
        if (value == '/' || value == '_')
        {
            return 63;
        }
        return -1;
    }

    internal void SetFailure(string code, string message, int offset)
    {
        if (ErrorCode == null)
        {
            ErrorCode = code;
            ErrorMessage = message;
            ErrorOffset = offset;
        }
    }
}
