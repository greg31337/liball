using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Represents the data table displayed below a chart.
/// </summary>
public class XlsxChartDataTable
{
    /// <summary>Indicates whether horizontal borders are shown.</summary>
    public bool ShowHorizontalBorder;
    /// <summary>Indicates whether vertical borders are shown.</summary>
    public bool ShowVerticalBorder;
    /// <summary>Indicates whether the outer border is shown.</summary>
    public bool ShowOutline;
    /// <summary>Indicates whether legend keys are shown.</summary>
    public bool ShowLegendKeys;

    /// <summary>Initializes a data table with borders and no legend keys.</summary>
    public XlsxChartDataTable()
    {
        ShowHorizontalBorder = true;
        ShowVerticalBorder = true;
        ShowOutline = true;
        ShowLegendKeys = false;
    }
}
