namespace svg;

/// <summary>Parses and encodes bounded RFC 2397 data URLs.</summary>
public class SvgDataUrl : ISvgDataUrl
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgDataUrl()
    {
    }

    /// <summary>The default maximum decoded payload size.</summary>
    internal const long DefaultMaxDecodedBytes = 16777216;

    /// <summary>Parses a data URL using the secure default decoded-byte limit.</summary>
    /// <param name="value">The complete data URL.</param>
    /// <returns>The decoded payload and media metadata.</returns>
    public SvgDataUrlResult ParseWithValue(string value)
    {
        return new SvgDataUrl().ParseWithValueAndMaxDecodedBytes(value, DefaultMaxDecodedBytes);
    }

    /// <summary>Parses a data URL using resource security limits.</summary>
    /// <param name="value">The complete data URL.</param>
    /// <param name="limits">The configured resource limits.</param>
    /// <returns>The decoded payload and media metadata.</returns>
    public SvgDataUrlResult ParseWithValueAndLimits(string value, SvgResourceLimits? limits)
    {
        if (limits == null) return new SvgDataUrl().ParseFailure(SvgDataUrlError.InvalidArgument, 0, value);
        return new SvgDataUrl().ParseWithValueAndMaxDecodedBytes(value, limits.MaxDataUrlBytes);
    }

    /// <summary>Parses a data URL with an explicit maximum decoded payload size.</summary>
    /// <param name="value">The complete data URL.</param>
    /// <param name="maxDecodedBytes">The maximum decoded byte count.</param>
    /// <returns>The decoded payload and media metadata, or a stable failure.</returns>
    public SvgDataUrlResult ParseWithValueAndMaxDecodedBytes(string? value, long maxDecodedBytes)
    {
        if (value == null || maxDecodedBytes < 0) return new SvgDataUrl().ParseFailure(SvgDataUrlError.InvalidArgument, 0, value);
        if (value.Length < 5 || !value.StartsWith("data:", System.StringComparison.OrdinalIgnoreCase)) return new SvgDataUrl().ParseFailure(SvgDataUrlError.InvalidScheme, 0, value);
        int comma = value.IndexOf(',', 5);
        if (comma < 0) return new SvgDataUrl().ParseFailure(SvgDataUrlError.MissingComma, value.Length, value);
        string metadataText = value.Substring(5, comma - 5);
        SvgDataMetadata metadata = new SvgDataUrl().ParseMetadata(metadataText, 5);
        if (!metadata.Success) return new SvgDataUrl().ParseFailure(metadata.Error, metadata.ErrorOffset, value);
        string payload = value.Substring(comma + 1);
        SvgDataBytes decoded = metadata.IsBase64 ? new SvgDataUrl().DecodeBase64(payload, comma + 1, maxDecodedBytes) : new SvgDataUrl().DecodePercent(payload, comma + 1, maxDecodedBytes);
        if (!decoded.Success) return new SvgDataUrl().ParseFailure(decoded.Error, decoded.ErrorOffset, value);
        SvgDataUrlResult result = new SvgDataUrlResult();
        result.Success = true;
        result.Error = SvgDataUrlError.None;
        result.ErrorOffset = -1;
        result.MediaType = metadata.MediaType;
        result.Charset = new SvgDataUrl().FindCharset(metadata.MediaType);
        result.IsBase64 = metadata.IsBase64;
        result.Bytes = decoded.Bytes;
        result.Source = value;
        result.DataOffset = comma + 1;
        return result;
    }

    /// <summary>Encodes bytes as a data URL using the secure default decoded-byte limit.</summary>
    /// <param name="bytes">The payload bytes.</param>
    /// <param name="mediaType">A complete media type, or an empty string for the RFC default.</param>
    /// <param name="useBase64">Whether to use base64 instead of percent encoding.</param>
    /// <returns>The encoded data URL or a stable failure.</returns>
    public SvgDataUrlEncodeResult EncodeWithBytesAndMediaTypeAndUseBase64(byte[] bytes, string mediaType, bool useBase64)
    {
        return new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(bytes, mediaType, useBase64, DefaultMaxDecodedBytes);
    }

    /// <summary>Encodes bytes as a data URL using resource security limits.</summary>
    /// <param name="bytes">The payload bytes.</param>
    /// <param name="mediaType">A complete media type, or an empty string for the RFC default.</param>
    /// <param name="useBase64">Whether to use base64 instead of percent encoding.</param>
    /// <param name="limits">The configured resource limits.</param>
    /// <returns>The encoded data URL or a stable failure.</returns>
    public SvgDataUrlEncodeResult EncodeWithBytesAndMediaTypeAndUseBase64AndLimits(byte[] bytes, string mediaType, bool useBase64, SvgResourceLimits? limits)
    {
        if (limits == null) return new SvgDataUrl().EncodeFailure(SvgDataUrlError.InvalidArgument);
        return new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(bytes, mediaType, useBase64, limits.MaxDataUrlBytes);
    }

    /// <summary>Encodes bytes as a data URL with an explicit decoded-byte limit.</summary>
    /// <param name="bytes">The payload bytes.</param>
    /// <param name="mediaType">A complete media type, or an empty string for the RFC default.</param>
    /// <param name="useBase64">Whether to use base64 instead of percent encoding.</param>
    /// <param name="maxDecodedBytes">The maximum payload byte count.</param>
    /// <returns>The encoded data URL or a stable failure.</returns>
    public SvgDataUrlEncodeResult EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(byte[]? bytes, string? mediaType, bool useBase64, long maxDecodedBytes)
    {
        if (bytes == null || mediaType == null || maxDecodedBytes < 0) return new SvgDataUrl().EncodeFailure(SvgDataUrlError.InvalidArgument);
        if (bytes.LongLength > maxDecodedBytes) return new SvgDataUrl().EncodeFailure(SvgDataUrlError.DecodedSizeLimitExceeded);
        SvgMediaType parsedMediaType;
        string metadata;
        if (mediaType.Length == 0)
        {
            parsedMediaType = new SvgDataUrl().DefaultMediaType("");
            metadata = "";
        }
        else
        {
            SvgMediaTypeParseResult parsed = new SvgMediaTypeParser().Parse(mediaType, 64, 8192);
            if (!parsed.Success) return new SvgDataUrl().EncodeFailure(SvgDataUrlError.InvalidMediaType);
            parsedMediaType = (parsed.MediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            metadata = new SvgDataUrl().EncodeMediaType(parsed.MediaType);
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder("data:");
        builder.Append(metadata);
        if (useBase64) builder.Append(";base64");
        builder.Append(',');
        if (useBase64) builder.Append(System.Convert.ToBase64String(bytes));
        else new SvgDataUrl().AppendPercentEncoded(builder, bytes);
        SvgDataUrlEncodeResult result = new SvgDataUrlEncodeResult();
        result.Success = true;
        result.Error = SvgDataUrlError.None;
        result.DataUrl = builder.ToString();
        result.MediaType = parsedMediaType;
        result.IsBase64 = useBase64;
        return result;
    }

    /// <summary>Encodes UTF-8 text as a data URL with an explicit media type.</summary>
    /// <param name="text">The Unicode text.</param>
    /// <param name="mediaType">A complete text media type.</param>
    /// <param name="useBase64">Whether to use base64 instead of percent encoding.</param>
    /// <returns>The encoded data URL or a stable failure.</returns>
    public SvgDataUrlEncodeResult EncodeText(string? text, string mediaType, bool useBase64)
    {
        if (text == null) return new SvgDataUrl().EncodeFailure(SvgDataUrlError.InvalidArgument);
        return new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(System.Text.Encoding.UTF8.GetBytes(text), mediaType, useBase64, DefaultMaxDecodedBytes);
    }
SvgDataMetadata ParseMetadata(string source, int sourceOffset)
    {
        SvgDataMetadata result = new SvgDataMetadata();
        string text = source;
        int marker = text.LastIndexOf(';');
        if (marker >= 0 && string.Equals(text.Substring(marker + 1), "base64", System.StringComparison.OrdinalIgnoreCase))
        {
            result.IsBase64 = true;
            text = text.Substring(0, marker);
        }
        if (text.Length == 0)
        {
            result.MediaType = new SvgDataUrl().DefaultMediaType(source);
            result.Success = true;
            result.Error = SvgDataUrlError.None;
            return result;
        }
        string[] pieces = text.Split(';');
        int parameterStart;
        string typeText;
        bool omittedType = text[0] == ';';
        if (omittedType)
        {
            typeText = "text/plain";
            parameterStart = 1;
        }
        else
        {
            typeText = pieces[0];
            parameterStart = 1;
        }
        SvgDataText typeDecoded = new SvgDataUrl().DecodeMetadataText(typeText, sourceOffset);
        if (!typeDecoded.Success) return new SvgDataUrl().MetadataFailure(typeDecoded.Error, typeDecoded.ErrorOffset);
        int slash = typeDecoded.Value.IndexOf('/');
        if (slash < 1 || slash != typeDecoded.Value.LastIndexOf('/') || slash == typeDecoded.Value.Length - 1)
        {
            return new SvgDataUrl().MetadataFailure(SvgDataUrlError.InvalidMediaType, sourceOffset);
        }
        string type = typeDecoded.Value.Substring(0, slash);
        string subtype = typeDecoded.Value.Substring(slash + 1);
        if (!SvgIriLexical.IsMimeToken(type) || !SvgIriLexical.IsMimeToken(subtype)) return new SvgDataUrl().MetadataFailure(SvgDataUrlError.InvalidMediaType, sourceOffset);
        System.Collections.Generic.List<SvgMediaTypeParameter> parameters = new System.Collections.Generic.List<SvgMediaTypeParameter>();
        int pieceIndex = parameterStart;
        int runningOffset = pieces.Length == 0 ? sourceOffset : sourceOffset + pieces[0].Length + 1;
        while (pieceIndex < pieces.Length)
        {
            if (parameters.Count >= 64 || pieces[pieceIndex].Length == 0) return new SvgDataUrl().MetadataFailure(SvgDataUrlError.InvalidMediaType, runningOffset);
            string piece = pieces[pieceIndex];
            int equals = piece.IndexOf('=');
            if (equals < 1 || equals == piece.Length - 1 || piece.IndexOf('=', equals + 1) >= 0) return new SvgDataUrl().MetadataFailure(SvgDataUrlError.InvalidMediaType, runningOffset);
            if (!new SvgDataUrl().ValidRawParameterValue(piece.Substring(equals + 1))) return new SvgDataUrl().MetadataFailure(SvgDataUrlError.InvalidMediaType, runningOffset + equals + 1);
            SvgDataText name = new SvgDataUrl().DecodeMetadataText(piece.Substring(0, equals), runningOffset);
            SvgDataText parameterValue = new SvgDataUrl().DecodeMetadataText(piece.Substring(equals + 1), runningOffset + equals + 1);
            if (!name.Success) return new SvgDataUrl().MetadataFailure(name.Error, name.ErrorOffset);
            if (!parameterValue.Success) return new SvgDataUrl().MetadataFailure(parameterValue.Error, parameterValue.ErrorOffset);
            if (!SvgIriLexical.IsMimeToken(name.Value) || !new SvgDataUrl().ValidParameterValue(parameterValue.Value)) return new SvgDataUrl().MetadataFailure(SvgDataUrlError.InvalidMediaType, runningOffset);
            parameters.Add(new SvgMediaTypeParameter(name.Value.ToLowerInvariant(), parameterValue.Value, runningOffset, runningOffset + piece.Length));
            runningOffset = runningOffset + piece.Length + 1;
            pieceIndex = pieceIndex + 1;
        }
        if (omittedType && !new SvgDataUrl().HasCharset(parameters)) parameters.Insert(0, new SvgMediaTypeParameter("charset", "US-ASCII", 0, 0));
        result.MediaType = new SvgMediaType(type, subtype, parameters, source, 0, source.Length);
        result.Success = true;
        result.Error = SvgDataUrlError.None;
        return result;
    }
SvgMediaType DefaultMediaType(string source)
    {
        System.Collections.Generic.List<SvgMediaTypeParameter> parameters = new System.Collections.Generic.List<SvgMediaTypeParameter>();
        parameters.Add(new SvgMediaTypeParameter("charset", "US-ASCII", 0, 0));
        return new SvgMediaType("text", "plain", parameters, source, 0, source.Length);
    }
SvgDataText DecodeMetadataText(string value, int sourceOffset)
    {
        SvgDataText result = new SvgDataText();
        System.Collections.Generic.List<byte> bytes = new System.Collections.Generic.List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (index + 2 >= value.Length || new SvgDataUrl().Hex(value[index + 1]) < 0 || new SvgDataUrl().Hex(value[index + 2]) < 0)
                {
                    result.Error = SvgDataUrlError.InvalidPercentEscape;
                    result.ErrorOffset = sourceOffset + index;
                    return result;
                }
                bytes.Add((byte)(new SvgDataUrl().Hex(value[index + 1]) * 16 + new SvgDataUrl().Hex(value[index + 2])));
                index = index + 3;
            }
            else if (character < 128)
            {
                if (!new SvgDataUrl().IsRawUrlCharacter(character) || character == ',' || character == ';' || character == '#')
                {
                    result.Error = SvgDataUrlError.InvalidMediaType;
                    result.ErrorOffset = sourceOffset + index;
                    return result;
                }
                bytes.Add((byte)character);
                index = index + 1;
            }
            else
            {
                int count = char.IsHighSurrogate(character) ? 2 : 1;
                if (count == 2 && (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1])) || char.IsLowSurrogate(character))
                {
                    result.Error = SvgDataUrlError.InvalidMediaType;
                    result.ErrorOffset = sourceOffset + index;
                    return result;
                }
                byte[] encoded = System.Text.Encoding.UTF8.GetBytes(value.Substring(index, count));
                int encodedIndex = 0;
                while (encodedIndex < encoded.Length)
                {
                    bytes.Add(encoded[encodedIndex]);
                    encodedIndex = encodedIndex + 1;
                }
                index = index + count;
            }
            if (bytes.Count > 8192)
            {
                result.Error = SvgDataUrlError.InvalidMediaType;
                result.ErrorOffset = sourceOffset + index;
                return result;
            }
        }
        byte[] array = bytes.ToArray();
        if (!new SvgDataUrl().ValidUtf8(array))
        {
            result.Error = SvgDataUrlError.InvalidMediaType;
            result.ErrorOffset = sourceOffset;
            return result;
        }
        result.Success = true;
        result.Error = SvgDataUrlError.None;
        result.ErrorOffset = -1;
        result.Value = System.Text.Encoding.UTF8.GetString(array);
        return result;
    }
SvgDataBytes DecodePercent(string payload, int sourceOffset, long maximum)
    {
        SvgDataBytes result = new SvgDataBytes();
        System.Collections.Generic.List<byte> bytes = new System.Collections.Generic.List<byte>();
        int index = 0;
        while (index < payload.Length)
        {
            char character = payload[index];
            if (character == '%')
            {
                if (index + 2 >= payload.Length || new SvgDataUrl().Hex(payload[index + 1]) < 0 || new SvgDataUrl().Hex(payload[index + 2]) < 0) return new SvgDataUrl().BytesFailure(SvgDataUrlError.InvalidPercentEscape, sourceOffset + index);
                if (!new SvgDataUrl().CanAppend(bytes.Count, 1, maximum)) return new SvgDataUrl().BytesFailure(SvgDataUrlError.DecodedSizeLimitExceeded, sourceOffset + index);
                bytes.Add((byte)(new SvgDataUrl().Hex(payload[index + 1]) * 16 + new SvgDataUrl().Hex(payload[index + 2])));
                index = index + 3;
            }
            else if (character < 128)
            {
                if (!new SvgDataUrl().IsRawUrlCharacter(character) || character == '#') return new SvgDataUrl().BytesFailure(SvgDataUrlError.InvalidPayload, sourceOffset + index);
                if (!new SvgDataUrl().CanAppend(bytes.Count, 1, maximum)) return new SvgDataUrl().BytesFailure(SvgDataUrlError.DecodedSizeLimitExceeded, sourceOffset + index);
                bytes.Add((byte)character);
                index = index + 1;
            }
            else
            {
                int count = char.IsHighSurrogate(character) ? 2 : 1;
                if (count == 2 && (index + 1 >= payload.Length || !char.IsLowSurrogate(payload[index + 1])) || char.IsLowSurrogate(character)) return new SvgDataUrl().BytesFailure(SvgDataUrlError.InvalidPayload, sourceOffset + index);
                byte[] encoded = System.Text.Encoding.UTF8.GetBytes(payload.Substring(index, count));
                if (!new SvgDataUrl().CanAppend(bytes.Count, encoded.Length, maximum)) return new SvgDataUrl().BytesFailure(SvgDataUrlError.DecodedSizeLimitExceeded, sourceOffset + index);
                int encodedIndex = 0;
                while (encodedIndex < encoded.Length)
                {
                    bytes.Add(encoded[encodedIndex]);
                    encodedIndex = encodedIndex + 1;
                }
                index = index + count;
            }
        }
        result.Success = true;
        result.Error = SvgDataUrlError.None;
        result.ErrorOffset = -1;
        result.Bytes = bytes.ToArray();
        return result;
    }
SvgDataBytes DecodeBase64(string payload, int sourceOffset, long maximum)
    {
        System.Text.StringBuilder normalized = new System.Text.StringBuilder();
        int index = 0;
        while (index < payload.Length)
        {
            char character = payload[index];
            int offset = index;
            if (character == '%')
            {
                if (index + 2 >= payload.Length || new SvgDataUrl().Hex(payload[index + 1]) < 0 || new SvgDataUrl().Hex(payload[index + 2]) < 0) return new SvgDataUrl().BytesFailure(SvgDataUrlError.InvalidPercentEscape, sourceOffset + index);
                character = (char)(new SvgDataUrl().Hex(payload[index + 1]) * 16 + new SvgDataUrl().Hex(payload[index + 2]));
                index = index + 3;
            }
            else index = index + 1;
            if (new SvgDataUrl().IsBase64Whitespace(character)) continue;
            if (new SvgDataUrl().Base64Value(character) < 0 && character != '=') return new SvgDataUrl().BytesFailure(SvgDataUrlError.InvalidBase64, sourceOffset + offset);
            normalized.Append(character);
            long encodedLimit = maximum > 1610612733 ? 2147483647 : ((maximum + 2) / 3) * 4 + 4;
            if (normalized.Length > encodedLimit) return new SvgDataUrl().BytesFailure(SvgDataUrlError.DecodedSizeLimitExceeded, sourceOffset + offset);
        }
        if (normalized.Length == 0) return new SvgDataUrl().BytesSuccess(System.Array.Empty<byte>());
        if (normalized.Length % 4 != 0) return new SvgDataUrl().BytesFailure(SvgDataUrlError.InvalidBase64, sourceOffset + payload.Length);
        int padding = 0;
        if (normalized[normalized.Length - 1] == '=') padding = padding + 1;
        if (normalized.Length > 1 && normalized[normalized.Length - 2] == '=') padding = padding + 1;
        long decodedLength = (long)(normalized.Length / 4) * 3 - padding;
        if (decodedLength > maximum || decodedLength > 2147483647) return new SvgDataUrl().BytesFailure(SvgDataUrlError.DecodedSizeLimitExceeded, sourceOffset + payload.Length);
        byte[] bytes = new byte[(int)decodedLength];
        int quartet = 0;
        int output = 0;
        while (quartet < normalized.Length)
        {
            int first = new SvgDataUrl().Base64Value(normalized[quartet]);
            int second = new SvgDataUrl().Base64Value(normalized[quartet + 1]);
            bool thirdPadding = normalized[quartet + 2] == '=';
            bool fourthPadding = normalized[quartet + 3] == '=';
            int third = thirdPadding ? 0 : new SvgDataUrl().Base64Value(normalized[quartet + 2]);
            int fourth = fourthPadding ? 0 : new SvgDataUrl().Base64Value(normalized[quartet + 3]);
            bool finalQuartet = quartet + 4 == normalized.Length;
            if (first < 0 || second < 0 || third < 0 || fourth < 0 || thirdPadding && !fourthPadding || (thirdPadding || fourthPadding) && !finalQuartet) return new SvgDataUrl().BytesFailure(SvgDataUrlError.InvalidBase64, sourceOffset + payload.Length);
            if (thirdPadding && (second & 15) != 0 || fourthPadding && !thirdPadding && (third & 3) != 0) return new SvgDataUrl().BytesFailure(SvgDataUrlError.InvalidBase64, sourceOffset + payload.Length);
            if (output < bytes.Length) bytes[output] = (byte)(first * 4 + second / 16);
            output = output + 1;
            if (!thirdPadding)
            {
                if (output < bytes.Length) bytes[output] = (byte)((second & 15) * 16 + third / 4);
                output = output + 1;
            }
            if (!fourthPadding)
            {
                if (output < bytes.Length) bytes[output] = (byte)((third & 3) * 64 + fourth);
                output = output + 1;
            }
            quartet = quartet + 4;
        }
        return new SvgDataUrl().BytesSuccess(bytes);
    }
string EncodeMediaType(SvgMediaType? mediaType)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        new SvgDataUrl().AppendMetadataToken(builder, (mediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Type);
        builder.Append('/');
        new SvgDataUrl().AppendMetadataToken(builder, mediaType.Subtype);
        int index = 0;
        while (index < mediaType.Parameters.Count)
        {
            SvgMediaTypeParameter parameter = mediaType.Parameters[index];
            builder.Append(';');
            new SvgDataUrl().AppendMetadataToken(builder, parameter.Name);
            builder.Append('=');
            new SvgDataUrl().AppendPercentEncoded(builder, System.Text.Encoding.UTF8.GetBytes(parameter.Value));
            index = index + 1;
        }
        return builder.ToString();
    }
void AppendMetadataToken(System.Text.StringBuilder builder, string value)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        int index = 0;
        while (index < bytes.Length)
        {
            byte current = bytes[index];
            if (new SvgDataUrl().IsSafeMetadataTokenByte(current)) builder.Append((char)current);
            else builder.Append('%').Append(current.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }
    }
void AppendPercentEncoded(System.Text.StringBuilder builder, byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            byte value = bytes[index];
            if (new SvgDataUrl().IsUnreservedByte(value)) builder.Append((char)value);
            else builder.Append('%').Append(value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }
    }
string FindCharset(SvgMediaType? mediaType)
    {
        string result = "";
        int index = 0;
        while (index < (mediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parameters.Count)
        {
            if (string.Equals(mediaType.Parameters[index].Name, "charset", System.StringComparison.OrdinalIgnoreCase)) result = mediaType.Parameters[index].Value;
            index = index + 1;
        }
        return result;
    }
bool HasCharset(System.Collections.Generic.List<SvgMediaTypeParameter> parameters)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            if (string.Equals(parameters[index].Name, "charset", System.StringComparison.OrdinalIgnoreCase)) return true;
            index = index + 1;
        }
        return false;
    }
bool ValidRawParameterValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                if (index + 2 >= value.Length || new SvgDataUrl().Hex(value[index + 1]) < 0 || new SvgDataUrl().Hex(value[index + 2]) < 0) return false;
                index = index + 3;
            }
            else
            {
                if (!SvgIriLexical.IsMimeTokenCharacter(value[index]) || value[index] == '#') return false;
                index = index + 1;
            }
        }
        return true;
    }
bool ValidParameterValue(string value)
    {
        if (value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 32 || value[index] == 127) return false;
            index = index + 1;
        }
        return true;
    }
SvgDataUrlResult ParseFailure(SvgDataUrlError error, int offset, string? source)
    {
        SvgDataUrlResult result = new SvgDataUrlResult();
        result.Error = error;
        result.ErrorOffset = offset;
        result.Source = source == null ? "" : source;
        return result;
    }
SvgDataUrlEncodeResult EncodeFailure(SvgDataUrlError error)
    {
        SvgDataUrlEncodeResult result = new SvgDataUrlEncodeResult();
        result.Error = error;
        return result;
    }
SvgDataMetadata MetadataFailure(SvgDataUrlError error, int offset)
    {
        SvgDataMetadata result = new SvgDataMetadata();
        result.Error = error;
        result.ErrorOffset = offset;
        return result;
    }
SvgDataBytes BytesFailure(SvgDataUrlError error, int offset)
    {
        SvgDataBytes result = new SvgDataBytes();
        result.Error = error;
        result.ErrorOffset = offset;
        return result;
    }
SvgDataBytes BytesSuccess(byte[] bytes)
    {
        SvgDataBytes result = new SvgDataBytes();
        result.Success = true;
        result.Error = SvgDataUrlError.None;
        result.ErrorOffset = -1;
        result.Bytes = bytes;
        return result;
    }
bool CanAppend(int count, int amount, long maximum)
    {
        return (long)count + amount <= maximum && (long)count + amount <= 2147483647;
    }
bool IsRawUrlCharacter(char value)
    {
        if (value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9') return true;
        if (value == '-' || value == '_' || value == '.' || value == '!' || value == '~' || value == '*' || value == '\'' || value == '(' || value == ')') return true;
        return value == ';' || value == '/' || value == '?' || value == ':' || value == '@' || value == '&' || value == '=' || value == '+' || value == '$' || value == ',';
    }
bool IsBase64Whitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }
int Base64Value(char value)
    {
        if (value >= 'A' && value <= 'Z') return value - 'A';
        if (value >= 'a' && value <= 'z') return value - 'a' + 26;
        if (value >= '0' && value <= '9') return value - '0' + 52;
        if (value == '+') return 62;
        if (value == '/') return 63;
        return -1;
    }
bool IsUnreservedByte(byte value)
    {
        return value >= 65 && value <= 90 || value >= 97 && value <= 122 || value >= 48 && value <= 57 || value == 45 || value == 46 || value == 95 || value == 126;
    }
bool IsSafeMetadataTokenByte(byte value)
    {
        if (new SvgDataUrl().IsUnreservedByte(value)) return true;
        return value == 33 || value == 36 || value == 38 || value == 39 || value == 42 || value == 43;
    }
int Hex(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        return -1;
    }
bool ValidUtf8(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int first = value[index];
            if (first < 128)
            {
                index = index + 1;
                continue;
            }
            int count;
            int minimum;
            int scalar;
            if (first >= 194 && first <= 223)
            {
                count = 2;
                minimum = 128;
                scalar = first & 31;
            }
            else if (first >= 224 && first <= 239)
            {
                count = 3;
                minimum = 2048;
                scalar = first & 15;
            }
            else if (first >= 240 && first <= 244)
            {
                count = 4;
                minimum = 65536;
                scalar = first & 7;
            }
            else return false;
            if (index + count > value.Length) return false;
            int continuation = 1;
            while (continuation < count)
            {
                int next = value[index + continuation];
                if (next < 128 || next > 191) return false;
                scalar = scalar * 64 + (next & 63);
                continuation = continuation + 1;
            }
            if (scalar < minimum || scalar > 1114111 || scalar >= 55296 && scalar <= 57343) return false;
            index = index + count;
        }
        return true;
    }
}
