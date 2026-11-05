namespace odf;

/// <summary>Provides creation, data binding, modification, and validation for charts.</summary>
public class OdfChartEditor : IOdfChartEditor
{
    /// <summary>Initializes a chart editor.</summary>
    public OdfChartEditor()
    {
    }

    /// <summary>Gets charts in document order.</summary>
    public OdfChart[] GetCharts(OdfDocument document)
    {
        return ChartEditingModel.GetCharts(document);
    }

    /// <summary>Creates or initializes the chart in a chart document body.</summary>
    public OdfChart CreateChart(
        OdfDocument document,
        OdfChartKind kind)
    {
        return ChartEditingModel.CreateChart(document, kind);
    }

    /// <summary>Binds a rectangular table range and generates chart series.</summary>
    public OdfChart BindChartData(
        OdfDocument document,
        OdfChart chart,
        OdfTable table,
        long rowIndex,
        long columnIndex,
        long rowCount,
        long columnCount,
        bool seriesInColumns,
        bool firstRowLabels,
        bool firstColumnLabels)
    {
        return ChartEditingModel.BindChartData(
            document,
            chart,
            table,
            rowIndex,
            columnIndex,
            rowCount,
            columnCount,
            seriesInColumns,
            firstRowLabels,
            firstColumnLabels);
    }

    /// <summary>Applies direct changes made to chart data fields.</summary>
    public void ApplyChartChanges(OdfDocument document)
    {
        ChartEditingModel.ApplyChartChanges(document);
    }

    /// <summary>Validates one chart's structure, styles, and source ranges.</summary>
    public OdfDiagnostic[] ValidateChart(
        OdfDocument document,
        OdfChart chart)
    {
        return ChartEditingModel.ValidateChart(document, chart);
    }
}
