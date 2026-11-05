using System;

namespace tiff;

internal class TiffGpsMetadataReader
{
    internal static TiffGpsReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        if (header == null)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF header is required to read GPS metadata.");
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        return Read(directory, fileBytes, header.ByteOrder, header.FileKind);
    }

    internal static TiffGpsReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        return Read(directory, fileBytes, byteOrder, fileKind, new TiffReadOptions());
    }

    internal static TiffGpsReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A parent directory is required to read GPS metadata.");
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        TiffIfdPointerReadResult pointerResult = TiffIfdPointerReader.Read(directory, fileBytes, byteOrder, fileKind, options);
        diagnostics.Merge(pointerResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        TiffIfdPointer gpsPointer = TiffExifMetadataReader.FindPointer(pointerResult.Pointers, TiffTagId.GpsIfd);
        if (gpsPointer.TagId == 0)
        {
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        if (gpsPointer.Directories.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidOffset, "GPS IFD pointer did not resolve to a directory.", directory.ImageIndex, TiffTagId.GpsIfd);
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        TiffDirectory gpsDirectory = gpsPointer.Directories[0];
        TiffLimits limits = TiffDirectoryReader.GetLimits(TiffDirectoryReader.NormalizeOptions(options));
        TiffTagValue[] values = TiffExifMetadataReader.ReadValues(gpsDirectory, fileBytes, byteOrder, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        TiffGpsMetadata metadata = TiffGpsMetadata.Create(true, directory.ImageIndex, directory.Offset, gpsPointer.Offset, gpsDirectory, values);
        return TiffGpsReadResult.Create(metadata, diagnostics);
    }
}
