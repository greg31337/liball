using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet dimension data used by the public workbook API.
/// </summary>
public class XlsxWorksheetDimension
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string? Reference;
    /// <summary>
    /// Initializes a new worksheet dimension with its default state.
    /// </summary>
    public XlsxWorksheetDimension()
    {
        Reference = "";
    }
}
