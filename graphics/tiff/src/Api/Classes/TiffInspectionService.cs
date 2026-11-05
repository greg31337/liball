using System;
using System.IO;

namespace tiff;

/// <summary>Creates compact descriptions of TIFF files.</summary>
public class TiffInspectionService : ITiffInspectionService
{
    /// <summary>Creates a TIFF inspection service.</summary>
    public TiffInspectionService()
    {
    }

    /// <summary>Inspects complete file bytes.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The inspection result.</returns>
    public TiffInspectionResult InspectBytes(byte[] fileBytes, TiffReadOptions options)
    {
        return TiffInspector.Inspect(fileBytes, options);
    }

    /// <summary>Inspects file memory.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The inspection result.</returns>
    public TiffInspectionResult InspectMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        return TiffInspector.Inspect(fileBytes, options);
    }

    /// <summary>Inspects a file path.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The inspection result.</returns>
    public TiffInspectionResult InspectPath(string path, TiffReadOptions options)
    {
        return TiffInspector.Inspect(path, options);
    }

    /// <summary>Inspects a stream.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The inspection result.</returns>
    public TiffInspectionResult InspectStream(Stream stream, TiffReadOptions options)
    {
        return TiffInspector.Inspect(stream, options);
    }
}
