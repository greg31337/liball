using System;
using System.IO;

namespace tiff;

/// <summary>Creates a compact, read-only description of a TIFF file for diagnostics and tooling.</summary>
internal static class TiffInspector
{
    /// <summary>Inspects the TIFF source and returns a compact description of its header, images, tags, and diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffInspectionResult Inspect(byte[] fileBytes)
    {
        return Inspect(fileBytes, new TiffReadOptions());
    }

    /// <summary>Inspects the TIFF source and returns a compact description of its header, images, tags, and diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffInspectionResult Inspect(byte[] fileBytes, TiffReadOptions options)
    {
        if (fileBytes == null)
        {
            return Inspect(ReadOnlySpan<byte>.Empty, options);
        }

        return Inspect(fileBytes.AsSpan(), options);
    }

    /// <summary>Inspects the TIFF source and returns a compact description of its header, images, tags, and diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffInspectionResult Inspect(ReadOnlySpan<byte> fileBytes)
    {
        return Inspect(fileBytes, new TiffReadOptions());
    }

    /// <summary>Inspects the TIFF source and returns a compact description of its header, images, tags, and diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffInspectionResult Inspect(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(fileBytes, options);
        return InspectAndClose(file);
    }

    /// <summary>Inspects the TIFF source and returns a compact description of its header, images, tags, and diagnostics.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffInspectionResult Inspect(string path)
    {
        return Inspect(path, new TiffReadOptions());
    }

    /// <summary>Inspects the TIFF source and returns a compact description of its header, images, tags, and diagnostics.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffInspectionResult Inspect(string path, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(path, options);
        return InspectAndClose(file);
    }

    /// <summary>Inspects the TIFF source and returns a compact description of its header, images, tags, and diagnostics.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffInspectionResult Inspect(Stream stream)
    {
        return Inspect(stream, new TiffReadOptions());
    }

    /// <summary>Inspects the TIFF source and returns a compact description of its header, images, tags, and diagnostics.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffInspectionResult Inspect(Stream stream, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(stream, options);
        return InspectAndClose(file);
    }

    internal static TiffInspectionResult InspectAndClose(TiffFile file)
    {
        TiffInspectionResult result = InspectFile(file);
        if (file != null)
        {
            file.Close();
        }

        return result;
    }

    internal static TiffInspectionResult InspectFile(TiffFile file)
    {
        if (file == null)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF file handle is required for inspection.");
            return TiffInspectionResult.Create(new TiffHeader(), new TiffInspectionImage[0], diagnostics, 0);
        }

        TiffInspectionImage[] images = new TiffInspectionImage[file.GetImageCount()];
        int index = 0;
        while (index < images.Length)
        {
            TiffImage image = file.GetImage(index);
            TiffImageInfo info = image.GetInfo();
            TiffIfdEntry[] tags = image.GetTags();
            int subImageCount = image.GetSubImageCount();
            images[index] = TiffInspectionImage.Create(index, info, tags, subImageCount);
            index = index + 1;
        }

        return TiffInspectionResult.Create(file.GetHeader(), images, file.GetDiagnostics(), file.GetFileByteCount());
    }
}
