using System;

namespace tiff;

internal class TiffRandomAccessSourceReader
{
    internal static bool ReadToBuffer(ITiffRandomAccessSource source, long offset, Span<byte> destination, TiffDiagnostics diagnostics)
    {
        if (source == null || !source.GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "An open random-access TIFF source is required.");
            return false;
        }

        if (!TiffMath.IsByteRangeWithinLength(offset, destination.Length, source.GetLength()))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "The requested byte range is outside the TIFF source.");
            return false;
        }

        int total = 0;
        while (total < destination.Length)
        {
            int read = source.Read(offset + total, destination.Slice(total));
            if (read <= 0)
            {
                diagnostics.AddError(TiffErrorCode.TruncatedStream, "The TIFF source ended before the requested byte range was read.");
                return false;
            }

            total = total + read;
        }

        return true;
    }

    internal static byte[] ReadBytes(ITiffRandomAccessSource source, long offset, int byteCount, TiffDiagnostics diagnostics)
    {
        if (source == null || !source.GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "An open random-access TIFF source is required.");
            return new byte[0];
        }

        if (offset < 0 || byteCount < 0 || !TiffMath.IsByteRangeWithinLength(offset, byteCount, source.GetLength()))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "The requested byte range is outside the TIFF source.");
            return new byte[0];
        }

        byte[] bytes = new byte[byteCount];
        if (!ReadToBuffer(source, offset, bytes, diagnostics))
        {
            return new byte[0];
        }

        return bytes;
    }

    internal static byte[] ReadAllBytes(ITiffRandomAccessSource source, TiffDiagnostics diagnostics)
    {
        if (source == null || !source.GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "An open random-access TIFF source is required.");
            return new byte[0];
        }

        long length = source.GetLength();
        if (length < 0 || length > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "The TIFF source is too large to return as one byte array.");
            return new byte[0];
        }

        return ReadBytes(source, 0, (int)length, diagnostics);
    }
}
