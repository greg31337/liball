using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML unknown part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxUnknownPart
{
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string Path;
    /// <summary>
    /// Stores the content type value used by this model.
    /// </summary>
    public string? ContentType;
    /// <summary>
    /// Stores the data value used by this model.
    /// </summary>
    public byte[] Data;
    /// <summary>
    /// Initializes a new unknown part with its default state.
    /// </summary>
    public XlsxUnknownPart()
    {
        Path = "";
        ContentType = "";
        Data = new byte[0];
    }
}
