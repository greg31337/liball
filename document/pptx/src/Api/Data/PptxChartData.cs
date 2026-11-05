using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents chart data data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxChartData
{
    /// <summary>
    /// Contains the categories in package or presentation order.
    /// </summary>
    public List<string> Categories;
    /// <summary>
    /// Contains the series in package or presentation order.
    /// </summary>
    public List<PptxChartSeries> Series;
    /// <summary>
    /// Contains the number formats in package or presentation order.
    /// </summary>
    public List<string> NumberFormats;
    /// <summary>
    /// Stores whether shared categories use text or numeric cache semantics.
    /// </summary>
    public PptxChartCategoryKind CategoryKind;
    /// <summary>
    /// Stores the worksheet formula for shared categories.
    /// </summary>
    public string CategoryFormula;
    /// <summary>
    /// Stores the number format used by shared numeric categories.
    /// </summary>
    public string CategoriesNumberFormat;

    /// <summary>
    /// Initializes a new chart data with safe defaults or the supplied values.
    /// </summary>
    public PptxChartData()
    {
        Categories = new List<string>();
        Series = new List<PptxChartSeries>();
        NumberFormats = new List<string>();
        CategoryKind = PptxChartCategoryKind.Text;
        CategoryFormula = "";
        CategoriesNumberFormat = "";
    }
    internal int CategoryCount()
    {
        return Categories.Count;
    }
    internal int SeriesCount()
    {
        return Series.Count;
    }
    internal int NumberFormatCount()
    {
        return NumberFormats.Count;
    }
    internal string GetCategory(int index)
    {
        return Categories[index];
    }
    internal PptxChartSeries GetSeries(int index)
    {
        return Series[index];
    }
    internal string GetNumberFormat(int index)
    {
        return NumberFormats[index];
    }
    internal List<List<string>> ToTable()
    {
        List<List<string>> table = new List<List<string>>();
        List<string> header = new List<string>();
        header.Add("Category");
        for (int seriesIndex = 0; seriesIndex < Series.Count; seriesIndex++)
        {
            header.Add(Series[seriesIndex].Name);
        }

        table.Add(header);
        int rowCount = GetTableRowCount();
        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            List<string> row = new List<string>();
            row.Add(GetTableCategory(rowIndex));
            for (int seriesIndex = 0; seriesIndex < Series.Count; seriesIndex++)
            {
                row.Add(GetTableValue(Series[seriesIndex], rowIndex));
            }

            table.Add(row);
        }

        return table;
    }

    internal int GetTableRowCount()
    {
        int rowCount = Categories.Count;
        for (int seriesIndex = 0; seriesIndex < Series.Count; seriesIndex++)
        {
            if (Series[seriesIndex].Values.Count > rowCount)
            {
                rowCount = Series[seriesIndex].Values.Count;
            }

            if (Series[seriesIndex].Categories.Count > rowCount)
            {
                rowCount = Series[seriesIndex].Categories.Count;
            }
        }

        return rowCount;
    }

    internal string GetTableCategory(int rowIndex)
    {
        if (rowIndex < Categories.Count)
        {
            return Categories[rowIndex];
        }

        for (int seriesIndex = 0; seriesIndex < Series.Count; seriesIndex++)
        {
            if (rowIndex < Series[seriesIndex].Categories.Count)
            {
                return Series[seriesIndex].Categories[rowIndex];
            }
        }

        return "";
    }

    internal static string GetTableValue(PptxChartSeries series, int rowIndex)
    {
        if (rowIndex < series.Values.Count)
        {
            return series.Values[rowIndex];
        }

        return "";
    }
    internal static PptxChartData FromChart(PptxChart chart)
    {
        if (chart == null)
        {
            throw new System.ArgumentNullException("chart");
        }

        return FromDocument(chart.GetXmlDocument());
    }

    internal static PptxChartData FromDocument(XmlDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        PptxChartData data = new PptxChartData();
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return data;
        }

        CollectNumberFormats(root, data.NumberFormats);
        List<XmlElement> seriesElements = new List<XmlElement>();
        CollectDescendants(root, PptxNamespaces.Chart, "ser", seriesElements);
        for (int index = 0; index < seriesElements.Count; index++)
        {
            PptxChartSeries series = LoadSeries(seriesElements[index]);
            AddSeriesInOrder(data.Series, series);
            if (data.Categories.Count == 0 && series.Categories.Count > 0)
            {
                CopyStrings(series.Categories, data.Categories);
                data.CategoryKind = series.CategoryKind;
                data.CategoryFormula = series.CategoryFormula;
                data.CategoriesNumberFormat = series.CategoriesNumberFormat;
            }
        }

        AssignStockRoles(data.Series);

        return data;
    }

    internal static PptxChartSeries LoadSeries(XmlElement seriesElement)
    {
        PptxChartSeries series = new PptxChartSeries();
        series.Index = ExtractSeriesOrdinal(seriesElement, "idx", 0);
        series.Order = ExtractSeriesOrdinal(seriesElement, "order", series.Index);
        series.ChartType = GetSeriesChartType(seriesElement);
        XmlElement? text = PptxXml.FindFirstChild(seriesElement, PptxNamespaces.Chart, "tx");
        series.Name = ExtractChartText(text);
        series.NameFormula = ExtractFormula(text);

        XmlElement? categories = PptxXml.FindFirstChild(seriesElement, PptxNamespaces.Chart, "cat");
        if (categories != null)
        {
            ExtractCacheValues(categories, series.Categories);
            series.CategoryKind = ExtractCategoryKind(categories);
            series.CategoryFormula = ExtractFormula(categories);
            series.CategoriesNumberFormat = ExtractCacheFormatCode(categories);
        }

        XmlElement? xValues = PptxXml.FindFirstChild(seriesElement, PptxNamespaces.Chart, "xVal");
        if (xValues != null)
        {
            ExtractCacheValues(xValues, series.XValues);
            CopyStrings(series.XValues, series.Categories);
            series.CategoryKind = PptxChartCategoryKind.Numeric;
            series.XFormula = ExtractFormula(xValues);
            series.CategoryFormula = series.XFormula;
            series.CategoriesNumberFormat = ExtractCacheFormatCode(xValues);
        }

        XmlElement? values = PptxXml.FindFirstChild(seriesElement, PptxNamespaces.Chart, "val");
        if (values != null)
        {
            ExtractCacheValues(values, series.Values);
            series.ValuesNumberFormat = ExtractCacheFormatCode(values);
            series.ValuesFormula = ExtractFormula(values);
        }

        XmlElement? yValues = PptxXml.FindFirstChild(seriesElement, PptxNamespaces.Chart, "yVal");
        if (yValues != null)
        {
            ExtractCacheValues(yValues, series.YValues);
            CopyStrings(series.YValues, series.Values);
            series.ValuesNumberFormat = ExtractCacheFormatCode(yValues);
            series.YFormula = ExtractFormula(yValues);
            series.ValuesFormula = series.YFormula;
        }

        XmlElement? bubbleSizes = PptxXml.FindFirstChild(seriesElement, PptxNamespaces.Chart, "bubbleSize");
        if (bubbleSizes != null)
        {
            ExtractCacheValues(bubbleSizes, series.BubbleSizes);
            series.BubbleSizeFormula = ExtractFormula(bubbleSizes);
            series.BubbleSizesNumberFormat = ExtractCacheFormatCode(bubbleSizes);
        }

        XmlElement? dataLabels = PptxXml.FindFirstChild(seriesElement, PptxNamespaces.Chart, "dLbls");
        if (dataLabels != null)
        {
            ExtractDataLabels(dataLabels, series.Labels);
            series.NumberFormat = ExtractNumberFormat(dataLabels);
            series.LabelFormula = ExtractFormula(dataLabels);
        }

        if (series.NumberFormat.Length == 0)
        {
            series.NumberFormat = ExtractNumberFormat(seriesElement);
        }

        return series;
    }

    internal static int ExtractSeriesOrdinal(XmlElement seriesElement, string localName, int fallback)
    {
        XmlElement? element = PptxXml.FindFirstChild(seriesElement, PptxNamespaces.Chart, localName);
        if (element == null)
        {
            return fallback;
        }

        return PptxXml.GetIntAttribute(element, "", "val", fallback);
    }

    internal static void AddSeriesInOrder(List<PptxChartSeries> series, PptxChartSeries value)
    {
        int insertIndex = series.Count;
        for (int index = 0; index < series.Count; index++)
        {
            if (series[index].Order > value.Order)
            {
                insertIndex = index;
                break;
            }
        }

        series.Insert(insertIndex, value);
    }

    internal static PptxChartCategoryKind ExtractCategoryKind(XmlElement element)
    {
        XmlElement? numeric = PptxXml.FindFirstDescendant(element, PptxNamespaces.Chart, "numCache");
        if (numeric != null)
        {
            return PptxChartCategoryKind.Numeric;
        }

        return PptxChartCategoryKind.Text;
    }

    internal static string ExtractFormula(XmlElement? element)
    {
        if (element == null)
        {
            return "";
        }

        XmlElement? formula = PptxXml.FindFirstDescendant(element, PptxNamespaces.Chart, "f");
        if (formula == null)
        {
            return "";
        }

        return formula.InnerText;
    }

    internal static string GetSeriesChartType(XmlElement seriesElement)
    {
        XmlNode? parent = seriesElement.ParentNode;
        if (parent == null || parent.NodeType != XmlNodeType.Element)
        {
            return "";
        }

        XmlElement chartElement = (XmlElement)parent;
        if (chartElement.LocalName == "barChart")
        {
            XmlElement? direction = PptxXml.FindFirstChild(chartElement, PptxNamespaces.Chart, "barDir");
            if (direction != null && PptxXml.GetAttributeValue(direction, "", "val") == "bar")
            {
                return PptxChartType.Bar;
            }

            return PptxChartType.Column;
        }

        if (chartElement.LocalName == "lineChart")
        {
            return PptxChartType.Line;
        }

        if (chartElement.LocalName == "pieChart")
        {
            return PptxChartType.Pie;
        }

        if (chartElement.LocalName == "doughnutChart")
        {
            return PptxChartType.Doughnut;
        }

        if (chartElement.LocalName == "areaChart")
        {
            return PptxChartType.Area;
        }

        if (chartElement.LocalName == "scatterChart")
        {
            return PptxChartType.Scatter;
        }

        if (chartElement.LocalName == "bubbleChart")
        {
            return PptxChartType.Bubble;
        }

        if (chartElement.LocalName == "radarChart")
        {
            return PptxChartType.Radar;
        }

        if (chartElement.LocalName == "stockChart")
        {
            return PptxChartType.Stock;
        }

        if (chartElement.LocalName == "surfaceChart")
        {
            return PptxChartType.Surface;
        }

        return "";
    }

    internal static void AssignStockRoles(List<PptxChartSeries> series)
    {
        int stockCount = 0;
        for (int index = 0; index < series.Count; index++)
        {
            if (series[index].ChartType == PptxChartType.Stock)
            {
                stockCount = stockCount + 1;
            }
        }

        int stockIndex = 0;
        for (int index = 0; index < series.Count; index++)
        {
            if (series[index].ChartType != PptxChartType.Stock)
            {
                continue;
            }

            series[index].StockRole = GetStockRole(stockCount, stockIndex);
            stockIndex = stockIndex + 1;
        }
    }

    internal static string GetStockRole(int seriesCount, int index)
    {
        if (seriesCount >= 5 && index == 0)
        {
            return PptxStockSeriesRole.Volume;
        }

        int priceIndex = index;
        if (seriesCount >= 5)
        {
            priceIndex = index - 1;
        }

        int priceCount = seriesCount;
        if (seriesCount >= 5)
        {
            priceCount = seriesCount - 1;
        }

        if (priceCount >= 4 && priceIndex == 0)
        {
            return PptxStockSeriesRole.Open;
        }

        int offset = 0;
        if (priceCount >= 4)
        {
            offset = 1;
        }

        if (priceIndex == offset)
        {
            return PptxStockSeriesRole.High;
        }

        if (priceIndex == offset + 1)
        {
            return PptxStockSeriesRole.Low;
        }

        if (priceIndex == offset + 2)
        {
            return PptxStockSeriesRole.Close;
        }

        return PptxStockSeriesRole.Unknown;
    }

    internal static string ExtractChartText(XmlElement? element)
    {
        if (element == null)
        {
            return "";
        }

        XmlElement? rich = PptxXml.FindFirstDescendant(element, PptxNamespaces.Chart, "rich");
        if (rich != null)
        {
            return PptxTextExtractorCore.GetRichText(rich);
        }

        XmlElement? value = PptxXml.FindFirstDescendant(element, PptxNamespaces.Chart, "v");
        if (value != null)
        {
            return value.InnerText;
        }

        XmlElement? drawingText = PptxXml.FindFirstDescendant(element, PptxNamespaces.Drawing, "t");
        if (drawingText != null)
        {
            return drawingText.InnerText;
        }

        return "";
    }

    internal static void ExtractCacheValues(XmlElement element, List<string> values)
    {
        XmlElement? cache = PptxXml.FindFirstDescendant(element, PptxNamespaces.Chart, "strCache");
        if (cache == null)
        {
            cache = PptxXml.FindFirstDescendant(element, PptxNamespaces.Chart, "numCache");
        }

        if (cache == null)
        {
            return;
        }

        List<XmlElement> points = new List<XmlElement>();
        CollectDescendants(cache, PptxNamespaces.Chart, "pt", points);
        for (int index = 0; index < points.Count; index++)
        {
            XmlElement? value = PptxXml.FindFirstChild(points[index], PptxNamespaces.Chart, "v");
            if (value != null)
            {
                values.Add(value.InnerText);
            }
        }
    }

    internal static string ExtractCacheFormatCode(XmlElement element)
    {
        XmlElement? formatCode = PptxXml.FindFirstDescendant(element, PptxNamespaces.Chart, "formatCode");
        if (formatCode == null)
        {
            return "";
        }

        return formatCode.InnerText;
    }

    internal static void ExtractDataLabels(XmlElement dataLabels, List<string> labels)
    {
        List<XmlElement> labelElements = new List<XmlElement>();
        CollectDescendants(dataLabels, PptxNamespaces.Chart, "dLbl", labelElements);
        for (int index = 0; index < labelElements.Count; index++)
        {
            XmlElement? text = PptxXml.FindFirstChild(labelElements[index], PptxNamespaces.Chart, "tx");
            string label = ExtractChartText(text);
            if (label.Length > 0)
            {
                labels.Add(label);
            }
        }
    }

    internal static string ExtractNumberFormat(XmlElement element)
    {
        XmlElement? numberFormat = PptxXml.FindFirstDescendant(element, PptxNamespaces.Chart, "numFmt");
        if (numberFormat == null)
        {
            return "";
        }

        return PptxXml.GetAttributeValue(numberFormat, "", "formatCode");
    }

    internal static void CollectNumberFormats(XmlElement element, List<string> formats)
    {
        if (element.NamespaceURI == PptxNamespaces.Chart && element.LocalName == "numFmt")
        {
            AddUnique(formats, PptxXml.GetAttributeValue(element, "", "formatCode"));
        }

        if (element.NamespaceURI == PptxNamespaces.Chart && element.LocalName == "formatCode")
        {
            AddUnique(formats, element.InnerText);
        }

        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            CollectNumberFormats((XmlElement)child, formats);
        }
    }

    internal static void CollectDescendants(XmlElement element, string namespaceUri, string localName, List<XmlElement> matches)
    {
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement childElement = (XmlElement)child;
            if (childElement.NamespaceURI == namespaceUri && childElement.LocalName == localName)
            {
                matches.Add(childElement);
            }

            CollectDescendants(childElement, namespaceUri, localName, matches);
        }
    }

    internal static void CopyStrings(List<string> source, List<string> destination)
    {
        for (int index = 0; index < source.Count; index++)
        {
            destination.Add(source[index]);
        }
    }

    internal static void AddUnique(List<string> values, string value)
    {
        string clean = PptxDiagnostic.CleanText(value);
        if (clean.Length == 0)
        {
            return;
        }

        for (int index = 0; index < values.Count; index++)
        {
            if (values[index] == clean)
            {
                return;
            }
        }

        values.Add(clean);
    }
}
