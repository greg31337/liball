using System;

namespace tiff;

internal class TiffSparseSegment
{
    internal static byte[] CreateZeroDecodedBytes(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateExpectedDecodedByteCount(context, diagnostics))
        {
            return new byte[0];
        }

        return new byte[(int)context.ExpectedDecodedByteCount];
    }

    internal static bool IsAllZero(ReadOnlySpan<byte> bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
