using System;
using System.IO;

namespace tiff;

/// <summary>Defines compact TIFF inspection operations.</summary>
public interface ITiffInspectionService
{
    /// <summary>Inspects complete file bytes.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The inspection result.</returns>
    TiffInspectionResult InspectBytes(byte[] fileBytes, TiffReadOptions options);
    /// <summary>Inspects file memory.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The inspection result.</returns>
    TiffInspectionResult InspectMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options);
    /// <summary>Inspects a file path.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The inspection result.</returns>
    TiffInspectionResult InspectPath(string path, TiffReadOptions options);
    /// <summary>Inspects a stream.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The inspection result.</returns>
    TiffInspectionResult InspectStream(Stream stream, TiffReadOptions options);
}
