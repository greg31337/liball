using System;

namespace websocket;

internal class Base64EncodeResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedOutputCapacity;
    internal int ProducedCount;
}

internal class Base64DecodeResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int ConsumedCount;
    internal int ProducedCount;
}

internal class Base64ValidationResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int DecodedLength;
}

internal static class Base64Codec
{
    const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    const int MaximumEncodableSourceLength = 1610612733;

    internal static int MeasureEncodedLength(int sourceLength)
    {
        if (sourceLength < 0 || sourceLength > MaximumEncodableSourceLength)
        {
            return -1;
        }

        int completeGroupCount = sourceLength / 3;
        int encodedLength = completeGroupCount * 4;
        if (sourceLength % 3 != 0)
        {
            encodedLength = encodedLength + 4;
        }

        return encodedLength;
    }

    internal static Base64EncodeResult EncodeCanonical(ReadOnlySpan<byte> source, Span<char> destination)
    {
        Base64EncodeResult result = new Base64EncodeResult();
        int encodedLength = MeasureEncodedLength(source.Length);
        if (encodedLength < 0)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            return result;
        }

        if (destination.Length < encodedLength)
        {
            return result;
        }

        int sourceIndex = 0;
        int destinationIndex = 0;
        while (source.Length - sourceIndex >= 3)
        {
            int first = source[sourceIndex];
            int second = source[sourceIndex + 1];
            int third = source[sourceIndex + 2];
            destination[destinationIndex] = Alphabet[first >> 2];
            destination[destinationIndex + 1] = Alphabet[((first & 3) << 4) | (second >> 4)];
            destination[destinationIndex + 2] = Alphabet[((second & 15) << 2) | (third >> 6)];
            destination[destinationIndex + 3] = Alphabet[third & 63];
            sourceIndex = sourceIndex + 3;
            destinationIndex = destinationIndex + 4;
        }

        int remainingCount = source.Length - sourceIndex;
        if (remainingCount == 1)
        {
            int first = source[sourceIndex];
            destination[destinationIndex] = Alphabet[first >> 2];
            destination[destinationIndex + 1] = Alphabet[(first & 3) << 4];
            destination[destinationIndex + 2] = '=';
            destination[destinationIndex + 3] = '=';
        }
        else if (remainingCount == 2)
        {
            int first = source[sourceIndex];
            int second = source[sourceIndex + 1];
            destination[destinationIndex] = Alphabet[first >> 2];
            destination[destinationIndex + 1] = Alphabet[((first & 3) << 4) | (second >> 4)];
            destination[destinationIndex + 2] = Alphabet[(second & 15) << 2];
            destination[destinationIndex + 3] = '=';
        }

        result.Status = WebSocketParseStatus.Complete;
        result.ProducedCount = encodedLength;
        return result;
    }

    internal static Base64DecodeResult DecodeCanonical(ReadOnlySpan<char> source, Span<byte> destination)
    {
        Base64ValidationResult validation = ValidateCanonicalText(source);
        if (validation.Status != WebSocketParseStatus.Complete)
        {
            return new Base64DecodeResult();
        }

        return DecodeValidatedText(source, destination, validation.DecodedLength);
    }

    internal static Base64EncodeResult EncodeClientNonce(ReadOnlySpan<byte> source, Span<char> destination)
    {
        if (source.Length != WebSocketProtocolConstants.ClientNonceLength)
        {
            Base64EncodeResult result = new Base64EncodeResult();
            result.Status = WebSocketParseStatus.LimitRejected;
            return result;
        }

        return EncodeCanonical(source, destination);
    }

    internal static Base64DecodeResult DecodeClientNonce(ReadOnlySpan<char> source, Span<byte> destination)
    {
        Base64ValidationResult validation = ValidateCanonicalText(source);
        if (validation.Status != WebSocketParseStatus.Complete ||
            validation.DecodedLength != WebSocketProtocolConstants.ClientNonceLength)
        {
            return new Base64DecodeResult();
        }

        return DecodeValidatedText(source, destination, validation.DecodedLength);
    }

    internal static Base64EncodeResult EncodeAcceptDigest(ReadOnlySpan<byte> source, Span<char> destination)
    {
        if (source.Length != WebSocketProtocolConstants.AcceptDigestLength)
        {
            Base64EncodeResult result = new Base64EncodeResult();
            result.Status = WebSocketParseStatus.LimitRejected;
            return result;
        }

        return EncodeCanonical(source, destination);
    }

    internal static Base64DecodeResult DecodeAcceptDigest(ReadOnlySpan<char> source, Span<byte> destination)
    {
        Base64ValidationResult validation = ValidateCanonicalText(source);
        if (validation.Status != WebSocketParseStatus.Complete ||
            validation.DecodedLength != WebSocketProtocolConstants.AcceptDigestLength)
        {
            return new Base64DecodeResult();
        }

        return DecodeValidatedText(source, destination, validation.DecodedLength);
    }

    static Base64ValidationResult ValidateCanonicalText(ReadOnlySpan<char> source)
    {
        Base64ValidationResult result = new Base64ValidationResult();
        if (source.Length % 4 != 0)
        {
            return result;
        }

        if (source.Length == 0)
        {
            result.Status = WebSocketParseStatus.Complete;
            return result;
        }

        int paddingCount = 0;
        if (source[source.Length - 1] == '=')
        {
            paddingCount = 1;
            if (source[source.Length - 2] == '=')
            {
                paddingCount = 2;
            }
        }

        int dataLength = source.Length - paddingCount;
        int sourceIndex = 0;
        while (sourceIndex < dataLength)
        {
            if (DecodeCharacter(source[sourceIndex]) < 0)
            {
                return result;
            }

            sourceIndex = sourceIndex + 1;
        }

        while (sourceIndex < source.Length)
        {
            if (source[sourceIndex] != '=')
            {
                return result;
            }

            sourceIndex = sourceIndex + 1;
        }

        if (paddingCount == 1)
        {
            int finalValue = DecodeCharacter(source[source.Length - 2]);
            if ((finalValue & 3) != 0)
            {
                return result;
            }
        }
        else if (paddingCount == 2)
        {
            int finalValue = DecodeCharacter(source[source.Length - 3]);
            if ((finalValue & 15) != 0)
            {
                return result;
            }
        }

        int completeGroupCount = source.Length / 4;
        result.Status = WebSocketParseStatus.Complete;
        result.DecodedLength = completeGroupCount * 3 - paddingCount;
        return result;
    }

    static Base64DecodeResult DecodeValidatedText(
        ReadOnlySpan<char> source,
        Span<byte> destination,
        int decodedLength)
    {
        Base64DecodeResult result = new Base64DecodeResult();
        if (destination.Length < decodedLength)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
            return result;
        }

        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < source.Length)
        {
            int first = DecodeCharacter(source[sourceIndex]);
            int second = DecodeCharacter(source[sourceIndex + 1]);
            destination[destinationIndex] = (byte)((first << 2) | (second >> 4));
            destinationIndex = destinationIndex + 1;

            if (source[sourceIndex + 2] != '=')
            {
                int third = DecodeCharacter(source[sourceIndex + 2]);
                destination[destinationIndex] = (byte)(((second & 15) << 4) | (third >> 2));
                destinationIndex = destinationIndex + 1;

                if (source[sourceIndex + 3] != '=')
                {
                    int fourth = DecodeCharacter(source[sourceIndex + 3]);
                    destination[destinationIndex] = (byte)(((third & 3) << 6) | fourth);
                    destinationIndex = destinationIndex + 1;
                }
            }

            sourceIndex = sourceIndex + 4;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = source.Length;
        result.ProducedCount = decodedLength;
        return result;
    }

    static int DecodeCharacter(char value)
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

        if (value == '+')
        {
            return 62;
        }

        if (value == '/')
        {
            return 63;
        }

        return -1;
    }
}
