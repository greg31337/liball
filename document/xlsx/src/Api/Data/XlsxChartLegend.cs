using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents chart legend data used by the public workbook API.
/// </summary>
public class XlsxChartLegend
{
    /// <summary>
    /// Stores the position value used by this model.
    /// </summary>
    public string Position;
    /// <summary>
    /// Stores the overlay value used by this model.
    /// </summary>
    public bool Overlay;
    /// <summary>
    /// Indicates whether overlay is present and should be written.
    /// </summary>
    public bool HasOverlay;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new chart legend with its default state.
    /// </summary>
    public XlsxChartLegend()
    {
        Position = "r";
        Overlay = false;
        HasOverlay = false;
        RawXml = "";
    }
}
