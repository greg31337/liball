namespace chart;
/// <summary>
/// Represents a validation issue used by the chart model or rendering pipeline.
/// </summary>
public class ChartValidationIssue
{
    /// <summary>
    /// The severity used by this validation issue.
    /// </summary>
    public ChartValidationSeverity Severity;
    /// <summary>
    /// The stable validation code for value.
    /// </summary>
    public string Code;
    /// <summary>
    /// The message used by this validation issue.
    /// </summary>
    public string Message;
    /// <summary>
    /// The column name used by this validation issue.
    /// </summary>
    public string ColumnName;
    /// <summary>
    /// The row index used by this validation issue.
    /// </summary>
    public int RowIndex;
    /// <summary>
    /// Initializes a validation issue from the supplied severity, code, message, column name, and row index.
    /// </summary>
    /// <param name = "severity">The severity.</param>
    /// <param name = "code">The code.</param>
    /// <param name = "message">The message.</param>
    /// <param name = "columnName">The column name.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartValidationIssue(ChartValidationSeverity severity, string code, string message, string columnName, int rowIndex)
    {
        if (code == null)
        {
            throw new System.ArgumentNullException("code");
        }

        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }

        if (columnName == null)
        {
            throw new System.ArgumentNullException("columnName");
        }

        Severity = severity;
        Code = code;
        Message = message;
        ColumnName = columnName;
        RowIndex = rowIndex;
    }
}
