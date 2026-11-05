using System;

namespace tiff;

internal class TiffIccProfileReader
{
    internal static TiffIccProfileReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return Read(directory, fileBytes, new TiffLimits());
    }

    internal static TiffIccProfileReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read an ICC profile.");
            return TiffIccProfileReadResult.Create(new TiffIccProfile(), diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.IccProfile);
        if (entry == null)
        {
            return TiffIccProfileReadResult.Create(new TiffIccProfile(), diagnostics);
        }

        if (entry.DataType != TiffDataType.Undefined)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "ICC profile tag must use UNDEFINED data.", directory.ImageIndex, TiffTagId.IccProfile);
            return TiffIccProfileReadResult.Create(new TiffIccProfile(), diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return TiffIccProfileReadResult.Create(new TiffIccProfile(), diagnostics);
        }

        TiffIccProfile profile = TiffIccProfile.Create(true, loadResult.Bytes);
        return TiffIccProfileReadResult.Create(profile, diagnostics);
    }
}
