using System;
using System.IO;

namespace xlsx;

/// <summary>
/// Defines writer operations for authored slicer and timeline parts.
/// </summary>
public interface IXlsxSlicerTimelinePartWriter
{
    /// <summary>Writes a slicer or timeline part to XML text.</summary>
    /// <param name="part">The typed part.</param>
    /// <returns>The write result.</returns>
    XlsxSlicerPartWriteResult WriteString(XlsxSlicerTimelinePart? part);
    /// <summary>Writes a slicer or timeline part to UTF-8 bytes.</summary>
    /// <param name="part">The typed part.</param>
    /// <returns>The write result.</returns>
    XlsxSlicerPartWriteResult WriteBytes(XlsxSlicerTimelinePart? part);
    /// <summary>Writes a slicer or timeline part to a caller-owned stream.</summary>
    /// <param name="stream">The writable stream.</param>
    /// <param name="part">The typed part.</param>
    /// <returns>The write result.</returns>
    XlsxSlicerPartWriteResult WriteStream(Stream stream, XlsxSlicerTimelinePart? part);
}
