using System;

namespace tiff;

internal class TiffImageDescriptionReader
{
    internal static TiffImageDescriptionReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return Read(directory, fileBytes, new TiffLimits());
    }

    internal static TiffImageDescriptionReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read ImageDescription metadata.");
            return TiffImageDescriptionReadResult.Create(new TiffImageDescriptionMetadata(), diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.ImageDescription);
        if (entry == null)
        {
            return TiffImageDescriptionReadResult.Create(new TiffImageDescriptionMetadata(), diagnostics);
        }

        if (entry.DataType != TiffDataType.Ascii)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "ImageDescription must use ASCII data.", directory.ImageIndex, TiffTagId.ImageDescription);
            return TiffImageDescriptionReadResult.Create(new TiffImageDescriptionMetadata(), diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return TiffImageDescriptionReadResult.Create(new TiffImageDescriptionMetadata(), diagnostics);
        }

        TiffAsciiResult asciiResult = TiffAscii.Decode(loadResult.Bytes);
        diagnostics.Merge(asciiResult.Diagnostics);
        if (!asciiResult.Success)
        {
            return TiffImageDescriptionReadResult.Create(new TiffImageDescriptionMetadata(), diagnostics);
        }

        TiffImageDescriptionMetadata metadata = TiffImageDescriptionMetadata.Create(true, asciiResult.Text, asciiResult.Values, loadResult.Bytes);
        return TiffImageDescriptionReadResult.Create(metadata, diagnostics);
    }
}
