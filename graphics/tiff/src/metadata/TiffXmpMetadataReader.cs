using System;
using System.Text;

namespace tiff;

internal class TiffXmpMetadataReader
{
    internal static TiffXmpReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return Read(directory, fileBytes, new TiffLimits());
    }

    internal static TiffXmpReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read XMP metadata.");
            return TiffXmpReadResult.Create(new TiffXmpPacket(), diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.Xmp);
        if (entry == null)
        {
            return TiffXmpReadResult.Create(new TiffXmpPacket(), diagnostics);
        }

        if (entry.DataType != TiffDataType.Byte && entry.DataType != TiffDataType.Undefined)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "XMP packet tag must use BYTE or UNDEFINED data.", directory.ImageIndex, TiffTagId.Xmp);
            return TiffXmpReadResult.Create(new TiffXmpPacket(), diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return TiffXmpReadResult.Create(new TiffXmpPacket(), diagnostics);
        }

        string text = DecodeUtf8(loadResult.Bytes);
        TiffXmpPacket packet = TiffXmpPacket.Create(true, entry.DataType, loadResult.Bytes, text);
        return TiffXmpReadResult.Create(packet, diagnostics);
    }

    internal static string DecodeUtf8(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return "";
        }

        return Encoding.UTF8.GetString(bytes);
    }
}
