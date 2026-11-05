namespace asn1;

/// <summary>
/// Contains parsed assignments, diagnostics, and token progress.
/// </summary>
public class AsnParserResult
{
    /// <summary>
    /// Contains parsed modules in source order.
    /// </summary>
    public AsnModule[] Modules = [];

    /// <summary>
    /// Contains parsed assignments in declaration order.
    /// </summary>
    public AsnAssignment[] Assignments = [];

    /// <summary>
    /// Contains parser diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];

    /// <summary>
    /// Contains the number of input tokens consumed.
    /// </summary>
    public int ConsumedTokenCount;

    /// <summary>
    /// Indicates whether parsing reached the end of the supplied tokens.
    /// </summary>
    public bool Complete;
}
