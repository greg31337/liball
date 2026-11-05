namespace csv;

/// <summary>
/// Validates individual fields and describes field validation failures.
/// </summary>
public interface ICsvFieldValidator
{
    /// <summary>Decides whether a field is valid.</summary>
    /// <param name="row">The row containing the field.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="value">The field value, which may be null.</param>
    /// <returns><c>true</c> when the field is valid; otherwise <c>false</c>.</returns>
    bool IsValid(CsvRow row, int fieldIndex, string? value);

    /// <summary>Creates a diagnostic for an invalid field.</summary>
    /// <param name="row">The row containing the invalid field.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="value">The invalid field value, which may be null.</param>
    /// <returns>The field validation diagnostic.</returns>
    CsvDiagnostic CreateDiagnostic(CsvRow row, int fieldIndex, string? value);
}
