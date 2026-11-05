using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents chart series data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxChartSeries
{
    /// <summary>
    /// Stores the chart series identifier from the chart XML.
    /// </summary>
    public int Index;
    /// <summary>
    /// Stores the display order used to keep combo-chart series independently addressable.
    /// </summary>
    public int Order;
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Contains the categories in package or presentation order.
    /// </summary>
    public List<string> Categories;
    /// <summary>
    /// Contains the values in package or presentation order.
    /// </summary>
    public List<string> Values;
    /// <summary>
    /// Contains the labels in package or presentation order.
    /// </summary>
    public List<string> Labels;
    /// <summary>
    /// Contains scatter or bubble horizontal coordinates in point order.
    /// </summary>
    public List<string> XValues;
    /// <summary>
    /// Contains scatter or bubble vertical coordinates in point order.
    /// </summary>
    public List<string> YValues;
    /// <summary>
    /// Contains bubble sizes in point order.
    /// </summary>
    public List<string> BubbleSizes;
    /// <summary>
    /// Stores whether categories use text or numeric cache semantics.
    /// </summary>
    public PptxChartCategoryKind CategoryKind;
    /// <summary>
    /// Stores the chart type used by this series, particularly within combo charts.
    /// </summary>
    public string ChartType;
    /// <summary>
    /// Stores the stock-chart role assigned to this series.
    /// </summary>
    public string StockRole;
    /// <summary>
    /// Stores the worksheet formula for the series name.
    /// </summary>
    public string NameFormula;
    /// <summary>
    /// Stores the worksheet formula for category values.
    /// </summary>
    public string CategoryFormula;
    /// <summary>
    /// Stores the worksheet formula for ordinary series values.
    /// </summary>
    public string ValuesFormula;
    /// <summary>
    /// Stores the worksheet formula for horizontal coordinates.
    /// </summary>
    public string XFormula;
    /// <summary>
    /// Stores the worksheet formula for vertical coordinates.
    /// </summary>
    public string YFormula;
    /// <summary>
    /// Stores the worksheet formula for bubble sizes.
    /// </summary>
    public string BubbleSizeFormula;
    /// <summary>
    /// Stores the worksheet formula used by custom data labels.
    /// </summary>
    public string LabelFormula;
    /// <summary>
    /// Stores the number format used by numeric categories or horizontal coordinates.
    /// </summary>
    public string CategoriesNumberFormat;
    /// <summary>
    /// Stores the number format used by bubble sizes.
    /// </summary>
    public string BubbleSizesNumberFormat;
    /// <summary>
    /// Stores the number format associated with this object.
    /// </summary>
    public string NumberFormat;
    /// <summary>
    /// Stores the values number format associated with this object.
    /// </summary>
    public string ValuesNumberFormat;

    /// <summary>
    /// Initializes a new chart series with safe defaults or the supplied values.
    /// </summary>
    public PptxChartSeries()
    {
        Index = 0;
        Order = 0;
        Name = "";
        Categories = new List<string>();
        Values = new List<string>();
        Labels = new List<string>();
        XValues = new List<string>();
        YValues = new List<string>();
        BubbleSizes = new List<string>();
        CategoryKind = PptxChartCategoryKind.Text;
        ChartType = "";
        StockRole = PptxStockSeriesRole.Unknown;
        NameFormula = "";
        CategoryFormula = "";
        ValuesFormula = "";
        XFormula = "";
        YFormula = "";
        BubbleSizeFormula = "";
        LabelFormula = "";
        CategoriesNumberFormat = "";
        BubbleSizesNumberFormat = "";
        NumberFormat = "";
        ValuesNumberFormat = "";
    }
    internal int CategoryCount()
    {
        return Categories.Count;
    }
    internal int ValueCount()
    {
        return Values.Count;
    }
    internal int LabelCount()
    {
        return Labels.Count;
    }
    internal string GetCategory(int index)
    {
        return Categories[index];
    }
    internal string GetValue(int index)
    {
        return Values[index];
    }
    internal string GetLabel(int index)
    {
        return Labels[index];
    }
    internal int XValueCount()
    {
        return XValues.Count;
    }
    internal int YValueCount()
    {
        return YValues.Count;
    }
    internal int BubbleSizeCount()
    {
        return BubbleSizes.Count;
    }
    internal string GetXValue(int index)
    {
        return XValues[index];
    }
    internal string GetYValue(int index)
    {
        return YValues[index];
    }
    internal string GetBubbleSize(int index)
    {
        return BubbleSizes[index];
    }
}
