using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents chart axis data used by the public workbook API.
/// </summary>
public class XlsxChartAxis
{
    /// <summary>
    /// Stores the axis type value used by this model.
    /// </summary>
    public string AxisType;
    /// <summary>
    /// Stores the axis id value used by this model.
    /// </summary>
    public string AxisId;
    /// <summary>
    /// Stores the cross axis id value used by this model.
    /// </summary>
    public string CrossAxisId;
    /// <summary>
    /// Stores the position value used by this model.
    /// </summary>
    public string Position;
    /// <summary>
    /// Stores the number format value used by this model.
    /// </summary>
    public string NumberFormat;
    /// <summary>
    /// Stores the title text value used by this model.
    /// </summary>
    public string TitleText;
    /// <summary>
    /// Indicates whether title is present and should be written.
    /// </summary>
    public bool HasTitle;
    /// <summary>Indicates whether major gridlines are shown.</summary>
    public bool HasMajorGridlines;
    /// <summary>Indicates whether minor gridlines are shown.</summary>
    public bool HasMinorGridlines;
    /// <summary>Stores the major gridline RGB color.</summary>
    public string MajorGridlineColorRgb;
    /// <summary>Stores the minor gridline RGB color.</summary>
    public string MinorGridlineColorRgb;
    /// <summary>Stores an explicit minimum scale value.</summary>
    public string Minimum;
    /// <summary>Stores an explicit maximum scale value.</summary>
    public string Maximum;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new chart axis with its default state.
    /// </summary>
    public XlsxChartAxis()
    {
        AxisType = "cat";
        AxisId = "";
        CrossAxisId = "";
        Position = "";
        NumberFormat = "";
        TitleText = "";
        HasTitle = false;
        HasMajorGridlines = false;
        HasMinorGridlines = false;
        MajorGridlineColorRgb = "";
        MinorGridlineColorRgb = "";
        Minimum = "";
        Maximum = "";
        RawXml = "";
    }
}
