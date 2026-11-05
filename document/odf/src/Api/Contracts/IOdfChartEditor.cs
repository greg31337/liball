namespace odf;

/// <summary>Provides creation, data binding, modification, and validation for charts.</summary>
public interface IOdfChartEditor
{
    /// <summary>Gets charts in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of charts.</returns>
    public OdfChart[] GetCharts(OdfDocument document);
    /// <summary>Creates or initializes the chart in a chart document body.</summary>
    /// <param name="document">The chart document to modify.</param>
    /// <param name="kind">The standard chart kind.</param>
    /// <returns>The created chart.</returns>
    public OdfChart CreateChart(
        OdfDocument document,
        OdfChartKind kind);
    /// <summary>Binds a rectangular table range and generates chart series.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="chart">The chart to bind.</param>
    /// <param name="table">The source table.</param>
    /// <param name="rowIndex">The first source row.</param>
    /// <param name="columnIndex">The first source column.</param>
    /// <param name="rowCount">The source row count.</param>
    /// <param name="columnCount">The source column count.</param>
    /// <param name="seriesInColumns">Whether columns, rather than rows, form series.</param>
    /// <param name="firstRowLabels">Whether the first row contains labels.</param>
    /// <param name="firstColumnLabels">Whether the first column contains labels.</param>
    /// <returns>The refreshed chart.</returns>
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
        bool firstColumnLabels);
    /// <summary>Applies direct changes made to chart data fields.</summary>
    /// <param name="document">The document to modify.</param>
    public void ApplyChartChanges(OdfDocument document);
    /// <summary>Validates one chart's structure, styles, and source ranges.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="chart">The chart to validate.</param>
    /// <returns>The chart diagnostics.</returns>
    public OdfDiagnostic[] ValidateChart(
        OdfDocument document,
        OdfChart chart);
}
