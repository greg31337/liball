namespace unicode;

/// <summary>
/// Contains the prepared text, status, and diagnostics from PRECIS-style validation.
/// </summary>
public class UnicodePrecisValidationResult
{
    /// <summary>
    /// Whether validation completed without an error diagnostic.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The most severe validation status represented by this result or diagnostic.
    /// </summary>
    public UnicodePrecisStatus Status;
    /// <summary>
    /// The mapped and normalized text produced by the selected profile.
    /// </summary>
    public string PreparedText;
    /// <summary>
    /// The diagnostics in input order.
    /// </summary>
    public UnicodePrecisDiagnostic[] Diagnostics;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="isValid">Whether validation succeeded.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="preparedText">The prepared text.</param>
    /// <param name="diagnostics">The diagnostics to include in the result.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>preparedText</c>, <c>diagnostics</c> is <c>null</c>.</exception>
    public UnicodePrecisValidationResult(
        bool isValid,
        UnicodePrecisStatus status,
        string preparedText,
        UnicodePrecisDiagnostic[] diagnostics)
    {
        System.ArgumentNullException.ThrowIfNull(preparedText);
        System.ArgumentNullException.ThrowIfNull(diagnostics);

        IsValid = isValid;
        Status = status;
        PreparedText = preparedText;
        Diagnostics = diagnostics;
    }
}
