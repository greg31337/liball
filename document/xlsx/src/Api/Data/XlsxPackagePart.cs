using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Describes one physical part enumerated from an OOXML package.
/// </summary>
public class XlsxPackagePart
{
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string Path;
    /// <summary>
    /// Stores the content type resolved from the package content types part.
    /// </summary>
    public string? ContentType;
    /// <summary>
    /// Stores the compressed byte count reported by the ZIP directory.
    /// </summary>
    public long CompressedBytes;
    /// <summary>
    /// Stores the uncompressed byte count reported by the ZIP directory.
    /// </summary>
    public long UncompressedBytes;
    /// <summary>
    /// Indicates whether this item is a relationships part.
    /// </summary>
    public bool IsRelationshipPart;
    /// <summary>
    /// Contains the uncompressed part bytes.
    /// </summary>
    public byte[] Data;
    /// <summary>
    /// Initializes a new package part with its default state.
    /// </summary>
    public XlsxPackagePart()
    {
        Path = "";
        ContentType = "";
        CompressedBytes = 0;
        UncompressedBytes = 0;
        IsRelationshipPart = false;
        Data = new byte[0];
    }
}
