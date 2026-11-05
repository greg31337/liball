namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Python operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class PythonDiagnosticCodes
{
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "PYTHON_INVALID_VERSION";
}
