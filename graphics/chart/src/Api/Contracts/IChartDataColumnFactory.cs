namespace chart;
/// <summary>Defines substitutable chart data column factory operations.</summary>
public interface IChartDataColumnFactory
{
    /// <summary>
    /// Creates the requested number from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartDataColumn CreateNumber(string name, double[] values);
    /// <summary>
    /// Creates the requested text from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process; an element can be <c>null</c> when data is missing.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartDataColumn CreateText(string name, string? [] values);
    /// <summary>
    /// Creates the requested date time from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartDataColumn CreateDateTime(string name, System.DateTime[] values);
    /// <summary>
    /// Creates the requested category from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process; an element can be <c>null</c> when data is missing.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartDataColumn CreateCategory(string name, string? [] values);
    /// <summary>
    /// Creates the requested matrix from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "matrix">The matrix.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartDataColumn CreateMatrix(string name, ChartDataMatrix matrix);
}
