namespace chart;
/// <summary>Provides the public chart data validator operations.</summary>
public class ChartDataValidator : IChartDataValidator
{
    /// <summary>Initializes a stateless chart data validator facade.</summary>
    public ChartDataValidator()
    {
    }

    /// <summary>
    /// Validates number series and returns structured issues without rendering.
    /// </summary>
    /// <param name = "seriesName">The series name.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidateNumberSeries(string seriesName, double[] xValues, double[] yValues)
    {
        return ChartDataValidatorOperations.ValidateNumberSeries(seriesName, xValues, yValues);
    }

    /// <summary>
    /// Validates table and returns structured issues without rendering.
    /// </summary>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidateTable(ChartDataTable table)
    {
        return ChartDataValidatorOperations.ValidateTable(table);
    }

    /// <summary>
    /// Validates column and returns structured issues without rendering.
    /// </summary>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidateColumn(ChartDataColumn column)
    {
        return ChartDataValidatorOperations.ValidateColumn(column);
    }

    /// <summary>
    /// Validates matrix and returns structured issues without rendering.
    /// </summary>
    /// <param name = "matrix">The matrix.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationResult ValidateMatrix(ChartDataMatrix matrix)
    {
        return ChartDataValidatorOperations.ValidateMatrix(matrix);
    }
}
