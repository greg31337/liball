namespace asn1;

/// <summary>
/// Contains symbols imported from one ASN.1 module.
/// </summary>
public class AsnImport
{
    /// <summary>
    /// Contains the stable import identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the referenced module name.
    /// </summary>
    public string ModuleName = "";

    /// <summary>
    /// Contains the generated module reference name.
    /// </summary>
    public string GeneratedModuleName = "";

    /// <summary>
    /// Contains imported symbol names in source order.
    /// </summary>
    public string[] Symbols = [];

    /// <summary>
    /// Contains the assigned module object identifier when supplied.
    /// </summary>
    public AsnObjectIdentifierValue? ModuleIdentifier;

    /// <summary>
    /// Contains the import source span when available.
    /// </summary>
    public AsnSourceSpan? Span;

    /// <summary>
    /// Contains ordered derivation steps for the import.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];
}
