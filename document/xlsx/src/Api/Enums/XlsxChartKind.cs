using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Identifies a chart plot type supported by typed chart authoring.
/// </summary>
public enum XlsxChartKind
{
    /// <summary>A vertical column chart.</summary>
    Column = 0,
    /// <summary>A horizontal bar chart.</summary>
    Bar = 1,
    /// <summary>A line chart.</summary>
    Line = 2,
    /// <summary>An area chart.</summary>
    Area = 3,
    /// <summary>A pie chart.</summary>
    Pie = 4,
    /// <summary>A doughnut chart.</summary>
    Doughnut = 5,
    /// <summary>An X/Y scatter chart.</summary>
    Scatter = 6,
    /// <summary>A bubble chart.</summary>
    Bubble = 7,
    /// <summary>A radar chart.</summary>
    Radar = 8,
    /// <summary>A stock chart.</summary>
    Stock = 9,
    /// <summary>A surface chart.</summary>
    Surface = 10
}
