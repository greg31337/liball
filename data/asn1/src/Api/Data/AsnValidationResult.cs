namespace asn1;

/// <summary>
/// Contains validated assignments and semantic diagnostics.
/// </summary>
public class AsnValidationResult
{
    /// <summary>
    /// Indicates whether validation completed without error diagnostics.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains validated modules in stable dependency order.
    /// </summary>
    public AsnModule[] Modules = [];

    /// <summary>
    /// Contains validated assignments in stable declaration order.
    /// </summary>
    public AsnAssignment[] Assignments = [];

    /// <summary>
    /// Contains binding and validation diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
