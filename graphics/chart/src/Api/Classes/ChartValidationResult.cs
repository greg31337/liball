namespace chart;
/// <summary>
/// Represents a validation result used by the chart model or rendering pipeline.
/// </summary>
public class ChartValidationResult : IChartValidationResult
{
    internal ChartValidationIssue[] Issues;
    internal int IssueCount;
    /// <summary>
    /// Initializes an empty validation result with library defaults.
    /// </summary>
    public ChartValidationResult()
    {
        Issues = new ChartValidationIssue[8];
        IssueCount = 0;
    }

    /// <summary>
    /// Adds the supplied error to this object.
    /// </summary>
    /// <param name = "code">The code.</param>
    /// <param name = "message">The message.</param>
    /// <param name = "columnName">The column name.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    public void AddError(string code, string message, string columnName, int rowIndex)
    {
        AddIssue(ChartValidationSeverity.Error, code, message, columnName, rowIndex);
    }

    /// <summary>
    /// Adds the supplied warning to this object.
    /// </summary>
    /// <param name = "code">The code.</param>
    /// <param name = "message">The message.</param>
    /// <param name = "columnName">The column name.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    public void AddWarning(string code, string message, string columnName, int rowIndex)
    {
        AddIssue(ChartValidationSeverity.Warning, code, message, columnName, rowIndex);
    }

    /// <summary>
    /// Adds the supplied issue to this object.
    /// </summary>
    /// <param name = "severity">The severity.</param>
    /// <param name = "code">The code.</param>
    /// <param name = "message">The message.</param>
    /// <param name = "columnName">The column name.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    public void AddIssue(ChartValidationSeverity severity, string code, string message, string columnName, int rowIndex)
    {
        EnsureCapacity(IssueCount + 1);
        Issues[IssueCount] = new ChartValidationIssue(severity, code, message, columnName, rowIndex);
        IssueCount = IssueCount + 1;
    }

    /// <summary>
    /// Adds the supplied result to this object.
    /// </summary>
    /// <param name = "result">The validation result that receives any discovered issues.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void AddResult(ChartValidationResult result)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        int index = 0;
        while (index < result.IssueCount)
        {
            ChartValidationIssue issue = result.GetIssue(index);
            AddIssue(issue.Severity, issue.Code, issue.Message, issue.ColumnName, issue.RowIndex);
            index = index + 1;
        }
    }

    /// <summary>
    /// Returns the requested issue.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting validation issue.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartValidationIssue GetIssue(int index)
    {
        if (index < 0 || index >= IssueCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Issues[index];
    }

    /// <summary>
    /// Determines whether errors.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool HasErrors()
    {
        int index = 0;
        while (index < IssueCount)
        {
            if (Issues[index].Severity == ChartValidationSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Determines whether this value is valid.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return !HasErrors();
    }

    /// <summary>
    /// Determines whether code.
    /// </summary>
    /// <param name = "code">The code.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool HasCode(string code)
    {
        return CountCode(code) > 0;
    }

    /// <summary>
    /// Applies the count code operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "code">The code.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public int CountCode(string code)
    {
        if (code == null)
        {
            throw new System.ArgumentNullException("code");
        }

        int count = 0;
        int index = 0;
        while (index < IssueCount)
        {
            if (Issues[index].Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Creates an independent copy of the issues.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartValidationIssue[] CopyIssues()
    {
        ChartValidationIssue[] copy = new ChartValidationIssue[IssueCount];
        int index = 0;
        while (index < IssueCount)
        {
            ChartValidationIssue issue = Issues[index];
            copy[index] = new ChartValidationIssue(issue.Severity, issue.Code, issue.Message, issue.ColumnName, issue.RowIndex);
            index = index + 1;
        }
        return copy;
    }

    /// <summary>Formats issues as deterministic one-line diagnostics in insertion order.</summary>
    /// <returns>The diagnostic text, or an empty string when no issues exist.</returns>
    public string DiagnosticText()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < IssueCount)
        {
            if (index > 0)
            {
                builder.Append("\n");
            }

            ChartValidationIssue issue = Issues[index];
            builder.Append(issue.Severity.ToString());
            builder.Append(" ");
            builder.Append(issue.Code);
            if (issue.ColumnName.Length > 0)
            {
                builder.Append(" ");
                builder.Append(issue.ColumnName);
            }
            if (issue.RowIndex >= 0)
            {
                builder.Append("[");
                builder.Append(issue.RowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                builder.Append("]");
            }
            builder.Append(": ");
            builder.Append(issue.Message);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Issues.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Issues.Length, capacity);
        ChartValidationIssue[] issues = new ChartValidationIssue[newCapacity];
        System.Array.Copy(Issues, issues, IssueCount);
        Issues = issues;
    }

    /// <summary>Returns the current issues.</summary>
    /// <returns>The current field value.</returns>
    public ChartValidationIssue[] GetIssues()
    {
        return Issues;
    }

    /// <summary>Replaces the current issues.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetIssues(ChartValidationIssue[] value)
    {
        Issues = value;
    }

    /// <summary>Returns the current issue count.</summary>
    /// <returns>The current field value.</returns>
    public int GetIssueCount()
    {
        return IssueCount;
    }

    /// <summary>Replaces the current issue count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetIssueCount(int value)
    {
        IssueCount = value;
    }
}
