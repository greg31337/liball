using System;
using System.IO;

namespace tiff;

internal class TiffTagValueLoader
{
    internal static TiffTagValueLoadResult LoadSourceBytes(TiffIfdEntry entry, ITiffRandomAccessSource source, TiffLimits limits)
    {
        TiffLimits actualLimits = NormalizeLimits(limits);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateEntryForLoad(entry, actualLimits, diagnostics))
        {
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        int byteCount = (int)entry.ValueByteCount;
        if (!entry.HasExternalValue())
        {
            return LoadInlineBytes(entry, byteCount, diagnostics);
        }

        if (entry.ValueOffset > long.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "External tag value offset exceeds supported signed range.");
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        byte[] bytes = TiffRandomAccessSourceReader.ReadBytes(source, (long)entry.ValueOffset, byteCount, diagnostics);
        return TiffTagValueLoadResult.Create(bytes, false, diagnostics);
    }

    internal static TiffTagValueLoadResult LoadBytes(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes)
    {
        return LoadBytes(entry, fileBytes, new TiffLimits());
    }

    internal static TiffTagValueLoadResult LoadBytes(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffLimits actualLimits = NormalizeLimits(limits);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateEntryForLoad(entry, actualLimits, diagnostics))
        {
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        int byteCount = (int)entry.ValueByteCount;
        if (!entry.HasExternalValue())
        {
            return LoadInlineBytes(entry, byteCount, diagnostics);
        }

        if (entry.ValueOffset > long.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "External tag value offset exceeds supported signed range.");
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        if (!TiffMath.IsByteRangeWithinLength((long)entry.ValueOffset, entry.ValueByteCount, fileBytes.Length))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "External tag value byte range is outside the available TIFF data.");
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        byte[] bytes = CopyBytes(fileBytes.Slice((int)entry.ValueOffset, byteCount));
        return TiffTagValueLoadResult.Create(bytes, false, diagnostics);
    }

    internal static TiffTagValueLoadResult LoadBytes(TiffIfdEntry entry, Stream stream)
    {
        return LoadBytes(entry, stream, new TiffLimits());
    }

    internal static TiffTagValueLoadResult LoadBytes(TiffIfdEntry entry, Stream stream, TiffLimits limits)
    {
        TiffLimits actualLimits = NormalizeLimits(limits);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateEntryForLoad(entry, actualLimits, diagnostics))
        {
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        int byteCount = (int)entry.ValueByteCount;
        if (!entry.HasExternalValue())
        {
            return LoadInlineBytes(entry, byteCount, diagnostics);
        }

        if (stream == null)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "A stream is required to load an external tag value.");
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        if (!stream.CanRead)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "Stream must be readable to load an external tag value.");
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        if (!stream.CanSeek)
        {
            diagnostics.AddError(TiffErrorCode.NonSeekableStream, "Stream must be seekable to load an external tag value.");
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        if (entry.ValueOffset > long.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "External tag value offset exceeds supported signed range.");
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        if (!TiffMath.IsByteRangeWithinLength((long)entry.ValueOffset, entry.ValueByteCount, stream.Length))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "External tag value byte range is outside the stream.");
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        byte[] bytes = new byte[byteCount];
        long originalPosition = stream.Position;
        stream.Position = (long)entry.ValueOffset;
        int total = 0;
        while (total < byteCount)
        {
            int read = stream.Read(bytes, total, byteCount - total);
            if (read == 0)
            {
                stream.Position = originalPosition;
                diagnostics.AddError(TiffErrorCode.TruncatedStream, "Stream ended before the external tag value was fully read.");
                return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
            }

            total = total + read;
        }

        stream.Position = originalPosition;
        return TiffTagValueLoadResult.Create(bytes, false, diagnostics);
    }

    internal static bool ValidateEntryForLoad(TiffIfdEntry entry, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (entry == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "An IFD entry is required to load a tag value.");
            return false;
        }

        if (entry.ValueByteCount < 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "Tag value byte count cannot be negative.");
            return false;
        }

        if (entry.ValueSlotByteCount < 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "Tag value slot byte count cannot be negative.");
            return false;
        }

        if (entry.ValueByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Tag value byte count is too large to allocate.");
            return false;
        }

        if (!limits.AllowsAllocation(entry.ValueByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Tag value byte count exceeds the configured allocation limit.");
            return false;
        }

        return true;
    }

    internal static TiffTagValueLoadResult LoadInlineBytes(TiffIfdEntry entry, int byteCount, TiffDiagnostics diagnostics)
    {
        if (entry.ValueOffsetBytes == null)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Inline tag value slot bytes are missing.");
            return TiffTagValueLoadResult.Create(new byte[0], true, diagnostics);
        }

        if (byteCount > entry.ValueOffsetBytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Inline tag value byte count exceeds the entry value slot.");
            return TiffTagValueLoadResult.Create(new byte[0], true, diagnostics);
        }

        byte[] bytes = CopyBytes(entry.ValueOffsetBytes.AsSpan(0, byteCount));
        return TiffTagValueLoadResult.Create(bytes, true, diagnostics);
    }

    internal static byte[] CopyBytes(ReadOnlySpan<byte> source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static TiffLimits NormalizeLimits(TiffLimits limits)
    {
        if (limits == null)
        {
            return new TiffLimits();
        }

        return limits;
    }
}
