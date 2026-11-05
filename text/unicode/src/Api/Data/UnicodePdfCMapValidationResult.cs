namespace unicode;

/// <summary>
/// Contains the status and diagnostics produced when validating a PDF ToUnicode map.
/// </summary>
public class UnicodePdfCMapValidationResult
{
    /// <summary>
    /// Whether validation completed without an error diagnostic.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The most severe validation status represented by this result or diagnostic.
    /// </summary>
    public UnicodePdfCMapStatus Status;
    /// <summary>
    /// The diagnostics in input order.
    /// </summary>
    public UnicodePdfCMapDiagnostic[] Diagnostics;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="isValid">Whether validation succeeded.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="diagnostics">The diagnostics to include in the result.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>diagnostics</c> is <c>null</c>.</exception>
    public UnicodePdfCMapValidationResult(
        bool isValid,
        UnicodePdfCMapStatus status,
        UnicodePdfCMapDiagnostic[] diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        IsValid = isValid;
        Status = status;
        Diagnostics = diagnostics;
    }
}
