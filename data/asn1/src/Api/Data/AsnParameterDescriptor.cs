namespace asn1;

/// <summary>
/// Contains reflection metadata for one formal parameter.
/// </summary>
public class AsnParameterDescriptor
{
    /// <summary>
    /// Contains the stable parameter identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the ASN.1 dummy-reference name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the generated parameter name.
    /// </summary>
    public string GeneratedName = "";

    /// <summary>
    /// Contains the parameter category.
    /// </summary>
    public AsnParameterKind Kind;

    /// <summary>
    /// Contains the governing type category when present.
    /// </summary>
    public AsnTypeKind GovernorTypeKind;

    /// <summary>
    /// Contains the governing object-class name when present.
    /// </summary>
    public string GovernorObjectClassName = "";

    /// <summary>
    /// Contains the parameter source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the parameter.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];
}
