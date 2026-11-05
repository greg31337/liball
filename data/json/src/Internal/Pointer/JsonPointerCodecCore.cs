namespace json;

internal static class JsonPointerCodecCore
{
    internal static JsonPointer ParseStringRepresentation(string representation)
    {
        if (representation == null)
        {
            throw CreatePointerFailure(
                JsonDiagnosticCode.InvalidPointerSyntax,
                "A JSON Pointer string representation is required.",
                "null reference");
        }

        return ParseDecodedRepresentation(
            representation,
            JsonPointerEncoding.String,
            representation);
    }

    internal static string FormatStringRepresentation(JsonPointer pointer)
    {
        global::System.Collections.Generic.List<string> tokens =
            GetValidatedReferenceTokens(pointer);
        return BuildStringRepresentation(tokens);
    }

    internal static JsonPointer ParseFragmentRepresentation(string fragment)
    {
        if (fragment == null)
        {
            throw CreatePointerFailure(
                JsonDiagnosticCode.InvalidPointerSyntax,
                "A JSON Pointer URI fragment representation is required.",
                "null reference");
        }

        if (fragment.Length == 0 || fragment[0] != '#')
        {
            throw CreatePointerFailure(
                JsonDiagnosticCode.InvalidPointerSyntax,
                "A JSON Pointer URI fragment representation must begin with '#'.",
                "URI fragment");
        }

        global::System.Collections.Generic.List<byte> bytes =
            new global::System.Collections.Generic.List<byte>();
        int index = 1;
        while (index < fragment.Length)
        {
            char character = fragment[index];
            if (character == '%')
            {
                if (index + 2 >= fragment.Length)
                {
                    throw CreatePointerFailure(
                        JsonDiagnosticCode.InvalidPointerPercentEncoding,
                        "A URI fragment percent escape requires two hexadecimal digits.",
                        "URI fragment");
                }

                int high = GetHexadecimalValue(fragment[index + 1]);
                int low = GetHexadecimalValue(fragment[index + 2]);
                if (high < 0 || low < 0)
                {
                    throw CreatePointerFailure(
                        JsonDiagnosticCode.InvalidPointerPercentEncoding,
                        "A URI fragment percent escape contains a non-hexadecimal digit.",
                        "URI fragment");
                }

                bytes.Add((byte)((high << 4) | low));
                index += 3;
                continue;
            }

            if (character > 0x7F || !IsFragmentCharacter((byte)character))
            {
                throw CreatePointerFailure(
                    JsonDiagnosticCode.InvalidPointerPercentEncoding,
                    "A URI fragment character must be percent encoded.",
                    "URI fragment");
            }

            bytes.Add((byte)character);
            index++;
        }

        string? decoded = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            decoded = DecodeUtf8Fragment(bytes);
        });
        capture.ExecuteOperation();
        if (capture.GetFailure() != null || decoded == null)
        {
            throw CreatePointerFailure(
                JsonDiagnosticCode.InvalidPointerPercentEncoding,
                "A URI fragment contains invalid UTF-8 percent encoding.",
                "URI fragment");
        }

        return ParseDecodedRepresentation(
            decoded,
            JsonPointerEncoding.UriFragment,
            fragment);
    }

    internal static string FormatFragmentRepresentation(JsonPointer pointer)
    {
        global::System.Collections.Generic.List<string> tokens =
            GetValidatedReferenceTokens(pointer);
        string representation = BuildStringRepresentation(tokens);
        byte[] utf8 = global::System.Text.Encoding.UTF8.GetBytes(representation);
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        builder.Append('#');
        int index = 0;
        while (index < utf8.Length)
        {
            byte value = utf8[index];
            if (IsFragmentCharacter(value))
            {
                builder.Append((char)value);
            }
            else
            {
                builder.Append('%');
                builder.Append(GetUpperHexadecimalDigit(value >> 4));
                builder.Append(GetUpperHexadecimalDigit(value & 0x0F));
            }

            index++;
        }

        return builder.ToString();
    }

    static JsonPointer ParseDecodedRepresentation(
        string representation,
        JsonPointerEncoding encoding,
        string originalRepresentation)
    {
        global::System.Collections.Generic.List<string> tokens =
            new global::System.Collections.Generic.List<string>();
        if (representation.Length != 0)
        {
            if (representation[0] != '/')
            {
                throw CreatePointerFailure(
                    JsonDiagnosticCode.InvalidPointerSyntax,
                    "A non-empty JSON Pointer representation must begin with '/'.",
                    "pointer string");
            }

            global::System.Text.StringBuilder token = new global::System.Text.StringBuilder();
            int index = 1;
            while (index <= representation.Length)
            {
                if (index == representation.Length || representation[index] == '/')
                {
                    string decodedToken = token.ToString();
                    ValidateUnicodeText(decodedToken);
                    tokens.Add(decodedToken);
                    token.Clear();
                    index++;
                    continue;
                }

                char character = representation[index];
                if (character == '~')
                {
                    if (index + 1 >= representation.Length)
                    {
                        throw CreatePointerFailure(
                            JsonDiagnosticCode.InvalidPointerEscape,
                            "A JSON Pointer tilde escape is incomplete.",
                            "pointer string");
                    }

                    char escaped = representation[index + 1];
                    if (escaped == '0')
                    {
                        token.Append('~');
                    }
                    else if (escaped == '1')
                    {
                        token.Append('/');
                    }
                    else
                    {
                        throw CreatePointerFailure(
                            JsonDiagnosticCode.InvalidPointerEscape,
                            "A JSON Pointer tilde escape must be '~0' or '~1'.",
                            "pointer string");
                    }

                    index += 2;
                    continue;
                }

                token.Append(character);
                index++;
            }
        }

        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = encoding;
        pointer.OriginalRepresentation = originalRepresentation;
        pointer.ReferenceTokens = tokens;
        return pointer;
    }

    static global::System.Collections.Generic.List<string> GetValidatedReferenceTokens(
        JsonPointer? pointer)
    {
        if (pointer == null || pointer.ReferenceTokens == null)
        {
            throw CreatePointerFailure(
                JsonDiagnosticCode.InvalidPointerSyntax,
                "A decoded JSON Pointer with reference tokens is required.",
                "pointer model");
        }

        foreach (string? token in pointer.ReferenceTokens)
        {
            if (token == null)
            {
                throw CreatePointerFailure(
                    JsonDiagnosticCode.InvalidPointerSyntax,
                    "A JSON Pointer reference token cannot be null.",
                    "pointer model");
            }

            ValidateUnicodeText(token);
        }

        return pointer.ReferenceTokens;
    }

    static string BuildStringRepresentation(
        global::System.Collections.Generic.List<string> tokens)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        foreach (string token in tokens)
        {
            builder.Append('/');
            int index = 0;
            while (index < token.Length)
            {
                char character = token[index];
                if (character == '~')
                {
                    builder.Append("~0");
                }
                else if (character == '/')
                {
                    builder.Append("~1");
                }
                else
                {
                    builder.Append(character);
                }

                index++;
            }
        }

        return builder.ToString();
    }

    static string DecodeUtf8Fragment(global::System.Collections.Generic.List<byte> bytes)
    {
        Utf8Decoder decoder = new Utf8Decoder();
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        foreach (byte value in bytes)
        {
            int scalar = decoder.PushByte(value);
            if (scalar != Utf8Decoder.NeedMoreData)
            {
                AppendUnicodeScalar(builder, scalar);
            }
        }

        decoder.CompleteDecoder();
        return builder.ToString();
    }

    static void AppendUnicodeScalar(global::System.Text.StringBuilder builder, int scalar)
    {
        if (scalar <= 0xFFFF)
        {
            builder.Append((char)scalar);
            return;
        }

        int adjusted = scalar - 0x10000;
        builder.Append((char)(0xD800 + (adjusted >> 10)));
        builder.Append((char)(0xDC00 + (adjusted & 0x3FF)));
    }

    static void ValidateUnicodeText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            int character = text[index];
            if (character >= 0xD800 && character <= 0xDBFF)
            {
                if (index + 1 >= text.Length)
                {
                    throw CreatePointerFailure(
                        JsonDiagnosticCode.InvalidPointerSyntax,
                        "A JSON Pointer contains an unpaired high surrogate.",
                        "pointer text");
                }

                int low = text[index + 1];
                if (low < 0xDC00 || low > 0xDFFF)
                {
                    throw CreatePointerFailure(
                        JsonDiagnosticCode.InvalidPointerSyntax,
                        "A JSON Pointer contains an unpaired high surrogate.",
                        "pointer text");
                }

                index += 2;
                continue;
            }

            if (character >= 0xDC00 && character <= 0xDFFF)
            {
                throw CreatePointerFailure(
                    JsonDiagnosticCode.InvalidPointerSyntax,
                    "A JSON Pointer contains an unpaired low surrogate.",
                    "pointer text");
            }

            index++;
        }
    }

    static bool IsFragmentCharacter(byte value)
    {
        if ((value >= 0x41 && value <= 0x5A) ||
            (value >= 0x61 && value <= 0x7A) ||
            (value >= 0x30 && value <= 0x39))
        {
            return true;
        }

        return value == 0x2D ||
            value == 0x2E ||
            value == 0x5F ||
            value == 0x7E ||
            value == 0x21 ||
            value == 0x24 ||
            value == 0x26 ||
            value == 0x27 ||
            value == 0x28 ||
            value == 0x29 ||
            value == 0x2A ||
            value == 0x2B ||
            value == 0x2C ||
            value == 0x3B ||
            value == 0x3D ||
            value == 0x3A ||
            value == 0x40 ||
            value == 0x2F ||
            value == 0x3F;
    }

    static int GetHexadecimalValue(char character)
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

    static char GetUpperHexadecimalDigit(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }

        return (char)('A' + value - 10);
    }

    static JsonPointerException CreatePointerFailure(
        JsonDiagnosticCode code,
        string message,
        string actualToken)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ActualToken = actualToken;
        return new JsonPointerException(diagnostic);
    }
}
