namespace asn1;

/// <summary>
/// Contains reflection metadata for one ASN.1 module.
/// </summary>
public class AsnModuleDescriptor
{
    /// <summary>
    /// Contains the stable module identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the ASN.1 module name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the generated module name.
    /// </summary>
    public string GeneratedName = "";

    /// <summary>
    /// Contains the definitive object identifier text when present.
    /// </summary>
    public string ObjectIdentifier = "";

    /// <summary>
    /// Contains the definitive IRI when present.
    /// </summary>
    public string Iri = "";

    /// <summary>
    /// Contains the version module reference when present.
    /// </summary>
    public string VersionModule = "";

    /// <summary>
    /// Contains the version value when present.
    /// </summary>
    public string Version = "";

    /// <summary>
    /// Contains imports in source order.
    /// </summary>
    public AsnImport[] Imports = [];

    /// <summary>
    /// Contains exported assignment names in deterministic emission order.
    /// </summary>
    public string[] Exports = [];

    /// <summary>
    /// Contains all assignment descriptors in deterministic emission order.
    /// </summary>
    public AsnAssignmentDescriptor[] Assignments = [];

    /// <summary>
    /// Contains reflected type assignments.
    /// </summary>
    public AsnTypeDescriptor[] Types = [];

    /// <summary>
    /// Contains reflected value assignments.
    /// </summary>
    public AsnValueDescriptor[] Values = [];

    /// <summary>
    /// Contains encoding descriptors available for the module.
    /// </summary>
    public AsnEncodingDescriptor[] Encodings = [];

    /// <summary>
    /// Contains the module source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the module.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];

    /// <summary>
    /// Contains the source module when retained.
    /// </summary>
    public AsnModule? SourceModule;
}
