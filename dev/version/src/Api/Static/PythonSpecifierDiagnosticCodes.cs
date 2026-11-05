namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Python Specifier operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class PythonSpecifierDiagnosticCodes
{
    /// <summary>
    /// Identifies the missing specifier condition.
    /// </summary>
    public const string MissingSpecifier = "PYTHON_SPECIFIER_MISSING_SPECIFIER";
    /// <summary>
    /// Identifies the missing version condition.
    /// </summary>
    public const string MissingVersion = "PYTHON_SPECIFIER_MISSING_VERSION";
    /// <summary>
    /// Identifies the invalid operator condition.
    /// </summary>
    public const string InvalidOperator = "PYTHON_SPECIFIER_INVALID_OPERATOR";
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "PYTHON_SPECIFIER_INVALID_VERSION";
}
