using System;

namespace http;

internal static class Http1SpanWriter
{
    internal static int WriteAsciiCharacters(
        Span<byte> output,
        int offset,
        string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            output[offset + index] = (byte)value[index];
            index = index + 1;
        }

        return offset + value.Length;
    }

    internal static int WriteByteArray(
        Span<byte> output,
        int offset,
        byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            output[offset + index] = value[index];
            index = index + 1;
        }

        return offset + value.Length;
    }

    internal static int WriteSingleByte(
        Span<byte> output,
        int offset,
        byte value)
    {
        output[offset] = value;
        return offset + 1;
    }

    internal static int WriteLineEnding(Span<byte> output, int offset)
    {
        output[offset] = 0x0d;
        output[offset + 1] = 0x0a;
        return offset + 2;
    }

    internal static int WriteChunkSequence(
        Span<byte> output,
        int offset,
        byte[] content,
        int chunkSize,
        string chunkExtension,
        HttpHeaders trailers,
        bool preserveFieldNameCasing)
    {
        int contentOffset = 0;
        int outputOffset = offset;
        while (contentOffset < content.Length)
        {
            int count = content.Length - contentOffset;
            if (count > chunkSize)
            {
                count = chunkSize;
            }

            outputOffset = WriteHexadecimalNumber(output, outputOffset, count);
            outputOffset = WriteAsciiCharacters(
                output,
                outputOffset,
                chunkExtension);
            outputOffset = WriteLineEnding(output, outputOffset);
            int index = 0;
            while (index < count)
            {
                output[outputOffset + index] = content[contentOffset + index];
                index = index + 1;
            }

            outputOffset = outputOffset + count;
            outputOffset = WriteLineEnding(output, outputOffset);
            contentOffset = contentOffset + count;
        }

        outputOffset = WriteSingleByte(output, outputOffset, 0x30);
        outputOffset = WriteAsciiCharacters(output, outputOffset, chunkExtension);
        outputOffset = WriteLineEnding(output, outputOffset);
        int trailerIndex = 0;
        while (trailerIndex < trailers.Fields.Count)
        {
            HttpHeaderField field = trailers.Fields[trailerIndex];
            outputOffset = WriteTrailerFieldName(
                output,
                outputOffset,
                field.Name,
                preserveFieldNameCasing);
            outputOffset = WriteAsciiCharacters(output, outputOffset, ": ");
            outputOffset = WriteByteArray(output, outputOffset, field.Value);
            outputOffset = WriteLineEnding(output, outputOffset);
            trailerIndex = trailerIndex + 1;
        }

        return WriteLineEnding(output, outputOffset);
    }

    static int WriteTrailerFieldName(
        Span<byte> output,
        int offset,
        string value,
        bool preserveFieldNameCasing)
    {
        bool upper = true;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!preserveFieldNameCasing)
            {
                if (character >= 'A' && character <= 'Z' && !upper)
                {
                    character = (char)(character + 0x20);
                }
                else if (character >= 'a' && character <= 'z' && upper)
                {
                    character = (char)(character - 0x20);
                }
            }

            output[offset + index] = (byte)character;
            upper = character == '-';
            index = index + 1;
        }

        return offset + value.Length;
    }

    static int WriteHexadecimalNumber(
        Span<byte> output,
        int offset,
        int value)
    {
        int length = 1;
        int remaining = value;
        while (remaining >= 16)
        {
            remaining = remaining / 16;
            length = length + 1;
        }

        int index = length - 1;
        remaining = value;
        while (index >= 0)
        {
            int digit = remaining % 16;
            output[offset + index] = digit < 10 ?
                (byte)(0x30 + digit) : (byte)(0x41 + digit - 10);
            remaining = remaining / 16;
            index = index - 1;
        }

        return offset + length;
    }
}
