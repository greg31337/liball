using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxChartBuilder
{
    internal static byte[] CreateChartBytes(string chartType, PptxChartData data)
    {
        return PptxXml.WriteBytes(CreateChartDocument(chartType, data));
    }

    internal static XmlDocument CreateChartDocument(string chartType, PptxChartData data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        string normalized = PptxChartTypeCore.Normalize(chartType);
        XmlDocument document = PptxXml.CreateDocument();
        XmlElement chartSpace = document.CreateElement("c", "chartSpace", PptxNamespaces.Chart);
        document.AppendChild(chartSpace);
        XmlElement chart = document.CreateElement("c", "chart", PptxNamespaces.Chart);
        chartSpace.AppendChild(chart);
        XmlElement plotArea = document.CreateElement("c", "plotArea", PptxNamespaces.Chart);
        chart.AppendChild(plotArea);
        if (normalized == PptxChartType.Combo)
        {
            AppendComboCharts(document, plotArea, data);
        }
        else
        {
            XmlElement chartElement = document.CreateElement("c", ChartElementName(normalized), PptxNamespaces.Chart);
            AppendChartDefaults(document, chartElement, normalized);
            AppendSeries(document, chartElement, normalized, data, 0, data.Series.Count);
            AppendAxisIds(document, chartElement, normalized);
            plotArea.AppendChild(chartElement);
        }

        AppendDefaultAxes(document, plotArea, normalized);

        chart.AppendChild(document.CreateElement("c", "plotVisOnly", PptxNamespaces.Chart));
        return document;
    }

    internal static void AppendComboCharts(XmlDocument document, XmlElement plotArea, PptxChartData data)
    {
        List<string> chartTypes = new List<string>();
        for (int index = 0; index < data.Series.Count; index++)
        {
            string chartType = ResolveComboSeriesType(data, index);
            bool found = false;
            for (int typeIndex = 0; typeIndex < chartTypes.Count; typeIndex++)
            {
                if (chartTypes[typeIndex] == chartType)
                {
                    found = true;
                }
            }

            if (!found)
            {
                chartTypes.Add(chartType);
            }
        }

        if (chartTypes.Count == 0)
        {
            chartTypes.Add(PptxChartType.Column);
            chartTypes.Add(PptxChartType.Line);
        }

        for (int typeIndex = 0; typeIndex < chartTypes.Count; typeIndex++)
        {
            string chartType = chartTypes[typeIndex];
            XmlElement chartElement = document.CreateElement("c", ChartElementName(chartType), PptxNamespaces.Chart);
            AppendChartDefaults(document, chartElement, chartType);
            AppendComboSeries(document, chartElement, chartType, data);
            AppendAxisIds(document, chartElement, chartType);
            plotArea.AppendChild(chartElement);
        }
    }

    internal static string ResolveComboSeriesType(PptxChartData data, int index)
    {
        string requested = PptxDiagnostic.CleanText(data.Series[index].ChartType);
        if (requested.Length > 0 && requested != PptxChartType.Combo)
        {
            return PptxChartTypeCore.Normalize(requested);
        }

        int split = data.Series.Count / 2;
        if (split < 1)
        {
            split = 1;
        }

        if (index < split)
        {
            return PptxChartType.Column;
        }

        return PptxChartType.Line;
    }

    internal static void AppendComboSeries(XmlDocument document, XmlElement chartElement, string chartType, PptxChartData data)
    {
        for (int index = 0; index < data.Series.Count; index++)
        {
            if (ResolveComboSeriesType(data, index) == chartType)
            {
                AppendOneSeries(document, chartElement, chartType, data, index);
            }
        }
    }

    internal static void AppendChartDefaults(XmlDocument document, XmlElement chartElement, string chartType)
    {
        if (chartType == PptxChartType.Column || chartType == PptxChartType.Bar)
        {
            XmlElement barDirection = document.CreateElement("c", "barDir", PptxNamespaces.Chart);
            if (chartType == PptxChartType.Bar)
            {
                PptxXml.SetAttributeValue(barDirection, "", "", "val", "bar");
            }
            else
            {
                PptxXml.SetAttributeValue(barDirection, "", "", "val", "col");
            }

            chartElement.AppendChild(barDirection);
            XmlElement grouping = document.CreateElement("c", "grouping", PptxNamespaces.Chart);
            PptxXml.SetAttributeValue(grouping, "", "", "val", "clustered");
            chartElement.AppendChild(grouping);
            return;
        }

        if (chartType == PptxChartType.Scatter)
        {
            XmlElement style = document.CreateElement("c", "scatterStyle", PptxNamespaces.Chart);
            PptxXml.SetAttributeValue(style, "", "", "val", "lineMarker");
            chartElement.AppendChild(style);
            return;
        }

        if (chartType == PptxChartType.Radar)
        {
            XmlElement style = document.CreateElement("c", "radarStyle", PptxNamespaces.Chart);
            PptxXml.SetAttributeValue(style, "", "", "val", "marker");
            chartElement.AppendChild(style);
        }
    }

    internal static void AppendSeries(XmlDocument document, XmlElement chartElement, string chartType, PptxChartData data, int startIndex, int endIndex)
    {
        int actualEnd = endIndex;
        if (actualEnd > data.Series.Count)
        {
            actualEnd = data.Series.Count;
        }

        for (int index = startIndex; index < actualEnd; index++)
        {
            AppendOneSeries(document, chartElement, chartType, data, index);
        }
    }

    internal static void AppendOneSeries(XmlDocument document, XmlElement chartElement, string chartType, PptxChartData data, int index)
    {
        PptxChartSeries series = data.Series[index];
        XmlElement seriesElement = document.CreateElement("c", "ser", PptxNamespaces.Chart);
        AppendValuedElement(document, seriesElement, "idx", index.ToString());
        AppendValuedElement(document, seriesElement, "order", index.ToString());
        int nameColumn = index + 2;
        if (chartType == PptxChartType.Scatter || chartType == PptxChartType.Bubble)
        {
            nameColumn = CoordinateYColumn(chartType, index);
        }

        AppendSeriesText(document, seriesElement, series.Name, series.NameFormula, nameColumn);
        AppendDataLabels(document, seriesElement, series);
        if (chartType == PptxChartType.Scatter || chartType == PptxChartType.Bubble)
        {
            AppendValues(document, seriesElement, "xVal", GetXValues(data, series), series.CategoriesNumberFormat, GetXFormula(data, series), CoordinateXColumn(chartType, index));
            AppendValues(document, seriesElement, "yVal", GetYValues(series), series.ValuesNumberFormat, GetYFormula(series), CoordinateYColumn(chartType, index));
        }
        else
        {
            AppendCategories(document, seriesElement, data, series);
            AppendValues(document, seriesElement, "val", series.Values, series.ValuesNumberFormat, series.ValuesFormula, index + 2);
        }

        if (chartType == PptxChartType.Bubble)
        {
            AppendBubbleSizes(document, seriesElement, series, CoordinateSizeColumn(index));
        }

        chartElement.AppendChild(seriesElement);
    }

    internal static void AppendSeriesText(XmlDocument document, XmlElement seriesElement, string name, string requestedFormula, int columnIndex)
    {
        XmlElement text = document.CreateElement("c", "tx", PptxNamespaces.Chart);
        XmlElement reference = document.CreateElement("c", "strRef", PptxNamespaces.Chart);
        XmlElement formula = document.CreateElement("c", "f", PptxNamespaces.Chart);
        string formulaText = PptxDiagnostic.CleanText(requestedFormula);
        if (formulaText.Length == 0)
        {
            formulaText = "Sheet1!$" + ColumnName(columnIndex) + "$1";
        }

        formula.InnerText = formulaText;
        reference.AppendChild(formula);
        XmlElement cache = document.CreateElement("c", "strCache", PptxNamespaces.Chart);
        AppendPoint(document, cache, 0, name);
        reference.AppendChild(cache);
        text.AppendChild(reference);
        seriesElement.AppendChild(text);
    }

    internal static void AppendCategories(XmlDocument document, XmlElement seriesElement, PptxChartData data, PptxChartSeries series)
    {
        List<string> categories = GetCategories(data, series);
        XmlElement categoryElement = document.CreateElement("c", "cat", PptxNamespaces.Chart);
        bool numeric = series.CategoryKind == PptxChartCategoryKind.Numeric;
        if (series.Categories.Count == 0)
        {
            numeric = data.CategoryKind == PptxChartCategoryKind.Numeric;
        }

        string referenceName = "strRef";
        string cacheName = "strCache";
        if (numeric)
        {
            referenceName = "numRef";
            cacheName = "numCache";
        }

        XmlElement reference = document.CreateElement("c", referenceName, PptxNamespaces.Chart);
        XmlElement formula = document.CreateElement("c", "f", PptxNamespaces.Chart);
        string formulaText = GetCategoryFormula(data, series);
        if (formulaText.Length == 0)
        {
            formulaText = SheetRange(1, 2, categories.Count);
        }

        formula.InnerText = formulaText;
        reference.AppendChild(formula);
        XmlElement cache = document.CreateElement("c", cacheName, PptxNamespaces.Chart);
        if (numeric)
        {
            XmlElement formatCode = document.CreateElement("c", "formatCode", PptxNamespaces.Chart);
            string format = GetCategoryNumberFormat(data, series);
            if (format.Length == 0)
            {
                format = "General";
            }

            formatCode.InnerText = format;
            cache.AppendChild(formatCode);
        }

        for (int index = 0; index < categories.Count; index++)
        {
            AppendPoint(document, cache, index, categories[index]);
        }

        reference.AppendChild(cache);
        categoryElement.AppendChild(reference);
        seriesElement.AppendChild(categoryElement);
    }

    internal static void AppendValues(XmlDocument document, XmlElement seriesElement, string localName, List<string> values, string numberFormat, string requestedFormula, int columnIndex)
    {
        XmlElement valueElement = document.CreateElement("c", localName, PptxNamespaces.Chart);
        XmlElement reference = document.CreateElement("c", "numRef", PptxNamespaces.Chart);
        XmlElement formula = document.CreateElement("c", "f", PptxNamespaces.Chart);
        string formulaText = PptxDiagnostic.CleanText(requestedFormula);
        if (formulaText.Length == 0)
        {
            formulaText = SheetRange(columnIndex, 2, values.Count);
        }

        formula.InnerText = formulaText;
        reference.AppendChild(formula);
        XmlElement cache = document.CreateElement("c", "numCache", PptxNamespaces.Chart);
        XmlElement formatCode = document.CreateElement("c", "formatCode", PptxNamespaces.Chart);
        string format = PptxDiagnostic.CleanText(numberFormat);
        if (format.Length == 0)
        {
            format = "General";
        }

        formatCode.InnerText = format;
        cache.AppendChild(formatCode);
        for (int index = 0; index < values.Count; index++)
        {
            AppendPoint(document, cache, index, values[index]);
        }

        reference.AppendChild(cache);
        valueElement.AppendChild(reference);
        seriesElement.AppendChild(valueElement);
    }

    internal static void AppendBubbleSizes(XmlDocument document, XmlElement seriesElement, PptxChartSeries series, int columnIndex)
    {
        List<string> values = series.BubbleSizes;
        bool generated = false;
        if (values.Count == 0)
        {
            values = GetYValues(series);
            generated = true;
        }

        XmlElement bubbleSize = document.CreateElement("c", "bubbleSize", PptxNamespaces.Chart);
        XmlElement reference = document.CreateElement("c", "numRef", PptxNamespaces.Chart);
        XmlElement formula = document.CreateElement("c", "f", PptxNamespaces.Chart);
        string formulaText = PptxDiagnostic.CleanText(series.BubbleSizeFormula);
        if (formulaText.Length == 0)
        {
            formulaText = SheetRange(columnIndex, 2, values.Count);
        }

        formula.InnerText = formulaText;
        reference.AppendChild(formula);
        XmlElement cache = document.CreateElement("c", "numCache", PptxNamespaces.Chart);
        XmlElement formatCode = document.CreateElement("c", "formatCode", PptxNamespaces.Chart);
        string format = PptxDiagnostic.CleanText(series.BubbleSizesNumberFormat);
        if (format.Length == 0)
        {
            format = "General";
        }

        formatCode.InnerText = format;
        cache.AppendChild(formatCode);
        for (int index = 0; index < values.Count; index++)
        {
            string value = values[index];
            if (generated)
            {
                value = "1";
            }

            AppendPoint(document, cache, index, value);
        }

        reference.AppendChild(cache);
        bubbleSize.AppendChild(reference);
        seriesElement.AppendChild(bubbleSize);
    }

    internal static void AppendDataLabels(XmlDocument document, XmlElement seriesElement, PptxChartSeries series)
    {
        if (series.Labels.Count == 0 && series.NumberFormat.Length == 0)
        {
            return;
        }

        XmlElement labels = document.CreateElement("c", "dLbls", PptxNamespaces.Chart);
        for (int index = 0; index < series.Labels.Count; index++)
        {
            XmlElement label = document.CreateElement("c", "dLbl", PptxNamespaces.Chart);
            AppendValuedElement(document, label, "idx", index.ToString());
            XmlElement text = document.CreateElement("c", "tx", PptxNamespaces.Chart);
            string labelFormula = PptxDiagnostic.CleanText(series.LabelFormula);
            if (labelFormula.Length > 0)
            {
                XmlElement reference = document.CreateElement("c", "strRef", PptxNamespaces.Chart);
                XmlElement formula = document.CreateElement("c", "f", PptxNamespaces.Chart);
                formula.InnerText = labelFormula;
                reference.AppendChild(formula);
                XmlElement cache = document.CreateElement("c", "strCache", PptxNamespaces.Chart);
                AppendPoint(document, cache, 0, series.Labels[index]);
                reference.AppendChild(cache);
                text.AppendChild(reference);
            }
            else
            {
                XmlElement rich = document.CreateElement("c", "rich", PptxNamespaces.Chart);
                rich.AppendChild(document.CreateElement("a", "bodyPr", PptxNamespaces.Drawing));
                rich.AppendChild(document.CreateElement("a", "lstStyle", PptxNamespaces.Drawing));
                XmlElement paragraph = document.CreateElement("a", "p", PptxNamespaces.Drawing);
                XmlElement run = document.CreateElement("a", "r", PptxNamespaces.Drawing);
                XmlElement drawingText = document.CreateElement("a", "t", PptxNamespaces.Drawing);
                drawingText.InnerText = PptxDiagnostic.CleanText(series.Labels[index]);
                run.AppendChild(drawingText);
                paragraph.AppendChild(run);
                rich.AppendChild(paragraph);
                text.AppendChild(rich);
            }

            label.AppendChild(text);
            labels.AppendChild(label);
        }

        if (series.NumberFormat.Length > 0)
        {
            XmlElement numberFormat = document.CreateElement("c", "numFmt", PptxNamespaces.Chart);
            PptxXml.SetAttributeValue(numberFormat, "", "", "formatCode", series.NumberFormat);
            PptxXml.SetAttributeValue(numberFormat, "", "", "sourceLinked", "0");
            labels.AppendChild(numberFormat);
        }

        seriesElement.AppendChild(labels);
    }

    internal static void AppendPoint(XmlDocument document, XmlElement cache, int index, string value)
    {
        XmlElement point = document.CreateElement("c", "pt", PptxNamespaces.Chart);
        PptxXml.SetAttributeValue(point, "", "", "idx", index.ToString());
        XmlElement chartValue = document.CreateElement("c", "v", PptxNamespaces.Chart);
        chartValue.InnerText = PptxDiagnostic.CleanText(value);
        point.AppendChild(chartValue);
        cache.AppendChild(point);
    }

    internal static void AppendValuedElement(XmlDocument document, XmlElement parent, string localName, string value)
    {
        XmlElement element = document.CreateElement("c", localName, PptxNamespaces.Chart);
        PptxXml.SetAttributeValue(element, "", "", "val", value);
        parent.AppendChild(element);
    }

    internal static void AppendAxisIds(XmlDocument document, XmlElement chartElement, string chartType)
    {
        if (chartType == PptxChartType.Pie || chartType == PptxChartType.Doughnut)
        {
            return;
        }

        AppendValuedElement(document, chartElement, "axId", "100001");
        AppendValuedElement(document, chartElement, "axId", "100002");
    }

    internal static void AppendDefaultAxes(XmlDocument document, XmlElement plotArea, string chartType)
    {
        if (chartType == PptxChartType.Pie || chartType == PptxChartType.Doughnut)
        {
            return;
        }

        string horizontalType = "catAx";
        if (chartType == PptxChartType.Scatter || chartType == PptxChartType.Bubble)
        {
            horizontalType = "valAx";
        }

        plotArea.AppendChild(CreateAxisElement(document, horizontalType, "100001", "100002", "b"));
        plotArea.AppendChild(CreateAxisElement(document, "valAx", "100002", "100001", "l"));
    }

    internal static XmlElement CreateAxisElement(XmlDocument document, string localName, string axisId, string crossAxisId, string position)
    {
        XmlElement axis = document.CreateElement("c", localName, PptxNamespaces.Chart);
        AppendValuedElement(document, axis, "axId", axisId);
        XmlElement scaling = document.CreateElement("c", "scaling", PptxNamespaces.Chart);
        AppendValuedElement(document, scaling, "orientation", "minMax");
        axis.AppendChild(scaling);
        AppendValuedElement(document, axis, "delete", "0");
        AppendValuedElement(document, axis, "axPos", position);
        AppendValuedElement(document, axis, "crossAx", crossAxisId);
        AppendValuedElement(document, axis, "crosses", "autoZero");
        return axis;
    }

    internal static List<string> GetCategories(PptxChartData data, PptxChartSeries series)
    {
        if (series.Categories.Count > 0)
        {
            return series.Categories;
        }

        return data.Categories;
    }

    internal static List<string> GetXValues(PptxChartData data, PptxChartSeries series)
    {
        if (series.XValues.Count > 0)
        {
            return series.XValues;
        }

        return GetCategories(data, series);
    }

    internal static List<string> GetYValues(PptxChartSeries series)
    {
        if (series.YValues.Count > 0)
        {
            return series.YValues;
        }

        return series.Values;
    }

    internal static string GetXFormula(PptxChartData data, PptxChartSeries series)
    {
        string formula = PptxDiagnostic.CleanText(series.XFormula);
        if (formula.Length > 0)
        {
            return formula;
        }

        return GetCategoryFormula(data, series);
    }

    internal static string GetYFormula(PptxChartSeries series)
    {
        string formula = PptxDiagnostic.CleanText(series.YFormula);
        if (formula.Length > 0)
        {
            return formula;
        }

        return PptxDiagnostic.CleanText(series.ValuesFormula);
    }

    internal static string GetCategoryFormula(PptxChartData data, PptxChartSeries series)
    {
        string formula = PptxDiagnostic.CleanText(series.CategoryFormula);
        if (formula.Length > 0)
        {
            return formula;
        }

        return PptxDiagnostic.CleanText(data.CategoryFormula);
    }

    internal static string GetCategoryNumberFormat(PptxChartData data, PptxChartSeries series)
    {
        string format = PptxDiagnostic.CleanText(series.CategoriesNumberFormat);
        if (format.Length > 0)
        {
            return format;
        }

        return PptxDiagnostic.CleanText(data.CategoriesNumberFormat);
    }

    internal static int CoordinateXColumn(string chartType, int seriesIndex)
    {
        if (chartType == PptxChartType.Scatter)
        {
            return seriesIndex * 2 + 1;
        }

        return seriesIndex * 3 + 1;
    }

    internal static int CoordinateYColumn(string chartType, int seriesIndex)
    {
        if (chartType == PptxChartType.Scatter)
        {
            return seriesIndex * 2 + 2;
        }

        return seriesIndex * 3 + 2;
    }

    internal static int CoordinateSizeColumn(int seriesIndex)
    {
        return seriesIndex * 3 + 3;
    }

    internal static string ChartElementName(string chartType)
    {
        if (chartType == PptxChartType.Column || chartType == PptxChartType.Bar)
        {
            return "barChart";
        }

        if (chartType == PptxChartType.Line)
        {
            return "lineChart";
        }

        if (chartType == PptxChartType.Pie)
        {
            return "pieChart";
        }

        if (chartType == PptxChartType.Doughnut)
        {
            return "doughnutChart";
        }

        if (chartType == PptxChartType.Area)
        {
            return "areaChart";
        }

        if (chartType == PptxChartType.Scatter)
        {
            return "scatterChart";
        }

        if (chartType == PptxChartType.Bubble)
        {
            return "bubbleChart";
        }

        if (chartType == PptxChartType.Radar)
        {
            return "radarChart";
        }

        if (chartType == PptxChartType.Stock)
        {
            return "stockChart";
        }

        if (chartType == PptxChartType.Surface)
        {
            return "surfaceChart";
        }

        throw new System.ArgumentException("Unknown chart type.");
    }

    internal static string SheetRange(int columnIndex, int startRow, int count)
    {
        int endRow = startRow + count - 1;
        if (endRow < startRow)
        {
            endRow = startRow;
        }

        string column = ColumnName(columnIndex);
        return "Sheet1!$" + column + "$" + startRow.ToString() + ":$" + column + "$" + endRow.ToString();
    }

    internal static string ColumnName(int columnIndex)
    {
        int current = columnIndex;
        if (current < 1)
        {
            current = 1;
        }

        string result = "";
        while (current > 0)
        {
            int remainder = (current - 1) % 26;
            char value = (char)('A' + remainder);
            result = value.ToString() + result;
            current = (current - 1) / 26;
        }

        return result;
    }
}
