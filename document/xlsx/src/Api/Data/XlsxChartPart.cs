using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML chart part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxChartPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Indicates whether chart is present and should be written.
    /// </summary>
    public bool HasChart;
    /// <summary>
    /// Stores the style id value used by this model.
    /// </summary>
    public uint StyleId;
    /// <summary>
    /// Indicates whether style is present and should be written.
    /// </summary>
    public bool HasStyle;
    /// <summary>
    /// Stores the title text value used by this model.
    /// </summary>
    public string TitleText;
    /// <summary>
    /// Indicates whether title is present and should be written.
    /// </summary>
    public bool HasTitle;
    /// <summary>
    /// Stores the legend value used by this model.
    /// </summary>
    public XlsxChartLegend Legend;
    /// <summary>
    /// Indicates whether legend is present and should be written.
    /// </summary>
    public bool HasLegend;
    /// <summary>
    /// Contains chart types.
    /// </summary>
    public List<string> ChartTypes;
    /// <summary>
    /// Contains strongly typed plot kinds. Multiple entries form a combo chart.
    /// </summary>
    public List<XlsxChartKind> ChartKinds;
    /// <summary>
    /// Contains series.
    /// </summary>
    public List<XlsxChartSeriesReference> Series;
    /// <summary>
    /// Contains axes.
    /// </summary>
    public List<XlsxChartAxis> Axes;
    /// <summary>Stores the plot-area manual layout.</summary>
    public XlsxChartLayout Layout;
    /// <summary>Indicates whether a manual layout is present.</summary>
    public bool HasLayout;
    /// <summary>Stores the chart data table.</summary>
    public XlsxChartDataTable DataTable;
    /// <summary>Indicates whether a chart data table is present.</summary>
    public bool HasDataTable;
    /// <summary>
    /// Contains advanced chart elements.
    /// </summary>
    public List<XlsxChartRawElement> AdvancedChartElements;
    /// <summary>
    /// Contains preservation models for extension-based newer chart vocabularies.
    /// </summary>
    public List<XlsxChartRawElement> NewerChartElements;
    /// <summary>
    /// Stores the extension xml value used by this model.
    /// </summary>
    public List<string> ExtensionXml;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new chart part with its default state.
    /// </summary>
    public XlsxChartPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/charts/chart1.xml";
        NamespaceUri = XlsxNamespaces.Chart;
        RawXml = "";
        HasChart = false;
        StyleId = 0;
        HasStyle = false;
        TitleText = "";
        HasTitle = false;
        Legend = new XlsxChartLegend();
        HasLegend = false;
        ChartTypes = new List<string>();
        ChartKinds = new List<XlsxChartKind>();
        Series = new List<XlsxChartSeriesReference>();
        Axes = new List<XlsxChartAxis>();
        Layout = new XlsxChartLayout();
        HasLayout = false;
        DataTable = new XlsxChartDataTable();
        HasDataTable = false;
        AdvancedChartElements = new List<XlsxChartRawElement>();
        NewerChartElements = new List<XlsxChartRawElement>();
        ExtensionXml = new List<string>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Adds series to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "textFormula">Excel formula text without a leading equals sign.</param>
    /// <param name = "categoryFormula">Excel formula text without a leading equals sign.</param>
    /// <param name = "valuesFormula">Excel formula text without a leading equals sign.</param>
    /// <returns>The chart series reference produced by the operation.</returns>
    internal XlsxChartSeriesReference AddSeries(string textFormula, string categoryFormula, string valuesFormula)
    {
        XlsxChartSeriesReference series = new XlsxChartSeriesReference();
        series.Index = (uint)Series.Count;
        series.HasIndex = true;
        series.Order = (uint)Series.Count;
        series.HasOrder = true;
        series.TextFormula = XlsxCellValueConverter.NormalizeText(textFormula);
        series.CategoryFormula = XlsxCellValueConverter.NormalizeText(categoryFormula);
        series.ValuesFormula = XlsxCellValueConverter.NormalizeText(valuesFormula);
        Series.Add(series);
        RawXml = "";
        return series;
    }

    internal XlsxValidationResult AddChartKind(XlsxChartKind chartKind)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (!IsKnownChartKind(chartKind))
        {
            result.AddError("chart.kindInvalid", "Chart kind is not supported by typed chart authoring.");
            return result;
        }
        if (ChartKinds == null) { ChartKinds = new List<XlsxChartKind>(); }
        if (ChartTypes == null) { ChartTypes = new List<string>(); }
        bool exists = false;
        foreach (XlsxChartKind existing in ChartKinds)
        {
            if (existing == chartKind) { exists = true; }
        }
        if (!exists)
        {
            ChartKinds.Add(chartKind);
            ChartTypes.Add(GetChartKindText(chartKind));
        }
        HasChart = true;
        RawXml = "";
        return result;
    }

    internal XlsxChartSeriesReference AddSeriesForChart(XlsxChartKind chartKind, string textFormula, string categoryFormula, string valuesFormula, string bubbleSizeFormula)
    {
        AddChartKind(chartKind);
        XlsxChartSeriesReference series = AddSeries(textFormula, categoryFormula, valuesFormula);
        series.ChartKind = chartKind;
        series.HasChartKind = true;
        series.BubbleSizeFormula = XlsxCellValueConverter.NormalizeText(bubbleSizeFormula);
        return series;
    }

    internal XlsxChartAxis AddAxis(string axisType, string axisId, string crossAxisId, string position, string title)
    {
        XlsxChartAxis axis = new XlsxChartAxis();
        axis.AxisType = XlsxCellValueConverter.NormalizeText(axisType);
        axis.AxisId = XlsxCellValueConverter.NormalizeText(axisId);
        axis.CrossAxisId = XlsxCellValueConverter.NormalizeText(crossAxisId);
        axis.Position = XlsxCellValueConverter.NormalizeText(position);
        axis.TitleText = XlsxCellValueConverter.NormalizeText(title);
        axis.HasTitle = axis.TitleText.Length > 0;
        Axes.Add(axis);
        RawXml = "";
        return axis;
    }

    internal void SetManualLayout(string x, string y, string width, string height)
    {
        Layout.X = XlsxCellValueConverter.NormalizeText(x);
        Layout.Y = XlsxCellValueConverter.NormalizeText(y);
        Layout.Width = XlsxCellValueConverter.NormalizeText(width);
        Layout.Height = XlsxCellValueConverter.NormalizeText(height);
        HasLayout = true;
        RawXml = "";
    }

    internal void SetDataTable(bool showHorizontalBorder, bool showVerticalBorder, bool showOutline, bool showLegendKeys)
    {
        DataTable.ShowHorizontalBorder = showHorizontalBorder;
        DataTable.ShowVerticalBorder = showVerticalBorder;
        DataTable.ShowOutline = showOutline;
        DataTable.ShowLegendKeys = showLegendKeys;
        HasDataTable = true;
        RawXml = "";
    }

    internal void SetPresentation(string title, uint styleId, string legendPosition, bool legendOverlay)
    {
        TitleText = XlsxCellValueConverter.NormalizeText(title);
        HasTitle = TitleText.Length > 0;
        StyleId = styleId;
        HasStyle = true;
        Legend.Position = XlsxCellValueConverter.NormalizeText(legendPosition);
        Legend.Overlay = legendOverlay;
        Legend.HasOverlay = true;
        HasLegend = Legend.Position.Length > 0;
        RawXml = "";
    }

    internal XlsxValidationResult SetAxisGridlines(int axisIndex, bool major, string majorColorRgb, bool minor, string minorColorRgb, string minimum, string maximum)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (Axes == null || axisIndex < 0 || axisIndex >= Axes.Count)
        {
            result.AddError("chart.axisIndexRange", "Chart axis index is outside the axis list.");
            return result;
        }
        XlsxChartAxis axis = Axes[axisIndex];
        axis.HasMajorGridlines = major;
        axis.MajorGridlineColorRgb = XlsxCellValueConverter.NormalizeText(majorColorRgb);
        axis.HasMinorGridlines = minor;
        axis.MinorGridlineColorRgb = XlsxCellValueConverter.NormalizeText(minorColorRgb);
        axis.Minimum = XlsxCellValueConverter.NormalizeText(minimum);
        axis.Maximum = XlsxCellValueConverter.NormalizeText(maximum);
        axis.RawXml = "";
        RawXml = "";
        return result;
    }

    internal XlsxValidationResult SetSeriesAppearance(int seriesIndex, string colorRgb, string labelPosition, bool showValue, bool showCategoryName, bool showSeriesName)
    {
        XlsxValidationResult result = ValidateSeriesIndex(seriesIndex);
        if (!result.IsValid) { return result; }
        XlsxChartSeriesReference series = Series[seriesIndex];
        series.ColorRgb = XlsxCellValueConverter.NormalizeText(colorRgb);
        series.HasColor = series.ColorRgb.Length > 0;
        series.DataLabelPosition = XlsxCellValueConverter.NormalizeText(labelPosition);
        series.ShowLabelValue = showValue;
        series.ShowLabelCategoryName = showCategoryName;
        series.ShowLabelSeriesName = showSeriesName;
        series.HasDataLabels = series.DataLabelPosition.Length > 0 || showValue || showCategoryName || showSeriesName;
        series.RawXml = "";
        RawXml = "";
        return result;
    }

    internal XlsxValidationResult SetSeriesTrendline(int seriesIndex, string trendlineType, string name, int order, int period, bool displayEquation, bool displayRSquared)
    {
        XlsxValidationResult result = ValidateSeriesIndex(seriesIndex);
        if (!result.IsValid) { return result; }
        XlsxChartSeriesReference series = Series[seriesIndex];
        series.TrendlineType = XlsxCellValueConverter.NormalizeText(trendlineType);
        series.TrendlineName = XlsxCellValueConverter.NormalizeText(name);
        series.TrendlineOrder = order;
        series.TrendlinePeriod = period;
        series.DisplayTrendlineEquation = displayEquation;
        series.DisplayTrendlineRSquared = displayRSquared;
        series.HasTrendline = true;
        series.RawXml = "";
        RawXml = "";
        return result;
    }

    internal XlsxValidationResult SetSeriesErrorBars(int seriesIndex, string direction, string barType, string valueType, string value, bool noEndCap)
    {
        XlsxValidationResult result = ValidateSeriesIndex(seriesIndex);
        if (!result.IsValid) { return result; }
        XlsxChartSeriesReference series = Series[seriesIndex];
        series.ErrorBarDirection = XlsxCellValueConverter.NormalizeText(direction);
        series.ErrorBarType = XlsxCellValueConverter.NormalizeText(barType);
        series.ErrorBarValueType = XlsxCellValueConverter.NormalizeText(valueType);
        series.ErrorBarValue = XlsxCellValueConverter.NormalizeText(value);
        series.ErrorBarNoEndCap = noEndCap;
        series.HasErrorBars = true;
        series.RawXml = "";
        RawXml = "";
        return result;
    }

    internal XlsxValidationResult ValidateSeriesIndex(int seriesIndex)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (Series == null || seriesIndex < 0 || seriesIndex >= Series.Count)
        {
            result.AddError("chart.seriesIndexRange", "Chart series index is outside the series list.");
        }
        return result;
    }

    internal static bool IsKnownChartKind(XlsxChartKind chartKind)
    {
        int value = (int)chartKind;
        return value >= (int)XlsxChartKind.Column && value <= (int)XlsxChartKind.Surface;
    }

    internal static string GetChartKindText(XlsxChartKind chartKind)
    {
        if (chartKind == XlsxChartKind.Column) { return "column"; }
        if (chartKind == XlsxChartKind.Bar) { return "bar"; }
        if (chartKind == XlsxChartKind.Line) { return "line"; }
        if (chartKind == XlsxChartKind.Area) { return "area"; }
        if (chartKind == XlsxChartKind.Pie) { return "pie"; }
        if (chartKind == XlsxChartKind.Doughnut) { return "doughnut"; }
        if (chartKind == XlsxChartKind.Scatter) { return "scatter"; }
        if (chartKind == XlsxChartKind.Bubble) { return "bubble"; }
        if (chartKind == XlsxChartKind.Radar) { return "radar"; }
        if (chartKind == XlsxChartKind.Stock) { return "stock"; }
        if (chartKind == XlsxChartKind.Surface) { return "surface"; }
        return "";
    }
}
