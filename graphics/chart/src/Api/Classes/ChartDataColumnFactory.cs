namespace chart;
/// <summary>Creates and converts chart data column values.</summary>
public class ChartDataColumnFactory : IChartDataColumnFactory
{
    /// <summary>Initializes a stateless chart data column factory facade.</summary>
    public ChartDataColumnFactory()
    {
    }

    /// <summary>
    /// Creates the requested number from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartDataColumn CreateNumber(string name, double[] values)
    {
        return ChartDataColumn.CreateNumber(name, values);
    }

    /// <summary>
    /// Creates the requested text from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process; an element can be <c>null</c> when data is missing.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartDataColumn CreateText(string name, string? [] values)
    {
        return ChartDataColumn.CreateText(name, values);
    }

    /// <summary>
    /// Creates the requested date time from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartDataColumn CreateDateTime(string name, System.DateTime[] values)
    {
        return ChartDataColumn.CreateDateTime(name, values);
    }

    /// <summary>
    /// Creates the requested category from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "values">The input values to process; an element can be <c>null</c> when data is missing.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartDataColumn CreateCategory(string name, string? [] values)
    {
        return ChartDataColumn.CreateCategory(name, values);
    }

    /// <summary>
    /// Creates the requested matrix from the supplied data and configuration.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "matrix">The matrix.</param>
    /// <returns>The newly created data column.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartDataColumn CreateMatrix(string name, ChartDataMatrix matrix)
    {
        return ChartDataColumn.CreateMatrix(name, matrix);
    }
}
