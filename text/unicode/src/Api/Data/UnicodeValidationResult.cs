namespace unicode;

/// <summary>
/// Contains decoded scalars and diagnostics from UTF-16 validation.
/// </summary>
public class UnicodeValidationResult
{
    /// <summary>
    /// Whether validation completed without an error diagnostic.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The number of decoded Unicode scalars.
    /// </summary>
    public int ScalarCount;
    /// <summary>
    /// The number of malformed UTF-16 sequences replaced with U+FFFD.
    /// </summary>
    public int ReplacementCount;
    /// <summary>
    /// The diagnostics in input order.
    /// </summary>
    public UnicodeUtf16Diagnostic[] Diagnostics;
    /// <summary>
    /// The decoded Unicode scalar records in input order.
    /// </summary>
    public UnicodeScalarRecord[] Scalars;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="isValid">Whether validation succeeded.</param>
    /// <param name="scalarCount">The number of decoded Unicode scalars.</param>
    /// <param name="replacementCount">The number of malformed sequences replaced with U+FFFD.</param>
    /// <param name="diagnostics">The diagnostics to include in the result.</param>
    /// <param name="scalars">The Unicode scalars to include or inspect.</param>
    public UnicodeValidationResult(
        bool isValid,
        int scalarCount,
        int replacementCount,
        UnicodeUtf16Diagnostic[] diagnostics,
        UnicodeScalarRecord[] scalars)
    {
        IsValid = isValid;
        ScalarCount = scalarCount;
        ReplacementCount = replacementCount;
        Diagnostics = diagnostics;
        Scalars = scalars;
    }
}
