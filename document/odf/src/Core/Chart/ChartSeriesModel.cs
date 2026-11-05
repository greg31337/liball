using System.Collections.Generic;
using System.Text;

namespace odf;

internal static class ChartSeriesModel
{
    internal static void LoadSeriesAndMetadata(
        OdfDocument document,
        OdfChart chart)
    {
        chart.StyleMetadata = LoadChartStyleMetadata(
            document,
            chart.StyleName,
            chart.PartPath);
        OdfChartPlotArea? plot = chart.PlotArea;
        if (plot == null || plot.Node == null)
        {
            return;
        }
        plot.StyleMetadata = LoadChartStyleMetadata(
            document,
            plot.StyleName,
            chart.PartPath);
        plot.ThreeDimensional =
            ThreeDimensionalDrawingModel.CreateSceneMetadata(plot.Node);
        List<OdfChartSeries> series = new List<OdfChartSeries>();
        int index = 0;
        while (index < plot.Node.Children.Length)
        {
            OdfNode child = plot.Node.Children[index];
            if (IsChartElement(child, "series"))
            {
                series.Add(LoadSeries(
                    document,
                    child,
                    chart.PartPath,
                    chart));
            }
            index = index + 1;
        }
        plot.Series = series.ToArray();
        ChartAdvancedModel.LoadStockComponents(document, chart, plot);
    }

    internal static void SynchronizeSeriesStructure(OdfChart chart)
    {
        OdfChartPlotArea? plot = chart.PlotArea;
        if (plot == null)
        {
            return;
        }
        int index = 0;
        while (index < plot.Series.Length)
        {
            WriteSeries(plot.Series[index]);
            index = index + 1;
        }
        ThreeDimensionalDrawingModel.ApplySceneMetadata(
            plot.ThreeDimensional);
        ChartAdvancedModel.SynchronizeStockComponents(plot);
    }

    internal static void SynchronizeAllChartStyles(OdfDocument document)
    {
        int chartIndex = 0;
        while (chartIndex < document.Charts.Length)
        {
            OdfChart chart = document.Charts[chartIndex];
            WriteStyleMetadata(chart.StyleMetadata);
            OdfChartPlotArea? plot = chart.PlotArea;
            if (plot != null)
            {
                WriteStyleMetadata(plot.StyleMetadata);
                int seriesIndex = 0;
                while (seriesIndex < plot.Series.Length)
                {
                    OdfChartSeries series = plot.Series[seriesIndex];
                    WriteStyleMetadata(series.StyleMetadata);
                    WriteLabelStyle(series.Label);
                    int errorIndex = 0;
                    while (errorIndex < series.ErrorIndicators.Length)
                    {
                        WriteStyleMetadata(
                            series.ErrorIndicators[errorIndex].StyleMetadata);
                        errorIndex = errorIndex + 1;
                    }
                    int curveIndex = 0;
                    while (curveIndex < series.RegressionCurves.Length)
                    {
                        WriteStyleMetadata(
                            series.RegressionCurves[curveIndex].StyleMetadata);
                        curveIndex = curveIndex + 1;
                    }
                    int pointIndex = 0;
                    while (pointIndex < series.DataPoints.Length)
                    {
                        OdfChartDataPoint point = series.DataPoints[pointIndex];
                        WriteStyleMetadata(point.StyleMetadata);
                        WriteLabelStyle(point.Label);
                        pointIndex = pointIndex + 1;
                    }
                    seriesIndex = seriesIndex + 1;
                }
                WriteStockStyle(plot.StockGainMarker);
                WriteStockStyle(plot.StockLossMarker);
                WriteStockStyle(plot.StockRangeLine);
            }
            chartIndex = chartIndex + 1;
        }
    }

    static OdfChartSeries LoadSeries(
        OdfDocument document,
        OdfNode node,
        string partPath,
        OdfChart chart)
    {
        OdfChartSeries result = new OdfChartSeries();
        result.Node = node;
        result.Chart = chart;
        result.ClassName = Read(node, OdfNamespaces.Chart, "class");
        result.Kind = result.ClassName.Length == 0
            ? chart.Kind
            : ParseSeriesKind(result.ClassName);
        result.ValuesCellRangeAddress = Read(
            node,
            OdfNamespaces.Chart,
            "values-cell-range-address");
        result.LabelCellAddress = Read(
            node,
            OdfNamespaces.Chart,
            "label-cell-address");
        result.AttachedAxis = Read(
            node,
            OdfNamespaces.Chart,
            "attached-axis");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.StyleMetadata = LoadChartStyleMetadata(
            document,
            result.StyleName,
            partPath);
        List<OdfChartDomain> domains = new List<OdfChartDomain>();
        List<OdfChartDataPoint> points = new List<OdfChartDataPoint>();
        List<OdfChartErrorIndicator> errors =
            new List<OdfChartErrorIndicator>();
        List<OdfChartRegressionCurve> curves =
            new List<OdfChartRegressionCurve>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsChartElement(child, "domain"))
            {
                domains.Add(LoadDomain(child));
            }
            else if (IsChartElement(child, "data-point"))
            {
                points.Add(LoadDataPoint(document, child, partPath));
            }
            else if (IsChartElement(child, "data-label"))
            {
                result.Label = LoadDataLabel(document, child, partPath);
            }
            else if (IsChartElement(child, "error-indicator"))
            {
                errors.Add(ChartAdvancedModel.LoadErrorIndicator(
                    document,
                    child,
                    partPath));
            }
            else if (IsChartElement(child, "regression-curve"))
            {
                curves.Add(ChartAdvancedModel.LoadRegressionCurve(
                    document,
                    child,
                    partPath));
            }
            index = index + 1;
        }
        result.Domains = domains.ToArray();
        result.DataPoints = points.ToArray();
        result.ErrorIndicators = errors.ToArray();
        result.RegressionCurves = curves.ToArray();
        CaptureSeriesOriginalValues(result);
        return result;
    }

    static OdfChartDomain LoadDomain(OdfNode node)
    {
        OdfChartDomain result = new OdfChartDomain();
        result.Node = node;
        result.CellRangeAddress = Read(
            node,
            OdfNamespaces.Table,
            "cell-range-address");
        result.OriginalCellRangeAddress = result.CellRangeAddress;
        return result;
    }

    static OdfChartDataPoint LoadDataPoint(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfChartDataPoint result = new OdfChartDataPoint();
        result.Node = node;
        result.Repeated = Read(node, OdfNamespaces.Chart, "repeated");
        RefreshPointRepeat(result);
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.StyleMetadata = LoadChartStyleMetadata(
            document,
            result.StyleName,
            partPath);
        OdfNode? label = FindDirectChartChild(node, "data-label");
        result.Label = label == null
            ? null
            : LoadDataLabel(document, label, partPath);
        result.OriginalRepeated = result.Repeated;
        result.OriginalStyleName = result.StyleName;
        return result;
    }

    static OdfChartDataLabel LoadDataLabel(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfChartDataLabel result = new OdfChartDataLabel();
        result.Node = node;
        result.Text = TextContent(node);
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.StyleMetadata = LoadChartStyleMetadata(
            document,
            result.StyleName,
            partPath);
        CaptureLabelOriginalValues(result);
        return result;
    }

    internal static OdfChartStyleMetadata LoadChartStyleMetadata(
        OdfDocument document,
        string styleName,
        string partPath)
    {
        OdfChartStyleMetadata result = new OdfChartStyleMetadata();
        result.Style = FindChartStyle(document, styleName, partPath);
        OdfStyle? style = result.Style;
        if (style == null)
        {
            return result;
        }
        result.VerticalValue = ReadEffectiveChartProperty(document, style, "vertical");
        result.StackedValue = ReadEffectiveChartProperty(document, style, "stacked");
        result.PercentageValue = ReadEffectiveChartProperty(document, style, "percentage");
        result.ThreeDimensionalValue = ReadEffectiveChartProperty(document, style, "three-dimensional");
        result.DeepValue = ReadEffectiveChartProperty(document, style, "deep");
        result.LinesValue = ReadEffectiveChartProperty(document, style, "lines");
        result.ConnectBarsValue = ReadEffectiveChartProperty(document, style, "connect-bars");
        result.SymbolType = ReadEffectiveChartProperty(document, style, "symbol-type");
        result.SymbolName = ReadEffectiveChartProperty(document, style, "symbol-name");
        result.SymbolWidth = ReadEffectiveChartProperty(document, style, "symbol-width");
        result.SymbolHeight = ReadEffectiveChartProperty(document, style, "symbol-height");
        result.Interpolation = ReadEffectiveChartProperty(document, style, "interpolation");
        result.SplineOrder = ReadEffectiveChartProperty(document, style, "spline-order");
        result.SplineResolution = ReadEffectiveChartProperty(document, style, "spline-resolution");
        result.SolidType = ReadEffectiveChartProperty(document, style, "solid-type");
        result.PieOffset = ReadEffectiveChartProperty(document, style, "pie-offset");
        result.DataLabelNumber = ReadEffectiveChartProperty(document, style, "data-label-number");
        result.DataLabelTextValue = ReadEffectiveChartProperty(document, style, "data-label-text");
        result.DataLabelSymbolValue = ReadEffectiveChartProperty(document, style, "data-label-symbol");
        result.ErrorCategory = ReadEffectiveChartProperty(document, style, "error-category");
        result.ErrorPercentage = ReadEffectiveChartProperty(document, style, "error-percentage");
        result.ErrorMargin = ReadEffectiveChartProperty(document, style, "error-margin");
        result.ErrorLowerLimit = ReadEffectiveChartProperty(document, style, "error-lower-limit");
        result.ErrorUpperLimit = ReadEffectiveChartProperty(document, style, "error-upper-limit");
        result.ErrorUpperIndicatorValue = ReadEffectiveChartProperty(document, style, "error-upper-indicator");
        result.ErrorLowerIndicatorValue = ReadEffectiveChartProperty(document, style, "error-lower-indicator");
        result.ErrorLowerRange = ReadEffectiveChartProperty(document, style, "error-lower-range");
        result.ErrorUpperRange = ReadEffectiveChartProperty(document, style, "error-upper-range");
        result.RegressionType = ReadEffectiveChartProperty(document, style, "regression-type");
        result.RegressionMaximumDegree = ReadEffectiveChartProperty(document, style, "regression-max-degree");
        result.RegressionForceInterceptValue = ReadEffectiveChartProperty(document, style, "regression-force-intercept");
        result.RegressionInterceptValue = ReadEffectiveChartProperty(document, style, "regression-intercept-value");
        result.RegressionName = ReadEffectiveChartProperty(document, style, "regression-name");
        result.RegressionPeriod = ReadEffectiveChartProperty(document, style, "regression-period");
        result.RegressionMovingType = ReadEffectiveChartProperty(document, style, "regression-moving-type");
        result.SymbolImageHref = ReadEffectiveSymbolImage(document, style);
        RefreshMetadataBooleans(result);
        CaptureMetadataOriginalValues(result);
        return result;
    }

    static OdfStyle? FindChartStyle(
        OdfDocument document,
        string name,
        string partPath)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfStyle? fallback = null;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == name && style.Family == OdfStyleFamily.Chart)
            {
                if (style.PartPath == partPath)
                {
                    return style;
                }
                if (fallback == null)
                {
                    fallback = style;
                }
            }
            index = index + 1;
        }
        return fallback;
    }

    static string ReadEffectiveChartProperty(
        OdfDocument document,
        OdfStyle style,
        string localName)
    {
        StyleResolutionResult resolution =
            StyleResolver.ResolveEffectiveProperties(document, style);
        string result = "";
        int sourceIndex = 0;
        while (sourceIndex < resolution.Sources.Length)
        {
            OdfStyle source = resolution.Sources[sourceIndex];
            int propertyIndex = 0;
            while (propertyIndex < source.ChartProperties.Length)
            {
                OdfStyleProperty property =
                    source.ChartProperties[propertyIndex];
                if (property.Name.NamespaceName == OdfNamespaces.Chart
                    && property.Name.LocalName == localName)
                {
                    result = property.LexicalValue;
                }
                propertyIndex = propertyIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        return result;
    }

    static string ReadEffectiveSymbolImage(
        OdfDocument document,
        OdfStyle style)
    {
        StyleResolutionResult resolution =
            StyleResolver.ResolveEffectiveProperties(document, style);
        string result = "";
        int sourceIndex = 0;
        while (sourceIndex < resolution.Sources.Length)
        {
            OdfNode? image = FindSymbolImage(
                resolution.Sources[sourceIndex].ChartPropertyChildren);
            if (image != null)
            {
                result = Read(image, OdfNamespaces.XLink, "href");
            }
            sourceIndex = sourceIndex + 1;
        }
        return result;
    }

    static void WriteSeries(OdfChartSeries? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Chart, "class", OdfNamespaces.ChartPrefix, item.ClassName, item.OriginalClassName);
        WriteChanged(item.Node, OdfNamespaces.Chart, "values-cell-range-address", OdfNamespaces.ChartPrefix, item.ValuesCellRangeAddress, item.OriginalValuesCellRangeAddress);
        WriteChanged(item.Node, OdfNamespaces.Chart, "label-cell-address", OdfNamespaces.ChartPrefix, item.LabelCellAddress, item.OriginalLabelCellAddress);
        WriteChanged(item.Node, OdfNamespaces.Chart, "attached-axis", OdfNamespaces.ChartPrefix, item.AttachedAxis, item.OriginalAttachedAxis);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        int index = 0;
        while (index < item.Domains.Length)
        {
            WriteDomain(item.Domains[index]);
            index = index + 1;
        }
        index = 0;
        while (index < item.DataPoints.Length)
        {
            WriteDataPoint(item.DataPoints[index]);
            index = index + 1;
        }
        WriteDataLabel(item.Label);
        ChartAdvancedModel.SynchronizeSeriesElements(item);
        item.Kind = item.ClassName.Length == 0 && item.Chart != null
            ? item.Chart.Kind
            : ParseSeriesKind(item.ClassName);
        CaptureSeriesOriginalValues(item);
    }

    static void WriteDomain(OdfChartDomain? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Table, "cell-range-address", OdfNamespaces.TablePrefix, item.CellRangeAddress, item.OriginalCellRangeAddress);
        item.OriginalCellRangeAddress = item.CellRangeAddress;
    }

    static void WriteDataPoint(OdfChartDataPoint? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Chart, "repeated", OdfNamespaces.ChartPrefix, item.Repeated, item.OriginalRepeated);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        WriteDataLabel(item.Label);
        RefreshPointRepeat(item);
        item.OriginalRepeated = item.Repeated;
        item.OriginalStyleName = item.StyleName;
    }

    static void WriteDataLabel(OdfChartDataLabel? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteChanged(item.Node, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, item.X, item.OriginalX);
        WriteChanged(item.Node, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, item.Y, item.OriginalY);
        WriteChanged(item.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, item.StyleName, item.OriginalStyleName);
        if (item.Text != item.OriginalText)
        {
            SetLabelText(item.Node, item.Text);
        }
        CaptureLabelOriginalValues(item);
    }

    static void WriteLabelStyle(OdfChartDataLabel? label)
    {
        if (label != null)
        {
            WriteStyleMetadata(label.StyleMetadata);
        }
    }

    static void WriteStockStyle(OdfChartStockComponent? component)
    {
        if (component != null)
        {
            WriteStyleMetadata(component.StyleMetadata);
        }
    }

    static void WriteStyleMetadata(OdfChartStyleMetadata? item)
    {
        if (item == null || item.Style == null)
        {
            return;
        }
        PrepareMetadataBooleanValues(item);
        SetChangedChartProperty(item.Style, "vertical", item.VerticalValue, item.OriginalVerticalValue);
        SetChangedChartProperty(item.Style, "stacked", item.StackedValue, item.OriginalStackedValue);
        SetChangedChartProperty(item.Style, "percentage", item.PercentageValue, item.OriginalPercentageValue);
        SetChangedChartProperty(item.Style, "three-dimensional", item.ThreeDimensionalValue, item.OriginalThreeDimensionalValue);
        SetChangedChartProperty(item.Style, "deep", item.DeepValue, item.OriginalDeepValue);
        SetChangedChartProperty(item.Style, "lines", item.LinesValue, item.OriginalLinesValue);
        SetChangedChartProperty(item.Style, "connect-bars", item.ConnectBarsValue, item.OriginalConnectBarsValue);
        SetChangedChartProperty(item.Style, "symbol-type", item.SymbolType, item.OriginalSymbolType);
        SetChangedChartProperty(item.Style, "symbol-name", item.SymbolName, item.OriginalSymbolName);
        SetChangedChartProperty(item.Style, "symbol-width", item.SymbolWidth, item.OriginalSymbolWidth);
        SetChangedChartProperty(item.Style, "symbol-height", item.SymbolHeight, item.OriginalSymbolHeight);
        SetChangedChartProperty(item.Style, "interpolation", item.Interpolation, item.OriginalInterpolation);
        SetChangedChartProperty(item.Style, "spline-order", item.SplineOrder, item.OriginalSplineOrder);
        SetChangedChartProperty(item.Style, "spline-resolution", item.SplineResolution, item.OriginalSplineResolution);
        SetChangedChartProperty(item.Style, "solid-type", item.SolidType, item.OriginalSolidType);
        SetChangedChartProperty(item.Style, "pie-offset", item.PieOffset, item.OriginalPieOffset);
        SetChangedChartProperty(item.Style, "data-label-number", item.DataLabelNumber, item.OriginalDataLabelNumber);
        SetChangedChartProperty(item.Style, "data-label-text", item.DataLabelTextValue, item.OriginalDataLabelTextValue);
        SetChangedChartProperty(item.Style, "data-label-symbol", item.DataLabelSymbolValue, item.OriginalDataLabelSymbolValue);
        SetChangedChartProperty(item.Style, "error-category", item.ErrorCategory, item.OriginalErrorCategory);
        SetChangedChartProperty(item.Style, "error-percentage", item.ErrorPercentage, item.OriginalErrorPercentage);
        SetChangedChartProperty(item.Style, "error-margin", item.ErrorMargin, item.OriginalErrorMargin);
        SetChangedChartProperty(item.Style, "error-lower-limit", item.ErrorLowerLimit, item.OriginalErrorLowerLimit);
        SetChangedChartProperty(item.Style, "error-upper-limit", item.ErrorUpperLimit, item.OriginalErrorUpperLimit);
        SetChangedChartProperty(item.Style, "error-upper-indicator", item.ErrorUpperIndicatorValue, item.OriginalErrorUpperIndicatorValue);
        SetChangedChartProperty(item.Style, "error-lower-indicator", item.ErrorLowerIndicatorValue, item.OriginalErrorLowerIndicatorValue);
        SetChangedChartProperty(item.Style, "error-lower-range", item.ErrorLowerRange, item.OriginalErrorLowerRange);
        SetChangedChartProperty(item.Style, "error-upper-range", item.ErrorUpperRange, item.OriginalErrorUpperRange);
        SetChangedChartProperty(item.Style, "regression-type", item.RegressionType, item.OriginalRegressionType);
        SetChangedChartProperty(item.Style, "regression-max-degree", item.RegressionMaximumDegree, item.OriginalRegressionMaximumDegree);
        SetChangedChartProperty(item.Style, "regression-force-intercept", item.RegressionForceInterceptValue, item.OriginalRegressionForceInterceptValue);
        SetChangedChartProperty(item.Style, "regression-intercept-value", item.RegressionInterceptValue, item.OriginalRegressionInterceptValue);
        SetChangedChartProperty(item.Style, "regression-name", item.RegressionName, item.OriginalRegressionName);
        SetChangedChartProperty(item.Style, "regression-period", item.RegressionPeriod, item.OriginalRegressionPeriod);
        SetChangedChartProperty(item.Style, "regression-moving-type", item.RegressionMovingType, item.OriginalRegressionMovingType);
        if (item.SymbolImageHref != item.OriginalSymbolImageHref)
        {
            SetSymbolImage(item.Style, item.SymbolImageHref);
        }
        RefreshMetadataBooleans(item);
        CaptureMetadataOriginalValues(item);
    }

    static void PrepareMetadataBooleanValues(OdfChartStyleMetadata item)
    {
        if (item.IsVertical != item.OriginalIsVertical
            && item.VerticalValue == item.OriginalVerticalValue)
            item.VerticalValue = item.IsVertical ? "true" : "false";
        if (item.IsStacked != item.OriginalIsStacked
            && item.StackedValue == item.OriginalStackedValue)
            item.StackedValue = item.IsStacked ? "true" : "false";
        if (item.IsPercentage != item.OriginalIsPercentage
            && item.PercentageValue == item.OriginalPercentageValue)
            item.PercentageValue = item.IsPercentage ? "true" : "false";
        if (item.IsThreeDimensional != item.OriginalIsThreeDimensional
            && item.ThreeDimensionalValue == item.OriginalThreeDimensionalValue)
            item.ThreeDimensionalValue = item.IsThreeDimensional ? "true" : "false";
        if (item.IsDeep != item.OriginalIsDeep
            && item.DeepValue == item.OriginalDeepValue)
            item.DeepValue = item.IsDeep ? "true" : "false";
        if (item.HasLines != item.OriginalHasLines
            && item.LinesValue == item.OriginalLinesValue)
            item.LinesValue = item.HasLines ? "true" : "false";
        if (item.ConnectBars != item.OriginalConnectBars
            && item.ConnectBarsValue == item.OriginalConnectBarsValue)
            item.ConnectBarsValue = item.ConnectBars ? "true" : "false";
        if (item.HasDataLabelText != item.OriginalHasDataLabelText
            && item.DataLabelTextValue == item.OriginalDataLabelTextValue)
            item.DataLabelTextValue = item.HasDataLabelText ? "true" : "false";
        if (item.HasDataLabelSymbol != item.OriginalHasDataLabelSymbol
            && item.DataLabelSymbolValue == item.OriginalDataLabelSymbolValue)
            item.DataLabelSymbolValue = item.HasDataLabelSymbol ? "true" : "false";
        if (item.HasErrorUpperIndicator
                != item.OriginalHasErrorUpperIndicator
            && item.ErrorUpperIndicatorValue
                == item.OriginalErrorUpperIndicatorValue)
            item.ErrorUpperIndicatorValue = item.HasErrorUpperIndicator
                ? "true"
                : "false";
        if (item.HasErrorLowerIndicator
                != item.OriginalHasErrorLowerIndicator
            && item.ErrorLowerIndicatorValue
                == item.OriginalErrorLowerIndicatorValue)
            item.ErrorLowerIndicatorValue = item.HasErrorLowerIndicator
                ? "true"
                : "false";
        if (item.IsRegressionInterceptForced
                != item.OriginalIsRegressionInterceptForced
            && item.RegressionForceInterceptValue
                == item.OriginalRegressionForceInterceptValue)
            item.RegressionForceInterceptValue =
                item.IsRegressionInterceptForced ? "true" : "false";
    }

    static void SetChangedChartProperty(
        OdfStyle style,
        string localName,
        string value,
        string original)
    {
        if (value == original)
        {
            return;
        }
        int index = 0;
        while (index < style.ChartProperties.Length)
        {
            OdfStyleProperty property = style.ChartProperties[index];
            if (property.Name.NamespaceName == OdfNamespaces.Chart
                && property.Name.LocalName == localName)
            {
                if (value.Length == 0)
                {
                    RemoveChartProperty(style, index);
                }
                else
                {
                    property.LexicalValue = value;
                }
                return;
            }
            index = index + 1;
        }
        if (value.Length == 0)
        {
            return;
        }
        OdfStyleProperty added = new OdfStyleProperty();
        added.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Chart,
            localName,
            OdfNamespaces.ChartPrefix);
        added.LexicalValue = value;
        OdfStyleProperty[] properties = new OdfStyleProperty[
            style.ChartProperties.Length + 1];
        index = 0;
        while (index < style.ChartProperties.Length)
        {
            properties[index] = style.ChartProperties[index];
            index = index + 1;
        }
        properties[properties.Length - 1] = added;
        style.ChartProperties = properties;
    }

    static void RemoveChartProperty(OdfStyle style, int removeIndex)
    {
        OdfStyleProperty[] properties = new OdfStyleProperty[
            style.ChartProperties.Length - 1];
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < style.ChartProperties.Length)
        {
            if (sourceIndex != removeIndex)
            {
                properties[targetIndex] = style.ChartProperties[sourceIndex];
                targetIndex = targetIndex + 1;
            }
            sourceIndex = sourceIndex + 1;
        }
        style.ChartProperties = properties;
    }

    static void SetSymbolImage(OdfStyle style, string href)
    {
        OdfNode? image = FindSymbolImage(style.ChartPropertyChildren);
        if (image == null && href.Length == 0)
        {
            return;
        }
        if (image == null)
        {
            image = new OdfNode();
            image.Kind = OdfNodeKind.Element;
            image.Name = StyleDocumentModel.QualifiedName(
                OdfNamespaces.Chart,
                "symbol-image",
                OdfNamespaces.ChartPrefix);
            image.Location.PartPath = style.PartPath;
            image.WasEmptyElement = true;
            OdfNode[] children = new OdfNode[
                style.ChartPropertyChildren.Length + 1];
            int index = 0;
            while (index < style.ChartPropertyChildren.Length)
            {
                children[index] = style.ChartPropertyChildren[index];
                index = index + 1;
            }
            children[children.Length - 1] = image;
            style.ChartPropertyChildren = children;
        }
        if (href.Length == 0)
        {
            RemoveSymbolImage(style, image);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                image,
                OdfNamespaces.XLink,
                "href",
                OdfNamespaces.XLinkPrefix,
                href);
        }
    }

    static void RemoveSymbolImage(OdfStyle style, OdfNode image)
    {
        int count = 0;
        int index = 0;
        while (index < style.ChartPropertyChildren.Length)
        {
            if (style.ChartPropertyChildren[index] != image)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        OdfNode[] children = new OdfNode[count];
        int target = 0;
        index = 0;
        while (index < style.ChartPropertyChildren.Length)
        {
            if (style.ChartPropertyChildren[index] != image)
            {
                children[target] = style.ChartPropertyChildren[index];
                target = target + 1;
            }
            index = index + 1;
        }
        style.ChartPropertyChildren = children;
    }

    static OdfNode? FindSymbolImage(OdfNode[] children)
    {
        int index = 0;
        while (index < children.Length)
        {
            if (IsChartElement(children[index], "symbol-image"))
            {
                return children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static void RefreshMetadataBooleans(OdfChartStyleMetadata item)
    {
        OdfLexicalValue vertical = OdfLexicalParser.ParseBoolean(item.VerticalValue);
        item.IsVertical = vertical.IsValid && vertical.BooleanValue;
        item.IsVerticalValid = item.VerticalValue.Length == 0 || vertical.IsValid;
        OdfLexicalValue stacked = OdfLexicalParser.ParseBoolean(item.StackedValue);
        item.IsStacked = stacked.IsValid && stacked.BooleanValue;
        item.IsStackedValid = item.StackedValue.Length == 0 || stacked.IsValid;
        OdfLexicalValue percentage = OdfLexicalParser.ParseBoolean(item.PercentageValue);
        item.IsPercentage = percentage.IsValid && percentage.BooleanValue;
        item.IsPercentageValid = item.PercentageValue.Length == 0 || percentage.IsValid;
        OdfLexicalValue dimensional = OdfLexicalParser.ParseBoolean(item.ThreeDimensionalValue);
        item.IsThreeDimensional = dimensional.IsValid && dimensional.BooleanValue;
        item.IsThreeDimensionalValid = item.ThreeDimensionalValue.Length == 0 || dimensional.IsValid;
        OdfLexicalValue deep = OdfLexicalParser.ParseBoolean(item.DeepValue);
        item.IsDeep = deep.IsValid && deep.BooleanValue;
        item.IsDeepValid = item.DeepValue.Length == 0 || deep.IsValid;
        OdfLexicalValue lines = OdfLexicalParser.ParseBoolean(item.LinesValue);
        item.HasLines = lines.IsValid && lines.BooleanValue;
        item.IsLinesValid = item.LinesValue.Length == 0 || lines.IsValid;
        OdfLexicalValue connect = OdfLexicalParser.ParseBoolean(item.ConnectBarsValue);
        item.ConnectBars = connect.IsValid && connect.BooleanValue;
        item.IsConnectBarsValid = item.ConnectBarsValue.Length == 0 || connect.IsValid;
        OdfLexicalValue labelText = OdfLexicalParser.ParseBoolean(item.DataLabelTextValue);
        item.HasDataLabelText = labelText.IsValid && labelText.BooleanValue;
        item.IsDataLabelTextValid = item.DataLabelTextValue.Length == 0 || labelText.IsValid;
        OdfLexicalValue labelSymbol = OdfLexicalParser.ParseBoolean(item.DataLabelSymbolValue);
        item.HasDataLabelSymbol = labelSymbol.IsValid && labelSymbol.BooleanValue;
        item.IsDataLabelSymbolValid = item.DataLabelSymbolValue.Length == 0 || labelSymbol.IsValid;
        OdfLexicalValue upper = OdfLexicalParser.ParseBoolean(
            item.ErrorUpperIndicatorValue);
        item.HasErrorUpperIndicator = upper.IsValid && upper.BooleanValue;
        item.IsErrorUpperIndicatorValid =
            item.ErrorUpperIndicatorValue.Length == 0 || upper.IsValid;
        OdfLexicalValue lower = OdfLexicalParser.ParseBoolean(
            item.ErrorLowerIndicatorValue);
        item.HasErrorLowerIndicator = lower.IsValid && lower.BooleanValue;
        item.IsErrorLowerIndicatorValid =
            item.ErrorLowerIndicatorValue.Length == 0 || lower.IsValid;
        OdfLexicalValue force = OdfLexicalParser.ParseBoolean(
            item.RegressionForceInterceptValue);
        item.IsRegressionInterceptForced =
            force.IsValid && force.BooleanValue;
        item.IsRegressionForceInterceptValid =
            item.RegressionForceInterceptValue.Length == 0 || force.IsValid;
    }

    static void RefreshPointRepeat(OdfChartDataPoint item)
    {
        if (item.Repeated.Length == 0)
        {
            item.RepeatCount = 1;
            item.IsRepeatCountValid = true;
            return;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(item.Repeated);
        item.IsRepeatCountValid = parsed.IsValid && parsed.IntegerValue > 0;
        item.RepeatCount = item.IsRepeatCountValid
            ? parsed.IntegerValue
            : 1;
    }

    static OdfChartKind ParseSeriesKind(string value)
    {
        int colon = value.LastIndexOf(':');
        string name = colon < 0 ? value : value.Substring(colon + 1);
        if (name == "bar") return OdfChartKind.Bar;
        if (name == "line") return OdfChartKind.Line;
        if (name == "area") return OdfChartKind.Area;
        if (name == "circle" || name == "pie") return OdfChartKind.Pie;
        if (name == "ring") return OdfChartKind.Ring;
        if (name == "scatter") return OdfChartKind.Scatter;
        if (name == "radar") return OdfChartKind.Radar;
        if (name == "filled-radar") return OdfChartKind.FilledRadar;
        if (name == "stock") return OdfChartKind.Stock;
        if (name == "bubble") return OdfChartKind.Bubble;
        if (name == "surface") return OdfChartKind.Surface;
        if (name == "gantt") return OdfChartKind.Gantt;
        return OdfChartKind.Unknown;
    }

    static void SetLabelText(OdfNode parent, string value)
    {
        OdfNode? paragraph = FindDirectTextChild(parent);
        if (paragraph == null)
        {
            paragraph = new OdfNode();
            paragraph.Kind = OdfNodeKind.Element;
            paragraph.Name = StyleDocumentModel.QualifiedName(
                OdfNamespaces.Text,
                "p",
                OdfNamespaces.TextPrefix);
            paragraph.Location.PartPath = parent.Location.PartPath;
            AppendChild(parent, paragraph);
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = paragraph;
        text.Location.PartPath = paragraph.Location.PartPath;
        paragraph.Children = [text];
        paragraph.WasEmptyElement = false;
    }

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        int index = 0;
        while (index < parent.Children.Length)
        {
            children[index] = parent.Children[index];
            index = index + 1;
        }
        child.Parent = parent;
        children[children.Length - 1] = child;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static OdfNode? FindDirectChartChild(OdfNode parent, string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (IsChartElement(parent.Children[index], localName))
            {
                return parent.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectTextChild(OdfNode parent)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Text
                && child.Name.LocalName == "p")
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsChartElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Chart
            && node.Name.LocalName == localName;
    }

    static string TextContent(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        AppendText(node, result);
        return result.ToString();
    }

    static void AppendText(OdfNode node, StringBuilder result)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendText(node.Children[index], result);
            index = index + 1;
        }
    }

    static string Read(OdfNode node, string namespaceName, string localName)
    {
        return StyleDocumentModel.ReadAttribute(node, namespaceName, localName);
    }

    static void WriteChanged(OdfNode node, string namespaceName, string localName, string prefix, string value, string original)
    {
        if (value == original)
        {
            return;
        }
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(node, namespaceName, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(node, namespaceName, localName, prefix, value);
        }
    }

    static void CaptureSeriesOriginalValues(OdfChartSeries item)
    {
        item.OriginalClassName = item.ClassName;
        item.OriginalValuesCellRangeAddress = item.ValuesCellRangeAddress;
        item.OriginalLabelCellAddress = item.LabelCellAddress;
        item.OriginalAttachedAxis = item.AttachedAxis;
        item.OriginalStyleName = item.StyleName;
    }

    static void CaptureLabelOriginalValues(OdfChartDataLabel item)
    {
        item.OriginalText = item.Text;
        item.OriginalX = item.X;
        item.OriginalY = item.Y;
        item.OriginalStyleName = item.StyleName;
    }

    static void CaptureMetadataOriginalValues(OdfChartStyleMetadata item)
    {
        item.OriginalVerticalValue = item.VerticalValue;
        item.OriginalIsVertical = item.IsVertical;
        item.OriginalStackedValue = item.StackedValue;
        item.OriginalIsStacked = item.IsStacked;
        item.OriginalPercentageValue = item.PercentageValue;
        item.OriginalIsPercentage = item.IsPercentage;
        item.OriginalThreeDimensionalValue = item.ThreeDimensionalValue;
        item.OriginalIsThreeDimensional = item.IsThreeDimensional;
        item.OriginalDeepValue = item.DeepValue;
        item.OriginalIsDeep = item.IsDeep;
        item.OriginalLinesValue = item.LinesValue;
        item.OriginalHasLines = item.HasLines;
        item.OriginalConnectBarsValue = item.ConnectBarsValue;
        item.OriginalConnectBars = item.ConnectBars;
        item.OriginalSymbolType = item.SymbolType;
        item.OriginalSymbolName = item.SymbolName;
        item.OriginalSymbolWidth = item.SymbolWidth;
        item.OriginalSymbolHeight = item.SymbolHeight;
        item.OriginalSymbolImageHref = item.SymbolImageHref;
        item.OriginalInterpolation = item.Interpolation;
        item.OriginalSplineOrder = item.SplineOrder;
        item.OriginalSplineResolution = item.SplineResolution;
        item.OriginalSolidType = item.SolidType;
        item.OriginalPieOffset = item.PieOffset;
        item.OriginalDataLabelNumber = item.DataLabelNumber;
        item.OriginalDataLabelTextValue = item.DataLabelTextValue;
        item.OriginalHasDataLabelText = item.HasDataLabelText;
        item.OriginalDataLabelSymbolValue = item.DataLabelSymbolValue;
        item.OriginalHasDataLabelSymbol = item.HasDataLabelSymbol;
        item.OriginalErrorCategory = item.ErrorCategory;
        item.OriginalErrorPercentage = item.ErrorPercentage;
        item.OriginalErrorMargin = item.ErrorMargin;
        item.OriginalErrorLowerLimit = item.ErrorLowerLimit;
        item.OriginalErrorUpperLimit = item.ErrorUpperLimit;
        item.OriginalErrorUpperIndicatorValue =
            item.ErrorUpperIndicatorValue;
        item.OriginalHasErrorUpperIndicator = item.HasErrorUpperIndicator;
        item.OriginalErrorLowerIndicatorValue =
            item.ErrorLowerIndicatorValue;
        item.OriginalHasErrorLowerIndicator = item.HasErrorLowerIndicator;
        item.OriginalErrorLowerRange = item.ErrorLowerRange;
        item.OriginalErrorUpperRange = item.ErrorUpperRange;
        item.OriginalRegressionType = item.RegressionType;
        item.OriginalRegressionMaximumDegree =
            item.RegressionMaximumDegree;
        item.OriginalRegressionForceInterceptValue =
            item.RegressionForceInterceptValue;
        item.OriginalIsRegressionInterceptForced =
            item.IsRegressionInterceptForced;
        item.OriginalRegressionInterceptValue =
            item.RegressionInterceptValue;
        item.OriginalRegressionName = item.RegressionName;
        item.OriginalRegressionPeriod = item.RegressionPeriod;
        item.OriginalRegressionMovingType = item.RegressionMovingType;
    }
}
