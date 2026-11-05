namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartValidationResult"/>.</summary>
public interface IChartValidationResult
{
    /// <summary>
    /// Adds the supplied error to this object.
    /// </summary>
    /// <param name = "code">The code.</param>
    /// <param name = "message">The message.</param>
    /// <param name = "columnName">The column name.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    void AddError(string code, string message, string columnName, int rowIndex);
    /// <summary>
    /// Adds the supplied warning to this object.
    /// </summary>
    /// <param name = "code">The code.</param>
    /// <param name = "message">The message.</param>
    /// <param name = "columnName">The column name.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    void AddWarning(string code, string message, string columnName, int rowIndex);
    /// <summary>
    /// Adds the supplied issue to this object.
    /// </summary>
    /// <param name = "severity">The severity.</param>
    /// <param name = "code">The code.</param>
    /// <param name = "message">The message.</param>
    /// <param name = "columnName">The column name.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    void AddIssue(ChartValidationSeverity severity, string code, string message, string columnName, int rowIndex);
    /// <summary>
    /// Adds the supplied result to this object.
    /// </summary>
    /// <param name = "result">The validation result that receives any discovered issues.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void AddResult(ChartValidationResult result);
    /// <summary>
    /// Returns the requested issue.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting validation issue.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartValidationIssue GetIssue(int index);
    /// <summary>
    /// Determines whether errors.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool HasErrors();
    /// <summary>
    /// Determines whether this value is valid.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsValid();
    /// <summary>
    /// Determines whether code.
    /// </summary>
    /// <param name = "code">The code.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool HasCode(string code);
    /// <summary>
    /// Applies the count code operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "code">The code.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    int CountCode(string code);
    /// <summary>
    /// Creates an independent copy of the issues.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartValidationIssue[] CopyIssues();
    /// <summary>Formats issues as deterministic one-line diagnostics in insertion order.</summary>
    /// <returns>The diagnostic text, or an empty string when no issues exist.</returns>
    string DiagnosticText();
    /// <summary>Returns the current issues.</summary>
    /// <returns>The current field value.</returns>
    ChartValidationIssue[] GetIssues();
    /// <summary>Replaces the current issues.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetIssues(ChartValidationIssue[] value);
    /// <summary>Returns the current issue count.</summary>
    /// <returns>The current field value.</returns>
    int GetIssueCount();
    /// <summary>Replaces the current issue count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetIssueCount(int value);
}
