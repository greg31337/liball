namespace unicode;

/// <summary>
/// Contains the outcome, decoded scalars, and diagnostics from identifier validation.
/// </summary>
public class UnicodeIdentifierValidationResult
{
    /// <summary>
    /// Whether validation completed without an error diagnostic.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The most severe validation status represented by this result or diagnostic.
    /// </summary>
    public UnicodeIdentifierStatus Status;
    /// <summary>
    /// The diagnostics in input order.
    /// </summary>
    public UnicodeIdentifierDiagnostic[] Diagnostics;
    /// <summary>
    /// The decoded Unicode scalar records in input order.
    /// </summary>
    public UnicodeScalarRecord[] Scalars;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="isValid">Whether validation succeeded.</param>
    /// <param name="status">The validation status.</param>
    /// <param name="diagnostics">The diagnostics to include in the result.</param>
    /// <param name="scalars">The Unicode scalars to include or inspect.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>diagnostics</c>, <c>scalars</c> is <c>null</c>.</exception>
    public UnicodeIdentifierValidationResult(
        bool isValid,
        UnicodeIdentifierStatus status,
        UnicodeIdentifierDiagnostic[] diagnostics,
        UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(diagnostics);
        System.ArgumentNullException.ThrowIfNull(scalars);

        IsValid = isValid;
        Status = status;
        Diagnostics = diagnostics;
        Scalars = scalars;
    }
}
