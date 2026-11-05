using System.Collections.Generic;

namespace gltf;

internal class DataUriCodec
{
    internal Base64Codec Base64Codec = new Base64Codec();
    internal UriPercentCodec PercentCodec = new UriPercentCodec();

    internal DataUriData ParseDataUri(string uri)
    {
        return ParseLimitedDataUri(uri, int.MaxValue);
    }

    internal DataUriData ParseLimitedDataUri(string uri, int maximumByteLength)
    {
        if (uri == null)
        {
            throw new System.ArgumentNullException("uri");
        }

        if (maximumByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumByteLength");
        }

        if (!uri.StartsWith("data:", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new GltfResourceException("The URI does not use the data scheme.");
        }

        int commaIndex = uri.IndexOf(',', 5);
        if (commaIndex < 0)
        {
            throw new GltfResourceException("The data URI is missing its comma separator.");
        }

        string metadata = uri.Substring(5, commaIndex - 5);
        string payload = uri.Substring(commaIndex + 1);
        ValidateRawText(metadata, "metadata");
        ValidateRawText(payload, "payload");
        DataUriData result = new DataUriData();
        result.SourceUri = uri;
        ParseMetadata(metadata, result);
        if (result.Base64)
        {
            long maximumEncodedByteLength = (((long)maximumByteLength + 2L) / 3L) * 4L;
            if (maximumEncodedByteLength > int.MaxValue)
            {
                maximumEncodedByteLength = int.MaxValue;
            }

            byte[] payloadBytes = DecodeLimitedPayloadBytes(payload, (int)maximumEncodedByteLength);
            result.Bytes = Base64Codec.DecodeBase64Bytes(payloadBytes, maximumByteLength, false);
        }
        else
        {
            result.Bytes = DecodeLimitedPayloadBytes(payload, maximumByteLength);
        }

        return result;
    }

    internal void ParseMetadata(string metadata, DataUriData result)
    {
        List<string> segments = SplitMetadata(metadata);
        int segmentIndex = 0;
        if (segments.Count > 0 && segments[0].Length > 0)
        {
            string mediaType = PercentCodec.DecodeText(segments[0]);
            ValidateMediaType(mediaType);
            result.HasDeclaredMimeType = true;
            result.DeclaredMimeType = mediaType.ToLowerInvariant();
            result.EffectiveMimeType = result.DeclaredMimeType;
            result.EffectiveCharset = null;
            segmentIndex = 1;
        }
        else if (segments.Count > 0)
        {
            segmentIndex = 1;
        }

        while (segmentIndex < segments.Count)
        {
            string segment = segments[segmentIndex];
            if (segment.Length == 0)
            {
                throw new GltfResourceException("The data URI contains an empty metadata segment.");
            }

            int equalsIndex = segment.IndexOf('=');
            if (equalsIndex < 0
                && string.Equals(segment, "base64", System.StringComparison.OrdinalIgnoreCase))
            {
                if (result.Base64)
                {
                    throw new GltfResourceException("The data URI contains more than one base64 marker.");
                }

                if (segmentIndex != segments.Count - 1)
                {
                    throw new GltfResourceException("The data URI base64 marker must follow all media type parameters.");
                }

                result.Base64 = true;
            }
            else
            {
                if (equalsIndex <= 0 || equalsIndex == segment.Length - 1)
                {
                    throw new GltfResourceException(
                        $"Data URI metadata segment {segmentIndex} is not a valid parameter.");
                }

                string name = PercentCodec.DecodeText(segment.Substring(0, equalsIndex));
                string value = PercentCodec.DecodeText(segment.Substring(equalsIndex + 1));
                ValidateMimeToken(name, $"parameter name {segmentIndex}");
                DataUriParameter parameter = new DataUriParameter();
                parameter.Name = name.ToLowerInvariant();
                parameter.Value = value;
                result.Parameters.Add(parameter);
                if (string.Equals(name, "charset", System.StringComparison.OrdinalIgnoreCase))
                {
                    result.HasDeclaredCharset = true;
                    result.EffectiveCharset = value;
                }
            }

            segmentIndex++;
        }
    }

    internal List<string> SplitMetadata(string metadata)
    {
        List<string> result = new List<string>();
        int start = 0;
        int index = 0;
        while (index < metadata.Length)
        {
            if (metadata[index] == ';')
            {
                result.Add(metadata.Substring(start, index - start));
                start = index + 1;
            }

            index++;
        }

        result.Add(metadata.Substring(start));
        return result;
    }

    internal void ValidateMediaType(string mediaType)
    {
        int slashIndex = mediaType.IndexOf('/');
        if (slashIndex <= 0
            || slashIndex == mediaType.Length - 1
            || slashIndex != mediaType.LastIndexOf('/'))
        {
            throw new GltfResourceException($"Data URI media type '{mediaType}' is invalid.");
        }

        ValidateMimeToken(mediaType.Substring(0, slashIndex), "media type");
        ValidateMimeToken(mediaType.Substring(slashIndex + 1), "media subtype");
    }

    internal void ValidateMimeToken(string value, string subject)
    {
        if (value.Length == 0)
        {
            throw new GltfResourceException($"The data URI {subject} is empty.");
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= 0x20
                || character >= 0x7F
                || character == '('
                || character == ')'
                || character == '<'
                || character == '>'
                || character == '@'
                || character == ','
                || character == ';'
                || character == ':'
                || character == '\\'
                || character == '"'
                || character == '/'
                || character == '['
                || character == ']'
                || character == '?'
                || character == '=')
            {
                throw new GltfResourceException(
                    $"The data URI {subject} contains invalid MIME token character '{character}'.");
            }

            index++;
        }
    }

    internal void ValidateRawText(string value, string subject)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!IsDataUriRawCharacter(character))
            {
                throw new GltfResourceException(
                    $"The data URI {subject} contains an unsafe character at character {index}.");
            }

            index++;
        }
    }

    internal bool IsDataUriRawCharacter(char character)
    {
        return (character >= 'A' && character <= 'Z')
            || (character >= 'a' && character <= 'z')
            || (character >= '0' && character <= '9')
            || character == '-'
            || character == '_'
            || character == '.'
            || character == '!'
            || character == '~'
            || character == '*'
            || character == '\''
            || character == '('
            || character == ')'
            || character == ';'
            || character == '/'
            || character == '?'
            || character == ':'
            || character == '@'
            || character == '&'
            || character == '='
            || character == '+'
            || character == '$'
            || character == ','
            || character == '%';
    }

    internal byte[] DecodePayloadBytes(string payload)
    {
        return DecodeLimitedPayloadBytes(payload, int.MaxValue);
    }

    internal byte[] DecodeLimitedPayloadBytes(string payload, int maximumByteLength)
    {
        if (maximumByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumByteLength");
        }

        PercentCodec.ValidateEscapes(payload);
        List<byte> result = new List<byte>();
        int index = 0;
        while (index < payload.Length)
        {
            if (payload[index] == '%')
            {
                int high = PercentCodec.GetHexValue(payload[index + 1]);
                int low = PercentCodec.GetHexValue(payload[index + 2]);
                result.Add((byte)((high * 16) + low));
                index += 3;
            }
            else
            {
                result.Add((byte)payload[index]);
                index++;
            }

            if (result.Count > maximumByteLength)
            {
                throw new GltfResourceException(
                    $"Data URI decoded length exceeds the configured maximum {maximumByteLength}.");
            }
        }

        return result.ToArray();
    }

}

internal class DataUriData
{
    internal string SourceUri = "";
    internal bool HasDeclaredMimeType;
    internal string? DeclaredMimeType;
    internal string EffectiveMimeType = "text/plain";
    internal List<DataUriParameter> Parameters = new List<DataUriParameter>();
    internal bool HasDeclaredCharset;
    internal string? EffectiveCharset = "US-ASCII";
    internal bool Base64;
    internal byte[] Bytes = [];
}

internal class DataUriParameter
{
    internal string Name = "";
    internal string Value = "";
}
