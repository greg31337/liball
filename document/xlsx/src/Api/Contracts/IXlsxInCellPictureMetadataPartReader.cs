using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxInCellPictureMetadataPartReader"/>.
/// </summary>
public interface IXlsxInCellPictureMetadataPartReader
{
    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The in cell picture metadata part produced by the operation.</returns>
    XlsxInCellPictureMetadataPart ReadString(string partPath, string contentType, string xml);
    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The in cell picture metadata part produced by the operation.</returns>
    XlsxInCellPictureMetadataPart ReadBytes(string partPath, string? contentType, byte[]? bytes);
}
