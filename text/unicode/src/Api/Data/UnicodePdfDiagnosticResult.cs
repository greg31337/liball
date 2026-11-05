namespace unicode;

/// <summary>
/// Contains the status and diagnostics from a PDF Unicode check.
/// </summary>
public class UnicodePdfDiagnosticResult
{
    /// <summary>
    /// Whether validation completed without an error diagnostic.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The most severe validation status represented by this result or diagnostic.
    /// </summary>
    public UnicodePdfDiagnosticStatus Status;
    /// <summary>
    /// The diagnostics in input order.
    /// </summary>
    public UnicodePdfDiagnostic[] Diagnostics;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="isValid">Whether validation succeeded.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="diagnostics">The diagnostics to include in the result.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>diagnostics</c> is <c>null</c>.</exception>
    public UnicodePdfDiagnosticResult(
        bool isValid,
        UnicodePdfDiagnosticStatus status,
        UnicodePdfDiagnostic[] diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        IsValid = isValid;
        Status = status;
        Diagnostics = diagnostics;
    }
}
