using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Describes the package thumbnail used as document preview metadata.
/// </summary>
public class XlsxPackagePreviewMetadata
{
    /// <summary>
    /// Indicates whether the package contains a resolvable thumbnail relationship and part.
    /// </summary>
    public bool HasThumbnail;
    /// <summary>
    /// Stores the package relationship identifier for the thumbnail.
    /// </summary>
    public string? RelationshipId;
    /// <summary>
    /// Stores the normalized absolute thumbnail part path.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the thumbnail MIME content type.
    /// </summary>
    public string? ContentType;
    /// <summary>
    /// Stores the thumbnail byte count.
    /// </summary>
    public long ByteLength;
    /// <summary>
    /// Contains the thumbnail bytes without decoding the image.
    /// </summary>
    public byte[] Data;
    /// <summary>
    /// Initializes new package preview metadata with its default state.
    /// </summary>
    public XlsxPackagePreviewMetadata()
    {
        HasThumbnail = false;
        RelationshipId = "";
        PartPath = "";
        ContentType = "";
        ByteLength = 0;
        Data = new byte[0];
    }
}
