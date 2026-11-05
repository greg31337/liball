using System;

namespace tftp;

internal class NetasciiDecoder
{
    internal const byte CarriageReturn = 13;
    internal const byte LineFeed = 10;
    internal const byte Null = 0;

    internal TftpTransferMode Mode;
    internal bool HasPendingCarriageReturn = false;

    internal NetasciiDecoder(TftpTransferMode mode)
    {
        if (mode != TftpTransferMode.Octet && mode != TftpTransferMode.Netascii)
        {
            throw new TftpProtocolException(
                "The transfer mode is invalid for content decoding.",
                false,
                0);
        }

        Mode = mode;
    }

    internal NetasciiDecodeResult DecodeBytes(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        NetasciiDecodeResult result = new NetasciiDecodeResult();
        if (Mode == TftpTransferMode.Octet)
        {
            int copyLength = source.Length;
            if (copyLength > destination.Length)
            {
                copyLength = destination.Length;
            }

            source.Slice(0, copyLength).CopyTo(destination);
            result.Success = true;
            result.BytesConsumed = copyLength;
            result.BytesWritten = copyLength;
            result.NeedsMoreOutput = copyLength < source.Length;
            return result;
        }

        while (result.BytesConsumed < source.Length)
        {
            if (HasPendingCarriageReturn)
            {
                if (result.BytesWritten >= destination.Length)
                {
                    break;
                }

                byte pairedValue = source[result.BytesConsumed];
                if (pairedValue != LineFeed && pairedValue != Null)
                {
                    result.HasPendingCarriageReturn = true;
                    result.FailureMessage =
                        "A netascii carriage return must be followed by line feed or NUL.";
                    result.HasFailureOffset = true;
                    result.FailureOffset = result.BytesConsumed;
                    return result;
                }

                result.BytesConsumed = result.BytesConsumed + 1;
                if (pairedValue == LineFeed)
                {
                    destination[result.BytesWritten] = LineFeed;
                }
                else
                {
                    destination[result.BytesWritten] = CarriageReturn;
                }

                result.BytesWritten = result.BytesWritten + 1;
                HasPendingCarriageReturn = false;
            }
            else
            {
                byte value = source[result.BytesConsumed];
                if (value == CarriageReturn)
                {
                    result.BytesConsumed = result.BytesConsumed + 1;
                    HasPendingCarriageReturn = true;
                }
                else
                {
                    if (result.BytesWritten >= destination.Length)
                    {
                        break;
                    }

                    destination[result.BytesWritten] = value;
                    result.BytesConsumed = result.BytesConsumed + 1;
                    result.BytesWritten = result.BytesWritten + 1;
                }
            }
        }

        result.Success = true;
        result.HasPendingCarriageReturn = HasPendingCarriageReturn;
        result.NeedsMoreOutput = result.BytesConsumed < source.Length;
        return result;
    }

    internal NetasciiDecodeResult FinishDecoding()
    {
        NetasciiDecodeResult result = new NetasciiDecodeResult();
        result.HasPendingCarriageReturn = HasPendingCarriageReturn;
        if (Mode == TftpTransferMode.Netascii && HasPendingCarriageReturn)
        {
            result.FailureMessage =
                "The netascii stream ends with an incomplete carriage-return sequence.";
            return result;
        }

        result.Success = true;
        return result;
    }
}
