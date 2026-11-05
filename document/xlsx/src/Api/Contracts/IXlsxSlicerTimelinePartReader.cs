using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxSlicerTimelinePartReader"/>.
/// </summary>
public interface IXlsxSlicerTimelinePartReader
{
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "expectedRootName">The expected root name value used by the operation.</param>
    /// <returns>The slicer timeline part produced by the operation.</returns>
    XlsxSlicerTimelinePart ReadString(string partPath, string xml, string expectedRootName);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "expectedRootName">The expected root name value used by the operation.</param>
    /// <returns>The slicer timeline part produced by the operation.</returns>
    XlsxSlicerTimelinePart ReadBytes(string partPath, byte[] bytes, string expectedRootName);
    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "expectedRootName">The expected root name value used by the operation.</param>
    /// <returns>The slicer timeline part produced by the operation.</returns>
    XlsxSlicerTimelinePart ReadStream(string partPath, Stream stream, string expectedRootName);
}
