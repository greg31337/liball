using System;
using System.Text;

namespace tftp;

internal static class ZeroTerminatedAscii
{
    internal static int GetEncodedLength(
        string value,
        int maximumFieldLength,
        bool allowEmpty)
    {
        if (value == null)
        {
            throw new TftpProtocolException("An ASCII field cannot be null.", false, 0);
        }

        if (maximumFieldLength < 0)
        {
            throw new TftpProtocolException("The maximum ASCII field length cannot be negative.", false, 0);
        }

        if (!allowEmpty && value.Length == 0)
        {
            throw new TftpProtocolException("An empty ASCII field is not allowed.", false, 0);
        }

        if (value.Length > maximumFieldLength)
        {
            throw new TftpProtocolException("The ASCII field exceeds its maximum length.", false, 0);
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == 0)
            {
                throw new TftpProtocolException("An ASCII field cannot contain an embedded NUL.", false, 0);
            }

            if (character > 127)
            {
                throw new TftpProtocolException("An ASCII field contains a non-ASCII character.", false, 0);
            }

            index = index + 1;
        }

        return value.Length + 1;
    }

    internal static ZeroTerminatedAsciiReadResult ReadField(
        ReadOnlySpan<byte> source,
        int offset,
        int maximumFieldLength,
        bool allowEmpty)
    {
        ZeroTerminatedAsciiDecodeResult result = TryReadField(
            source,
            offset,
            maximumFieldLength,
            allowEmpty);
        if (!result.Success)
        {
            throw new TftpProtocolException(
                result.FailureMessage,
                result.HasFailureOffset,
                result.FailureOffset);
        }

        return new ZeroTerminatedAsciiReadResult(result.Value, result.NextOffset);
    }

    internal static ZeroTerminatedAsciiDecodeResult TryReadField(
        ReadOnlySpan<byte> source,
        int offset,
        int maximumFieldLength,
        bool allowEmpty)
    {
        ZeroTerminatedAsciiDecodeResult result = new ZeroTerminatedAsciiDecodeResult();
        if (offset < 0)
        {
            result.FailureMessage = "The ASCII field offset cannot be negative.";
            return result;
        }

        if (offset > source.Length)
        {
            result.FailureMessage = "The ASCII field offset exceeds the source length.";
            result.HasFailureOffset = true;
            result.FailureOffset = offset;
            return result;
        }

        if (maximumFieldLength < 0)
        {
            result.FailureMessage = "The maximum ASCII field length cannot be negative.";
            return result;
        }

        int index = offset;
        int characterCount = 0;
        while (index < source.Length && characterCount <= maximumFieldLength)
        {
            byte value = source[index];
            if (value == 0)
            {
                if (!allowEmpty && characterCount == 0)
                {
                    result.FailureMessage = "An empty ASCII field is not allowed.";
                    result.HasFailureOffset = true;
                    result.FailureOffset = offset;
                    return result;
                }

                result.Success = true;
                result.Value = Encoding.ASCII.GetString(source.Slice(offset, characterCount));
                result.NextOffset = index + 1;
                return result;
            }

            if (value > 127)
            {
                result.FailureMessage = "An ASCII field contains a non-ASCII byte.";
                result.HasFailureOffset = true;
                result.FailureOffset = index;
                return result;
            }

            index = index + 1;
            characterCount = characterCount + 1;
        }

        if (characterCount > maximumFieldLength)
        {
            result.FailureMessage = "The ASCII field exceeds its maximum length.";
            result.HasFailureOffset = true;
            result.FailureOffset = offset;
            return result;
        }

        result.FailureMessage = "The ASCII field is missing its zero terminator.";
        result.HasFailureOffset = true;
        result.FailureOffset = offset;
        return result;
    }

    internal static int WriteField(
        string value,
        Span<byte> destination,
        int offset,
        int maximumFieldLength,
        bool allowEmpty)
    {
        int encodedLength = GetEncodedLength(value, maximumFieldLength, allowEmpty);
        if (offset < 0)
        {
            throw new TftpProtocolException("The ASCII field offset cannot be negative.", false, 0);
        }

        if (offset > destination.Length - encodedLength)
        {
            throw new TftpProtocolException(
                "The destination is too small for the zero-terminated ASCII field.",
                true,
                offset);
        }

        int index = 0;
        while (index < encodedLength - 1)
        {
            destination[offset + index] = (byte)value[index];
            index = index + 1;
        }

        int nextOffset = offset + encodedLength;
        destination[nextOffset - 1] = 0;
        return nextOffset;
    }
}
