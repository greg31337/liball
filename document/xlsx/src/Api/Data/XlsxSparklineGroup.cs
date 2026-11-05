using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents sparkline group data used by the public workbook API.
/// </summary>
public class XlsxSparklineGroup
{
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Contains display empty cells as.
    /// </summary>
    public string DisplayEmptyCellsAs;
    /// <summary>
    /// Contains sparklines.
    /// </summary>
    public List<XlsxSparkline> Sparklines;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new sparkline group with its default state.
    /// </summary>
    public XlsxSparklineGroup()
    {
        Type = "line";
        DisplayEmptyCellsAs = "gap";
        Sparklines = new List<XlsxSparkline>();
        RawXml = "";
    }
}
