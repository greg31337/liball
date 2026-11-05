namespace asn1;

/// <summary>
/// Contains reflection metadata for one structured-type field.
/// </summary>
public class AsnFieldDescriptor
{
    /// <summary>
    /// Contains the stable field identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the ASN.1 component name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the generated field name.
    /// </summary>
    public string GeneratedName = "";

    /// <summary>
    /// Contains the zero-based schema index.
    /// </summary>
    public int Index;

    /// <summary>
    /// Contains the field type when resolved.
    /// </summary>
    public AsnTypeDescriptor? Type;

    /// <summary>
    /// Indicates whether the field is optional.
    /// </summary>
    public bool Optional;

    /// <summary>
    /// Indicates whether the field has a default value.
    /// </summary>
    public bool HasDefaultValue;

    /// <summary>
    /// Contains the default value descriptor when HasDefaultValue is true.
    /// </summary>
    public AsnValueDescriptor? DefaultValue;

    /// <summary>
    /// Indicates whether this field belongs to an extension addition.
    /// </summary>
    public bool Extension;

    /// <summary>
    /// Contains the extension-addition group index, or minus one for an ungrouped component.
    /// </summary>
    public int ExtensionGroupIndex = -1;

    /// <summary>
    /// Contains effective field tags from outermost to innermost.
    /// </summary>
    public AsnTagDescriptor[] Tags = [];

    /// <summary>
    /// Contains occurrence-specific encodings for this field.
    /// </summary>
    public AsnEncodingDescriptor[] Encodings = [];

    /// <summary>
    /// Contains the field source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the field.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];
}
