using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Represents a chart manual layout using normalized chart coordinates.
/// </summary>
public class XlsxChartLayout
{
    /// <summary>Stores the layout target, such as <c>inner</c> or <c>outer</c>.</summary>
    public string Target;
    /// <summary>Stores the horizontal position mode.</summary>
    public string XMode;
    /// <summary>Stores the vertical position mode.</summary>
    public string YMode;
    /// <summary>Stores the width mode.</summary>
    public string WidthMode;
    /// <summary>Stores the height mode.</summary>
    public string HeightMode;
    /// <summary>Stores the normalized horizontal position.</summary>
    public string X;
    /// <summary>Stores the normalized vertical position.</summary>
    public string Y;
    /// <summary>Stores the normalized width.</summary>
    public string Width;
    /// <summary>Stores the normalized height.</summary>
    public string Height;

    /// <summary>Initializes a default edge-based inner layout.</summary>
    public XlsxChartLayout()
    {
        Target = "inner";
        XMode = "edge";
        YMode = "edge";
        WidthMode = "factor";
        HeightMode = "factor";
        X = "0";
        Y = "0";
        Width = "1";
        Height = "1";
    }
}
