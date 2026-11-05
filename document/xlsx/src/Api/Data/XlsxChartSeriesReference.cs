using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents chart series reference data used by the public workbook API.
/// </summary>
public class XlsxChartSeriesReference
{
    /// <summary>Stores the plot kind for this series in a combo chart.</summary>
    public XlsxChartKind ChartKind;
    /// <summary>Indicates whether the series is assigned to a specific plot kind.</summary>
    public bool HasChartKind;
    /// <summary>
    /// Stores the index value used by this model.
    /// </summary>
    public uint Index;
    /// <summary>
    /// Indicates whether index is present and should be written.
    /// </summary>
    public bool HasIndex;
    /// <summary>
    /// Stores the order value used by this model.
    /// </summary>
    public uint Order;
    /// <summary>
    /// Indicates whether order is present and should be written.
    /// </summary>
    public bool HasOrder;
    /// <summary>
    /// Stores the text formula value used by this model.
    /// </summary>
    public string TextFormula;
    /// <summary>
    /// Stores the category formula value used by this model.
    /// </summary>
    public string CategoryFormula;
    /// <summary>
    /// Stores the values formula value used by this model.
    /// </summary>
    public string ValuesFormula;
    /// <summary>Stores the bubble-size formula for bubble charts.</summary>
    public string BubbleSizeFormula;
    /// <summary>
    /// Stores the color rgb value used by this model.
    /// </summary>
    public string ColorRgb;
    /// <summary>
    /// Indicates whether color is present and should be written.
    /// </summary>
    public bool HasColor;
    /// <summary>
    /// Stores the data label position value used by this model.
    /// </summary>
    public string DataLabelPosition;
    /// <summary>
    /// Indicates whether data labels is present and should be written.
    /// </summary>
    public bool HasDataLabels;
    /// <summary>Indicates whether values are displayed in data labels.</summary>
    public bool ShowLabelValue;
    /// <summary>Indicates whether category names are displayed in data labels.</summary>
    public bool ShowLabelCategoryName;
    /// <summary>Indicates whether series names are displayed in data labels.</summary>
    public bool ShowLabelSeriesName;
    /// <summary>Indicates whether legend keys are displayed in data labels.</summary>
    public bool ShowLabelLegendKey;
    /// <summary>
    /// Stores the trendline type value used by this model.
    /// </summary>
    public string TrendlineType;
    /// <summary>
    /// Indicates whether trendline is present and should be written.
    /// </summary>
    public bool HasTrendline;
    /// <summary>Stores the optional trendline name.</summary>
    public string TrendlineName;
    /// <summary>Stores the polynomial trendline order.</summary>
    public int TrendlineOrder;
    /// <summary>Stores the moving-average trendline period.</summary>
    public int TrendlinePeriod;
    /// <summary>Indicates whether the trendline equation is displayed.</summary>
    public bool DisplayTrendlineEquation;
    /// <summary>Indicates whether the trendline R-squared value is displayed.</summary>
    public bool DisplayTrendlineRSquared;
    /// <summary>
    /// Stores the error bar type value used by this model.
    /// </summary>
    public string ErrorBarType;
    /// <summary>
    /// Stores the error bar value value used by this model.
    /// </summary>
    public string ErrorBarValue;
    /// <summary>
    /// Indicates whether error bars is present and should be written.
    /// </summary>
    public bool HasErrorBars;
    /// <summary>Stores the error bar direction.</summary>
    public string ErrorBarDirection;
    /// <summary>Stores the error value type.</summary>
    public string ErrorBarValueType;
    /// <summary>Indicates whether error bar end caps are suppressed.</summary>
    public bool ErrorBarNoEndCap;
    /// <summary>
    /// Stores the extension xml value used by this model.
    /// </summary>
    public List<string> ExtensionXml;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new chart series reference with its default state.
    /// </summary>
    public XlsxChartSeriesReference()
    {
        ChartKind = XlsxChartKind.Column;
        HasChartKind = false;
        Index = 0;
        HasIndex = false;
        Order = 0;
        HasOrder = false;
        TextFormula = "";
        CategoryFormula = "";
        ValuesFormula = "";
        BubbleSizeFormula = "";
        ColorRgb = "";
        HasColor = false;
        DataLabelPosition = "";
        HasDataLabels = false;
        ShowLabelValue = false;
        ShowLabelCategoryName = false;
        ShowLabelSeriesName = false;
        ShowLabelLegendKey = false;
        TrendlineType = "";
        HasTrendline = false;
        TrendlineName = "";
        TrendlineOrder = 2;
        TrendlinePeriod = 2;
        DisplayTrendlineEquation = false;
        DisplayTrendlineRSquared = false;
        ErrorBarType = "";
        ErrorBarValue = "";
        HasErrorBars = false;
        ErrorBarDirection = "y";
        ErrorBarValueType = "fixedVal";
        ErrorBarNoEndCap = false;
        ExtensionXml = new List<string>();
        RawXml = "";
    }
}
