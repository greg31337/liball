using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CddlStringResult
{
    internal bool IsValid;
    internal CddlLiteralKind LiteralKind = CddlLiteralKind.None;
    internal string? StringValue;
    internal byte[]? ByteStringValue;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal CddlSourceSpan ErrorSpan = new CddlSourceSpan();
}

internal class CddlStringParser
{
    internal CddlToken Token;
    internal string Raw;
    internal int Index;
    internal int End;
    internal bool ByteMode;
    internal string Qualifier = "";
    internal StringBuilder Decoded = new StringBuilder();
    internal List<int> DecodedOrigins = new List<int>();
    internal StringBuilder Encoded = new StringBuilder();
    internal List<int> EncodedOrigins = new List<int>();
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal int ErrorRelativeOffset;
    internal int ErrorLength = 1;

    internal CddlStringParser(CddlToken token)
    {
        Token = token;
        Raw = token.Text;
    }

    internal CddlStringResult ParseLiteral()
    {
        CddlStringResult result = new CddlStringResult();
        if (!InitializeBounds())
        {
            return CreateFailureResult();
        }
        if (!DecodeCharacters())
        {
            return CreateFailureResult();
        }
        if (!ByteMode)
        {
            result.IsValid = true;
            result.LiteralKind = CddlLiteralKind.TextString;
            result.StringValue = Decoded.ToString();
            return result;
        }
        if (Qualifier.Length == 0)
        {
            result.IsValid = true;
            result.LiteralKind = CddlLiteralKind.Utf8ByteString;
            result.ByteStringValue = Encoding.UTF8.GetBytes(Decoded.ToString());
            return result;
        }
        if (!RemoveEncodedTrivia())
        {
            return CreateFailureResult();
        }
        byte[]? bytes;
        if (Qualifier == "h")
        {
            bytes = DecodeHexBytes();
            result.LiteralKind = CddlLiteralKind.HexByteString;
        }
        else
        {
            bytes = DecodeBase64Bytes();
            result.LiteralKind = CddlLiteralKind.Base64UrlByteString;
        }
        if (bytes == null)
        {
            return CreateFailureResult();
        }
        result.IsValid = true;
        result.ByteStringValue = bytes;
        return result;
    }

    internal bool InitializeBounds()
    {
        ByteMode = Token.Kind == CddlTokenKind.ByteString;
        if (!ByteMode)
        {
            if (Raw.Length < 2 || Raw[0] != '"' || Raw[Raw.Length - 1] != '"')
            {
                SetFailure(
                    "CDDL_LITERAL_TOKEN_INVALID",
                    "A text literal token requires matching double quotes.",
                    0,
                    Raw.Length
                );
                return false;
            }
            Index = 1;
            End = Raw.Length - 1;
            return true;
        }
        if (Raw.Length >= 2 && Raw[0] == '\'' && Raw[Raw.Length - 1] == '\'')
        {
            Index = 1;
            End = Raw.Length - 1;
            return true;
        }
        if (Raw.Length >= 3
            && (Raw[0] == 'h' || Raw[0] == 'H') && Raw[1] == '\''
            && Raw[Raw.Length - 1] == '\'')
        {
            Qualifier = "h";
            Index = 2;
            End = Raw.Length - 1;
            return true;
        }
        if (Raw.Length >= 5
            && (Raw[0] == 'b' || Raw[0] == 'B')
            && Raw[1] == '6' && Raw[2] == '4'
            && Raw[3] == '\'' && Raw[Raw.Length - 1] == '\'')
        {
            Qualifier = "b64";
            Index = 4;
            End = Raw.Length - 1;
            return true;
        }
        SetFailure(
            "CDDL_LITERAL_TOKEN_INVALID",
            "A byte literal token requires a supported qualifier and matching quotes.",
            0,
            Raw.Length
        );
        return false;
    }

    internal bool DecodeCharacters()
    {
        while (Index < End)
        {
            char current = Raw[Index];
            if (current == '\\')
            {
                if (!DecodeEscape())
                {
                    return false;
                }
            }
            else if (current == '\n')
            {
                if (!ByteMode)
                {
                    SetFailure(
                        "CDDL_TEXT_CHARACTER_INVALID",
                        "A raw line feed is not allowed in a CDDL text literal.",
                        Index,
                        1
                    );
                    return false;
                }
                AppendDecodedText("\n", Index);
                Index = Index + 1;
            }
            else if (current == '\r')
            {
                if (!ByteMode || Index + 1 >= End || Raw[Index + 1] != '\n')
                {
                    SetFailure(
                        ByteMode
                            ? "CDDL_BYTE_CHARACTER_INVALID"
                            : "CDDL_TEXT_CHARACTER_INVALID",
                        "A carriage return is valid only as part of CRLF in a byte literal.",
                        Index,
                        1
                    );
                    return false;
                }
                AppendDecodedText("\r\n", Index);
                Index = Index + 2;
            }
            else if (IsDirectAsciiAllowed(current))
            {
                AppendDecodedText(current.ToString(), Index);
                Index = Index + 1;
            }
            else if (!DecodeDirectNonAscii())
            {
                return false;
            }
        }
        return true;
    }

    internal bool IsDirectAsciiAllowed(char value)
    {
        if (!ByteMode)
        {
            return (value >= 0x20 && value <= 0x21)
                || (value >= 0x23 && value <= 0x5b)
                || (value >= 0x5d && value <= 0x7e);
        }
        return (value >= 0x20 && value <= 0x26)
            || (value >= 0x28 && value <= 0x5b)
            || (value >= 0x5d && value <= 0x7e);
    }

    internal bool DecodeDirectNonAscii()
    {
        char current = Raw[Index];
        if (current >= 0x00a0 && current <= 0xd7ff
            || current >= 0xe000)
        {
            AppendDecodedText(current.ToString(), Index);
            Index = Index + 1;
            return true;
        }
        if (char.IsHighSurrogate(current)
            && Index + 1 < End
            && char.IsLowSurrogate(Raw[Index + 1]))
        {
            int scalar = char.ConvertToUtf32(current, Raw[Index + 1]);
            if (scalar <= 0x10fffd)
            {
                AppendDecodedText(Raw.Substring(Index, 2), Index);
                Index = Index + 2;
                return true;
            }
            SetFailure(
                "CDDL_UNICODE_SCALAR_INVALID",
                "An unescaped CDDL source scalar cannot exceed U+10FFFD.",
                Index,
                2
            );
            return false;
        }
        string code = ByteMode
            ? "CDDL_BYTE_CHARACTER_INVALID"
            : "CDDL_TEXT_CHARACTER_INVALID";
        string message = ByteMode
            ? "The byte literal contains a character outside BCHAR."
            : "The text literal contains a character outside SCHAR.";
        if (char.IsSurrogate(current))
        {
            code = "CDDL_UNICODE_SCALAR_INVALID";
            message = "The CDDL literal contains an unpaired surrogate.";
        }
        SetFailure(code, message, Index, 1);
        return false;
    }

    internal bool DecodeEscape()
    {
        int origin = Index;
        Index = Index + 1;
        if (Index >= End)
        {
            SetFailure(
                "CDDL_ESCAPE_INCOMPLETE",
                "The CDDL literal ends inside an escape sequence.",
                origin,
                1
            );
            return false;
        }
        char escaped = Raw[Index];
        Index = Index + 1;
        if (escaped == '"')
        {
            AppendDecodedText("\"", origin);
            return true;
        }
        if (escaped == '/')
        {
            AppendDecodedText("/", origin);
            return true;
        }
        if (escaped == '\\')
        {
            AppendDecodedText("\\", origin);
            return true;
        }
        if (escaped == 'b')
        {
            AppendDecodedText("\b", origin);
            return true;
        }
        if (escaped == 'f')
        {
            AppendDecodedText("\f", origin);
            return true;
        }
        if (escaped == 'n')
        {
            AppendDecodedText("\n", origin);
            return true;
        }
        if (escaped == 'r')
        {
            AppendDecodedText("\r", origin);
            return true;
        }
        if (escaped == 't')
        {
            AppendDecodedText("\t", origin);
            return true;
        }
        if (escaped == '\'' && ByteMode)
        {
            AppendDecodedText("'", origin);
            return true;
        }
        if (escaped == 'u')
        {
            return DecodeUnicodeEscape(origin);
        }
        SetFailure(
            "CDDL_ESCAPE_INVALID",
            "The CDDL literal contains an unsupported escape sequence.",
            origin,
            2
        );
        return false;
    }

    internal bool DecodeUnicodeEscape(int origin)
    {
        if (Index < End && Raw[Index] == '{')
        {
            return DecodeBracedUnicodeEscape(origin);
        }
        return DecodeFourDigitUnicodeEscape(origin);
    }

    internal bool DecodeBracedUnicodeEscape(int origin)
    {
        Index = Index + 1;
        int digitsStart = Index;
        while (Index < End && Raw[Index] != '}')
        {
            if (HexValue(Raw[Index]) < 0)
            {
                SetFailure(
                    "CDDL_UNICODE_ESCAPE_INVALID",
                    "A braced Unicode escape contains a non-hexadecimal digit.",
                    Index,
                    1
                );
                return false;
            }
            Index = Index + 1;
        }
        if (Index >= End)
        {
            SetFailure(
                "CDDL_UNICODE_ESCAPE_INCOMPLETE",
                "A braced Unicode escape requires a closing brace.",
                origin,
                End - origin
            );
            return false;
        }
        int digitsEnd = Index;
        Index = Index + 1;
        if (digitsEnd == digitsStart)
        {
            SetFailure(
                "CDDL_UNICODE_ESCAPE_INVALID",
                "A braced Unicode escape requires at least one hexadecimal digit.",
                digitsStart,
                1
            );
            return false;
        }
        int significant = digitsStart;
        while (significant < digitsEnd && Raw[significant] == '0')
        {
            significant = significant + 1;
        }
        int scalar = 0;
        if (significant < digitsEnd)
        {
            if (digitsEnd - significant > 6)
            {
                SetFailure(
                    "CDDL_UNICODE_SCALAR_INVALID",
                    "The braced Unicode escape exceeds the Unicode scalar range.",
                    origin,
                    Index - origin
                );
                return false;
            }
            int scalarIndex = significant;
            while (scalarIndex < digitsEnd)
            {
                scalar = scalar * 16 + HexValue(Raw[scalarIndex]);
                scalarIndex = scalarIndex + 1;
            }
        }
        if (!AppendDecodedScalar(scalar, origin))
        {
            return false;
        }
        return true;
    }

    internal bool DecodeFourDigitUnicodeEscape(int origin)
    {
        if (Index > End - 4)
        {
            SetFailure(
                "CDDL_UNICODE_ESCAPE_INCOMPLETE",
                "A Unicode escape requires four hexadecimal digits.",
                origin,
                End - origin
            );
            return false;
        }
        int first = ReadHexQuad(origin);
        if (first < 0)
        {
            return false;
        }
        if (first >= 0xd800 && first <= 0xdbff)
        {
            if (Index > End - 6 || Raw[Index] != '\\' || Raw[Index + 1] != 'u')
            {
                SetFailure(
                    "CDDL_UNICODE_SCALAR_INVALID",
                    "An escaped high surrogate requires an escaped low surrogate.",
                    origin,
                    Index - origin
                );
                return false;
            }
            int secondOrigin = Index;
            Index = Index + 2;
            int second = ReadHexQuad(secondOrigin);
            if (second < 0)
            {
                return false;
            }
            if (second < 0xdc00 || second > 0xdfff)
            {
                SetFailure(
                    "CDDL_UNICODE_SCALAR_INVALID",
                    "An escaped high surrogate requires an escaped low surrogate.",
                    secondOrigin,
                    6
                );
                return false;
            }
            int scalar = 0x10000 + ((first - 0xd800) << 10) + second - 0xdc00;
            return AppendDecodedScalar(scalar, origin);
        }
        if (first >= 0xdc00 && first <= 0xdfff)
        {
            SetFailure(
                "CDDL_UNICODE_SCALAR_INVALID",
                "An escaped low surrogate requires a preceding high surrogate.",
                origin,
                6
            );
            return false;
        }
        return AppendDecodedScalar(first, origin);
    }

    internal int ReadHexQuad(int origin)
    {
        int value = 0;
        int count = 0;
        while (count < 4)
        {
            int digit = HexValue(Raw[Index + count]);
            if (digit < 0)
            {
                SetFailure(
                    "CDDL_UNICODE_ESCAPE_INVALID",
                    "A Unicode escape contains a non-hexadecimal digit.",
                    Index + count,
                    1
                );
                return -1;
            }
            value = value * 16 + digit;
            count = count + 1;
        }
        Index = Index + 4;
        return value;
    }

    internal bool AppendDecodedScalar(int scalar, int origin)
    {
        if (scalar < 0 || scalar > 0x10ffff
            || (scalar >= 0xd800 && scalar <= 0xdfff))
        {
            SetFailure(
                "CDDL_UNICODE_SCALAR_INVALID",
                "The Unicode escape does not identify a Unicode scalar value.",
                origin,
                Index - origin
            );
            return false;
        }
        AppendDecodedText(char.ConvertFromUtf32(scalar), origin);
        return true;
    }

    internal void AppendDecodedText(string value, int origin)
    {
        Decoded.Append(value);
        int index = 0;
        while (index < value.Length)
        {
            DecodedOrigins.Add(origin);
            index = index + 1;
        }
    }

    internal bool RemoveEncodedTrivia()
    {
        string text = Decoded.ToString();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == ' ' || current == '\n')
            {
                index = index + 1;
            }
            else if (current == '\r' && index + 1 < text.Length
                && text[index + 1] == '\n')
            {
                index = index + 2;
            }
            else if (current == ';')
            {
                int commentStart = index;
                index = index + 1;
                bool terminated = false;
                while (index < text.Length && !terminated)
                {
                    current = text[index];
                    if (current == '\n')
                    {
                        index = index + 1;
                        terminated = true;
                    }
                    else if (current == '\r' && index + 1 < text.Length
                        && text[index + 1] == '\n')
                    {
                        index = index + 2;
                        terminated = true;
                    }
                    else
                    {
                        int scalarLength = EncodedCommentScalarLength(text, index);
                        if (scalarLength == 0)
                        {
                            SetFailure(
                                "CDDL_ENCODED_COMMENT_CHARACTER_INVALID",
                                "An encoded byte-string comment contains a character outside PCHAR.",
                                DecodedOrigins[index],
                                1
                            );
                            return false;
                        }
                        index = index + scalarLength;
                    }
                }
                if (!terminated)
                {
                    SetFailure(
                        "CDDL_ENCODED_COMMENT_UNTERMINATED",
                        "An encoded byte-string comment must end with LF or CRLF.",
                        DecodedOrigins[commentStart],
                        1
                    );
                    return false;
                }
            }
            else
            {
                Encoded.Append(current);
                EncodedOrigins.Add(DecodedOrigins[index]);
                index = index + 1;
            }
        }
        return true;
    }

    internal int EncodedCommentScalarLength(string text, int index)
    {
        char current = text[index];
        if (current >= 0x20 && current <= 0x7e)
        {
            return 1;
        }
        if (current >= 0x00a0 && current <= 0xd7ff
            || current >= 0xe000 && !char.IsSurrogate(current))
        {
            return 1;
        }
        if (char.IsHighSurrogate(current)
            && index + 1 < text.Length
            && char.IsLowSurrogate(text[index + 1])
            && char.ConvertToUtf32(current, text[index + 1]) <= 0x10fffd)
        {
            return 2;
        }
        return 0;
    }

    internal byte[]? DecodeHexBytes()
    {
        if (Encoded.Length % 2 != 0)
        {
            int origin = EncodedOrigins.Count == 0
                ? Index
                : EncodedOrigins[EncodedOrigins.Count - 1];
            SetFailure(
                "CDDL_HEX_LENGTH_INVALID",
                "A hexadecimal byte literal requires an even number of digits.",
                origin,
                1
            );
            return null;
        }
        byte[] bytes = new byte[Encoded.Length / 2];
        int index = 0;
        while (index < bytes.Length)
        {
            int high = HexValue(Encoded[index * 2]);
            int low = HexValue(Encoded[index * 2 + 1]);
            if (high < 0)
            {
                SetFailure(
                    "CDDL_HEX_CHARACTER_INVALID",
                    "A hexadecimal byte literal contains a non-hexadecimal digit.",
                    EncodedOrigins[index * 2],
                    1
                );
                return null;
            }
            if (low < 0)
            {
                SetFailure(
                    "CDDL_HEX_CHARACTER_INVALID",
                    "A hexadecimal byte literal contains a non-hexadecimal digit.",
                    EncodedOrigins[index * 2 + 1],
                    1
                );
                return null;
            }
            bytes[index] = (byte)(high * 16 + low);
            index = index + 1;
        }
        return bytes;
    }

    internal byte[]? DecodeBase64Bytes()
    {
        if (Encoded.Length % 4 == 1)
        {
            int origin = EncodedOrigins.Count == 0
                ? Index
                : EncodedOrigins[EncodedOrigins.Count - 1];
            SetFailure(
                "CDDL_BASE64URL_LENGTH_INVALID",
                "An unpadded base64 byte literal has an invalid length.",
                origin,
                1
            );
            return null;
        }
        List<byte> bytes = new List<byte>();
        int buffer = 0;
        int bitCount = 0;
        bool standardAlphabet = false;
        bool urlAlphabet = false;
        int index = 0;
        while (index < Encoded.Length)
        {
            char current = Encoded[index];
            if (current == '+' || current == '/')
            {
                standardAlphabet = true;
            }
            if (current == '-' || current == '_')
            {
                urlAlphabet = true;
            }
            if (standardAlphabet && urlAlphabet)
            {
                SetFailure(
                    "CDDL_BASE64URL_ALPHABET_MIXED",
                    "A byte literal cannot mix base64 and base64url alphabets.",
                    EncodedOrigins[index],
                    1
                );
                return null;
            }
            int digit = Base64Value(current);
            if (digit < 0)
            {
                SetFailure(
                    "CDDL_BASE64URL_CHARACTER_INVALID",
                    "A base64 byte literal contains a character outside its alphabets.",
                    EncodedOrigins[index],
                    1
                );
                return null;
            }
            buffer = (buffer << 6) | digit;
            bitCount = bitCount + 6;
            if (bitCount >= 8)
            {
                bitCount = bitCount - 8;
                bytes.Add((byte)(buffer >> bitCount));
                if (bitCount == 0)
                {
                    buffer = 0;
                }
                else
                {
                    buffer = buffer & ((1 << bitCount) - 1);
                }
            }
            index = index + 1;
        }
        if (buffer != 0)
        {
            int origin = EncodedOrigins.Count == 0
                ? Index
                : EncodedOrigins[EncodedOrigins.Count - 1];
            SetFailure(
                "CDDL_BASE64URL_TRAILING_BITS",
                "Unused base64 bits must be zero.",
                origin,
                1
            );
            return null;
        }
        return bytes.ToArray();
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

    internal void SetFailure(
        string code,
        string message,
        int relativeOffset,
        int length)
    {
        if (ErrorCode != null)
        {
            return;
        }
        ErrorCode = code;
        ErrorMessage = message;
        ErrorRelativeOffset = relativeOffset;
        ErrorLength = length < 0 ? 0 : length;
    }

    internal CddlStringResult CreateFailureResult()
    {
        CddlStringResult result = new CddlStringResult();
        result.ErrorCode = ErrorCode ?? "CDDL_LITERAL_INVALID";
        result.ErrorMessage = ErrorMessage ?? "The CDDL literal is invalid.";
        result.ErrorSpan = CreateErrorSpan();
        return result;
    }

    internal CddlSourceSpan CreateErrorSpan()
    {
        int relative = ErrorRelativeOffset;
        if (relative < 0)
        {
            relative = 0;
        }
        if (relative > Raw.Length)
        {
            relative = Raw.Length;
        }
        int length = ErrorLength;
        if (length > Raw.Length - relative)
        {
            length = Raw.Length - relative;
        }
        int line = Token.Span.StartLine;
        int column = Token.Span.StartColumn;
        int index = 0;
        while (index < relative)
        {
            if (Raw[index] == '\r' && index + 1 < relative
                && Raw[index + 1] == '\n')
            {
                index = index + 2;
                line = line + 1;
                column = 1;
            }
            else if (Raw[index] == '\n')
            {
                index = index + 1;
                line = line + 1;
                column = 1;
            }
            else
            {
                index = index + 1;
                column = column + 1;
            }
        }
        int startLine = line;
        int startColumn = column;
        int end = relative + length;
        while (index < end)
        {
            if (Raw[index] == '\r' && index + 1 < end
                && Raw[index + 1] == '\n')
            {
                index = index + 2;
                line = line + 1;
                column = 1;
            }
            else if (Raw[index] == '\n')
            {
                index = index + 1;
                line = line + 1;
                column = 1;
            }
            else
            {
                index = index + 1;
                column = column + 1;
            }
        }
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = Token.Span.SourceName;
        span.Start = Token.Span.Start + relative;
        span.Length = length;
        span.StartLine = startLine;
        span.StartColumn = startColumn;
        span.EndLine = line;
        span.EndColumn = column;
        return span;
    }
}
