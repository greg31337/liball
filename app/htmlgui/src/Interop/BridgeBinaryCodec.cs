using System;

namespace htmlgui;

internal class BridgeBinaryResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string Encoded = string.Empty;
    internal byte[] Data = Array.Empty<byte>();
}

internal class BridgeBinaryCodec
{
    internal BridgeBinaryResult EncodeBridgeBinary(byte[]? data, int maximumBytes)
    {
        if (maximumBytes < 0)
        {
            return CreateBinaryFailure("bridge.invalid-binary-limit");
        }

        if (data == null)
        {
            return CreateBinaryFailure("bridge.null-binary");
        }

        if (data.Length > maximumBytes)
        {
            return CreateBinaryFailure("bridge.binary-too-large");
        }

        byte[] copy = new byte[data.Length];
        Array.Copy(data, copy, data.Length);
        return new BridgeBinaryResult
        {
            Success = true,
            Encoded = Convert.ToBase64String(copy),
            Data = copy
        };
    }

    internal BridgeBinaryResult DecodeBridgeBinary(string? encoded, int maximumBytes)
    {
        if (maximumBytes < 0)
        {
            return CreateBinaryFailure("bridge.invalid-binary-limit");
        }

        if (encoded == null)
        {
            return CreateBinaryFailure("bridge.null-base64");
        }

        if (encoded.Length == 0)
        {
            return new BridgeBinaryResult { Success = true };
        }

        long maximumEncodedLength = ((long)maximumBytes + 2L) / 3L * 4L;
        if (encoded.Length > maximumEncodedLength || encoded.Length % 4 != 0)
        {
            return CreateBinaryFailure("bridge.invalid-base64");
        }

        int padding = 0;
        if (encoded[encoded.Length - 1] == '=')
        {
            padding++;
        }

        if (encoded[encoded.Length - 2] == '=')
        {
            padding++;
        }

        int outputLength = encoded.Length / 4 * 3 - padding;
        if (outputLength > maximumBytes)
        {
            return CreateBinaryFailure("bridge.binary-too-large");
        }

        byte[] data = new byte[outputLength];
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < encoded.Length)
        {
            char firstCharacter = encoded[inputIndex];
            char secondCharacter = encoded[inputIndex + 1];
            char thirdCharacter = encoded[inputIndex + 2];
            char fourthCharacter = encoded[inputIndex + 3];
            int first = ReadBase64Value(firstCharacter);
            int second = ReadBase64Value(secondCharacter);
            int third = thirdCharacter == '=' ? 0 : ReadBase64Value(thirdCharacter);
            int fourth = fourthCharacter == '=' ? 0 : ReadBase64Value(fourthCharacter);
            bool finalGroup = inputIndex + 4 == encoded.Length;
            if (first < 0 || second < 0 || third < 0 || fourth < 0)
            {
                return CreateBinaryFailure("bridge.invalid-base64");
            }

            if (firstCharacter == '=' || secondCharacter == '=')
            {
                return CreateBinaryFailure("bridge.invalid-base64");
            }

            if (thirdCharacter == '=' && (!finalGroup || fourthCharacter != '=' || (second & 15) != 0))
            {
                return CreateBinaryFailure("bridge.invalid-base64");
            }

            if (fourthCharacter == '=' && thirdCharacter != '=' && (!finalGroup || (third & 3) != 0))
            {
                return CreateBinaryFailure("bridge.invalid-base64");
            }

            if (!finalGroup && (thirdCharacter == '=' || fourthCharacter == '='))
            {
                return CreateBinaryFailure("bridge.invalid-base64");
            }

            uint material = (uint)first << 18 | (uint)second << 12 | (uint)third << 6 | (uint)fourth;
            if (outputIndex < data.Length)
            {
                data[outputIndex] = (byte)(material >> 16 & 255U);
                outputIndex++;
            }

            if (outputIndex < data.Length)
            {
                data[outputIndex] = (byte)(material >> 8 & 255U);
                outputIndex++;
            }

            if (outputIndex < data.Length)
            {
                data[outputIndex] = (byte)(material & 255U);
                outputIndex++;
            }

            inputIndex += 4;
        }

        return new BridgeBinaryResult { Success = true, Encoded = encoded, Data = data };
    }

    internal int ReadBase64Value(char value)
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

    internal BridgeBinaryResult CreateBinaryFailure(string code)
    {
        return new BridgeBinaryResult { Code = code };
    }
}
