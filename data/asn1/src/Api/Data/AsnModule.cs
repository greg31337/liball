namespace asn1;

/// <summary>
/// Contains one parsed or validated ASN.1 module.
/// </summary>
public class AsnModule
{
    /// <summary>
    /// Contains the module reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains imported module references.
    /// </summary>
    public AsnImport[] Imports = [];

    /// <summary>
    /// Contains assignments in declaration order.
    /// </summary>
    public AsnAssignment[] Assignments = [];

    /// <summary>
    /// Contains module-level encoding instructions.
    /// </summary>
    public AsnEncodingInstruction[] EncodingInstructions = [];

    /// <summary>
    /// Contains the module source span when available.
    /// </summary>
    public AsnSourceSpan? Span;
}
