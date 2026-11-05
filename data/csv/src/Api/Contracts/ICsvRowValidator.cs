namespace csv;

/// <summary>
/// Validates rows and describes validation failures.
/// </summary>
public interface ICsvRowValidator
{
    /// <summary>
    /// Decides whether a row is valid.
    /// </summary>
    /// <param name="row">The row to inspect.</param>
    /// <returns><c>true</c> when the row is valid; otherwise <c>false</c>.</returns>
    bool IsValid(CsvRow row);

    /// <summary>
    /// Creates a diagnostic for an invalid row.
    /// </summary>
    /// <param name="row">The invalid row.</param>
    /// <returns>The validation diagnostic.</returns>
    CsvDiagnostic CreateDiagnostic(CsvRow row);
}
