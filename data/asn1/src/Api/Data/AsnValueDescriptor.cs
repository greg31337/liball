namespace asn1;

/// <summary>
/// Contains reflection metadata for one named or default ASN.1 value.
/// </summary>
public class AsnValueDescriptor
{
    /// <summary>
    /// Contains the stable value identity.
    /// </summary>
    public string StableId = "";

    /// <summary>
    /// Contains the ASN.1 value name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Contains the generated value name.
    /// </summary>
    public string GeneratedName = "";

    /// <summary>
    /// Contains the abstract-value category.
    /// </summary>
    public AsnValueKind Kind;

    /// <summary>
    /// Contains the normalized schema value when retained.
    /// </summary>
    public AsnValue? Value;

    /// <summary>
    /// Contains the value source span when available.
    /// </summary>
    public AsnSourceSpan? SourceSpan;

    /// <summary>
    /// Contains ordered derivation steps for the value.
    /// </summary>
    public AsnDerivationDescriptor[] Derivations = [];
}
