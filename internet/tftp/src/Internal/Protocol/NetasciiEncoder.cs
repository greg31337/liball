using System;

namespace tftp;

internal class NetasciiEncoder
{
    internal const byte CarriageReturn = 13;
    internal const byte LineFeed = 10;
    internal const byte Null = 0;

    internal TftpTransferMode Mode;
    internal bool HasPendingByte = false;
    internal byte PendingByte = 0;

    internal NetasciiEncoder(TftpTransferMode mode)
    {
        if (mode != TftpTransferMode.Octet && mode != TftpTransferMode.Netascii)
        {
            throw new TftpProtocolException(
                "The transfer mode is invalid for content encoding.",
                false,
                0);
        }

        Mode = mode;
    }

    internal NetasciiEncodeResult EncodeBytes(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        NetasciiEncodeResult result = new NetasciiEncodeResult();
        if (Mode == TftpTransferMode.Octet)
        {
            int copyLength = source.Length;
            if (copyLength > destination.Length)
            {
                copyLength = destination.Length;
            }

            source.Slice(0, copyLength).CopyTo(destination);
            result.BytesConsumed = copyLength;
            result.BytesWritten = copyLength;
            result.NeedsMoreOutput = copyLength < source.Length;
            return result;
        }

        if (HasPendingByte)
        {
            if (destination.Length == 0)
            {
                result.HasPendingOutput = true;
                result.NeedsMoreOutput = true;
                return result;
            }

            destination[0] = PendingByte;
            HasPendingByte = false;
            PendingByte = 0;
            result.BytesWritten = 1;
        }

        while (result.BytesConsumed < source.Length &&
            result.BytesWritten < destination.Length)
        {
            byte value = source[result.BytesConsumed];
            result.BytesConsumed = result.BytesConsumed + 1;
            if (value == LineFeed || value == CarriageReturn)
            {
                destination[result.BytesWritten] = CarriageReturn;
                result.BytesWritten = result.BytesWritten + 1;

                byte secondValue = Null;
                if (value == LineFeed)
                {
                    secondValue = LineFeed;
                }

                if (result.BytesWritten < destination.Length)
                {
                    destination[result.BytesWritten] = secondValue;
                    result.BytesWritten = result.BytesWritten + 1;
                }
                else
                {
                    HasPendingByte = true;
                    PendingByte = secondValue;
                }
            }
            else
            {
                destination[result.BytesWritten] = value;
                result.BytesWritten = result.BytesWritten + 1;
            }
        }

        result.HasPendingOutput = HasPendingByte;
        result.NeedsMoreOutput = HasPendingByte ||
            result.BytesConsumed < source.Length;
        return result;
    }
}
