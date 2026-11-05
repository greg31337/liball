using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes chart part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxChartPartWriter : IXlsxChartPartWriter
{
    /// <summary>
    /// Initializes a new chart part writer with its default state.
    /// </summary>
    public XlsxChartPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxChartPartWriteResult WriteString(XlsxChartPart part)
    {
        XlsxChartPartWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxChartPartWriteResult WriteBytes(XlsxChartPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxChartPartWriteResult result = WriteStream(stream, part);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxChartPartWriteResult WriteStream(Stream stream, XlsxChartPart part)
    {
        XlsxChartPartWriteResult result = ValidateChartPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(part.RawXml);
            stream.Write(bytes, 0, bytes.Length);
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("c", "chartSpace", XlsxNamespaces.Chart);
            writer.WriteAttributeString("xmlns", "a", "http://www.w3.org/2000/xmlns/", XlsxNamespaces.Drawing);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            if (part.HasStyle)
            {
                WriteValueElement(writer, "style", part.StyleId.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteStartElement("c", "chart", XlsxNamespaces.Chart);
            if (part.HasTitle || (part.TitleText != null && part.TitleText.Length > 0))
            {
                WriteTitle(writer, part.TitleText);
            }

            writer.WriteStartElement("c", "plotArea", XlsxNamespaces.Chart);
            WriteLayout(writer, part);
            WriteChartTypes(writer, part);
            if (part.Axes != null && part.Axes.Count > 0)
            {
                WriteAxes(writer, part.Axes);
            }
            else if (NeedsCategoryValueAxes(part))
            {
                WriteCategoryValueAxes(writer);
            }
            else if (NeedsScatterAxes(part))
            {
                WriteScatterAxes(writer);
            }

            if (part.HasDataTable)
            {
                WriteDataTable(writer, part.DataTable);
            }

            writer.WriteEndElement();
            if (part.HasLegend)
            {
                WriteLegend(writer, part.Legend);
            }

            writer.WriteEndElement();
            WriteUnknownXml(writer, part.UnknownXml);
            WriteChartExtensions(writer, part);
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxChartPartWriteResult ValidateChartPart(XlsxChartPart part)
    {
        XlsxChartPartWriteResult result = new XlsxChartPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.partNull", "Chart part must not be null."));
            return result;
        }

        if (part.RawXml != null && part.RawXml.Length > 0)
        {
            return result;
        }

        if (part.NamespaceUri == null || part.NamespaceUri.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.namespaceMissing", "Chart namespace must not be empty."));
        }
        else if (part.NamespaceUri != XlsxNamespaces.Chart)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.namespaceInvalid", "Chart namespace must be the DrawingML chart namespace."));
        }

        if (part.ChartTypes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.chartTypesNull", "Chart type list must not be null."));
        }
        else if (part.ChartTypes.Count == 0 && (part.ChartKinds == null || part.ChartKinds.Count == 0))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.chartTypeMissing", "Chart part must contain at least one chart type."));
        }


        if (part.ChartKinds == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.chartKindsNull", "Typed chart kind list must not be null."));
        }
        if (part.ChartKinds != null)
        {
            foreach (XlsxChartKind chartKind in part.ChartKinds)
            {
                if (!XlsxChartPart.IsKnownChartKind(chartKind))
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.chartKindInvalid", "Typed chart kind is outside the supported range."));
                }
            }
        }

        if (part.HasLayout && part.Layout == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.layoutNull", "Chart layout must not be null when enabled."));
        }

        if (part.HasDataTable && part.DataTable == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.dataTableNull", "Chart data table must not be null when enabled."));
        }
        if (part.ChartTypes != null)
        {
            foreach (string chartType in part.ChartTypes)
            {
                if (!IsSupportedChartType(chartType) && !HasAdvancedChartElement(part, chartType))
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.chartTypeUnsupported", "Chart type is not supported by the basic chart writer."));
                }
            }
        }

        if (part.Series == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.seriesNull", "Chart series list must not be null."));
        }
        else if (part.Series.Count == 0 && !HasAnyAdvancedChartElement(part))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.seriesMissing", "Chart part must contain at least one series."));
        }
        else
        {
            foreach (XlsxChartSeriesReference series in part.Series)
            {
                ValidateSeries(result, series);
            }
        }

        if (part.Axes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.axesNull", "Chart axis list must not be null."));
        }
        else
        {
            foreach (XlsxChartAxis axis in part.Axes)
            {
                ValidateAxis(result, axis);
            }
        }

        if (part.HasLegend && part.Legend == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.legendNull", "Chart legend must not be null when HasLegend is true."));
        }

        if (part.AdvancedChartElements == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.advancedChartElementsNull", "Chart advanced element list must not be null."));
        }
        else
        {
            ValidateRawChartElements(result, part.AdvancedChartElements);
        }

        if (part.NewerChartElements == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.newerChartElementsNull", "Newer chart preservation element list must not be null."));
        }
        else
        {
            ValidateRawChartElements(result, part.NewerChartElements);
        }

        if (part.ExtensionXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.extensionXmlNull", "Chart extension XML list must not be null."));
        }

        if (part.UnknownRootAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.unknownRootAttributesNull", "Chart unknown root attribute list must not be null."));
        }

        if (part.UnknownXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.unknownXmlNull", "Chart unknown XML list must not be null."));
        }

        return result;
    }

    internal static void ValidateSeries(XlsxChartPartWriteResult result, XlsxChartSeriesReference series)
    {
        if (series == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.seriesItemNull", "Chart series list must not contain null entries."));
            return;
        }

        if (series.ValuesFormula == null || series.ValuesFormula.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.seriesValuesFormulaMissing", "Chart series must contain a values formula."));
        }

        if (series.ExtensionXml == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.seriesExtensionXmlNull", "Chart series extension XML list must not be null."));
        }
    }

    internal static void ValidateRawChartElements(XlsxChartPartWriteResult result, List<XlsxChartRawElement> elements)
    {
        foreach (XlsxChartRawElement element in elements)
        {
            if (element == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.advancedChartElementNull", "Chart advanced element list must not contain null entries."));
            }
            else if (element.RawXml == null || element.RawXml.Length == 0)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.advancedChartElementXmlMissing", "Chart advanced element must contain preserved XML."));
            }
        }
    }

    internal static void ValidateAxis(XlsxChartPartWriteResult result, XlsxChartAxis axis)
    {
        if (axis == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.axisNull", "Chart axis list must not contain null entries."));
            return;
        }

        if (axis.AxisType != "cat" && axis.AxisType != "val")
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chartWriter.axisTypeInvalid", "Chart axis type must be cat or val."));
        }
    }

    internal static void WriteChartTypes(XmlWriter writer, XlsxChartPart part)
    {
        if (part.ChartKinds != null && part.ChartKinds.Count > 0)
        {
            foreach (XlsxChartKind chartKind in part.ChartKinds)
            {
                if (!HasAdvancedChartElement(part, GetChartKindElementName(chartKind)))
                {
                    WriteTypedChartKind(writer, part.Series, chartKind);
                }
            }
            WriteAdvancedChartElements(writer, part.AdvancedChartElements);
            return;
        }

        foreach (string chartType in part.ChartTypes)
        {
            string normalized = NormalizeChartType(chartType);
            if (normalized == "combo")
            {
                WriteColumnChart(writer, part.Series, XlsxChartKind.Column);
                WriteLineChart(writer, part.Series, XlsxChartKind.Line);
            }
            else if (normalized == "column")
            {
                WriteColumnChart(writer, part.Series, XlsxChartKind.Column);
            }
            else if (normalized == "bar")
            {
                WriteBarChart(writer, part.Series, XlsxChartKind.Bar);
            }
            else if (normalized == "line")
            {
                WriteLineChart(writer, part.Series, XlsxChartKind.Line);
            }
            else if (normalized == "pie")
            {
                WritePieChart(writer, part.Series, XlsxChartKind.Pie);
            }
            else if (normalized == "area")
            {
                WriteAreaChart(writer, part.Series, XlsxChartKind.Area);
            }
            else if (normalized == "scatter")
            {
                WriteScatterChart(writer, part.Series, XlsxChartKind.Scatter);
            }
            else if (normalized == "doughnut")
            {
                WriteDoughnutChart(writer, part.Series, XlsxChartKind.Doughnut);
            }
            else if (normalized == "bubble")
            {
                WriteBubbleChart(writer, part.Series, XlsxChartKind.Bubble);
            }
            else if (normalized == "radar")
            {
                WriteRadarChart(writer, part.Series, XlsxChartKind.Radar);
            }
            else if (normalized == "stock")
            {
                WriteStockChart(writer, part.Series, XlsxChartKind.Stock);
            }
            else if (normalized == "surface")
            {
                WriteSurfaceChart(writer, part.Series, XlsxChartKind.Surface);
            }
        }

        WriteAdvancedChartElements(writer, part.AdvancedChartElements);
    }

    internal static void WriteTypedChartKind(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        if (chartKind == XlsxChartKind.Column) { WriteColumnChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Bar) { WriteBarChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Line) { WriteLineChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Area) { WriteAreaChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Pie) { WritePieChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Doughnut) { WriteDoughnutChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Scatter) { WriteScatterChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Bubble) { WriteBubbleChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Radar) { WriteRadarChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Stock) { WriteStockChart(writer, series, chartKind); }
        else if (chartKind == XlsxChartKind.Surface) { WriteSurfaceChart(writer, series, chartKind); }
    }

    internal static string GetChartKindElementName(XlsxChartKind chartKind)
    {
        if (chartKind == XlsxChartKind.Column || chartKind == XlsxChartKind.Bar) { return "barChart"; }
        if (chartKind == XlsxChartKind.Line) { return "lineChart"; }
        if (chartKind == XlsxChartKind.Area) { return "areaChart"; }
        if (chartKind == XlsxChartKind.Pie) { return "pieChart"; }
        if (chartKind == XlsxChartKind.Doughnut) { return "doughnutChart"; }
        if (chartKind == XlsxChartKind.Scatter) { return "scatterChart"; }
        if (chartKind == XlsxChartKind.Bubble) { return "bubbleChart"; }
        if (chartKind == XlsxChartKind.Radar) { return "radarChart"; }
        if (chartKind == XlsxChartKind.Stock) { return "stockChart"; }
        if (chartKind == XlsxChartKind.Surface) { return "surfaceChart"; }
        return "";
    }

    internal static void WriteColumnChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "barChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "barDir", "col");
        WriteValueElement(writer, "grouping", "clustered");
        WriteValueElement(writer, "varyColors", "0");
        WriteSeriesList(writer, series, false, false, chartKind);
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WriteBarChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "barChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "barDir", "bar");
        WriteValueElement(writer, "grouping", "clustered");
        WriteValueElement(writer, "varyColors", "0");
        WriteSeriesList(writer, series, false, false, chartKind);
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WriteLineChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "lineChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "grouping", "standard");
        WriteValueElement(writer, "varyColors", "0");
        WriteSeriesList(writer, series, false, false, chartKind);
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WritePieChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "pieChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "varyColors", "1");
        WriteSeriesList(writer, series, false, false, chartKind);
        writer.WriteEndElement();
    }

    internal static void WriteAreaChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "areaChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "grouping", "standard");
        WriteValueElement(writer, "varyColors", "0");
        WriteSeriesList(writer, series, false, false, chartKind);
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WriteScatterChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "scatterChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "scatterStyle", "lineMarker");
        WriteSeriesList(writer, series, true, false, chartKind);
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WriteDoughnutChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "doughnutChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "varyColors", "1");
        WriteSeriesList(writer, series, false, false, chartKind);
        WriteValueElement(writer, "firstSliceAng", "0");
        WriteValueElement(writer, "holeSize", "50");
        writer.WriteEndElement();
    }

    internal static void WriteBubbleChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "bubbleChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "varyColors", "0");
        WriteSeriesList(writer, series, true, true, chartKind);
        WriteValueElement(writer, "bubbleScale", "100");
        WriteValueElement(writer, "showNegBubbles", "0");
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WriteRadarChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "radarChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "radarStyle", "marker");
        WriteValueElement(writer, "varyColors", "0");
        WriteSeriesList(writer, series, false, false, chartKind);
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WriteStockChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "stockChart", XlsxNamespaces.Chart);
        WriteSeriesList(writer, series, false, false, chartKind);
        writer.WriteStartElement("c", "hiLowLines", XlsxNamespaces.Chart);
        writer.WriteEndElement();
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WriteSurfaceChart(XmlWriter writer, List<XlsxChartSeriesReference> series, XlsxChartKind chartKind)
    {
        writer.WriteStartElement("c", "surfaceChart", XlsxNamespaces.Chart);
        WriteValueElement(writer, "wireframe", "0");
        WriteSeriesList(writer, series, false, false, chartKind);
        writer.WriteStartElement("c", "bandFmts", XlsxNamespaces.Chart);
        writer.WriteEndElement();
        WriteAxisIds(writer);
        writer.WriteEndElement();
    }

    internal static void WriteLayout(XmlWriter writer, XlsxChartPart part)
    {
        writer.WriteStartElement("c", "layout", XlsxNamespaces.Chart);
        if (part.HasLayout)
        {
            XlsxChartLayout layout = part.Layout;
            writer.WriteStartElement("c", "manualLayout", XlsxNamespaces.Chart);
            WriteValueElement(writer, "layoutTarget", NormalizeAxisText(layout.Target, "inner"));
            WriteValueElement(writer, "xMode", NormalizeAxisText(layout.XMode, "edge"));
            WriteValueElement(writer, "yMode", NormalizeAxisText(layout.YMode, "edge"));
            WriteValueElement(writer, "wMode", NormalizeAxisText(layout.WidthMode, "factor"));
            WriteValueElement(writer, "hMode", NormalizeAxisText(layout.HeightMode, "factor"));
            WriteValueElement(writer, "x", NormalizeAxisText(layout.X, "0"));
            WriteValueElement(writer, "y", NormalizeAxisText(layout.Y, "0"));
            WriteValueElement(writer, "w", NormalizeAxisText(layout.Width, "1"));
            WriteValueElement(writer, "h", NormalizeAxisText(layout.Height, "1"));
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteDataTable(XmlWriter writer, XlsxChartDataTable table)
    {
        writer.WriteStartElement("c", "dTable", XlsxNamespaces.Chart);
        WriteValueElement(writer, "showHorzBorder", XlsxXmlValueWriter.WriteBoolean(table.ShowHorizontalBorder));
        WriteValueElement(writer, "showVertBorder", XlsxXmlValueWriter.WriteBoolean(table.ShowVerticalBorder));
        WriteValueElement(writer, "showOutline", XlsxXmlValueWriter.WriteBoolean(table.ShowOutline));
        WriteValueElement(writer, "showKeys", XlsxXmlValueWriter.WriteBoolean(table.ShowLegendKeys));
        writer.WriteEndElement();
    }

    internal static void WriteSeriesList(XmlWriter writer, List<XlsxChartSeriesReference> seriesList, bool scatter, bool bubble, XlsxChartKind chartKind)
    {
        int index = 0;
        foreach (XlsxChartSeriesReference series in seriesList)
        {
            if (!series.HasChartKind || series.ChartKind == chartKind)
            {
                WriteSeries(writer, series, index, scatter, bubble);
            }
            index = index + 1;
        }
    }

    internal static void WriteSeries(XmlWriter writer, XlsxChartSeriesReference series, int index, bool scatter, bool bubble)
    {
        writer.WriteStartElement("c", "ser", XlsxNamespaces.Chart);
        uint seriesIndex = (uint)index;
        uint seriesOrder = (uint)index;
        if (series.HasIndex)
        {
            seriesIndex = series.Index;
        }

        if (series.HasOrder)
        {
            seriesOrder = series.Order;
        }

        WriteValueElement(writer, "idx", seriesIndex.ToString(CultureInfo.InvariantCulture));
        WriteValueElement(writer, "order", seriesOrder.ToString(CultureInfo.InvariantCulture));
        if (series.TextFormula != null && series.TextFormula.Length > 0)
        {
            writer.WriteStartElement("c", "tx", XlsxNamespaces.Chart);
            WriteStringReference(writer, series.TextFormula);
            writer.WriteEndElement();
        }

        if (series.HasColor)
        {
            WriteSeriesColor(writer, series.ColorRgb);
        }

        if (scatter)
        {
            writer.WriteStartElement("c", "xVal", XlsxNamespaces.Chart);
            WriteNumberReference(writer, series.CategoryFormula);
            writer.WriteEndElement();
            writer.WriteStartElement("c", "yVal", XlsxNamespaces.Chart);
            WriteNumberReference(writer, series.ValuesFormula);
            writer.WriteEndElement();
            if (bubble)
            {
                writer.WriteStartElement("c", "bubbleSize", XlsxNamespaces.Chart);
                string bubbleFormula = series.BubbleSizeFormula;
                if (bubbleFormula.Length == 0) { bubbleFormula = series.ValuesFormula; }
                WriteNumberReference(writer, bubbleFormula);
                writer.WriteEndElement();
            }
        }
        else
        {
            if (series.CategoryFormula != null && series.CategoryFormula.Length > 0)
            {
                writer.WriteStartElement("c", "cat", XlsxNamespaces.Chart);
                WriteStringReference(writer, series.CategoryFormula);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("c", "val", XlsxNamespaces.Chart);
            WriteNumberReference(writer, series.ValuesFormula);
            writer.WriteEndElement();
        }

        if (series.HasDataLabels)
        {
            WriteSeriesDataLabels(writer, series);
        }

        if (series.HasTrendline)
        {
            WriteSeriesTrendline(writer, series);
        }

        if (series.HasErrorBars)
        {
            WriteSeriesErrorBars(writer, series);
        }

        WriteExtensionXml(writer, series.ExtensionXml);
        writer.WriteEndElement();
    }

    internal static void WriteSeriesColor(XmlWriter writer, string colorRgb)
    {
        writer.WriteStartElement("c", "spPr", XlsxNamespaces.Chart);
        writer.WriteStartElement("a", "solidFill", XlsxNamespaces.Drawing);
        writer.WriteStartElement("a", "srgbClr", XlsxNamespaces.Drawing);
        writer.WriteAttributeString("val", XlsxCellValueConverter.NormalizeText(colorRgb));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteSeriesDataLabels(XmlWriter writer, XlsxChartSeriesReference series)
    {
        writer.WriteStartElement("c", "dLbls", XlsxNamespaces.Chart);
        if (series.DataLabelPosition != null && series.DataLabelPosition.Length > 0)
        {
            WriteValueElement(writer, "dLblPos", series.DataLabelPosition);
        }
        WriteValueElement(writer, "showLegendKey", XlsxXmlValueWriter.WriteBoolean(series.ShowLabelLegendKey));
        WriteValueElement(writer, "showVal", XlsxXmlValueWriter.WriteBoolean(series.ShowLabelValue));
        WriteValueElement(writer, "showCatName", XlsxXmlValueWriter.WriteBoolean(series.ShowLabelCategoryName));
        WriteValueElement(writer, "showSerName", XlsxXmlValueWriter.WriteBoolean(series.ShowLabelSeriesName));

        writer.WriteEndElement();
    }

    internal static void WriteSeriesTrendline(XmlWriter writer, XlsxChartSeriesReference series)
    {
        writer.WriteStartElement("c", "trendline", XlsxNamespaces.Chart);
        string trendlineType = series.TrendlineType;
        if (trendlineType == null || trendlineType.Length == 0)
        {
            trendlineType = "linear";
        }

        WriteValueElement(writer, "trendlineType", trendlineType);
        if (series.TrendlineName.Length > 0) { WriteValueElement(writer, "name", series.TrendlineName); }
        if (trendlineType == "poly") { WriteValueElement(writer, "order", series.TrendlineOrder.ToString(CultureInfo.InvariantCulture)); }
        if (trendlineType == "movingAvg") { WriteValueElement(writer, "period", series.TrendlinePeriod.ToString(CultureInfo.InvariantCulture)); }
        WriteValueElement(writer, "dispRSqr", XlsxXmlValueWriter.WriteBoolean(series.DisplayTrendlineRSquared));
        WriteValueElement(writer, "dispEq", XlsxXmlValueWriter.WriteBoolean(series.DisplayTrendlineEquation));
        writer.WriteEndElement();
    }

    internal static void WriteSeriesErrorBars(XmlWriter writer, XlsxChartSeriesReference series)
    {
        writer.WriteStartElement("c", "errBars", XlsxNamespaces.Chart);
        string errorBarType = series.ErrorBarType;
        if (errorBarType == null || errorBarType.Length == 0)
        {
            errorBarType = "both";
        }

        string errorBarValue = series.ErrorBarValue;
        if (errorBarValue == null || errorBarValue.Length == 0)
        {
            errorBarValue = "1";
        }

        WriteValueElement(writer, "errBarType", errorBarType);
        WriteValueElement(writer, "errDir", NormalizeAxisText(series.ErrorBarDirection, "y"));
        WriteValueElement(writer, "errValType", NormalizeAxisText(series.ErrorBarValueType, "fixedVal"));
        WriteValueElement(writer, "noEndCap", XlsxXmlValueWriter.WriteBoolean(series.ErrorBarNoEndCap));
        WriteValueElement(writer, "val", errorBarValue);
        writer.WriteEndElement();
    }

    internal static void WriteTitle(XmlWriter writer, string titleText)
    {
        writer.WriteStartElement("c", "title", XlsxNamespaces.Chart);
        writer.WriteStartElement("c", "tx", XlsxNamespaces.Chart);
        writer.WriteStartElement("c", "rich", XlsxNamespaces.Chart);
        writer.WriteStartElement("a", "bodyPr", XlsxNamespaces.Drawing);
        writer.WriteEndElement();
        writer.WriteStartElement("a", "p", XlsxNamespaces.Drawing);
        writer.WriteStartElement("a", "r", XlsxNamespaces.Drawing);
        writer.WriteStartElement("a", "t", XlsxNamespaces.Drawing);
        writer.WriteString(XlsxCellValueConverter.NormalizeText(titleText));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("c", "layout", XlsxNamespaces.Chart);
        writer.WriteEndElement();
        WriteValueElement(writer, "overlay", "0");
        writer.WriteEndElement();
    }

    internal static void WriteStringReference(XmlWriter writer, string formula)
    {
        writer.WriteStartElement("c", "strRef", XlsxNamespaces.Chart);
        WriteFormula(writer, formula);
        writer.WriteEndElement();
    }

    internal static void WriteNumberReference(XmlWriter writer, string formula)
    {
        writer.WriteStartElement("c", "numRef", XlsxNamespaces.Chart);
        WriteFormula(writer, formula);
        writer.WriteEndElement();
    }

    internal static void WriteFormula(XmlWriter writer, string formula)
    {
        writer.WriteStartElement("c", "f", XlsxNamespaces.Chart);
        writer.WriteString(XlsxCellValueConverter.NormalizeText(formula));
        writer.WriteEndElement();
    }

    internal static void WriteAxisIds(XmlWriter writer)
    {
        WriteValueElement(writer, "axId", "48650112");
        WriteValueElement(writer, "axId", "48672768");
    }

    internal static void WriteAxes(XmlWriter writer, List<XlsxChartAxis> axes)
    {
        foreach (XlsxChartAxis axis in axes)
        {
            WriteAxis(writer, axis);
        }
    }

    internal static void WriteAxis(XmlWriter writer, XlsxChartAxis axis)
    {
        string elementName = "catAx";
        if (axis.AxisType == "val")
        {
            elementName = "valAx";
        }

        writer.WriteStartElement("c", elementName, XlsxNamespaces.Chart);
        WriteValueElement(writer, "axId", NormalizeAxisText(axis.AxisId, axis.AxisType == "val" ? "48672768" : "48650112"));
        writer.WriteStartElement("c", "scaling", XlsxNamespaces.Chart);
        WriteValueElement(writer, "orientation", "minMax");
        if (axis.Maximum.Length > 0) { WriteValueElement(writer, "max", axis.Maximum); }
        if (axis.Minimum.Length > 0) { WriteValueElement(writer, "min", axis.Minimum); }
        writer.WriteEndElement();
        WriteValueElement(writer, "axPos", NormalizeAxisText(axis.Position, axis.AxisType == "val" ? "l" : "b"));
        if (axis.HasTitle)
        {
            WriteTitle(writer, axis.TitleText);
        }

        if (axis.HasMajorGridlines) { WriteGridlines(writer, "majorGridlines", axis.MajorGridlineColorRgb); }
        if (axis.HasMinorGridlines) { WriteGridlines(writer, "minorGridlines", axis.MinorGridlineColorRgb); }

        if (axis.AxisType == "val")
        {
            if (!axis.HasMajorGridlines)
            {
                writer.WriteStartElement("c", "majorGridlines", XlsxNamespaces.Chart);
                writer.WriteEndElement();
            }
            if (axis.NumberFormat != null && axis.NumberFormat.Length > 0)
            {
                WriteNumberFormat(writer, axis.NumberFormat);
            }
            else
            {
                WriteNumberFormat(writer);
            }
        }

        WriteValueElement(writer, "tickLblPos", "nextTo");
        WriteValueElement(writer, "crossAx", NormalizeAxisText(axis.CrossAxisId, axis.AxisType == "val" ? "48650112" : "48672768"));
        WriteValueElement(writer, "crosses", "autoZero");
        if (axis.AxisType == "cat")
        {
            WriteValueElement(writer, "auto", "1");
            WriteValueElement(writer, "lblAlgn", "ctr");
            WriteValueElement(writer, "lblOffset", "100");
        }
        else
        {
            WriteValueElement(writer, "crossBetween", "between");
        }

        writer.WriteEndElement();
    }

    internal static void WriteCategoryValueAxes(XmlWriter writer)
    {
        writer.WriteStartElement("c", "catAx", XlsxNamespaces.Chart);
        WriteValueElement(writer, "axId", "48650112");
        writer.WriteStartElement("c", "scaling", XlsxNamespaces.Chart);
        WriteValueElement(writer, "orientation", "minMax");
        writer.WriteEndElement();
        WriteValueElement(writer, "axPos", "b");
        WriteValueElement(writer, "tickLblPos", "nextTo");
        WriteValueElement(writer, "crossAx", "48672768");
        WriteValueElement(writer, "crosses", "autoZero");
        WriteValueElement(writer, "auto", "1");
        WriteValueElement(writer, "lblAlgn", "ctr");
        WriteValueElement(writer, "lblOffset", "100");
        writer.WriteEndElement();
        writer.WriteStartElement("c", "valAx", XlsxNamespaces.Chart);
        WriteValueElement(writer, "axId", "48672768");
        writer.WriteStartElement("c", "scaling", XlsxNamespaces.Chart);
        WriteValueElement(writer, "orientation", "minMax");
        writer.WriteEndElement();
        WriteValueElement(writer, "axPos", "l");
        writer.WriteStartElement("c", "majorGridlines", XlsxNamespaces.Chart);
        writer.WriteEndElement();
        WriteNumberFormat(writer);
        WriteValueElement(writer, "tickLblPos", "nextTo");
        WriteValueElement(writer, "crossAx", "48650112");
        WriteValueElement(writer, "crosses", "autoZero");
        WriteValueElement(writer, "crossBetween", "between");
        writer.WriteEndElement();
    }

    internal static void WriteGridlines(XmlWriter writer, string elementName, string colorRgb)
    {
        writer.WriteStartElement("c", elementName, XlsxNamespaces.Chart);
        if (colorRgb.Length > 0)
        {
            writer.WriteStartElement("c", "spPr", XlsxNamespaces.Chart);
            writer.WriteStartElement("a", "ln", XlsxNamespaces.Drawing);
            writer.WriteStartElement("a", "solidFill", XlsxNamespaces.Drawing);
            writer.WriteStartElement("a", "srgbClr", XlsxNamespaces.Drawing);
            writer.WriteAttributeString("val", colorRgb);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    internal static void WriteScatterAxes(XmlWriter writer)
    {
        WriteScatterAxis(writer, "48650112", "b", "48672768");
        WriteScatterAxis(writer, "48672768", "l", "48650112");
    }

    internal static void WriteScatterAxis(XmlWriter writer, string axisId, string axisPosition, string crossAxisId)
    {
        writer.WriteStartElement("c", "valAx", XlsxNamespaces.Chart);
        WriteValueElement(writer, "axId", axisId);
        writer.WriteStartElement("c", "scaling", XlsxNamespaces.Chart);
        WriteValueElement(writer, "orientation", "minMax");
        writer.WriteEndElement();
        WriteValueElement(writer, "axPos", axisPosition);
        WriteValueElement(writer, "tickLblPos", "nextTo");
        WriteValueElement(writer, "crossAx", crossAxisId);
        WriteValueElement(writer, "crosses", "autoZero");
        writer.WriteEndElement();
    }

    internal static void WriteValueElement(XmlWriter writer, string elementName, string value)
    {
        writer.WriteStartElement("c", elementName, XlsxNamespaces.Chart);
        writer.WriteAttributeString("val", value);
        writer.WriteEndElement();
    }

    internal static void WriteNumberFormat(XmlWriter writer)
    {
        WriteNumberFormat(writer, "General");
    }

    internal static void WriteNumberFormat(XmlWriter writer, string formatCode)
    {
        writer.WriteStartElement("c", "numFmt", XlsxNamespaces.Chart);
        writer.WriteAttributeString("formatCode", XlsxCellValueConverter.NormalizeText(formatCode));
        writer.WriteAttributeString("sourceLinked", "1");
        writer.WriteEndElement();
    }

    internal static void WriteLegend(XmlWriter writer, XlsxChartLegend legend)
    {
        writer.WriteStartElement("c", "legend", XlsxNamespaces.Chart);
        WriteValueElement(writer, "legendPos", NormalizeAxisText(legend.Position, "r"));
        writer.WriteStartElement("c", "layout", XlsxNamespaces.Chart);
        writer.WriteEndElement();
        string overlay = "0";
        if (legend.HasOverlay && legend.Overlay)
        {
            overlay = "1";
        }

        WriteValueElement(writer, "overlay", overlay);
        writer.WriteEndElement();
    }

    internal static string NormalizeAxisText(string value, string defaultValue)
    {
        if (value == null || value.Length == 0)
        {
            return defaultValue;
        }

        return value;
    }

    internal static bool NeedsCategoryValueAxes(XlsxChartPart part)
    {
        foreach (string chartType in part.ChartTypes)
        {
            string normalized = NormalizeChartType(chartType);
            if (normalized == "column" || normalized == "bar" || normalized == "line" || normalized == "area" || normalized == "radar" || normalized == "stock" || normalized == "surface" || normalized == "combo")
            {
                return true;
            }
        }

        return false;
    }

    internal static bool NeedsScatterAxes(XlsxChartPart part)
    {
        if (NeedsCategoryValueAxes(part))
        {
            return false;
        }

        foreach (string chartType in part.ChartTypes)
        {
            string normalized = NormalizeChartType(chartType);
            if (normalized == "scatter" || normalized == "bubble")
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsSupportedChartType(string chartType)
    {
        return NormalizeChartType(chartType).Length > 0;
    }

    internal static bool HasAdvancedChartElement(XlsxChartPart part, string chartType)
    {
        if (part.AdvancedChartElements == null)
        {
            return false;
        }

        foreach (XlsxChartRawElement element in part.AdvancedChartElements)
        {
            if (element != null && element.LocalName == chartType && element.RawXml != null && element.RawXml.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasAnyAdvancedChartElement(XlsxChartPart part)
    {
        if (part.AdvancedChartElements == null)
        {
            return false;
        }

        return part.AdvancedChartElements.Count > 0;
    }

    internal static string NormalizeChartType(string chartType)
    {
        if (chartType == null)
        {
            return "";
        }

        if (chartType == "column" || chartType == "col" || chartType == "barChart")
        {
            return "column";
        }

        if (chartType == "bar")
        {
            return "bar";
        }

        if (chartType == "line" || chartType == "lineChart")
        {
            return "line";
        }

        if (chartType == "pie" || chartType == "pieChart")
        {
            return "pie";
        }

        if (chartType == "area" || chartType == "areaChart")
        {
            return "area";
        }

        if (chartType == "scatter" || chartType == "scatterChart")
        {
            return "scatter";
        }

        if (chartType == "doughnut" || chartType == "doughnutChart")
        {
            return "doughnut";
        }

        if (chartType == "bubble" || chartType == "bubbleChart")
        {
            return "bubble";
        }

        if (chartType == "radar" || chartType == "radarChart")
        {
            return "radar";
        }

        if (chartType == "stock" || chartType == "stockChart")
        {
            return "stock";
        }

        if (chartType == "surface" || chartType == "surfaceChart" || chartType == "surface3DChart")
        {
            return "surface";
        }

        if (chartType == "combo")
        {
            return "combo";
        }

        return "";
    }

    internal static void WriteUnknownXml(XmlWriter writer, List<string> unknownXml)
    {
        foreach (string item in unknownXml)
        {
            if (item != null && item.Length > 0)
            {
                writer.WriteRaw(item);
            }
        }
    }

    internal static void WriteExtensionXml(XmlWriter writer, List<string> extensionXml)
    {
        foreach (string item in extensionXml)
        {
            if (item != null && item.Length > 0)
            {
                writer.WriteRaw(item);
            }
        }
    }

    internal static void WriteChartExtensions(XmlWriter writer, XlsxChartPart part)
    {
        if (part.NewerChartElements != null && part.NewerChartElements.Count > 0)
        {
            foreach (XlsxChartRawElement element in part.NewerChartElements)
            {
                writer.WriteRaw(element.RawXml);
            }
            foreach (string item in part.ExtensionXml)
            {
                bool alreadyWritten = false;
                foreach (XlsxChartRawElement element in part.NewerChartElements)
                {
                    if (element.RawXml == item) { alreadyWritten = true; }
                }
                if (!alreadyWritten && item.Length > 0) { writer.WriteRaw(item); }
            }
            return;
        }
        WriteExtensionXml(writer, part.ExtensionXml);
    }

    internal static void WriteAdvancedChartElements(XmlWriter writer, List<XlsxChartRawElement> elements)
    {
        foreach (XlsxChartRawElement element in elements)
        {
            writer.WriteRaw(element.RawXml);
        }
    }
}
