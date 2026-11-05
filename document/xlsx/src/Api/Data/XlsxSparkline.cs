using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents sparkline data used by the public workbook API.
/// </summary>
public class XlsxSparkline
{
    /// <summary>
    /// Stores the formula value used by this model.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string CellReference;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new sparkline with its default state.
    /// </summary>
    public XlsxSparkline()
    {
        Formula = "";
        CellReference = "";
        RawXml = "";
    }
}
