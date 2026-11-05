using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls the behavior of worksheet print operations.
/// </summary>
public class XlsxWorksheetPrintOptions
{
    /// <summary>
    /// Stores the horizontal centered value used by this model.
    /// </summary>
    public bool HorizontalCentered;
    /// <summary>
    /// Indicates whether horizontal centered is present and should be written.
    /// </summary>
    public bool HasHorizontalCentered;
    /// <summary>
    /// Stores the vertical centered value used by this model.
    /// </summary>
    public bool VerticalCentered;
    /// <summary>
    /// Indicates whether vertical centered is present and should be written.
    /// </summary>
    public bool HasVerticalCentered;
    /// <summary>
    /// Contains headings.
    /// </summary>
    public bool Headings;
    /// <summary>
    /// Indicates whether headings is present and should be written.
    /// </summary>
    public bool HasHeadings;
    /// <summary>
    /// Contains grid lines.
    /// </summary>
    public bool GridLines;
    /// <summary>
    /// Indicates whether grid lines is present and should be written.
    /// </summary>
    public bool HasGridLines;
    /// <summary>
    /// Initializes a new worksheet print options with its default state.
    /// </summary>
    public XlsxWorksheetPrintOptions()
    {
        HorizontalCentered = false;
        HasHorizontalCentered = false;
        VerticalCentered = false;
        HasVerticalCentered = false;
        Headings = false;
        HasHeadings = false;
        GridLines = false;
        HasGridLines = false;
    }
}
