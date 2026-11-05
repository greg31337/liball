namespace gltf;

internal class Base64Codec
{
    internal const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    internal byte[] DecodeBase64Bytes(
        byte[] encodedBytes,
        int maximumDecodedByteLength,
        bool allowWhitespace)
    {
        if (encodedBytes == null)
        {
            throw new System.ArgumentNullException("encodedBytes");
        }

        if (maximumDecodedByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumDecodedByteLength");
        }

        System.ReadOnlySpan<byte> input = encodedBytes;
        int symbolCount = CountSymbols(input, allowWhitespace);
        if (symbolCount % 4 != 0)
        {
            throw new GltfResourceException("Base64 input length is not a multiple of four.");
        }

        int paddingByteLength = CountPadding(input, allowWhitespace, symbolCount);
        long decodedByteLength = ((long)symbolCount / 4L) * 3L - paddingByteLength;
        if (decodedByteLength > int.MaxValue)
        {
            throw new GltfResourceException("Base64 decoded length exceeds the supported signed byte length.");
        }

        if (decodedByteLength > maximumDecodedByteLength)
        {
            throw new GltfResourceException(
                $"Base64 decoded length {decodedByteLength} exceeds the configured maximum {maximumDecodedByteLength}.");
        }

        byte[] symbols = CopySymbols(input, symbolCount, allowWhitespace);
        ValidatePadding(symbols, paddingByteLength);
        byte[] result = new byte[(int)decodedByteLength];
        DecodeSymbols(symbols, result, paddingByteLength);
        return result;
    }

    internal string EncodeBase64Bytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        long encodedByteLength = (((long)bytes.Length + 2L) / 3L) * 4L;
        if (encodedByteLength > int.MaxValue)
        {
            throw new GltfResourceException("Base64 encoded length exceeds the supported signed byte length.");
        }

        byte[] encodedBytes = new byte[(int)encodedByteLength];
        System.ReadOnlySpan<byte> input = bytes;
        System.Span<byte> output = encodedBytes;
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex + 3 <= input.Length)
        {
            int first = input[inputIndex];
            int second = input[inputIndex + 1];
            int third = input[inputIndex + 2];
            output[outputIndex] = (byte)Alphabet[first >> 2];
            output[outputIndex + 1] = (byte)Alphabet[((first & 0x03) << 4) | (second >> 4)];
            output[outputIndex + 2] = (byte)Alphabet[((second & 0x0F) << 2) | (third >> 6)];
            output[outputIndex + 3] = (byte)Alphabet[third & 0x3F];
            inputIndex += 3;
            outputIndex += 4;
        }

        int remainingByteLength = input.Length - inputIndex;
        if (remainingByteLength == 1)
        {
            int first = input[inputIndex];
            output[outputIndex] = (byte)Alphabet[first >> 2];
            output[outputIndex + 1] = (byte)Alphabet[(first & 0x03) << 4];
            output[outputIndex + 2] = (byte)'=';
            output[outputIndex + 3] = (byte)'=';
        }
        else if (remainingByteLength == 2)
        {
            int first = input[inputIndex];
            int second = input[inputIndex + 1];
            output[outputIndex] = (byte)Alphabet[first >> 2];
            output[outputIndex + 1] = (byte)Alphabet[((first & 0x03) << 4) | (second >> 4)];
            output[outputIndex + 2] = (byte)Alphabet[(second & 0x0F) << 2];
            output[outputIndex + 3] = (byte)'=';
        }

        return System.Text.Encoding.ASCII.GetString(encodedBytes);
    }

    internal int CountSymbols(System.ReadOnlySpan<byte> input, bool allowWhitespace)
    {
        int symbolCount = 0;
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            if (IsWhitespace(value))
            {
                if (!allowWhitespace)
                {
                    throw new GltfResourceException($"Base64 input contains whitespace at byte {index}.");
                }
            }
            else
            {
                if (value != '=' && GetAlphabetValue(value) < 0)
                {
                    throw new GltfResourceException(
                        $"Base64 input contains an invalid character at byte {index}.");
                }

                symbolCount++;
            }

            index++;
        }

        return symbolCount;
    }

    internal int CountPadding(
        System.ReadOnlySpan<byte> input,
        bool allowWhitespace,
        int symbolCount)
    {
        int paddingByteLength = 0;
        int symbolsVisited = 0;
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            if (!IsWhitespace(value) || !allowWhitespace)
            {
                symbolsVisited++;
                if (value == '=' && symbolsVisited > symbolCount - 2)
                {
                    paddingByteLength++;
                }
            }

            index++;
        }

        return paddingByteLength;
    }

    internal byte[] CopySymbols(
        System.ReadOnlySpan<byte> input,
        int symbolCount,
        bool allowWhitespace)
    {
        byte[] result = new byte[symbolCount];
        System.Span<byte> output = result;
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < input.Length)
        {
            byte value = input[inputIndex];
            if (!allowWhitespace || !IsWhitespace(value))
            {
                output[outputIndex] = value;
                outputIndex++;
            }

            inputIndex++;
        }

        return result;
    }

    internal void ValidatePadding(byte[] symbols, int paddingByteLength)
    {
        int index = 0;
        while (index < symbols.Length - paddingByteLength)
        {
            if (symbols[index] == '=')
            {
                throw new GltfResourceException("Base64 padding must appear only at the end.");
            }

            index++;
        }

        if (paddingByteLength > 2)
        {
            throw new GltfResourceException("Base64 input contains too much padding.");
        }

        index = symbols.Length - paddingByteLength;
        while (index < symbols.Length)
        {
            if (symbols[index] != '=')
            {
                throw new GltfResourceException("Base64 padding must be contiguous at the end.");
            }

            index++;
        }
    }

    internal void DecodeSymbols(byte[] symbols, byte[] result, int paddingByteLength)
    {
        System.ReadOnlySpan<byte> input = symbols;
        System.Span<byte> output = result;
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < input.Length)
        {
            int first = GetAlphabetValue(input[inputIndex]);
            int second = GetAlphabetValue(input[inputIndex + 1]);
            int third = 0;
            int fourth = 0;
            if (input[inputIndex + 2] != '=')
            {
                third = GetAlphabetValue(input[inputIndex + 2]);
            }

            if (input[inputIndex + 3] != '=')
            {
                fourth = GetAlphabetValue(input[inputIndex + 3]);
            }

            bool finalGroup = inputIndex + 4 == input.Length;
            if (finalGroup && paddingByteLength == 2 && (second & 0x0F) != 0)
            {
                throw new GltfResourceException("Base64 input has non-zero unused bits before padding.");
            }

            if (finalGroup && paddingByteLength == 1 && (third & 0x03) != 0)
            {
                throw new GltfResourceException("Base64 input has non-zero unused bits before padding.");
            }

            if (outputIndex < output.Length)
            {
                output[outputIndex] = (byte)((first << 2) | (second >> 4));
                outputIndex++;
            }

            if (outputIndex < output.Length)
            {
                output[outputIndex] = (byte)(((second & 0x0F) << 4) | (third >> 2));
                outputIndex++;
            }

            if (outputIndex < output.Length)
            {
                output[outputIndex] = (byte)(((third & 0x03) << 6) | fourth);
                outputIndex++;
            }

            inputIndex += 4;
        }
    }

    internal bool IsWhitespace(byte value)
    {
        return value == 0x20 || value == 0x09 || value == 0x0D || value == 0x0A;
    }

    internal int GetAlphabetValue(byte value)
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
