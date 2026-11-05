using System;

namespace tiff;

internal class TiffGdalMetadataReader
{
    internal static TiffGdalMetadataReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return Read(directory, fileBytes, new TiffLimits());
    }

    internal static TiffGdalMetadataReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read GDAL metadata.");
            return TiffGdalMetadataReadResult.Create(new TiffGdalMetadata(), diagnostics);
        }

        TiffGdalAsciiValue metadata = ReadAsciiValue(directory, fileBytes, limits, TiffTagId.GdalMetadata, "GDAL_METADATA", diagnostics);
        TiffGdalAsciiValue noData = ReadAsciiValue(directory, fileBytes, limits, TiffTagId.GdalNoData, "GDAL_NODATA", diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGdalMetadataReadResult.Create(new TiffGdalMetadata(), diagnostics);
        }

        TiffGdalMetadata result = TiffGdalMetadata.Create(metadata.HasValue, metadata.Text, metadata.Values, metadata.RawBytes, noData.HasValue, noData.Text, noData.Values, noData.RawBytes);
        return TiffGdalMetadataReadResult.Create(result, diagnostics);
    }

    internal static TiffGdalAsciiValue ReadAsciiValue(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits, int tagId, string tagName, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return new TiffGdalAsciiValue();
        }

        if (entry.DataType != TiffDataType.Ascii)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, tagName + " must use ASCII data.", directory.ImageIndex, tagId);
            return new TiffGdalAsciiValue();
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return new TiffGdalAsciiValue();
        }

        TiffAsciiResult asciiResult = TiffAscii.Decode(loadResult.Bytes);
        diagnostics.Merge(asciiResult.Diagnostics);
        if (asciiResult.Diagnostics.HasErrors())
        {
            return new TiffGdalAsciiValue();
        }

        return TiffGdalAsciiValue.Create(true, asciiResult.Text, asciiResult.Values, loadResult.Bytes);
    }
}
