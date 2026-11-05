using System;

namespace fix;

internal static class BodyLengthCalculator
{
    internal static BodyLengthResult CalculateBodyLength(ReadOnlySpan<byte> message)
    {
        BodyLengthResult result = new BodyLengthResult();
        if (message.Length < 2 || message[0] != 56 || message[1] != 61)
        {
            result.ErrorIndex = 0;
            result.ErrorCode = "missing-begin-string";
            return result;
        }

        int beginEnd = ByteSearch.IndexOfByte(message, FixDefaults.Soh);
        if (beginEnd < 0)
        {
            result.ErrorIndex = message.Length;
            result.ErrorCode = "incomplete-begin-string";
            return result;
        }
        int lengthFieldStart = beginEnd + 1;
        if (lengthFieldStart + 2 > message.Length || message[lengthFieldStart] != 57 || message[lengthFieldStart + 1] != 61)
        {
            result.ErrorIndex = lengthFieldStart;
            result.ErrorCode = "missing-body-length";
            return result;
        }

        int relativeLengthEnd = ByteSearch.IndexOfByte(message.Slice(lengthFieldStart), FixDefaults.Soh);
        if (relativeLengthEnd < 0)
        {
            result.ErrorIndex = message.Length;
            result.ErrorCode = "incomplete-body-length";
            return result;
        }
        int lengthFieldEnd = lengthFieldStart + relativeLengthEnd;
        int lengthValueStart = lengthFieldStart + 2;
        AsciiIntegerResult parsedLength = AsciiDigits.ParseUnsignedInt(message.Slice(lengthValueStart, lengthFieldEnd - lengthValueStart), FixDefaults.MaxMessageSize);
        if (!parsedLength.Success)
        {
            result.ErrorIndex = parsedLength.ErrorIndex < 0 ? lengthValueStart : lengthValueStart + parsedLength.ErrorIndex;
            result.ErrorCode = "invalid-body-length";
            return result;
        }

        int bodyStart = lengthFieldEnd + 1;
        int checksumStart = message.Length - 7;
        if (checksumStart < bodyStart || message.Length < 7 || message[checksumStart] != 49 || message[checksumStart + 1] != 48 || message[checksumStart + 2] != 61 || message[message.Length - 1] != FixDefaults.Soh)
        {
            result.ErrorIndex = checksumStart < 0 ? 0 : checksumStart;
            result.ErrorCode = "invalid-checksum-field";
            return result;
        }
        AsciiIntegerResult parsedChecksum = ChecksumCalculator.ParseChecksum(message.Slice(checksumStart + 3, 3));
        if (!parsedChecksum.Success)
        {
            result.ErrorIndex = checksumStart + 3;
            result.ErrorCode = "invalid-checksum-field";
            return result;
        }

        result.Success = true;
        result.DeclaredLength = parsedLength.Value;
        result.CalculatedLength = checksumStart - bodyStart;
        result.BodyStart = bodyStart;
        result.ChecksumStart = checksumStart;
        result.Matches = result.DeclaredLength == result.CalculatedLength;
        return result;
    }

    internal static AsciiIntegerResult WriteBodyLength(int bodyLength, Span<byte> destination)
    {
        return AsciiDigits.WriteUnsignedInt(bodyLength, destination);
    }
}
