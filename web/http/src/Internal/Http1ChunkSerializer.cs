using System;
using System.Text;

namespace http;

internal class Http1ChunkSerializationResult
{
    internal HttpOperationStatus Status;
    internal string ChunkExtension = "";
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1ChunkSerializer
{
    internal static Http1ChunkSerializationResult PrepareChunkedMessage(
        HttpHeaders headers,
        HttpHeaders trailers,
        Http1FramingDecision framing,
        Http1SerializationOptions options)
    {
        Http1ChunkSerializationResult result = new Http1ChunkSerializationResult();
        Http1TrailerValidationResult declarations =
            Http1TrailerValidator.ValidateDeclarations(headers, options.Limits);
        if (declarations.Status != HttpOperationStatus.Done)
        {
            return RejectChunkSerialization(
                result,
                declarations.ErrorCode ?? "invalid-http1-trailer-declaration",
                declarations.ErrorMessage ??
                    "The HTTP/1 Trailer field declaration is invalid.");
        }

        if (!ValidateSerializedTrailers(trailers, options.Limits, result))
        {
            return result;
        }

        if (framing.BodyFraming != Http1BodyFraming.Chunked)
        {
            if (trailers.Fields.Count != 0)
            {
                return RejectChunkSerialization(
                    result,
                    "http1-trailers-require-chunked",
                    "HTTP/1 trailers require chunked transfer coding.");
            }

            result.Status = HttpOperationStatus.Done;
            return result;
        }

        if (!ValidateAndNormalizeExtension(options.ChunkExtension, result))
        {
            return result;
        }

        if (trailers.Fields.Count != 0 && options.DeclareTrailers)
        {
            AddMissingTrailerDeclaration(
                headers,
                trailers,
                options.PreserveFieldNameCasing);
            declarations = Http1TrailerValidator.ValidateDeclarations(
                headers,
                options.Limits);
            if (declarations.Status != HttpOperationStatus.Done)
            {
                return RejectChunkSerialization(
                    result,
                    declarations.ErrorCode ?? "invalid-http1-trailer-declaration",
                    declarations.ErrorMessage ??
                        "The generated HTTP/1 Trailer field exceeds configured limits.");
            }
        }

        result.Status = HttpOperationStatus.Done;
        return result;
    }

    internal static long CalculateEncodedSize(
        byte[] content,
        int chunkSize,
        string chunkExtension,
        HttpHeaders trailers)
    {
        long size = 0;
        int offset = 0;
        while (offset < content.Length)
        {
            int count = content.Length - offset;
            if (count > chunkSize)
            {
                count = chunkSize;
            }

            size = size + HexadecimalSizeLength(count) + chunkExtension.Length +
                2L + count + 2;
            offset = offset + count;
        }

        size = size + 1L + chunkExtension.Length + 2;
        int trailerIndex = 0;
        while (trailerIndex < trailers.Fields.Count)
        {
            HttpHeaderField field = trailers.Fields[trailerIndex];
            size = size + field.Name.Length + 2L + field.Value.Length + 2;
            trailerIndex = trailerIndex + 1;
        }

        return size + 2;
    }

    static bool ValidateAndNormalizeExtension(
        string? extension,
        Http1ChunkSerializationResult result)
    {
        if (extension == null)
        {
            result.ChunkExtension = "";
            return true;
        }

        if (extension.Length == 0)
        {
            RejectChunkSerialization(
                result,
                "invalid-http1-chunk-extension",
                "An HTTP/1 chunk extension cannot be empty.");
            return false;
        }

        int characterIndex = 0;
        while (characterIndex < extension.Length)
        {
            if (extension[characterIndex] > 0xff)
            {
                RejectChunkSerialization(
                    result,
                    "invalid-http1-chunk-extension",
                    "An HTTP/1 chunk extension must contain only octet characters.");
                return false;
            }

            characterIndex = characterIndex + 1;
        }

        string normalized = extension;
        if (extension[0] != ';' && extension[0] != 0x20 && extension[0] != 0x09)
        {
            normalized = ";" + extension;
        }

        byte[] line = CreateChunkLineBytes(normalized);
        Http1ChunkLineResult parsed = Http1ChunkCodec.ParseChunkLine(line);
        if (parsed.Status != HttpOperationStatus.Done || parsed.ChunkSize != 1)
        {
            RejectChunkSerialization(
                result,
                "invalid-http1-chunk-extension",
                "The configured HTTP/1 chunk extension is malformed.");
            return false;
        }

        result.ChunkExtension = normalized;
        return true;
    }

    static byte[] CreateChunkLineBytes(string extension)
    {
        byte[] result = new byte[extension.Length + 1];
        result[0] = 0x31;
        int index = 0;
        while (index < extension.Length)
        {
            result[index + 1] = (byte)extension[index];
            index = index + 1;
        }

        return result;
    }

    static bool ValidateSerializedTrailers(
        HttpHeaders trailers,
        HttpLimits limits,
        Http1ChunkSerializationResult result)
    {
        if (trailers.Fields.Count > limits.MaximumTrailerCount)
        {
            RejectChunkSerialization(
                result,
                "http1-trailer-count-limit-exceeded",
                "The HTTP/1 trailer count exceeds the configured limit.");
            return false;
        }

        long sectionSize = 0;
        int index = 0;
        while (index < trailers.Fields.Count)
        {
            HttpHeaderField field = trailers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                RejectChunkSerialization(
                    result,
                    "invalid-http1-trailer-field",
                    "The HTTP/1 trailer contains an invalid field name or value.");
                return false;
            }

            if (HeaderStore.IsForbiddenTrailerName(field.Name))
            {
                RejectChunkSerialization(
                    result,
                    "forbidden-http1-trailer-field",
                    "The field is forbidden in an HTTP/1 trailer section.");
                return false;
            }

            long lineSize = (long)field.Name.Length + 2 + field.Value.Length;
            if (field.Value.Length > limits.MaximumFieldValueSize)
            {
                RejectChunkSerialization(
                    result,
                    "http1-field-value-limit-exceeded",
                    "An HTTP/1 trailer field value exceeds the configured limit.");
                return false;
            }

            if (lineSize > limits.MaximumFieldLineSize)
            {
                RejectChunkSerialization(
                    result,
                    "http1-field-line-limit-exceeded",
                    "An HTTP/1 trailer field line exceeds the configured limit.");
                return false;
            }

            sectionSize = sectionSize + lineSize;
            if (sectionSize > limits.MaximumTrailerSectionSize)
            {
                RejectChunkSerialization(
                    result,
                    "http1-trailer-section-limit-exceeded",
                    "The HTTP/1 trailer section exceeds the configured limit.");
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void AddMissingTrailerDeclaration(
        HttpHeaders headers,
        HttpHeaders trailers,
        bool preserveFieldNameCasing)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < trailers.Fields.Count)
        {
            string name = trailers.Fields[index].Name;
            if (!IsTrailerNameDeclared(headers, name) &&
                !AppearsEarlierInTrailers(trailers, index, name))
            {
                if (value.Length != 0)
                {
                    value.Append(", ");
                }

                value.Append(preserveFieldNameCasing ?
                    name : CanonicalizeDeclaredTrailerName(name));
            }

            index = index + 1;
        }

        if (value.Length != 0)
        {
            HttpHeaderField declaration = new HttpHeaderField();
            declaration.Name = HttpHeaderNames.Trailer;
            declaration.Value = Encoding.ASCII.GetBytes(value.ToString());
            headers.Fields.Add(declaration);
        }
    }

    static string CanonicalizeDeclaredTrailerName(string value)
    {
        StringBuilder result = new StringBuilder();
        bool upper = true;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= 'A' && character <= 'Z' && !upper)
            {
                character = (char)(character + 0x20);
            }
            else if (character >= 'a' && character <= 'z' && upper)
            {
                character = (char)(character - 0x20);
            }

            result.Append(character);
            upper = character == '-';
            index = index + 1;
        }

        return result.ToString();
    }

    static bool IsTrailerNameDeclared(HttpHeaders headers, string name)
    {
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Trailer))
            {
                FieldValueParseResult parsed = FieldValueParser.ParseTrailer(
                    field.Value,
                    int.MaxValue,
                    int.MaxValue);
                int nameIndex = 0;
                while (nameIndex < parsed.Values.Count)
                {
                    string declared = Encoding.ASCII.GetString(parsed.Values[nameIndex]);
                    if (Ascii.EqualsStringsIgnoreCase(declared, name))
                    {
                        return true;
                    }

                    nameIndex = nameIndex + 1;
                }
            }

            fieldIndex = fieldIndex + 1;
        }

        return false;
    }

    static bool AppearsEarlierInTrailers(
        HttpHeaders trailers,
        int end,
        string name)
    {
        int index = 0;
        while (index < end)
        {
            if (Ascii.EqualsStringsIgnoreCase(trailers.Fields[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static int HexadecimalSizeLength(int value)
    {
        int length = 1;
        while (value >= 16)
        {
            value = value / 16;
            length = length + 1;
        }

        return length;
    }

    static Http1ChunkSerializationResult RejectChunkSerialization(
        Http1ChunkSerializationResult result,
        string code,
        string message)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
