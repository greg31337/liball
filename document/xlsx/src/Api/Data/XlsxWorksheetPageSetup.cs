using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet page setup data used by the public workbook API.
/// </summary>
public class XlsxWorksheetPageSetup
{
    /// <summary>
    /// Stores the paper size value used by this model.
    /// </summary>
    public uint PaperSize;
    /// <summary>
    /// Indicates whether paper size is present and should be written.
    /// </summary>
    public bool HasPaperSize;
    /// <summary>
    /// Stores the orientation value used by this model.
    /// </summary>
    public string? Orientation;
    /// <summary>
    /// Stores the scale value used by this model.
    /// </summary>
    public uint Scale;
    /// <summary>
    /// Indicates whether scale is present and should be written.
    /// </summary>
    public bool HasScale;
    /// <summary>
    /// Stores the fit to width value used by this model.
    /// </summary>
    public uint FitToWidth;
    /// <summary>
    /// Indicates whether fit to width is present and should be written.
    /// </summary>
    public bool HasFitToWidth;
    /// <summary>
    /// Stores the fit to height value used by this model.
    /// </summary>
    public uint FitToHeight;
    /// <summary>
    /// Indicates whether fit to height is present and should be written.
    /// </summary>
    public bool HasFitToHeight;
    /// <summary>
    /// Stores the first page number value used by this model.
    /// </summary>
    public uint FirstPageNumber;
    /// <summary>
    /// Indicates whether first page number is present and should be written.
    /// </summary>
    public bool HasFirstPageNumber;
    /// <summary>
    /// Stores the use first page number value used by this model.
    /// </summary>
    public bool UseFirstPageNumber;
    /// <summary>
    /// Indicates whether use first page number is present and should be written.
    /// </summary>
    public bool HasUseFirstPageNumber;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new worksheet page setup with its default state.
    /// </summary>
    public XlsxWorksheetPageSetup()
    {
        PaperSize = 0;
        HasPaperSize = false;
        Orientation = "";
        Scale = 0;
        HasScale = false;
        FitToWidth = 0;
        HasFitToWidth = false;
        FitToHeight = 0;
        HasFitToHeight = false;
        FirstPageNumber = 0;
        HasFirstPageNumber = false;
        UseFirstPageNumber = false;
        HasUseFirstPageNumber = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
