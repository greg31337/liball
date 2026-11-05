using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads chart part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxChartPartReader : IXlsxChartPartReader
{
    /// <summary>
    /// Initializes a new chart part reader with its default state.
    /// </summary>
    public XlsxChartPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The chart part produced by the operation.</returns>
    public XlsxChartPart ReadString(string xml)
    {
        return ReadStringFromPart("/xl/charts/chart1.xml", xml);
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The chart part produced by the operation.</returns>
    public XlsxChartPart ReadStringFromPart(string partPath, string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxChartPart part = ReadBytesFromPart(partPath, bytes);
        part.RawXml = xml;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The chart part produced by the operation.</returns>
    public XlsxChartPart ReadBytes(byte[] bytes)
    {
        return ReadBytesFromPart("/xl/charts/chart1.xml", bytes);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The chart part produced by the operation.</returns>
    public XlsxChartPart ReadBytesFromPart(string partPath, byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult(partPath);
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            XlsxChartPart part = ReadStreamFromPart(partPath, stream);
            return part;
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The chart part produced by the operation.</returns>
    public XlsxChartPart ReadStream(Stream stream)
    {
        return ReadStreamFromPart("/xl/charts/chart1.xml", stream);
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The chart part produced by the operation.</returns>
    public XlsxChartPart ReadStreamFromPart(string partPath, Stream stream)
    {
        XlsxChartPart part = new XlsxChartPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        bool sawRoot = false;
        using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                if (!sawRoot)
                {
                    sawRoot = true;
                    ReadRootElement(part, reader);
                    reader.Read();
                    continue;
                }

                if (reader.NamespaceURI == XlsxNamespaces.Chart)
                {
                    ReadChartElement(part, reader);
                    continue;
                }

                reader.Read();
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chart.missingRoot", "Chart XML must contain a chartSpace root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxChartPart CreateMissingRootResult(string partPath)
    {
        XlsxChartPart part = new XlsxChartPart();
        part.PartPath = XlsxCellValueConverter.NormalizeText(partPath);
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chart.missingRoot", "Chart XML must contain a chartSpace root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxChartPart part, XmlReader reader)
    {
        if (reader.LocalName != "chartSpace" || reader.NamespaceURI != XlsxNamespaces.Chart)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "chart.invalidRoot", "Chart XML root must be a chartSpace element in the chart namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        XlsxCorePropertiesReader.ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadChartElement(XlsxChartPart part, XmlReader reader)
    {
        string localName = reader.LocalName;
        if (localName == "chart")
        {
            part.HasChart = true;
            reader.Read();
            return;
        }

        if (localName == "style")
        {
            SetChartStyle(part, reader.GetAttribute("val"));
            reader.ReadOuterXml();
            return;
        }

        if (localName == "title" && !part.HasTitle)
        {
            string rawXml = reader.ReadOuterXml();
            part.TitleText = ExtractText(rawXml);
            part.HasTitle = part.TitleText.Length > 0;
            return;
        }

        if (localName == "legend")
        {
            string rawXml = reader.ReadOuterXml();
            part.Legend = ReadLegend(rawXml);
            part.HasLegend = true;
            return;
        }

        if (localName == "layout")
        {
            string rawXml = reader.ReadOuterXml();
            ReadLayout(part, rawXml);
            return;
        }

        if (localName == "dTable")
        {
            string rawXml = reader.ReadOuterXml();
            ReadDataTable(part, rawXml);
            return;
        }

        if (localName == "catAx" || localName == "valAx")
        {
            string rawXml = reader.ReadOuterXml();
            XlsxChartAxis axis = ReadAxis(rawXml, localName);
            part.Axes.Add(axis);
            return;
        }

        if (localName == "extLst")
        {
            string rawXml = reader.ReadOuterXml();
            part.ExtensionXml.Add(rawXml);
            XlsxChartRawElement element = new XlsxChartRawElement();
            element.Kind = "newerChartExtension";
            element.LocalName = "extLst";
            element.NamespaceUri = XlsxNamespaces.Chart;
            element.RawXml = rawXml;
            part.NewerChartElements.Add(element);
            return;
        }

        if (IsChartTypeElement(localName))
        {
            string rawXml = reader.ReadOuterXml();
            AddChartType(part, localName);
            XlsxChartKind chartKind = ReadChartKind(localName, rawXml);
            AddTypedChartKind(part, chartKind);
            ReadChartTypeSeries(part, rawXml, chartKind);
            if (!IsBasicWritableChartTypeElement(localName))
            {
                AddRawChartElement(part, "advancedChart", localName, rawXml);
            }
            return;
        }

        if (localName == "ser")
        {
            string rawXml = reader.ReadOuterXml();
            XlsxChartSeriesReference series = ReadSeries(rawXml);
            part.Series.Add(series);
            return;
        }

        reader.Read();
    }

    internal static void AddRawChartElement(XlsxChartPart part, string kind, string localName, string rawXml)
    {
        XlsxChartRawElement element = new XlsxChartRawElement();
        element.Kind = kind;
        element.LocalName = localName;
        element.NamespaceUri = XlsxNamespaces.Chart;
        element.RawXml = rawXml;
        part.AdvancedChartElements.Add(element);
    }

    internal static XlsxChartKind ReadChartKind(string localName, string rawXml)
    {
        if (localName == "barChart")
        {
            using (StringReader stringReader = new StringReader(rawXml))
            {
                using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
                {
                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart && reader.LocalName == "barDir" && XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val")) == "bar")
                        {
                            return XlsxChartKind.Bar;
                        }
                        reader.Read();
                    }
                }
            }
            return XlsxChartKind.Column;
        }
        if (localName == "lineChart" || localName == "line3DChart") { return XlsxChartKind.Line; }
        if (localName == "areaChart" || localName == "area3DChart") { return XlsxChartKind.Area; }
        if (localName == "pieChart" || localName == "pie3DChart" || localName == "ofPieChart") { return XlsxChartKind.Pie; }
        if (localName == "doughnutChart") { return XlsxChartKind.Doughnut; }
        if (localName == "scatterChart") { return XlsxChartKind.Scatter; }
        if (localName == "bubbleChart") { return XlsxChartKind.Bubble; }
        if (localName == "radarChart") { return XlsxChartKind.Radar; }
        if (localName == "stockChart") { return XlsxChartKind.Stock; }
        if (localName == "surfaceChart" || localName == "surface3DChart") { return XlsxChartKind.Surface; }
        return XlsxChartKind.Column;
    }

    internal static void AddTypedChartKind(XlsxChartPart part, XlsxChartKind chartKind)
    {
        foreach (XlsxChartKind existing in part.ChartKinds)
        {
            if (existing == chartKind) { return; }
        }
        part.ChartKinds.Add(chartKind);
    }

    internal static void ReadChartTypeSeries(XlsxChartPart part, string rawXml, XlsxChartKind chartKind)
    {
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart && reader.LocalName == "ser")
                    {
                        XlsxChartSeriesReference series = ReadSeries(reader.ReadOuterXml());
                        series.ChartKind = chartKind;
                        series.HasChartKind = true;
                        part.Series.Add(series);
                        continue;
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static void ReadLayout(XlsxChartPart part, string rawXml)
    {
        if (rawXml.IndexOf("manualLayout", System.StringComparison.Ordinal) < 0) { return; }
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart)
                    {
                        string value = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        if (reader.LocalName == "layoutTarget") { part.Layout.Target = value; }
                        else if (reader.LocalName == "xMode") { part.Layout.XMode = value; }
                        else if (reader.LocalName == "yMode") { part.Layout.YMode = value; }
                        else if (reader.LocalName == "wMode") { part.Layout.WidthMode = value; }
                        else if (reader.LocalName == "hMode") { part.Layout.HeightMode = value; }
                        else if (reader.LocalName == "x") { part.Layout.X = value; }
                        else if (reader.LocalName == "y") { part.Layout.Y = value; }
                        else if (reader.LocalName == "w") { part.Layout.Width = value; }
                        else if (reader.LocalName == "h") { part.Layout.Height = value; }
                    }
                    reader.Read();
                }
            }
        }
        part.HasLayout = true;
    }

    internal static void ReadDataTable(XlsxChartPart part, string rawXml)
    {
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart)
                    {
                        bool value = XlsxXmlValueParser.ParseBoolean(reader.GetAttribute("val")).BooleanValue;
                        if (reader.LocalName == "showHorzBorder") { part.DataTable.ShowHorizontalBorder = value; }
                        else if (reader.LocalName == "showVertBorder") { part.DataTable.ShowVerticalBorder = value; }
                        else if (reader.LocalName == "showOutline") { part.DataTable.ShowOutline = value; }
                        else if (reader.LocalName == "showKeys") { part.DataTable.ShowLegendKeys = value; }
                    }
                    reader.Read();
                }
            }
        }
        part.HasDataTable = true;
    }

    internal static string ExtractText(string rawXml)
    {
        StringBuilder builder = new StringBuilder();
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && ((reader.NamespaceURI == XlsxNamespaces.Drawing && reader.LocalName == "t") || (reader.NamespaceURI == XlsxNamespaces.Chart && reader.LocalName == "v")))
                    {
                        builder.Append(reader.ReadElementContentAsString());
                        continue;
                    }

                    reader.Read();
                }
            }
        }

        return builder.ToString();
    }

    internal static void SetChartStyle(XlsxChartPart part, string? text)
    {
        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            return;
        }

        part.StyleId = result.UnsignedIntValue;
        part.HasStyle = true;
    }

    internal static XlsxChartLegend ReadLegend(string rawXml)
    {
        XlsxChartLegend legend = new XlsxChartLegend();
        legend.RawXml = rawXml;
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart)
                    {
                        if (reader.LocalName == "legendPos")
                        {
                            legend.Position = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "overlay")
                        {
                            legend.Overlay = XlsxXmlValueParser.ParseBoolean(reader.GetAttribute("val")).BooleanValue;
                            legend.HasOverlay = true;
                        }
                    }

                    reader.Read();
                }
            }
        }

        return legend;
    }

    internal static XlsxChartAxis ReadAxis(string rawXml, string elementName)
    {
        XlsxChartAxis axis = new XlsxChartAxis();
        axis.RawXml = rawXml;
        if (elementName == "valAx")
        {
            axis.AxisType = "val";
        }
        else
        {
            axis.AxisType = "cat";
        }

        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart)
                    {
                        if (reader.LocalName == "axId")
                        {
                            axis.AxisId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "crossAx")
                        {
                            axis.CrossAxisId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "axPos")
                        {
                            axis.Position = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "numFmt")
                        {
                            axis.NumberFormat = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("formatCode"));
                        }
                        else if (reader.LocalName == "title")
                        {
                            string titleXml = reader.ReadOuterXml();
                            axis.TitleText = ExtractText(titleXml);
                            axis.HasTitle = axis.TitleText.Length > 0;
                            continue;
                        }
                        else if (reader.LocalName == "majorGridlines")
                        {
                            axis.HasMajorGridlines = true;
                            axis.MajorGridlineColorRgb = ReadLineColor(reader.ReadOuterXml());
                            continue;
                        }
                        else if (reader.LocalName == "minorGridlines")
                        {
                            axis.HasMinorGridlines = true;
                            axis.MinorGridlineColorRgb = ReadLineColor(reader.ReadOuterXml());
                            continue;
                        }
                        else if (reader.LocalName == "min")
                        {
                            axis.Minimum = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "max")
                        {
                            axis.Maximum = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                    }

                    reader.Read();
                }
            }
        }

        return axis;
    }

    internal static string ReadLineColor(string rawXml)
    {
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "srgbClr")
                    {
                        return XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                    }
                    reader.Read();
                }
            }
        }
        return "";
    }

    internal static XlsxChartSeriesReference ReadSeries(string rawXml)
    {
        XlsxChartSeriesReference series = new XlsxChartSeriesReference();
        series.RawXml = rawXml;
        string formulaContext = "";
        int formulaIndex = 0;
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart)
                    {
                        string localName = reader.LocalName;
                        if (localName == "idx")
                        {
                            SetSeriesUInt(series, reader.GetAttribute("val"), "idx");
                            reader.ReadOuterXml();
                            continue;
                        }

                        if (localName == "order")
                        {
                            SetSeriesUInt(series, reader.GetAttribute("val"), "order");
                            reader.ReadOuterXml();
                            continue;
                        }

                        if (localName == "spPr")
                        {
                            string shapeXml = reader.ReadOuterXml();
                            ReadSeriesShapeProperties(series, shapeXml);
                            continue;
                        }

                        if (localName == "dLbls")
                        {
                            string labelsXml = reader.ReadOuterXml();
                            ReadSeriesDataLabels(series, labelsXml);
                            continue;
                        }

                        if (localName == "trendline")
                        {
                            string trendlineXml = reader.ReadOuterXml();
                            ReadSeriesTrendline(series, trendlineXml);
                            continue;
                        }

                        if (localName == "errBars")
                        {
                            string errorBarsXml = reader.ReadOuterXml();
                            ReadSeriesErrorBars(series, errorBarsXml);
                            continue;
                        }

                        if (localName == "extLst")
                        {
                            series.ExtensionXml.Add(reader.ReadOuterXml());
                            continue;
                        }

                        if (localName == "tx" || localName == "cat" || localName == "val" || localName == "xVal" || localName == "yVal" || localName == "bubbleSize")
                        {
                            formulaContext = localName;
                            reader.Read();
                            continue;
                        }

                        if (localName == "f")
                        {
                            string formula = XlsxCellValueConverter.NormalizeText(reader.ReadElementContentAsString());
                            SetSeriesFormula(series, formulaContext, formula, formulaIndex);
                            formulaIndex = formulaIndex + 1;
                            continue;
                        }
                    }

                    reader.Read();
                }
            }
        }

        return series;
    }

    internal static void ReadSeriesShapeProperties(XlsxChartSeriesReference series, string rawXml)
    {
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Drawing && reader.LocalName == "srgbClr")
                    {
                        series.ColorRgb = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        series.HasColor = series.ColorRgb.Length > 0;
                    }

                    reader.Read();
                }
            }
        }
    }

    internal static void ReadSeriesDataLabels(XlsxChartSeriesReference series, string rawXml)
    {
        series.HasDataLabels = true;
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart && reader.LocalName == "dLblPos")
                    {
                        series.DataLabelPosition = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart)
                    {
                        bool value = XlsxXmlValueParser.ParseBoolean(reader.GetAttribute("val")).BooleanValue;
                        if (reader.LocalName == "showLegendKey") { series.ShowLabelLegendKey = value; }
                        else if (reader.LocalName == "showVal") { series.ShowLabelValue = value; }
                        else if (reader.LocalName == "showCatName") { series.ShowLabelCategoryName = value; }
                        else if (reader.LocalName == "showSerName") { series.ShowLabelSeriesName = value; }
                    }

                    reader.Read();
                }
            }
        }
    }

    internal static void ReadSeriesTrendline(XlsxChartSeriesReference series, string rawXml)
    {
        series.HasTrendline = true;
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart && reader.LocalName == "trendlineType")
                    {
                        series.TrendlineType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                    }
                    else if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart)
                    {
                        string value = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        if (reader.LocalName == "name") { series.TrendlineName = value; }
                        else if (reader.LocalName == "order") { series.TrendlineOrder = ParseInt(value, 2); }
                        else if (reader.LocalName == "period") { series.TrendlinePeriod = ParseInt(value, 2); }
                        else if (reader.LocalName == "dispEq") { series.DisplayTrendlineEquation = XlsxXmlValueParser.ParseBoolean(value).BooleanValue; }
                        else if (reader.LocalName == "dispRSqr") { series.DisplayTrendlineRSquared = XlsxXmlValueParser.ParseBoolean(value).BooleanValue; }
                    }

                    reader.Read();
                }
            }
        }
    }

    internal static void ReadSeriesErrorBars(XlsxChartSeriesReference series, string rawXml)
    {
        series.HasErrorBars = true;
        using (StringReader stringReader = new StringReader(rawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == XlsxNamespaces.Chart)
                    {
                        if (reader.LocalName == "errBarType")
                        {
                            series.ErrorBarType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "val")
                        {
                            series.ErrorBarValue = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "errDir")
                        {
                            series.ErrorBarDirection = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "errValType")
                        {
                            series.ErrorBarValueType = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("val"));
                        }
                        else if (reader.LocalName == "noEndCap")
                        {
                            series.ErrorBarNoEndCap = XlsxXmlValueParser.ParseBoolean(reader.GetAttribute("val")).BooleanValue;
                        }
                    }

                    reader.Read();
                }
            }
        }
    }

    internal static void SetSeriesUInt(XlsxChartSeriesReference series, string? text, string target)
    {
        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            return;
        }

        if (target == "idx")
        {
            series.Index = result.UnsignedIntValue;
            series.HasIndex = true;
        }
        else if (target == "order")
        {
            series.Order = result.UnsignedIntValue;
            series.HasOrder = true;
        }
    }

    internal static void SetSeriesFormula(XlsxChartSeriesReference series, string context, string formula, int formulaIndex)
    {
        if (context == "tx")
        {
            series.TextFormula = formula;
            return;
        }

        if (context == "cat")
        {
            series.CategoryFormula = formula;
            return;
        }

        if (context == "xVal")
        {
            series.CategoryFormula = formula;
            return;
        }

        if (context == "val")
        {
            series.ValuesFormula = formula;
            return;
        }

        if (context == "yVal")
        {
            series.ValuesFormula = formula;
            return;
        }

        if (context == "bubbleSize")
        {
            series.BubbleSizeFormula = formula;
            return;
        }

        if (formulaIndex == 0)
        {
            series.TextFormula = formula;
        }
        else if (formulaIndex == 1)
        {
            series.CategoryFormula = formula;
        }
        else if (formulaIndex == 2)
        {
            series.ValuesFormula = formula;
        }
    }

    internal static void AddChartType(XlsxChartPart part, string localName)
    {
        foreach (string chartType in part.ChartTypes)
        {
            if (chartType == localName)
            {
                return;
            }
        }

        part.ChartTypes.Add(localName);
    }

    internal static int ParseInt(string text, int defaultValue)
    {
        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseInteger(text);
        if (result.IsValid) { return result.IntValue; }
        return defaultValue;
    }

    internal static bool IsChartTypeElement(string localName)
    {
        return localName == "areaChart" || localName == "area3DChart" || localName == "lineChart" || localName == "line3DChart" || localName == "stockChart" || localName == "radarChart" || localName == "scatterChart" || localName == "pieChart" || localName == "pie3DChart" || localName == "doughnutChart" || localName == "barChart" || localName == "bar3DChart" || localName == "ofPieChart" || localName == "surfaceChart" || localName == "surface3DChart" || localName == "bubbleChart";
    }

    internal static bool IsBasicWritableChartTypeElement(string localName)
    {
        return localName == "areaChart" || localName == "lineChart" || localName == "scatterChart" || localName == "pieChart" || localName == "barChart";
    }
}
