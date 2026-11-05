using System;

namespace fix;

internal static class ChecksumCalculator
{
    internal static int CalculateChecksum(ReadOnlySpan<byte> source)
    {
        int checksum = 0;
        for (int index = 0; index < source.Length; index++)
        {
            checksum = (checksum + source[index]) & 255;
        }
        return checksum;
    }

    internal static int CalculateChecksumRange(ReadOnlySpan<byte> source, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > source.Length || length > source.Length - offset)
        {
            return -1;
        }
        return CalculateChecksum(source.Slice(offset, length));
    }

    internal static AsciiIntegerResult ParseChecksum(ReadOnlySpan<byte> source)
    {
        if (source.Length != FixDefaults.ChecksumDigits)
        {
            AsciiIntegerResult invalidLength = new AsciiIntegerResult();
            invalidLength.Required = FixDefaults.ChecksumDigits;
            invalidLength.ErrorCode = source.Length < FixDefaults.ChecksumDigits ? "incomplete" : "invalid-length";
            return invalidLength;
        }
        return AsciiDigits.ParseUnsignedInt(source, FixDefaults.ChecksumModulo - 1);
    }

    internal static bool WriteChecksum(int checksum, Span<byte> destination)
    {
        if (checksum < 0 || checksum >= FixDefaults.ChecksumModulo || destination.Length < FixDefaults.ChecksumDigits)
        {
            return false;
        }
        destination[0] = (byte)(48 + checksum / 100);
        destination[1] = (byte)(48 + checksum / 10 % 10);
        destination[2] = (byte)(48 + checksum % 10);
        return true;
    }
}
