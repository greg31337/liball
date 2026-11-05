namespace asn1;

/// <summary>
/// Contains reflection metadata for one effective ASN.1 type.
/// </summary>
public class AsnTypeDescriptor
{
    internal ResolvedTypeSyntax? Resolution;

    /// <summary>
    /// Contains the stable type identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the ASN.1 type name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the generated type name.
    /// </summary>
    public string GeneratedName = "";

    /// <summary>
    /// Contains the effective type category.
    /// </summary>
    public AsnTypeKind Kind;

    /// <summary>
    /// Contains reflected fields in schema order.
    /// </summary>
    public AsnFieldDescriptor[] Fields = [];

    /// <summary>
    /// Contains root fields in schema order.
    /// </summary>
    public AsnFieldDescriptor[] RootFields = [];

    /// <summary>
    /// Contains extension-addition fields in schema order.
    /// </summary>
    public AsnFieldDescriptor[] ExtensionFields = [];

    /// <summary>
    /// Contains the element type for collection types when present.
    /// </summary>
    public AsnTypeDescriptor? ElementType;

    /// <summary>
    /// Contains effective tags from outermost to innermost.
    /// </summary>
    public AsnTagDescriptor[] Tags = [];

    /// <summary>
    /// Contains named values in schema order.
    /// </summary>
    public AsnValueDescriptor[] NamedValues = [];

    /// <summary>
    /// Contains root named values in schema order.
    /// </summary>
    public AsnValueDescriptor[] RootNamedValues = [];

    /// <summary>
    /// Contains extension-addition named values in schema order.
    /// </summary>
    public AsnValueDescriptor[] ExtensionNamedValues = [];

    /// <summary>
    /// Contains normalized constraint descriptors.
    /// </summary>
    public AsnConstraintDescriptor[] Constraints = [];

    /// <summary>
    /// Contains encoding descriptors available for this type.
    /// </summary>
    public AsnEncodingDescriptor[] Encodings = [];

    /// <summary>
    /// Contains associated information-object dispatch tables.
    /// </summary>
    public AsnObjectTableDescriptor[] ObjectTables = [];

    /// <summary>
    /// Indicates whether the effective type has an extension partition.
    /// </summary>
    public bool Extensible;

    /// <summary>
    /// Indicates whether this type originated as a formal parameter reference.
    /// </summary>
    public bool ParameterReference;

    /// <summary>
    /// Contains the type source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the type.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];

    /// <summary>
    /// Contains the source type when retained.
    /// </summary>
    public AsnType? SourceType;
}
